using System.ComponentModel.DataAnnotations;
using Dapper;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Controllers.Transport;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HamgamCementWeb.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/currency-exchanges")]
[Authorize]
public class CurrencyExchangeController : FinanceControllerBase
{
    private readonly ICurrencyExchangeService _exchanges;
    private readonly ISqlConnectionFactory _sql;

    public CurrencyExchangeController(
        AppDbContext db,
        ICurrencyExchangeService exchanges,
        ISqlConnectionFactory sql) : base(db)
    {
        _exchanges = exchanges;
        _sql = sql;
    }

    [HttpPost("datatable")]
    [HasPermission("accounting.currency-exchange.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);
        var search = request.Search?.Value?.Trim();

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);
        const string baseWhere = "WHERE t.IsDeleted = 0";
        var where = baseWhere;
        var parameters = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += """
                 AND (
                    ISNULL(t.Description, '') LIKE @Search
                    OR CAST(t.CurrencyExchangeTxnID AS varchar(20)) LIKE @Search
                    OR ISNULL(fc.CurrencyCode, '') LIKE @Search
                    OR ISNULL(tc.CurrencyCode, '') LIKE @Search
                 )
                """;
            parameters.Add("Search", $"%{search}%");
        }

        var recordsTotal = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM CurrencyExchangeTxns t {baseWhere}");
        var recordsFiltered = await connection.ExecuteScalarAsync<int>(
            $"""
             SELECT COUNT(1)
             FROM CurrencyExchangeTxns t
             LEFT JOIN Currencies fc ON fc.CurrencyID = t.FromCurrencyId
             LEFT JOIN Currencies tc ON tc.CurrencyID = t.ToCurrencyId
             {where}
             """, parameters);

        parameters.Add("Offset", start);
        parameters.Add("Fetch", length);

        var rows = (await connection.QueryAsync(
            $"""
             SELECT t.CurrencyExchangeTxnID AS currencyExchangeTxnId,
                    t.ExchangeDate AS exchangeDate,
                    t.FromCurrencyId AS fromCurrencyId,
                    fc.CurrencyCode AS fromCurrencyCode,
                    t.FromAmount AS fromAmount,
                    t.FromAmountInBaseCurrency AS fromAmountInBaseCurrency,
                    t.ToCurrencyId AS toCurrencyId,
                    tc.CurrencyCode AS toCurrencyCode,
                    t.ToAmount AS toAmount,
                    t.ToAmountInBaseCurrency AS toAmountInBaseCurrency,
                    t.DealRate AS dealRate,
                    t.RecognizeFxDifference AS recognizeFxDifference,
                    t.FxDifferenceInBaseCurrency AS fxDifferenceInBaseCurrency,
                    t.FromCashBoxId AS fromCashBoxId,
                    fcb.Name AS fromCashBoxName,
                    t.FromBankAccountId AS fromBankAccountId,
                    fba.Name AS fromBankAccountName,
                    t.ToCashBoxId AS toCashBoxId,
                    tcb.Name AS toCashBoxName,
                    t.ToBankAccountId AS toBankAccountId,
                    tba.Name AS toBankAccountName,
                    t.Description AS description,
                    t.JournalEntryId AS journalEntryId
             FROM CurrencyExchangeTxns t
             LEFT JOIN Currencies fc ON fc.CurrencyID = t.FromCurrencyId
             LEFT JOIN Currencies tc ON tc.CurrencyID = t.ToCurrencyId
             LEFT JOIN CashBoxes fcb ON fcb.CashBoxID = t.FromCashBoxId
             LEFT JOIN BankAccounts fba ON fba.BankAccountID = t.FromBankAccountId
             LEFT JOIN CashBoxes tcb ON tcb.CashBoxID = t.ToCashBoxId
             LEFT JOIN BankAccounts tba ON tba.BankAccountID = t.ToBankAccountId
             {where}
             ORDER BY t.ExchangeDate DESC, t.CurrencyExchangeTxnID DESC
             OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
             """, parameters)).ToList();

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select((r, i) =>
            {
                var d = (IDictionary<string, object>)r;
                var recognize = Convert.ToBoolean(d["recognizeFxDifference"]);
                var fxDiff = Convert.ToDecimal(d["fxDifferenceInBaseCurrency"] ?? 0m);
                return new
                {
                    rowNumber = start + i + 1,
                    currencyExchangeTxnId = d["currencyExchangeTxnId"],
                    exchangeDate = Convert.ToDateTime(d["exchangeDate"]).ToString("yyyy-MM-dd"),
                    fromCurrencyId = d["fromCurrencyId"],
                    fromCurrencyCode = d["fromCurrencyCode"],
                    fromAmount = d["fromAmount"],
                    fromAmountInBaseCurrency = d["fromAmountInBaseCurrency"],
                    toCurrencyId = d["toCurrencyId"],
                    toCurrencyCode = d["toCurrencyCode"],
                    toAmount = d["toAmount"],
                    toAmountInBaseCurrency = d["toAmountInBaseCurrency"],
                    dealRate = d["dealRate"],
                    recognizeFxDifference = recognize,
                    modeLabel = recognize ? "با تسعیر" : "فقط معامله",
                    fxDifferenceInBaseCurrency = fxDiff,
                    fromWallet = d["fromCashBoxName"]?.ToString()
                        ?? d["fromBankAccountName"]?.ToString()
                        ?? "—",
                    toWallet = d["toCashBoxName"]?.ToString()
                        ?? d["toBankAccountName"]?.ToString()
                        ?? "—",
                    description = d["description"],
                    journalEntryId = d["journalEntryId"],
                };
            }),
        });
    }

    [HttpPost]
    [HasPermission("accounting.currency-exchange.create")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCurrencyExchangeRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var txn = await _exchanges.PostAsync(
                new CurrencyExchangeRequest(
                    request.ExchangeDate ?? DateTime.Now,
                    request.FromCurrencyId,
                    request.FromAmount,
                    request.ToCurrencyId,
                    request.ToAmount,
                    request.RecognizeFxDifference,
                    request.FromCashBoxId,
                    request.FromBankAccountId,
                    request.ToCashBoxId,
                    request.ToBankAccountId,
                    request.Description),
                ResolveCurrentUserId(),
                cancellationToken);

            return Ok(new
            {
                message = "تبدیل ارز ثبت شد.",
                currencyExchangeTxnId = txn.CurrencyExchangeTxnID,
                journalEntryId = txn.JournalEntryId,
                fxDifferenceInBaseCurrency = txn.FxDifferenceInBaseCurrency,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [HasPermission("accounting.currency-exchange.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _exchanges.SoftDeleteAsync(id, ResolveCurrentUserId(), cancellationToken);
            return Ok(new { message = "سند تبدیل ارز حذف شد." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class CreateCurrencyExchangeRequest
{
    public DateTime? ExchangeDate { get; set; }

    [Required]
    public int FromCurrencyId { get; set; }

    [Required]
    public decimal FromAmount { get; set; }

    [Required]
    public int ToCurrencyId { get; set; }

    [Required]
    public decimal ToAmount { get; set; }

    public bool RecognizeFxDifference { get; set; }

    public int? FromCashBoxId { get; set; }

    public int? FromBankAccountId { get; set; }

    public int? ToCashBoxId { get; set; }

    public int? ToBankAccountId { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}
