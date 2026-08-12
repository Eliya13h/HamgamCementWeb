using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.People;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Controllers.Employees;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    // تعداد روز کاری پیش‌فرض ماه برای محاسبه غیبت پیشنهادی
    public const int DefaultWorkDaysPerMonth = 26;

    private readonly AppDbContext _db;

    public AttendanceController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// لیست حضور در یک بازهٔ تاریخ — فرانت ماه شمسی را به from/to میلادی تبدیل می‌کند.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRange(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        var fromDay = from.Date;
        var toDay = to.Date;
        if (toDay < fromDay)
        {
            return BadRequest(new { message = "بازه تاریخ معتبر نیست." });
        }

        var employees = await _db.Employees
            .AsNoTracking()
            .Where(e => e.IsDeleted != true && e.IsActive == true)
            .OrderBy(e => e.Family)
            .ThenBy(e => e.Name)
            .Select(e => new
            {
                employeeId = e.EmployeeID,
                fullName = (e.Name + " " + e.Family).Trim(),
                departmentName = e.Department != null ? e.Department.Name : "",
                baseSalary = e.Sallary,
            })
            .ToListAsync(cancellationToken);

        var rows = await _db.Attendances
            .AsNoTracking()
            .Where(a =>
                a.IsDeleted != true
                && a.Date >= fromDay
                && a.Date <= toDay)
            .Select(a => new
            {
                attendanceId = a.AttendanceID,
                employeeId = a.EmployeeId,
                date = a.Date.ToString("yyyy-MM-dd"),
                isPresent = a.IsPresent,
                lateMinutes = a.LateMinutes,
                overtimeMinutes = a.OvertimeMinutes,
                note = a.Note,
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            from = fromDay.ToString("yyyy-MM-dd"),
            to = toDay.ToString("yyyy-MM-dd"),
            workDaysPerMonth = DefaultWorkDaysPerMonth,
            employees,
            attendances = rows,
        });
    }

    /// <summary>
    /// ثبت یا به‌روزرسانی یک روز برای یک کارمند.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Upsert(
        [FromBody] SaveAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.LateMinutes < 0 || request.OvertimeMinutes < 0)
        {
            return BadRequest(new { message = "دقیقه دیرکرد و اضافه‌کاری نمی‌تواند منفی باشد." });
        }

        var employeeExists = await _db.Employees
            .AnyAsync(e => e.EmployeeID == request.EmployeeId && e.IsDeleted != true, cancellationToken);
        if (!employeeExists)
        {
            return BadRequest(new { message = "کارمند یافت نشد." });
        }

        var day = request.Date.Date;
        var row = await _db.Attendances
            .FirstOrDefaultAsync(
                a => a.EmployeeId == request.EmployeeId
                     && a.Date == day
                     && a.IsDeleted != true,
                cancellationToken);

        var userId = ResolveCurrentUserId();

        if (row is null)
        {
            row = new Attendance
            {
                EmployeeId = request.EmployeeId,
                Date = day,
                IsPresent = request.IsPresent,
                LateMinutes = request.IsPresent ? request.LateMinutes : 0,
                OvertimeMinutes = request.IsPresent ? request.OvertimeMinutes : 0,
                Note = request.Note?.Trim(),
                CreatedAt = DateTime.Now,
                CreatedBy = userId,
                IsActive = true,
                IsDeleted = false,
            };
            _db.Attendances.Add(row);
        }
        else
        {
            row.IsPresent = request.IsPresent;
            row.LateMinutes = request.IsPresent ? request.LateMinutes : 0;
            row.OvertimeMinutes = request.IsPresent ? request.OvertimeMinutes : 0;
            row.Note = request.Note?.Trim();
            row.UpdatedAt = DateTime.Now;
            row.UpdatedBy = userId;
            row.IsUpdated = true;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "حضور ثبت شد.",
            attendanceId = row.AttendanceID,
            date = row.Date.ToString("yyyy-MM-dd"),
        });
    }

    /// <summary>
    /// ثبت سریع حضور چند کارمند برای یک روز (چک‌باکس گروهی).
    /// </summary>
    [HttpPut("day")]
    public async Task<IActionResult> UpsertDay(
        [FromBody] SaveAttendanceDayRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            return BadRequest(new { message = "لیست کارمندان خالی است." });
        }

        var day = request.Date.Date;
        var userId = ResolveCurrentUserId();
        var employeeIds = request.Items.Select(i => i.EmployeeId).Distinct().ToList();

        var existing = await _db.Attendances
            .Where(a =>
                a.IsDeleted != true
                && a.Date == day
                && employeeIds.Contains(a.EmployeeId))
            .ToListAsync(cancellationToken);

        var byEmployee = existing.ToDictionary(a => a.EmployeeId);

        foreach (var item in request.Items)
        {
            if (item.LateMinutes < 0 || item.OvertimeMinutes < 0)
            {
                return BadRequest(new { message = "دقیقه دیرکرد و اضافه‌کاری نمی‌تواند منفی باشد." });
            }

            if (byEmployee.TryGetValue(item.EmployeeId, out var row))
            {
                row.IsPresent = item.IsPresent;
                row.LateMinutes = item.IsPresent ? item.LateMinutes : 0;
                row.OvertimeMinutes = item.IsPresent ? item.OvertimeMinutes : 0;
                row.Note = item.Note?.Trim();
                row.UpdatedAt = DateTime.Now;
                row.UpdatedBy = userId;
                row.IsUpdated = true;
            }
            else
            {
                _db.Attendances.Add(new Attendance
                {
                    EmployeeId = item.EmployeeId,
                    Date = day,
                    IsPresent = item.IsPresent,
                    LateMinutes = item.IsPresent ? item.LateMinutes : 0,
                    OvertimeMinutes = item.IsPresent ? item.OvertimeMinutes : 0,
                    Note = item.Note?.Trim(),
                    CreatedAt = DateTime.Now,
                    CreatedBy = userId,
                    IsActive = true,
                    IsDeleted = false,
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "حضور روز ثبت شد.", date = day.ToString("yyyy-MM-dd"), count = request.Items.Count });
    }

    private int? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public class SaveAttendanceRequest
    {
        [Range(1, int.MaxValue)]
        public int EmployeeId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public bool IsPresent { get; set; }

        public int LateMinutes { get; set; }

        public int OvertimeMinutes { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }
    }

    public class SaveAttendanceDayRequest
    {
        [Required]
        public DateTime Date { get; set; }

        [Required]
        public List<AttendanceDayItem> Items { get; set; } = [];
    }

    public class AttendanceDayItem
    {
        [Range(1, int.MaxValue)]
        public int EmployeeId { get; set; }

        public bool IsPresent { get; set; }

        public int LateMinutes { get; set; }

        public int OvertimeMinutes { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }
    }
}
