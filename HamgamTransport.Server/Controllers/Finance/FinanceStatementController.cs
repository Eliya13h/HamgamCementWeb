using System.Globalization;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HamgamTransport.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/statements")]
[Authorize]
public class FinanceStatementController : ControllerBase
{
    private readonly IFinanceStatementService _statements;

    public FinanceStatementController(IFinanceStatementService statements)
    {
        _statements = statements;
    }

    // صورت سود و زیان
    [HttpGet("profit-loss")]
    public async Task<IActionResult> ProfitAndLoss(
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] string? compareFrom,
        [FromQuery] string? compareTo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var from = ParseOptionalDate(dateFrom);
            var to = ParseOptionalDate(dateTo);
            var compareStart = ParseOptionalDate(compareFrom);
            var compareEnd = ParseOptionalDate(compareTo);
            var result = await _statements.GetProfitAndLossAsync(from, to, compareStart, compareEnd, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // تراز کلی شرکت
    [HttpGet("balance-sheet")]
    public async Task<IActionResult> BalanceSheet(
        [FromQuery] string? asOf,
        [FromQuery] string? compareAsOf,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var date = ParseOptionalDate(asOf);
            var compareDate = ParseOptionalDate(compareAsOf);
            var result = await _statements.GetBalanceSheetAsync(date, compareDate, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // تراز آزمایشی تا تاریخ
    [HttpGet("trial-balance")]
    public async Task<IActionResult> TrialBalance(
        [FromQuery] string? asOf,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var date = ParseOptionalDate(asOf);
            var result = await _statements.GetTrialBalanceAsync(date, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // سررسید دریافتنی
    [HttpGet("aging/ar")]
    public async Task<IActionResult> ArAging(
        [FromQuery] string? asOf,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var date = ParseOptionalDate(asOf);
            var result = await _statements.GetArAgingAsync(date, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // سررسید پرداختنی
    [HttpGet("aging/ap")]
    public async Task<IActionResult> ApAging(
        [FromQuery] string? asOf,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var date = ParseOptionalDate(asOf);
            var result = await _statements.GetApAgingAsync(date, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // صورت جریان وجوه نقد
    [HttpGet("cash-flow")]
    public async Task<IActionResult> CashFlow(
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var from = ParseOptionalDate(dateFrom);
            var to = ParseOptionalDate(dateTo);
            var result = await _statements.GetCashFlowAsync(from, to, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static DateTime? ParseOptionalDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var normalized = value.Trim()
            .Replace('\u06F0', '0').Replace('\u06F1', '1').Replace('\u06F2', '2').Replace('\u06F3', '3')
            .Replace('\u06F4', '4').Replace('\u06F5', '5').Replace('\u06F6', '6').Replace('\u06F7', '7')
            .Replace('\u06F8', '8').Replace('\u06F9', '9')
            .Replace('\u0660', '0').Replace('\u0661', '1').Replace('\u0662', '2').Replace('\u0663', '3')
            .Replace('\u0664', '4').Replace('\u0665', '5').Replace('\u0666', '6').Replace('\u0667', '7')
            .Replace('\u0668', '8').Replace('\u0669', '9');

        if (DateTime.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
        {
            return parsed.Date;
        }

        throw new InvalidOperationException("فرمت تاریخ نامعتبر است.");
    }
}
