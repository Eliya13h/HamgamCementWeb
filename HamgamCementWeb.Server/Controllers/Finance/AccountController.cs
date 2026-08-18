using System.Globalization;
using Dapper;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/accounts")]
[Authorize]
public class AccountController : FinanceControllerBase
{
    private readonly IFinanceReadService _reads;
    private readonly ISqlConnectionFactory _sql;

    public AccountController(AppDbContext db, IFinanceReadService reads, ISqlConnectionFactory sql) : base(db)
    {
        _reads = reads;
        _sql = sql;
    }

    [HttpGet("tree")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> Tree(CancellationToken cancellationToken)
    {
        var accounts = await _reads.GetAccountTreeAsync(cancellationToken);
        return Ok(accounts.Select(a => new
        {
            accountId = a.AccountId,
            code = a.Code,
            name = a.Name,
            level = a.Level,
            parentAccountId = a.ParentAccountId,
            accountType = a.AccountType,
            nature = a.Nature,
            isPostable = a.IsPostable,
            isSystem = a.IsSystem,
            systemCode = a.SystemCode,
        }));
    }

    [HttpGet("{id:int}")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var account = await Db.Accounts
            .AsNoTracking()
            .Where(a => a.AccountID == id && a.IsDeleted != true)
            .Select(a => new
            {
                accountId = a.AccountID,
                code = a.Code,
                name = a.Name,
                level = (int)a.Level,
                parentAccountId = a.ParentAccountId,
                accountType = a.AccountType,
                nature = a.Nature,
                isPostable = a.IsPostable,
                isSystem = a.IsSystem,
                description = a.Description,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return account is null ? NotFound(new { message = "حساب یافت نشد." }) : Ok(account);
    }

    // گردش حساب — مانده اول دوره + خطوط دفتر در بازه
    [HttpGet("{id:int}/ledger")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> Ledger(
        int id,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] int? partyId,
        [FromQuery] int? costCenterId,
        CancellationToken cancellationToken)
    {
        var account = await Db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountID == id && a.IsDeleted != true, cancellationToken);
        if (account is null)
        {
            return NotFound(new { message = "حساب یافت نشد." });
        }

        DateTime? from;
        DateTime? to;
        try
        {
            from = ParseOptionalDate(dateFrom);
            to = ParseOptionalDate(dateTo);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var today = DateTime.Today;
        var solarYear = JalaliDateHelper.GetSolarYear(today);
        var (yearStart, _) = JalaliDateHelper.GetSolarYearRange(solarYear);
        var start = (from ?? yearStart).Date;
        var end = (to ?? today).Date;
        if (start > end)
        {
            return BadRequest(new { message = "تاریخ شروع نباید بعد از تاریخ پایان باشد." });
        }

        var endInclusive = end.AddDays(1).AddTicks(-1);
        string? costCenterLabel = null;
        if (costCenterId is > 0)
        {
            costCenterLabel = await Db.CostCenters.AsNoTracking()
                .Where(c => c.CostCenterID == costCenterId && c.IsDeleted != true)
                .Select(c => c.Code + " — " + c.Name)
                .FirstOrDefaultAsync(cancellationToken);
            if (costCenterLabel is null)
            {
                return BadRequest(new { message = "مرکز هزینه یافت نشد." });
            }
        }

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        var opening = await connection.QueryFirstAsync<(decimal Debit, decimal Credit)>(
            """
            SELECT ISNULL(SUM(jl.DebitInBaseCurrency), 0) AS Debit,
                   ISNULL(SUM(jl.CreditInBaseCurrency), 0) AS Credit
            FROM JournalLines jl
            INNER JOIN JournalEntries je ON je.JournalEntryID = jl.JournalEntryId
            WHERE jl.AccountId = @AccountId
              AND ISNULL(jl.IsDeleted, 0) = 0
              AND ISNULL(je.IsDeleted, 0) = 0
              AND je.IsPosted = 1
              AND je.EntryDate < @Start
              AND (@PartyId IS NULL OR jl.PartyId = @PartyId)
              AND (@CostCenterId IS NULL OR jl.CostCenterId = @CostCenterId)
            """,
            new { AccountId = id, Start = start, PartyId = partyId, CostCenterId = costCenterId });

        // نکته: LineNo / LINENO کلمهٔ رزرو SQL Server است؛ alias و ارجاع باید داخل [] باشد
        var lines = (await connection.QueryAsync<LedgerLineRow>(
            """
            SELECT je.JournalEntryID AS JournalEntryId,
                   je.EntryNumber AS EntryNumber,
                   CONVERT(varchar(10), je.EntryDate, 23) AS EntryDate,
                   je.Description AS EntryDescription,
                   CAST(je.Source AS int) AS Source,
                   l.JournalLineID AS JournalLineId,
                   l.[LineNo] AS LineNumber,
                   l.Description AS LineDescription,
                   l.DebitInBaseCurrency AS DebitInBase,
                   l.CreditInBaseCurrency AS CreditInBase,
                   l.PartyId AS PartyId,
                   l.CashBoxId AS CashBoxId,
                   l.CostCenterId AS CostCenterId,
                   cc.Code AS CostCenterCode,
                   cc.Name AS CostCenterName
            FROM JournalLines l
            INNER JOIN JournalEntries je ON je.JournalEntryID = l.JournalEntryId
            LEFT JOIN CostCenters cc ON cc.CostCenterID = l.CostCenterId AND ISNULL(cc.IsDeleted, 0) = 0
            WHERE l.AccountId = @AccountId
              AND ISNULL(l.IsDeleted, 0) = 0
              AND ISNULL(je.IsDeleted, 0) = 0
              AND je.IsPosted = 1
              AND je.EntryDate >= @Start
              AND je.EntryDate <= @EndInclusive
              AND (@PartyId IS NULL OR l.PartyId = @PartyId)
              AND (@CostCenterId IS NULL OR l.CostCenterId = @CostCenterId)
            ORDER BY je.EntryDate, je.JournalEntryID, l.[LineNo]
            """,
            new
            {
                AccountId = id,
                Start = start,
                EndInclusive = endInclusive,
                PartyId = partyId,
                CostCenterId = costCenterId,
            })).AsList();

        var openingBalance = opening.Debit - opening.Credit;
        var running = openingBalance;
        var mapped = new List<object>();
        foreach (var line in lines)
        {
            running += line.DebitInBase - line.CreditInBase;
            mapped.Add(new
            {
                journalEntryId = line.JournalEntryId,
                entryNumber = line.EntryNumber,
                entryDate = line.EntryDate,
                entryDescription = line.EntryDescription,
                source = line.Source,
                journalLineId = line.JournalLineId,
                lineNo = line.LineNumber,
                lineDescription = line.LineDescription,
                debitInBase = line.DebitInBase,
                creditInBase = line.CreditInBase,
                partyId = line.PartyId,
                cashBoxId = line.CashBoxId,
                costCenterId = line.CostCenterId,
                costCenterLabel = line.CostCenterId is > 0 && !string.IsNullOrWhiteSpace(line.CostCenterCode)
                    ? $"{line.CostCenterCode} — {line.CostCenterName}"
                    : null,
                runningBalance = running,
            });
        }

        return Ok(new
        {
            accountId = account.AccountID,
            code = account.Code,
            name = account.Name,
            from = JalaliDateHelper.FormatDate(start),
            to = JalaliDateHelper.FormatDate(end),
            fromLabel = JalaliDateHelper.FormatDateWithMonthName(start),
            toLabel = JalaliDateHelper.FormatDateWithMonthName(end),
            costCenterId,
            costCenterLabel,
            openingDebit = opening.Debit,
            openingCredit = opening.Credit,
            openingBalance,
            closingBalance = running,
            lines = mapped,
        });
    }

    [HttpPost]
    [HasPermission("accounting.expenses.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var parent = await Db.Accounts
                .FirstOrDefaultAsync(a => a.AccountID == request.ParentAccountId && a.IsDeleted != true, cancellationToken)
                ?? throw new InvalidOperationException("حساب والد یافت نشد.");

            if (parent.Level >= AccountLevel.Tafsili)
            {
                throw new InvalidOperationException("زیر حساب تفصیلی نمی‌توان حساب جدید ساخت.");
            }

            var level = (AccountLevel)((int)parent.Level + 1);
            var code = string.IsNullOrWhiteSpace(request.Code)
                ? await NextChildCodeAsync(parent, cancellationToken)
                : request.Code.Trim();

            if (await Db.Accounts.AnyAsync(a => a.Code == code && a.IsDeleted != true, cancellationToken))
            {
                throw new InvalidOperationException($"کد حساب «{code}» تکراری است.");
            }

            var isPostable = request.IsPostable ?? (level is AccountLevel.Moein or AccountLevel.Tafsili);
            if (isPostable && level is AccountLevel.Group or AccountLevel.Kol)
            {
                throw new InvalidOperationException("حساب گروه/کل نمی‌تواند قابل‌ثبت باشد.");
            }

            // والد قابل‌ثبت نباید فرزند بگیرد مگر اینکه از حالت postable خارج شود
            if (parent.IsPostable)
            {
                var hasLines = await Db.JournalLines.AnyAsync(
                    l => l.AccountId == parent.AccountID && l.IsDeleted != true, cancellationToken);
                if (hasLines)
                {
                    throw new InvalidOperationException(
                        "حساب والد دارای گردش است و نمی‌توان زیر آن حساب جدید ساخت.");
                }

                parent.IsPostable = false;
                parent.UpdatedAt = DateTime.Now;
                parent.IsUpdated = true;
                parent.UpdatedBy = ResolveCurrentUserId();
            }

            var account = new Account
            {
                Code = code,
                Name = request.Name.Trim(),
                Level = level,
                ParentAccountId = parent.AccountID,
                AccountType = parent.AccountType,
                Nature = parent.Nature,
                IsPostable = isPostable,
                IsSystem = false,
                Description = request.Description?.Trim(),
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreatedBy = ResolveCurrentUserId(),
            };

            Db.Accounts.Add(account);
            await Db.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(Get), new { id = account.AccountID }, new
            {
                message = "حساب با موفقیت ایجاد شد.",
                accountId = account.AccountID,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [HasPermission("accounting.expenses.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var account = await Db.Accounts
            .FirstOrDefaultAsync(a => a.AccountID == id && a.IsDeleted != true, cancellationToken);
        if (account is null)
        {
            return NotFound(new { message = "حساب یافت نشد." });
        }

        if (account.IsSystem)
        {
            // فقط نام نمایشی و شرح حساب سیستمی قابل ویرایش است
            account.Name = request.Name.Trim();
            account.Description = request.Description?.Trim();
            account.UpdatedAt = DateTime.Now;
            account.IsUpdated = true;
            account.UpdatedBy = ResolveCurrentUserId();
            await Db.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "حساب سیستمی به‌روزرسانی شد (فقط نام/شرح)." });
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(request.Code) && request.Code.Trim() != account.Code)
            {
                var code = request.Code.Trim();
                if (await Db.Accounts.AnyAsync(a => a.Code == code && a.AccountID != id && a.IsDeleted != true, cancellationToken))
                {
                    throw new InvalidOperationException($"کد حساب «{code}» تکراری است.");
                }

                account.Code = code;
            }

            var hasChildren = await Db.Accounts.AnyAsync(
                a => a.ParentAccountId == id && a.IsDeleted != true, cancellationToken);
            var isPostable = request.IsPostable ?? account.IsPostable;
            if (isPostable && hasChildren)
            {
                throw new InvalidOperationException("حساب دارای زیرمجموعه نمی‌تواند قابل‌ثبت باشد.");
            }

            if (isPostable && account.Level is AccountLevel.Group or AccountLevel.Kol)
            {
                throw new InvalidOperationException("حساب گروه/کل نمی‌تواند قابل‌ثبت باشد.");
            }

            account.Name = request.Name.Trim();
            account.Description = request.Description?.Trim();
            account.IsPostable = isPostable;
            account.UpdatedAt = DateTime.Now;
            account.IsUpdated = true;
            account.UpdatedBy = ResolveCurrentUserId();
            await Db.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "حساب با موفقیت ویرایش شد." });
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
        var account = await Db.Accounts
            .FirstOrDefaultAsync(a => a.AccountID == id && a.IsDeleted != true, cancellationToken);
        if (account is null)
        {
            return NotFound(new { message = "حساب یافت نشد." });
        }

        if (account.IsSystem)
        {
            return BadRequest(new { message = "حذف حساب سیستمی مجاز نیست." });
        }

        var hasChildren = await Db.Accounts.AnyAsync(
            a => a.ParentAccountId == id && a.IsDeleted != true, cancellationToken);
        if (hasChildren)
        {
            return BadRequest(new { message = "ابتدا زیرحساب‌ها را حذف کنید." });
        }

        var hasLines = await Db.JournalLines.AnyAsync(
            l => l.AccountId == id && l.IsDeleted != true, cancellationToken);
        if (hasLines)
        {
            return BadRequest(new { message = "حساب دارای گردش دفتر است و قابل حذف نیست." });
        }

        account.IsDeleted = true;
        account.IsActive = false;
        account.DeletedAt = DateTime.Now;
        account.DeletedBy = ResolveCurrentUserId();
        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "حساب با موفقیت حذف شد." });
    }

    private async Task<string> NextChildCodeAsync(Account parent, CancellationToken cancellationToken)
    {
        var siblings = await Db.Accounts
            .AsNoTracking()
            .Where(a => a.ParentAccountId == parent.AccountID && a.IsDeleted != true)
            .Select(a => a.Code)
            .ToListAsync(cancellationToken);

        var maxSeq = 0;
        var prefix = parent.Code;
        foreach (var code in siblings)
        {
            if (!code.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var suffix = code[prefix.Length..].TrimStart('-');
            if (int.TryParse(suffix, out var n) && n > maxSeq)
            {
                maxSeq = n;
            }
        }

        var next = maxSeq + 1;
        return parent.Level switch
        {
            AccountLevel.Group => $"{prefix}{next}",
            AccountLevel.Kol => $"{prefix}{next}",
            _ => $"{prefix}-{next:D2}",
        };
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

    public class SaveAccountRequest
    {
        public int ParentAccountId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public bool? IsPostable { get; set; }
    }

    private sealed class LedgerLineRow
    {
        public int JournalEntryId { get; set; }
        public string EntryNumber { get; set; } = string.Empty;
        public string EntryDate { get; set; } = string.Empty;
        public string? EntryDescription { get; set; }
        public int Source { get; set; }
        public int JournalLineId { get; set; }
        public int LineNumber { get; set; }
        public string? LineDescription { get; set; }
        public decimal DebitInBase { get; set; }
        public decimal CreditInBase { get; set; }
        public int? PartyId { get; set; }
        public int? CashBoxId { get; set; }
        public int? CostCenterId { get; set; }
        public string? CostCenterCode { get; set; }
        public string? CostCenterName { get; set; }
    }
}
