using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.People;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Employees;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    // تعداد روز کاری پیش‌فرض ماه برای محاسبه غیبت پیشنهادی
    public const int DefaultWorkDaysPerMonth = 26;

    // ضریب پیش‌فرض اضافه‌کار
    public const decimal DefaultOvertimeCoefficient = 1.5m;

    private readonly AppDbContext _db;
    private readonly IAttendanceReadService _attendanceRead;

    public AttendanceController(AppDbContext db, IAttendanceReadService attendanceRead)
    {
        _db = db;
        _attendanceRead = attendanceRead;
    }

    /// <summary>
    /// خلاصه حضور ماهانه شمسی — کارمندان فعال + رکوردهای همان ماه.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMonth(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        if (year < 1300 || year > 1600)
        {
            return BadRequest(new { message = "سال معتبر نیست." });
        }

        if (month < 1 || month > 12)
        {
            return BadRequest(new { message = "ماه معتبر نیست." });
        }

        var employees = await _attendanceRead.ListActiveEmployeesAsync(cancellationToken);
        var saved = await _attendanceRead.ListMonthAsync(year, month, cancellationToken);
        var byEmployee = saved.ToDictionary(a => a.EmployeeId);

        var rows = employees.Select(emp =>
        {
            byEmployee.TryGetValue(emp.EmployeeId, out var row);
            return new
            {
                attendanceId = row?.AttendanceID,
                employeeId = emp.EmployeeId,
                fullName = emp.FullName,
                departmentName = emp.DepartmentName,
                baseSalary = emp.BaseSalary,
                year,
                month,
                presentDays = row?.PresentDays ?? 0,
                absentDays = row?.AbsentDays ?? 0,
                leavePaidDays = row?.LeavePaidDays ?? 0,
                leaveUnpaidDays = row?.LeaveUnpaidDays ?? 0,
                holidayPaidDays = row?.HolidayPaidDays ?? 0,
                holidayUnpaidDays = row?.HolidayUnpaidDays ?? 0,
                lateHours = row?.LateHours ?? 0m,
                earlyLeaveHours = row?.EarlyLeaveHours ?? 0m,
                overtimeHours = row?.OvertimeHours ?? 0m,
                overtimeCoefficient = row?.OvertimeCoefficient ?? DefaultOvertimeCoefficient,
                note = row?.Note ?? "",
                isSaved = row is not null,
            };
        }).ToList();

        return Ok(new
        {
            year,
            month,
            workDaysPerMonth = DefaultWorkDaysPerMonth,
            defaultOvertimeCoefficient = DefaultOvertimeCoefficient,
            rows,
        });
    }

    /// <summary>
    /// ثبت/به‌روزرسانی دسته‌ای خلاصه حضور برای یک ماه شمسی.
    /// </summary>
    [HttpPut("month")]
    public async Task<IActionResult> UpsertMonth(
        [FromBody] SaveAttendanceMonthRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.Year < 1300 || request.Year > 1600)
        {
            return BadRequest(new { message = "سال معتبر نیست." });
        }

        if (request.Month < 1 || request.Month > 12)
        {
            return BadRequest(new { message = "ماه معتبر نیست." });
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            return BadRequest(new { message = "لیست کارمندان خالی است." });
        }

        foreach (var item in request.Items)
        {
            var validationError = ValidateItem(item);
            if (validationError is not null)
            {
                return BadRequest(new { message = validationError });
            }
        }

        var employeeIds = request.Items.Select(i => i.EmployeeId).Distinct().ToList();
        if (employeeIds.Count != request.Items.Count)
        {
            return BadRequest(new { message = "کارمند تکراری در لیست وجود دارد." });
        }

        var validEmployeeIds = await _db.Employees
            .AsNoTracking()
            .Where(e => e.IsDeleted != true && employeeIds.Contains(e.EmployeeID))
            .Select(e => e.EmployeeID)
            .ToListAsync(cancellationToken);

        if (validEmployeeIds.Count != employeeIds.Count)
        {
            return BadRequest(new { message = "یک یا چند کارمند معتبر نیست." });
        }

        var userId = ResolveCurrentUserId();
        var existing = await _db.Attendances
            .Where(a =>
                a.IsDeleted != true
                && a.Year == request.Year
                && a.Month == request.Month
                && employeeIds.Contains(a.EmployeeId))
            .ToListAsync(cancellationToken);

        var byEmployee = existing.ToDictionary(a => a.EmployeeId);

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var item in request.Items)
            {
                if (byEmployee.TryGetValue(item.EmployeeId, out var row))
                {
                    ApplyItem(row, item);
                    row.UpdatedAt = DateTime.Now;
                    row.UpdatedBy = userId;
                    row.IsUpdated = true;
                }
                else
                {
                    var created = new Attendance
                    {
                        EmployeeId = item.EmployeeId,
                        Year = request.Year,
                        Month = request.Month,
                        CreatedAt = DateTime.Now,
                        CreatedBy = userId,
                        IsActive = true,
                        IsDeleted = false,
                    };
                    ApplyItem(created, item);
                    _db.Attendances.Add(created);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        return Ok(new
        {
            message = "خلاصه حضور ماه ثبت شد.",
            year = request.Year,
            month = request.Month,
            count = request.Items.Count,
        });
    }

    private static string? ValidateItem(AttendanceMonthItem item)
    {
        if (item.PresentDays < 0
            || item.AbsentDays < 0
            || item.LeavePaidDays < 0
            || item.LeaveUnpaidDays < 0
            || item.HolidayPaidDays < 0
            || item.HolidayUnpaidDays < 0)
        {
            return "تعداد روزها نمی‌تواند منفی باشد.";
        }

        if (item.LateHours < 0
            || item.EarlyLeaveHours < 0
            || item.OvertimeHours < 0
            || item.OvertimeCoefficient < 0)
        {
            return "ساعت‌ها و ضریب اضافه‌کار نمی‌تواند منفی باشد.";
        }

        return null;
    }

    private static void ApplyItem(Attendance row, AttendanceMonthItem item)
    {
        row.PresentDays = item.PresentDays;
        row.AbsentDays = item.AbsentDays;
        row.LeavePaidDays = item.LeavePaidDays;
        row.LeaveUnpaidDays = item.LeaveUnpaidDays;
        row.HolidayPaidDays = item.HolidayPaidDays;
        row.HolidayUnpaidDays = item.HolidayUnpaidDays;
        row.LateHours = item.LateHours;
        row.EarlyLeaveHours = item.EarlyLeaveHours;
        row.OvertimeHours = item.OvertimeHours;
        row.OvertimeCoefficient = item.OvertimeCoefficient <= 0
            ? DefaultOvertimeCoefficient
            : item.OvertimeCoefficient;
        row.Note = string.IsNullOrWhiteSpace(item.Note) ? null : item.Note.Trim();
    }

    private int? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public class SaveAttendanceMonthRequest
    {
        [Range(1300, 1600)]
        public int Year { get; set; }

        [Range(1, 12)]
        public int Month { get; set; }

        [Required]
        public List<AttendanceMonthItem> Items { get; set; } = [];
    }

    public class AttendanceMonthItem
    {
        [Range(1, int.MaxValue)]
        public int EmployeeId { get; set; }

        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LeavePaidDays { get; set; }
        public int LeaveUnpaidDays { get; set; }
        public int HolidayPaidDays { get; set; }
        public int HolidayUnpaidDays { get; set; }

        public decimal LateHours { get; set; }
        public decimal EarlyLeaveHours { get; set; }
        public decimal OvertimeHours { get; set; }

        public decimal OvertimeCoefficient { get; set; } = DefaultOvertimeCoefficient;

        [MaxLength(2000)]
        public string? Note { get; set; }
    }
}
