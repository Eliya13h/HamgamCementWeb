using System.ComponentModel.DataAnnotations;
using Dapper;
using HamgamTransport.Server.Authorization;
using HamgamTransport.Server.Controllers.Common;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HamgamTransport.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/settlements")]
[Authorize]
public class PartySettlementController : FinanceControllerBase
{
    private readonly IPartySettlementService _settlements;
    private readonly ISqlConnectionFactory _sql;

    public PartySettlementController(
        AppDbContext db,
        IPartySettlementService settlements,
        ISqlConnectionFactory sql) : base(db)
    {
        _settlements = settlements;
        _sql = sql;
    }

    [HttpPost("datatable")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);
        var search = request.Search?.Value?.Trim();

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);
        const string baseWhere = "WHERE s.IsDeleted = 0";
        var where = baseWhere;
        var parameters = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += """
                 AND (
                    ISNULL(s.Description, '') LIKE @Search
                    OR CAST(s.PartySettlementID AS varchar(20)) LIKE @Search
                    OR ISNULL(c.Name, '') LIKE @Search
                    OR ISNULL(sup.Name, '') LIKE @Search
                    OR ISNULL(vo.Name, '') LIKE @Search
                    OR ISNULL(dr.Name, '') LIKE @Search
                 )
                """;
            parameters.Add("Search", $"%{search}%");
        }

        var recordsTotal = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM PartySettlements s {baseWhere}");
        var recordsFiltered = await connection.ExecuteScalarAsync<int>(
            $"""
             SELECT COUNT(1)
             FROM PartySettlements s
             LEFT JOIN Customers c ON c.CustomerID = s.PartyId AND s.PartyType = 1
             LEFT JOIN Suppliers sup ON sup.SupplierID = s.PartyId AND s.PartyType = 2
             LEFT JOIN VehicleOwners vo ON vo.VehicleOwnerId = s.PartyId AND s.PartyType = 3
             LEFT JOIN Drivers dr ON dr.DriverId = s.PartyId AND s.PartyType = 4
             {where}
             """, parameters);

        parameters.Add("Offset", start);
        parameters.Add("Fetch", length);

        var rows = (await connection.QueryAsync(
            $"""
             SELECT s.PartySettlementID AS partySettlementId,
                    s.PartyType AS partyType,
                    s.PartyId AS partyId,
                    CASE
                        WHEN s.PartyType = 1 THEN c.Name
                        WHEN s.PartyType = 2 THEN sup.Name
                        WHEN s.PartyType = 3 THEN vo.Name
                        WHEN s.PartyType = 4 THEN dr.Name
                        ELSE NULL
                    END AS partyName,
                    s.SettlementDate AS settlementDate,
                    s.CurrencyId AS currencyId,
                    cur.CurrencyCode AS currencyCode,
                    s.Amount AS amount,
                    s.AmountInBaseCurrency AS amountInBaseCurrency,
                    s.CashBoxId AS cashBoxId,
                    cb.Name AS cashBoxName,
                    s.BankAccountId AS bankAccountId,
                    ba.Name AS bankAccountName,
                    s.Description AS description,
                    s.JournalEntryId AS journalEntryId
             FROM PartySettlements s
             LEFT JOIN Customers c ON c.CustomerID = s.PartyId AND s.PartyType = 1
             LEFT JOIN Suppliers sup ON sup.SupplierID = s.PartyId AND s.PartyType = 2
             LEFT JOIN VehicleOwners vo ON vo.VehicleOwnerId = s.PartyId AND s.PartyType = 3
             LEFT JOIN Drivers dr ON dr.DriverId = s.PartyId AND s.PartyType = 4
             LEFT JOIN Currencies cur ON cur.CurrencyID = s.CurrencyId
             LEFT JOIN CashBoxes cb ON cb.CashBoxID = s.CashBoxId
             LEFT JOIN BankAccounts ba ON ba.BankAccountID = s.BankAccountId
             {where}
             ORDER BY s.SettlementDate DESC, s.PartySettlementID DESC
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
                var partyType = Convert.ToInt32(d["partyType"]);
                var partyTypeLabel = partyType switch
                {
                    1 => "مشتری",
                    2 => "تأمین‌کننده",
                    3 => "مالک وسیله",
                    4 => "راننده",
                    _ => partyType.ToString(),
                };
                return new
                {
                    rowNumber = start + i + 1,
                    partySettlementId = d["partySettlementId"],
                    partyType,
                    partyTypeLabel,
                    partyId = d["partyId"],
                    partyName = d["partyName"]?.ToString() ?? string.Empty,
                    settlementDate = Convert.ToDateTime(d["settlementDate"]).ToString("yyyy-MM-dd"),
                    currencyId = d["currencyId"],
                    currencyCode = d["currencyCode"],
                    amount = d["amount"],
                    amountInBaseCurrency = d["amountInBaseCurrency"],
                    cashBoxId = d["cashBoxId"],
                    cashBoxName = d["cashBoxName"],
                    bankAccountId = d["bankAccountId"],
                    bankAccountName = d["bankAccountName"],
                    description = d["description"],
                    journalEntryId = d["journalEntryId"],
                };
            }),
        });
    }

    [HttpPost]
    [HasPermission("accounting.expenses.create")]
    public async Task<IActionResult> Create([FromBody] CreatePartySettlementRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var settlement = await _settlements.PostAsync(
                new PartySettlementRequest(
                    (PartySettlementPartyType)request.PartyType,
                    request.PartyId,
                    request.SettlementDate ?? DateTime.Now,
                    request.CurrencyId,
                    request.Amount,
                    request.AmountInBaseCurrency,
                    request.CashBoxId,
                    request.BankAccountId,
                    request.Description),
                ResolveCurrentUserId(),
                cancellationToken);

            return Ok(new
            {
                message = "تسویه ثبت شد.",
                partySettlementId = settlement.PartySettlementID,
                journalEntryId = settlement.JournalEntryId,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [HasPermission("accounting.expenses.edit")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _settlements.SoftDeleteAsync(id, ResolveCurrentUserId(), cancellationToken);
            return Ok(new { message = "تسویه حذف شد." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class CreatePartySettlementRequest
{
    [Required]
    public int PartyType { get; set; }

    [Required]
    public int PartyId { get; set; }

    public DateTime? SettlementDate { get; set; }

    [Required]
    public int CurrencyId { get; set; }

    [Required]
    public decimal Amount { get; set; }

    public decimal? AmountInBaseCurrency { get; set; }

    public int? CashBoxId { get; set; }

    public int? BankAccountId { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}
