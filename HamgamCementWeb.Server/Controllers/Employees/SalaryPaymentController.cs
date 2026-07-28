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
[Route("api/salary-payments")]
[Authorize]
public class SalaryPaymentController : ControllerBase
{
    // ۸ ساعت کاری در روز
    private const int MinutesPerWorkDay = 480;
    private const int WorkDaysPerMonth = AttendanceController.DefaultWorkDaysPerMonth;

    private readonly AppDbContext _db;
    private readonly IJournalPostingService _journal;
    private readonly IAccountLookupService _accounts;
    private readonly ICashBoxService _cashBoxes;
    private readonly ICashBalanceService _cashBalances;

    public SalaryPaymentController(
        AppDbContext db,
        IJournalPostingService journal,
        IAccountLookupService accounts,
        ICashBoxService cashBoxes,
        ICashBalanceService cashBalances)
    {
        _db = db;
        _journal = journal;
        _accounts = accounts;
        _cashBoxes = cashBoxes;
        _cashBalances = cashBalances;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
    {
        var query = _db.SalaryPayments
            .AsNoTracking()
            .Include(s => s.Employee)
            .Where(s => s.IsDeleted != true);

        if (year is int y)
        {
            query = query.Where(s => s.Year == y);
        }

        if (month is int m)
        {
            query = query.Where(s => s.Month == m);
        }

        var rows = await query
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ThenBy(s => s.Employee!.Family)
            .Select(s => new
            {
                salaryPaymentId = s.SalaryPaymentID,
                employeeId = s.EmployeeId,
                employeeName = s.Employee != null
                    ? (s.Employee.Name + " " + s.Employee.Family).Trim()
                    : "",
                year = s.Year,
                month = s.Month,
                paymentDate = s.PaymentDate.ToString("yyyy-MM-dd"),
                baseSalary = s.BaseSalary,
                overtimeAmount = s.OvertimeAmount,
                lateDeduction = s.LateDeduction,
                absenceDeduction = s.AbsenceDeduction,
                benefitAmount = s.BenefitAmount,
                otherDeduction = s.OtherDeduction,
                netAmount = s.NetAmount,
                presentDays = s.PresentDays,
                absentDays = s.AbsentDays,
                totalLateMinutes = s.TotalLateMinutes,
                totalOvertimeMinutes = s.TotalOvertimeMinutes,
                journalEntryId = s.JournalEntryId,
                description = s.Description,
            })
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    /// <summary>
    /// پیش‌نویس محاسبه از روی حضور — مبالغ پیشنهادی قابل تغییر در فرم ثبت هستند.
    /// year/month برچسب دوره (معمولاً شمسی) هستند؛ from/to بازهٔ واقعی حضور.
    /// </summary>
    [HttpGet("preview")]
    public async Task<IActionResult> Preview(
        [FromQuery] int employeeId,
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        if (month < 1 || month > 12)
        {
            return BadRequest(new { message = "ماه معتبر نیست." });
        }

        var fromDay = from.Date;
        var toDay = to.Date;
        if (toDay < fromDay)
        {
            return BadRequest(new { message = "بازه تاریخ معتبر نیست." });
        }

        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeID == employeeId && e.IsDeleted != true, cancellationToken);

        if (employee is null)
        {
            return NotFound(new { message = "کارمند یافت نشد." });
        }

        var alreadyPaid = await _db.SalaryPayments
            .AnyAsync(
                s => s.EmployeeId == employeeId
                     && s.Year == year
                     && s.Month == month
                     && s.IsDeleted != true,
                cancellationToken);

        if (alreadyPaid)
        {
            return Conflict(new { message = "برای این کارمند در این ماه قبلاً حقوق ثبت شده است." });
        }

        var attendances = await _db.Attendances
            .AsNoTracking()
            .Where(a =>
                a.IsDeleted != true
                && a.EmployeeId == employeeId
                && a.Date >= fromDay
                && a.Date <= toDay)
            .ToListAsync(cancellationToken);

        var presentDays = attendances.Count(a => a.IsPresent);
        var totalLate = attendances.Where(a => a.IsPresent).Sum(a => a.LateMinutes);
        var totalOt = attendances.Where(a => a.IsPresent).Sum(a => a.OvertimeMinutes);
        var absentDays = Math.Max(0, WorkDaysPerMonth - presentDays);

        var baseSalary = employee.Sallary;
        var minuteRate = WorkDaysPerMonth > 0 && MinutesPerWorkDay > 0
            ? baseSalary / (WorkDaysPerMonth * MinutesPerWorkDay)
            : 0m;
        var dayRate = WorkDaysPerMonth > 0 ? baseSalary / WorkDaysPerMonth : 0m;

        // پیشنهاد اولیه — کاربر در فرم می‌تواند دستی عوض کند
        var suggestedOvertime = RoundMoney(totalOt * minuteRate * 1.5m);
        var suggestedLate = RoundMoney(totalLate * minuteRate);
        var suggestedAbsence = RoundMoney(absentDays * dayRate);

        var suggestedNet = RoundMoney(
            baseSalary + suggestedOvertime - suggestedLate - suggestedAbsence);

        return Ok(new
        {
            employeeId = employee.EmployeeID,
            employeeName = $"{employee.Name} {employee.Family}".Trim(),
            year,
            month,
            from = fromDay.ToString("yyyy-MM-dd"),
            to = toDay.ToString("yyyy-MM-dd"),
            workDaysPerMonth = WorkDaysPerMonth,
            minutesPerWorkDay = MinutesPerWorkDay,
            baseSalary,
            presentDays,
            absentDays,
            totalLateMinutes = totalLate,
            totalOvertimeMinutes = totalOt,
            suggestedOvertimeAmount = suggestedOvertime,
            suggestedLateDeduction = suggestedLate,
            suggestedAbsenceDeduction = suggestedAbsence,
            suggestedBenefitAmount = 0m,
            suggestedOtherDeduction = 0m,
            suggestedNetAmount = suggestedNet,
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveSalaryPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.Month < 1 || request.Month > 12)
        {
            return BadRequest(new { message = "ماه معتبر نیست." });
        }

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.EmployeeID == request.EmployeeId && e.IsDeleted != true, cancellationToken);

        if (employee is null)
        {
            return BadRequest(new { message = "کارمند یافت نشد." });
        }

        var duplicate = await _db.SalaryPayments
            .AnyAsync(
                s => s.EmployeeId == request.EmployeeId
                     && s.Year == request.Year
                     && s.Month == request.Month
                     && s.IsDeleted != true,
                cancellationToken);

        if (duplicate)
        {
            return Conflict(new { message = "برای این کارمند در این ماه قبلاً حقوق ثبت شده است." });
        }

        var net = RoundMoney(
            request.BaseSalary
            + request.OvertimeAmount
            + request.BenefitAmount
            - request.LateDeduction
            - request.AbsenceDeduction
            - request.OtherDeduction);

        if (net < 0)
        {
            return BadRequest(new { message = "مبلغ خالص حقوق نمی‌تواند منفی باشد." });
        }

        if (net == 0)
        {
            return BadRequest(new { message = "مبلغ خالص حقوق باید بزرگ‌تر از صفر باشد." });
        }

        var userId = ResolveCurrentUserId();
        var paymentDate = request.PaymentDate?.Date ?? DateTime.Now.Date;

        var cashBoxId = request.CashBoxId
            ?? await _cashBoxes.ResolveUserCashBoxIdAsync(userId, cancellationToken);
        var baseCurrencyId = await ResolveBaseCurrencyIdAsync(cancellationToken);

        if (cashBoxId is int boxId)
        {
            try
            {
                await _cashBalances.EnsureSufficientBalanceAsync(boxId, baseCurrencyId, net, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        var payment = new SalaryPayment
        {
            EmployeeId = request.EmployeeId,
            Year = request.Year,
            Month = request.Month,
            PaymentDate = paymentDate,
            BaseSalary = RoundMoney(request.BaseSalary),
            OvertimeAmount = RoundMoney(request.OvertimeAmount),
            LateDeduction = RoundMoney(request.LateDeduction),
            AbsenceDeduction = RoundMoney(request.AbsenceDeduction),
            BenefitAmount = RoundMoney(request.BenefitAmount),
            OtherDeduction = RoundMoney(request.OtherDeduction),
            NetAmount = net,
            PresentDays = request.PresentDays,
            AbsentDays = request.AbsentDays,
            TotalLateMinutes = request.TotalLateMinutes,
            TotalOvertimeMinutes = request.TotalOvertimeMinutes,
            CashBoxId = cashBoxId,
            Description = request.Description?.Trim(),
            CreatedAt = DateTime.Now,
            CreatedBy = userId,
            IsActive = true,
            IsDeleted = false,
        };

        _db.SalaryPayments.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);

        // ثبت سند دفتر: بدهکار ناخالص حقوق / بستانکار کسورات و خالص پرداختی
        var salaryAccount = await _accounts.GetBySystemCodeAsync(AccountSystemCode.SalaryExpense, cancellationToken);
        var deductionsAccount = await _accounts.GetBySystemCodeAsync(AccountSystemCode.SalaryDeductions, cancellationToken);
        var creditAccountId = await ResolveCreditAccountIdAsync(cashBoxId, cancellationToken);

        var employeeName = $"{employee.Name} {employee.Family}".Trim();
        var title = $"حقوق {employeeName} — {request.Year}/{request.Month:D2}";

        var gross = RoundMoney(payment.BaseSalary + payment.OvertimeAmount + payment.BenefitAmount);
        var deductions = RoundMoney(payment.LateDeduction + payment.AbsenceDeduction + payment.OtherDeduction);
        var lines = new List<JournalLineDraft>
        {
            new(salaryAccount.AccountID, gross, 0, gross, 0, baseCurrencyId, title),
            new(creditAccountId, 0, net, 0, net, baseCurrencyId, title, CashBoxId: cashBoxId),
        };
        if (deductions > 0)
        {
            lines.Add(new JournalLineDraft(
                deductionsAccount.AccountID, 0, deductions, 0, deductions, baseCurrencyId,
                $"کسورات حقوق — {employeeName}"));
        }

        var journal = await _journal.PostAsync(
            paymentDate,
            title,
            JournalSource.SalaryPayment,
            payment.SalaryPaymentID,
            baseCurrencyId,
            lines,
            userId,
            cancellationToken);

        payment.JournalEntryId = journal.JournalEntryID;
        payment.CashBoxId = cashBoxId;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "حقوق با موفقیت ثبت شد.",
            salaryPaymentId = payment.SalaryPaymentID,
            journalEntryId = journal.JournalEntryID,
            netAmount = payment.NetAmount,
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var payment = await _db.SalaryPayments
            .FirstOrDefaultAsync(s => s.SalaryPaymentID == id && s.IsDeleted != true, cancellationToken);

        if (payment is null)
        {
            return NotFound(new { message = "پرداخت حقوق یافت نشد." });
        }

        var userId = ResolveCurrentUserId();

        await _journal.ReverseBySourceAsync(
            JournalSource.SalaryPayment,
            payment.SalaryPaymentID,
            userId,
            cancellationToken: cancellationToken);

        payment.IsDeleted = true;
        payment.IsActive = false;
        payment.DeletedAt = DateTime.Now;
        payment.DeletedBy = userId;
        payment.JournalEntryId = null;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "پرداخت حقوق با موفقیت حذف شد." });
    }

    private async Task<int> ResolveCreditAccountIdAsync(int? cashBoxId, CancellationToken cancellationToken)
    {
        if (cashBoxId is int id)
        {
            var box = await _db.CashBoxes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CashBoxID == id && c.IsDeleted != true, cancellationToken);
            if (box is not null)
            {
                return box.AccountId;
            }
        }

        throw new InvalidOperationException("صندوق برای پرداخت حقوق الزامی است.");
    }

    private async Task<int> ResolveBaseCurrencyIdAsync(CancellationToken cancellationToken)
    {
        var baseId = await _db.Currencies
            .Where(c => c.IsBaseCurrency && c.IsDeleted != true)
            .Select(c => c.CurrencyID)
            .FirstOrDefaultAsync(cancellationToken);

        if (baseId != 0)
        {
            return baseId;
        }

        return await _db.Currencies
            .Where(c => c.IsDeleted != true)
            .OrderBy(c => c.CurrencyID)
            .Select(c => c.CurrencyID)
            .FirstAsync(cancellationToken);
    }

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private int? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public class SaveSalaryPaymentRequest
    {
        [Range(1, int.MaxValue)]
        public int EmployeeId { get; set; }

        public int Year { get; set; }
        public int Month { get; set; }

        public DateTime? PaymentDate { get; set; }

        public decimal BaseSalary { get; set; }
        public decimal OvertimeAmount { get; set; }
        public decimal LateDeduction { get; set; }
        public decimal AbsenceDeduction { get; set; }
        public decimal BenefitAmount { get; set; }
        public decimal OtherDeduction { get; set; }

        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int TotalLateMinutes { get; set; }
        public int TotalOvertimeMinutes { get; set; }

        public int? CashBoxId { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }
    }
}
