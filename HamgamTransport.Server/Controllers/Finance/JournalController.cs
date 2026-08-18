using System.Globalization;
using Dapper;
using HamgamTransport.Server.Authorization;
using HamgamTransport.Server.Controllers.Common;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/journal")]
[Authorize]
public class JournalController : FinanceControllerBase
{
    private readonly ISqlConnectionFactory _sql;
    private readonly IJournalPostingService _journal;
    private readonly ICurrencyConversionService _currency;
    private readonly IAccountingIntegrityService _integrity;
    private readonly IAccountLookupService _accounts;

    public JournalController(
        AppDbContext db,
        ISqlConnectionFactory sql,
        IJournalPostingService journal,
        ICurrencyConversionService currency,
        IAccountingIntegrityService integrity,
        IAccountLookupService accounts) : base(db)
    {
        _sql = sql;
        _journal = journal;
        _currency = currency;
        _integrity = integrity;
        _accounts = accounts;
    }

    // بررسی یکپارچگی دابل‌انتری — فقط‌خواندنی، برای آماده‌سازی پرداکشن
    [HttpGet("integrity-check")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> IntegrityCheck(CancellationToken cancellationToken)
    {
        var issues = await _integrity.CheckAsync(cancellationToken);
        return Ok(new
        {
            ok = issues.Count == 0,
            issueCount = issues.Count,
            issues = issues.Select(i => new
            {
                code = i.Code,
                message = i.Message,
                relatedId = i.RelatedId,
            }),
        });
    }

    // پیشنهاد حساب تفصیلی طرف‌حساب برای پر کردن فرم سند دستی
    [HttpGet("party-account")]
    [HasPermission("accounting.expenses.create")]
    public async Task<IActionResult> PartyAccount(
        [FromQuery] int partyType,
        [FromQuery] int partyId,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(PartySettlementPartyType), partyType))
        {
            return BadRequest(new { message = "نوع طرف‌حساب نامعتبر است." });
        }

        if (partyId <= 0)
        {
            return BadRequest(new { message = "شناسه طرف‌حساب نامعتبر است." });
        }

        try
        {
            var type = (PartySettlementPartyType)partyType;
            var resolved = await ResolvePartyAccountAsync(type, partyId, cancellationToken);
            return Ok(new
            {
                accountId = resolved.Account.AccountID,
                code = resolved.Account.Code,
                name = resolved.Account.Name,
                partyName = resolved.PartyName,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
                canDelete = r.Source == (int)JournalSource.Manual,
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
                   l.[LineNo] AS [lineNo],
                   l.AccountId AS accountId,
                   a.Code AS accountCode,
                   a.Name AS accountName,
                   l.Description AS description,
                   l.Debit AS debit,
                   l.Credit AS credit,
                   l.DebitInBaseCurrency AS debitInBaseCurrency,
                   l.CreditInBaseCurrency AS creditInBaseCurrency,
                   l.CurrencyId AS currencyId,
                   l.CashBoxId AS cashBoxId,
                   l.PartyId AS partyId,
                   l.PartyType AS partyType,
                   CASE l.PartyType
                       WHEN 1 THEN c.Name
                       WHEN 2 THEN s.Name
                       WHEN 3 THEN o.Name
                       WHEN 4 THEN d.Name
                       ELSE NULL
                   END AS partyName,
                   l.CostCenterId AS costCenterId
            FROM JournalLines l
            INNER JOIN Accounts a ON a.AccountID = l.AccountId
            LEFT JOIN Customers c ON c.CustomerID = l.PartyId AND l.PartyType = 1 AND c.IsDeleted = 0
            LEFT JOIN Suppliers s ON s.SupplierID = l.PartyId AND l.PartyType = 2 AND s.IsDeleted = 0
            LEFT JOIN VehicleOwners o ON o.VehicleOwnerId = l.PartyId AND l.PartyType = 3 AND o.IsDeleted = 0
            LEFT JOIN Drivers d ON d.DriverId = l.PartyId AND l.PartyType = 4 AND d.IsDeleted = 0
            WHERE l.JournalEntryId = @Id AND l.IsDeleted = 0
            ORDER BY l.[LineNo]
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
            canDelete = entry.Source == (int)JournalSource.Manual,
            lines,
        });
    }

    [HttpPost]
    [HasPermission("accounting.expenses.create")]
    public async Task<IActionResult> Create(
        [FromBody] ManualJournalRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.Lines is null || request.Lines.Count < 2)
        {
            return BadRequest(new { message = "سند دستی باید حداقل دو ردیف داشته باشد." });
        }

        try
        {
            var entryDate = ParseRequiredDate(request.EntryDate);
            var baseCurrency = await _currency.GetBaseCurrencyAsync(cancellationToken);
            var drafts = new List<JournalLineDraft>();

            foreach (var line in request.Lines)
            {
                if (line.AccountId <= 0)
                {
                    throw new InvalidOperationException("حساب هر ردیف الزامی است.");
                }

                var debit = line.Debit;
                var credit = line.Credit;
                if (debit < 0 || credit < 0)
                {
                    throw new InvalidOperationException("مبالغ نمی‌توانند منفی باشند.");
                }

                if ((debit > 0 && credit > 0) || (debit == 0 && credit == 0))
                {
                    throw new InvalidOperationException("هر ردیف باید فقط دیبت یا فقط کریدیت باشد.");
                }

                var hasPartyType = line.PartyType is > 0;
                var hasPartyId = line.PartyId is > 0;
                if (hasPartyType != hasPartyId)
                {
                    throw new InvalidOperationException("برای طرف‌حساب باید هم نوع و هم شخص انتخاب شوند.");
                }

                PartySettlementPartyType? partyType = null;
                int? partyId = null;
                if (hasPartyType && hasPartyId)
                {
                    if (!Enum.IsDefined(typeof(PartySettlementPartyType), line.PartyType!.Value))
                    {
                        throw new InvalidOperationException("نوع طرف‌حساب نامعتبر است.");
                    }

                    partyType = (PartySettlementPartyType)line.PartyType.Value;
                    partyId = line.PartyId;
                    await EnsurePartyExistsAsync(partyType.Value, partyId.Value, cancellationToken);
                }

                var currencyId = line.CurrencyId > 0 ? line.CurrencyId : baseCurrency.CurrencyID;
                var snapshot = await _currency.GetSnapshotAsync(currencyId, entryDate, cancellationToken);
                var debitBase = _currency.ConvertToBase(debit, snapshot);
                var creditBase = _currency.ConvertToBase(credit, snapshot);

                drafts.Add(new JournalLineDraft(
                    line.AccountId,
                    debit,
                    credit,
                    debitBase,
                    creditBase,
                    currencyId,
                    line.Description?.Trim(),
                    line.CashBoxId,
                    partyId,
                    line.CostCenterId,
                    partyType));
            }

            var entry = await _journal.PostAsync(
                entryDate,
                string.IsNullOrWhiteSpace(request.Description) ? "سند دستی" : request.Description.Trim(),
                JournalSource.Manual,
                null,
                baseCurrency.CurrencyID,
                drafts,
                ResolveCurrentUserId(),
                cancellationToken);

            return Ok(new
            {
                message = "سند دستی ثبت شد.",
                journalEntryId = entry.JournalEntryID,
                entryNumber = entry.EntryNumber,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [HasPermission("accounting.expenses.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _journal.SoftDeleteEntryAsync(id, ResolveCurrentUserId(), cancellationToken);
            return Ok(new { message = "سند دستی ثبت‌شده با سند معکوس برگشت داده شد؛ پیش‌نویس حذف شد." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/reverse")]
    [HasPermission("accounting.expenses.create")]
    public async Task<IActionResult> Reverse(int id, [FromQuery] DateTime? reverseDate, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await _journal.ReverseEntryAsync(id, ResolveCurrentUserId(), reverseDate, cancellationToken);
            return Ok(new { message = "سند معکوس ثبت شد.", journalEntryId = entry.JournalEntryID, entryNumber = entry.EntryNumber });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public static string SourceLabel(int source) => JournalSourceLabels.Label(source);

    private async Task EnsurePartyExistsAsync(
        PartySettlementPartyType partyType,
        int partyId,
        CancellationToken cancellationToken)
    {
        var exists = partyType switch
        {
            PartySettlementPartyType.Customer => await Db.Customers
                .AnyAsync(c => c.CustomerID == partyId && c.IsDeleted != true, cancellationToken),
            PartySettlementPartyType.Supplier => await Db.Suppliers
                .AnyAsync(s => s.SupplierID == partyId && s.IsDeleted != true, cancellationToken),
            PartySettlementPartyType.VehicleOwner => await Db.VehicleOwners
                .AnyAsync(o => o.VehicleOwnerId == partyId && o.IsDeleted != true, cancellationToken),
            PartySettlementPartyType.Driver => await Db.Drivers
                .AnyAsync(d => d.DriverId == partyId && d.IsDeleted != true, cancellationToken),
            _ => false,
        };

        if (!exists)
        {
            throw new InvalidOperationException("طرف‌حساب انتخاب‌شده یافت نشد.");
        }
    }

    private async Task<(Data.Models.Finance.Account Account, string PartyName)> ResolvePartyAccountAsync(
        PartySettlementPartyType partyType,
        int partyId,
        CancellationToken cancellationToken)
    {
        switch (partyType)
        {
            case PartySettlementPartyType.Customer:
            {
                var customer = await Db.Customers.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CustomerID == partyId && c.IsDeleted != true, cancellationToken)
                    ?? throw new InvalidOperationException("مشتری یافت نشد.");
                var account = await _accounts.EnsureCustomerAccountAsync(customer.CustomerID, customer.Name, cancellationToken);
                return (account, customer.Name);
            }
            case PartySettlementPartyType.Supplier:
            {
                var supplier = await Db.Suppliers.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SupplierID == partyId && s.IsDeleted != true, cancellationToken)
                    ?? throw new InvalidOperationException("تأمین‌کننده یافت نشد.");
                var account = await _accounts.EnsureSupplierAccountAsync(supplier.SupplierID, supplier.Name, cancellationToken);
                return (account, supplier.Name);
            }
            case PartySettlementPartyType.VehicleOwner:
            {
                var owner = await Db.VehicleOwners.AsNoTracking()
                    .FirstOrDefaultAsync(o => o.VehicleOwnerId == partyId && o.IsDeleted != true, cancellationToken)
                    ?? throw new InvalidOperationException("مالک وسیله یافت نشد.");
                var account = await _accounts.EnsureVehicleOwnerAccountAsync(owner.VehicleOwnerId, owner.Name, cancellationToken);
                return (account, owner.Name);
            }
            case PartySettlementPartyType.Driver:
            {
                var driver = await Db.Drivers.AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DriverId == partyId && d.IsDeleted != true, cancellationToken)
                    ?? throw new InvalidOperationException("راننده یافت نشد.");
                var account = await _accounts.EnsureDriverAccountAsync(driver.DriverId, driver.Name, cancellationToken);
                return (account, driver.Name);
            }
            default:
                throw new InvalidOperationException("نوع طرف‌حساب نامعتبر است.");
        }
    }

    private static DateTime ParseRequiredDate(string? value)
    {
        var parsed = ParseOptionalDate(value);
        if (parsed is null)
        {
            throw new InvalidOperationException("تاریخ سند الزامی است.");
        }

        return parsed.Value;
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

    public class ManualJournalRequest
    {
        public string EntryDate { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<ManualJournalLineRequest> Lines { get; set; } = [];
    }

    public class ManualJournalLineRequest
    {
        public int AccountId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public int CurrencyId { get; set; }
        public string? Description { get; set; }
        public int? CashBoxId { get; set; }
        public int? PartyId { get; set; }
        public int? PartyType { get; set; }
        public int? CostCenterId { get; set; }
    }

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
