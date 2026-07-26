using Dapper;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Controllers.Transport;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HamgamCementWeb.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/journal")]
[Authorize]
public class JournalController : FinanceControllerBase
{
    private readonly ISqlConnectionFactory _sql;

    public JournalController(AppDbContext db, ISqlConnectionFactory sql) : base(db)
    {
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

        const string baseWhere = "WHERE e.IsDeleted = 0";
        var where = baseWhere;
        var parameters = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += " AND (e.EntryNumber LIKE @Search OR e.Description LIKE @Search)";
            parameters.Add("Search", $"%{search}%");
        }

        var recordsTotal = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM JournalEntries e {baseWhere}");
        var recordsFiltered = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM JournalEntries e {where}", parameters);

        parameters.Add("Offset", start);
        parameters.Add("Fetch", length);

        var rows = (await connection.QueryAsync<JournalListRow>(
            $"""
             SELECT e.JournalEntryID AS JournalEntryId,
                    e.EntryNumber,
                    e.EntryDate,
                    e.Description,
                    e.Source,
                    e.TotalDebitInBaseCurrency,
                    e.TotalCreditInBaseCurrency
             FROM JournalEntries e
             {where}
             ORDER BY e.EntryDate DESC, e.JournalEntryID DESC
             OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
             """, parameters)).ToList();

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select((r, i) => new
            {
                rowNumber = start + i + 1,
                journalEntryId = r.JournalEntryId,
                entryNumber = r.EntryNumber,
                entryDate = r.EntryDate.ToString("yyyy-MM-dd"),
                description = r.Description,
                source = r.Source,
                sourceLabel = SourceLabel(r.Source),
                totalDebitInBaseCurrency = r.TotalDebitInBaseCurrency,
                totalCreditInBaseCurrency = r.TotalCreditInBaseCurrency,
            }),
        });
    }

    [HttpGet("{id:int}")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        var entry = await connection.QueryFirstOrDefaultAsync<JournalListRow>(
            """
            SELECT JournalEntryID AS JournalEntryId, EntryNumber, EntryDate, Description, Source,
                   TotalDebitInBaseCurrency, TotalCreditInBaseCurrency
            FROM JournalEntries
            WHERE JournalEntryID = @Id AND IsDeleted = 0
            """, new { Id = id });

        if (entry is null)
        {
            return NotFound(new { message = "سند یافت نشد." });
        }

        var lines = await connection.QueryAsync(
            """
            SELECT l.JournalLineID AS journalLineId,
                   l.[lineNo] AS [lineNo],
                   l.AccountId AS accountId,
                   a.Code AS accountCode,
                   a.Name AS accountName,
                   l.Description AS description,
                   l.Debit AS debit,
                   l.Credit AS credit,
                   l.DebitInBaseCurrency AS debitInBaseCurrency,
                   l.CreditInBaseCurrency AS creditInBaseCurrency
            FROM JournalLines l
            INNER JOIN Accounts a ON a.AccountID = l.AccountId
            WHERE l.JournalEntryId = @Id AND l.IsDeleted = 0
            ORDER BY l.[lineNo]
            """, new { Id = id });

        return Ok(new
        {
            journalEntryId = entry.JournalEntryId,
            entryNumber = entry.EntryNumber,
            entryDate = entry.EntryDate.ToString("yyyy-MM-dd"),
            description = entry.Description,
            source = entry.Source,
            sourceLabel = SourceLabel(entry.Source),
            totalDebitInBaseCurrency = entry.TotalDebitInBaseCurrency,
            totalCreditInBaseCurrency = entry.TotalCreditInBaseCurrency,
            lines,
        });
    }

    private static string SourceLabel(int source) => source switch
    {
        1 => "فاکتور خرید",
        2 => "فاکتور فروش",
        3 => "مصرف",
        4 => "عاید",
        5 => "تولید",
        6 => "انتقال صندوق",
        7 => "دستی",
        8 => "حقوق",
        9 => "انبارگردانی",
        10 => "انتقال انبار",
        11 => "اختتام سال مالی",
        12 => "معکوس اختتام سال",
        13 => "خرید دارایی ثابت",
        14 => "استهلاک دارایی ثابت",
        15 => "فروش/اسقاط دارایی ثابت",
        _ => source.ToString(),
    };

    private sealed class JournalListRow
    {
        public int JournalEntryId { get; set; }
        public string EntryNumber { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Source { get; set; }
        public decimal TotalDebitInBaseCurrency { get; set; }
        public decimal TotalCreditInBaseCurrency { get; set; }
    }
}
