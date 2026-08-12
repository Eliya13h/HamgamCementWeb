using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamTransport.Server.Authorization;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Seed;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AppUser = HamgamTransport.Server.Data.Models.People.User;

namespace HamgamTransport.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/fiscal-years")]
[Authorize]
public class FiscalYearController : FinanceControllerBase
{
    private readonly IFiscalYearCloseService _fiscalYears;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public FiscalYearController(
        AppDbContext db,
        IFiscalYearCloseService fiscalYears,
        IPasswordHasher<AppUser> passwordHasher) : base(db)
    {
        _fiscalYears = fiscalYears;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    [HasPermission("settings.view")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        await _fiscalYears.EnsureCurrentYearsAsync(cancellationToken);

        var rows = await Db.FiscalYears
            .AsNoTracking()
            .Where(y => y.IsDeleted != true)
            .OrderByDescending(y => y.SolarYear)
            .Select(y => new
            {
                fiscalYearId = y.FiscalYearID,
                solarYear = y.SolarYear,
                startDate = y.StartDate,
                endDate = y.EndDate,
                status = (int)y.Status,
                statusLabel = y.Status == FiscalYearStatus.Closed ? "بسته" : "باز",
                closedAt = y.ClosedAt,
                closingJournalEntryId = y.ClosingJournalEntryId,
                closingEntryNumber = y.ClosingJournalEntry != null
                    ? y.ClosingJournalEntry.EntryNumber
                    : null,
                netIncomeInBaseCurrency = y.NetIncomeInBaseCurrency,
            })
            .ToListAsync(cancellationToken);

        var isAdmin = await CurrentUserIsAdminAsync(cancellationToken);

        return Ok(new
        {
            isAdmin,
            adminRoleName = DataSeeder.DefaultRoleName,
            items = rows,
        });
    }

    [HttpGet("{id:int}/closing-preview")]
    [HasPermission("settings.view")]
    public async Task<IActionResult> ClosingPreview(int id, CancellationToken cancellationToken)
    {
        try
        {
            var preview = await _fiscalYears.GetClosingPreviewAsync(id, cancellationToken);
            return Ok(new
            {
                fiscalYearId = preview.FiscalYearId,
                solarYear = preview.SolarYear,
                startDate = preview.StartDate,
                endDate = preview.EndDate,
                totalRevenueInBase = preview.TotalRevenueInBase,
                totalExpenseInBase = preview.TotalExpenseInBase,
                totalCogsInBase = preview.TotalCogsInBase,
                netIncomeInBase = preview.NetIncomeInBase,
                temporaryAccountCount = preview.TemporaryAccountCount,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/close")]
    public async Task<IActionResult> Close(
        int id,
        [FromBody] FiscalYearPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var gate = await GateAdminWithPasswordAsync(request?.Password, cancellationToken);
        if (gate.Result is not null)
        {
            return gate.Result;
        }

        try
        {
            var year = await _fiscalYears.CloseAsync(id, gate.UserId, cancellationToken);
            return Ok(new
            {
                message = $"سال مالی {year.SolarYear} با موفقیت بسته شد.",
                fiscalYearId = year.FiscalYearID,
                solarYear = year.SolarYear,
                status = (int)year.Status,
                closingJournalEntryId = year.ClosingJournalEntryId,
                netIncomeInBaseCurrency = year.NetIncomeInBaseCurrency,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/reopen")]
    public async Task<IActionResult> Reopen(
        int id,
        [FromBody] FiscalYearPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var gate = await GateAdminWithPasswordAsync(request?.Password, cancellationToken);
        if (gate.Result is not null)
        {
            return gate.Result;
        }

        try
        {
            var year = await _fiscalYears.ReopenAsync(id, gate.UserId, cancellationToken);
            return Ok(new
            {
                message = $"سال مالی {year.SolarYear} بازگشایی شد.",
                fiscalYearId = year.FiscalYearID,
                solarYear = year.SolarYear,
                status = (int)year.Status,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<bool> CurrentUserIsAdminAsync(CancellationToken cancellationToken)
    {
        var roleClaim = User.FindFirstValue(ClaimTypes.Role);
        if (string.Equals(roleClaim, DataSeeder.DefaultRoleName, StringComparison.Ordinal))
        {
            return true;
        }

        var userId = ResolveCurrentUserId();
        if (userId is null)
        {
            return false;
        }

        return await Db.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.UserID == userId
                     && u.IsDeleted != true
                     && u.Role.Name == DataSeeder.DefaultRoleName,
                cancellationToken);
    }

    private async Task<(IActionResult? Result, int UserId)> GateAdminWithPasswordAsync(
        string? password,
        CancellationToken cancellationToken)
    {
        var userId = ResolveCurrentUserId();
        if (userId is null)
        {
            return (Unauthorized(new { message = "نشست شما منقضی شده است." }), 0);
        }

        var user = await Db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(
                u => u.UserID == userId && u.IsDeleted != true && u.IsActive == true,
                cancellationToken);

        if (user is null)
        {
            return (Unauthorized(new { message = "کاربر یافت نشد." }), 0);
        }

        // فقط نقش مدیر سیستم — نام کاربری مهم نیست
        if (!string.Equals(user.Role?.Name, DataSeeder.DefaultRoleName, StringComparison.Ordinal))
        {
            return (StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = $"فقط کاربر با نقش «{DataSeeder.DefaultRoleName}» می‌تواند سال مالی را ببندد یا بازگشایی کند.",
            }), 0);
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return (BadRequest(new { message = "رمز عبور الزامی است." }), 0);
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return (Unauthorized(new { message = "رمز عبور اشتباه است." }), 0);
        }

        return (null, user.UserID);
    }

    public class FiscalYearPasswordRequest
    {
        [Required(ErrorMessage = "رمز عبور الزامی است.")]
        public string Password { get; set; } = string.Empty;
    }
}
