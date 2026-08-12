using HamgamTransport.Server.Authorization;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/fiscal-periods")]
[Authorize]
public class FiscalPeriodController : FinanceControllerBase
{
    public FiscalPeriodController(AppDbContext db) : base(db)
    {
    }

    [HttpGet]
    [HasPermission("settings.view")]
    public async Task<IActionResult> List([FromQuery] int? solarYear, CancellationToken cancellationToken)
    {
        var year = solarYear ?? JalaliDateHelper.GetSolarYear(DateTime.Today);
        await EnsureYearPeriodsAsync(year, cancellationToken);

        var rows = await Db.FiscalPeriods.AsNoTracking()
            .Where(p => p.IsDeleted != true && p.SolarYear == year)
            .OrderBy(p => p.Month)
            .Select(p => new
            {
                fiscalPeriodId = p.FiscalPeriodID,
                solarYear = p.SolarYear,
                month = p.Month,
                monthName = JalaliDateHelper.AfghanMonthNames[p.Month - 1],
                status = (int)p.Status,
                statusLabel = p.Status == FiscalYearStatus.Closed ? "بسته" : "باز",
                closedAt = p.ClosedAt,
            })
            .ToListAsync(cancellationToken);

        return Ok(new { solarYear = year, items = rows });
    }

    [HttpPost("{id:int}/close")]
    [HasPermission("settings.edit")]
    public async Task<IActionResult> Close(int id, CancellationToken cancellationToken)
    {
        var period = await Db.FiscalPeriods.FirstOrDefaultAsync(
            p => p.FiscalPeriodID == id && p.IsDeleted != true, cancellationToken);
        if (period is null) return NotFound(new { message = "دوره یافت نشد." });
        if (period.Status == FiscalYearStatus.Closed)
        {
            return BadRequest(new { message = "این دوره از قبل بسته است." });
        }

        period.Status = FiscalYearStatus.Closed;
        period.ClosedAt = DateTime.Now;
        period.ClosedByUserId = ResolveCurrentUserId();
        period.UpdatedAt = DateTime.Now;
        period.UpdatedBy = ResolveCurrentUserId();
        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "دوره ماهانه بسته شد." });
    }

    [HttpPost("{id:int}/reopen")]
    [HasPermission("settings.edit")]
    public async Task<IActionResult> Reopen(int id, CancellationToken cancellationToken)
    {
        var period = await Db.FiscalPeriods.FirstOrDefaultAsync(
            p => p.FiscalPeriodID == id && p.IsDeleted != true, cancellationToken);
        if (period is null) return NotFound(new { message = "دوره یافت نشد." });

        period.Status = FiscalYearStatus.Open;
        period.ClosedAt = null;
        period.ClosedByUserId = null;
        period.UpdatedAt = DateTime.Now;
        period.UpdatedBy = ResolveCurrentUserId();
        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "دوره ماهانه بازگشایی شد." });
    }

    private async Task EnsureYearPeriodsAsync(int solarYear, CancellationToken cancellationToken)
    {
        var existing = await Db.FiscalPeriods
            .Where(p => p.IsDeleted != true && p.SolarYear == solarYear)
            .Select(p => p.Month)
            .ToListAsync(cancellationToken);

        var now = DateTime.Now;
        var userId = ResolveCurrentUserId();
        for (var m = 1; m <= 12; m++)
        {
            if (existing.Contains(m)) continue;
            Db.FiscalPeriods.Add(new FiscalPeriod
            {
                SolarYear = solarYear,
                Month = m,
                Status = FiscalYearStatus.Open,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = userId,
            });
        }

        await Db.SaveChangesAsync(cancellationToken);
    }
}
