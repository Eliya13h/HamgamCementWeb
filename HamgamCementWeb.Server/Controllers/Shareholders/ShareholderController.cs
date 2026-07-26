using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Data.Models.People;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Shareholders;

[ApiController]
[Route("api/shareholders")]
[Authorize]
public class ShareholderController : ControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = "FullName",
        [3] = nameof(Shareholder.ProfitShare),
        [4] = nameof(Shareholder.LossShare),
        [5] = nameof(Shareholder.InitialBalance),
        [6] = nameof(Shareholder.IsActive),
    };

    private readonly AppDbContext _db;
    private readonly IAccountLookupService _accounts;
    private readonly IShareholderEquityPostingService _equity;
    private readonly ICurrencyConversionService _currency;

    public ShareholderController(
        AppDbContext db,
        IAccountLookupService accounts,
        IShareholderEquityPostingService equity,
        ICurrencyConversionService currency)
    {
        _db = db;
        _accounts = accounts;
        _equity = equity;
        _currency = currency;
    }

    [HttpGet("options")]
    [HasPermission("people.shareholders.view")]
    public async Task<IActionResult> Options(CancellationToken cancellationToken)
    {
        var rows = await _db.Shareholders
            .AsNoTracking()
            .Where(s => s.IsDeleted != true && s.IsActive == true)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Select(s => new
            {
                value = s.ShareholderID,
                label = (s.FirstName + " " + s.LastName).Trim(),
                profitShare = s.ProfitShare,
                lossShare = s.LossShare,
                accountId = s.AccountId,
            })
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost("datatable")]
    [HasPermission("people.shareholders.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var draw = request.Draw;
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = _db.Shareholders
            .AsNoTracking()
            .Where(s => s.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(s =>
                s.FirstName.Contains(searchValue) ||
                s.LastName.Contains(searchValue) ||
                (s.Description != null && s.Description.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var orderedQuery = ApplyOrdering(query, request.Order);
        var rows = await orderedQuery
            .Skip(start)
            .Take(length)
            .Select(s => new ShareholderTableRow
            {
                ShareholderId = s.ShareholderID,
                Title = s.Title,
                FirstName = s.FirstName,
                LastName = s.LastName,
                InitialBalance = s.InitialBalance,
                Description = s.Description ?? string.Empty,
                ProfitShare = s.ProfitShare,
                LossShare = s.LossShare,
                IsActive = s.IsActive == true,
                AccountId = s.AccountId,
                AccountCode = s.Account != null ? s.Account.Code : null,
            })
            .ToListAsync(cancellationToken);

        var ids = rows.Select(r => r.ShareholderId).ToList();
        var openedIds = await _db.ShareholderEquityTxns
            .AsNoTracking()
            .Where(t =>
                ids.Contains(t.ShareholderId)
                && t.IsDeleted != true
                && t.TxnType == ShareholderEquityTxnType.OpeningBalance)
            .Select(t => t.ShareholderId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var openedSet = openedIds.ToHashSet();

        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].RowNumber = start + i + 1;
            rows[i].FullName = $"{rows[i].FirstName} {rows[i].LastName}".Trim();
            rows[i].HasOpeningBalance = openedSet.Contains(rows[i].ShareholderId);
        }

        return Ok(new
        {
            draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select(r => new
            {
                r.RowNumber,
                r.ShareholderId,
                title = (int)r.Title,
                r.FirstName,
                r.LastName,
                r.FullName,
                r.InitialBalance,
                r.Description,
                r.ProfitShare,
                r.LossShare,
                r.IsActive,
                r.AccountId,
                r.AccountCode,
                r.HasOpeningBalance,
            }),
        });
    }

    [HttpPost]
    [HasPermission("people.shareholders.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveShareholderRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var shareholder = new Shareholder
        {
            Title = request.Title,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            InitialBalance = request.InitialBalance,
            Description = request.Description?.Trim(),
            ProfitShare = request.ProfitShare,
            LossShare = request.LossShare,
            CreatedBy = ResolveCurrentUserId(),
            CreatedAt = DateTime.Now,
            IsActive = request.IsActive,
            IsDeleted = false,
        };

        _db.Shareholders.Add(shareholder);
        await _db.SaveChangesAsync(cancellationToken);

        var fullName = $"{shareholder.FirstName} {shareholder.LastName}".Trim();
        var account = await _accounts.EnsureShareholderAccountAsync(
            shareholder.ShareholderID,
            fullName,
            cancellationToken);
        shareholder.AccountId = account.AccountID;
        await _db.SaveChangesAsync(cancellationToken);

        if (shareholder.InitialBalance > 0)
        {
            await PostOpeningBalanceAsync(shareholder, cancellationToken);
        }

        return CreatedAtAction(
            nameof(Update),
            new { id = shareholder.ShareholderID },
            new { message = "سهام‌دار با موفقیت ایجاد شد.", shareholderId = shareholder.ShareholderID });
    }

    [HttpPut("{id:int}")]
    [HasPermission("people.shareholders.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveShareholderRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var shareholder = await _db.Shareholders
            .FirstOrDefaultAsync(s => s.ShareholderID == id && s.IsDeleted != true, cancellationToken);

        if (shareholder is null)
        {
            return NotFound(new { message = "سهام‌دار یافت نشد." });
        }

        shareholder.Title = request.Title;
        shareholder.FirstName = request.FirstName.Trim();
        shareholder.LastName = request.LastName.Trim();
        shareholder.InitialBalance = request.InitialBalance;
        shareholder.Description = request.Description?.Trim();
        shareholder.ProfitShare = request.ProfitShare;
        shareholder.LossShare = request.LossShare;
        shareholder.IsActive = request.IsActive;
        shareholder.UpdatedAt = DateTime.Now;
        shareholder.IsUpdated = true;
        shareholder.UpdatedBy = ResolveCurrentUserId();

        var fullName = $"{shareholder.FirstName} {shareholder.LastName}".Trim();
        var account = await _accounts.EnsureShareholderAccountAsync(
            shareholder.ShareholderID,
            fullName,
            cancellationToken);
        shareholder.AccountId = account.AccountID;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "سهام‌دار با موفقیت ویرایش شد." });
    }

    // ثبت صریح مانده اولیه اگر هنوز سند Opening ندارد
    [HttpPost("{id:int}/opening-balance")]
    [HasPermission("people.shareholders.edit")]
    public async Task<IActionResult> PostOpeningBalance(int id, CancellationToken cancellationToken)
    {
        var shareholder = await _db.Shareholders
            .FirstOrDefaultAsync(s => s.ShareholderID == id && s.IsDeleted != true, cancellationToken);

        if (shareholder is null)
        {
            return NotFound(new { message = "سهام‌دار یافت نشد." });
        }

        if (shareholder.InitialBalance <= 0)
        {
            return BadRequest(new { message = "مانده اولیه باید بزرگ‌تر از صفر باشد." });
        }

        var exists = await _db.ShareholderEquityTxns.AnyAsync(
            t => t.ShareholderId == id
                 && t.IsDeleted != true
                 && t.TxnType == ShareholderEquityTxnType.OpeningBalance,
            cancellationToken);
        if (exists)
        {
            return BadRequest(new { message = "مانده اولیه این سهامدار قبلاً ثبت شده است." });
        }

        var txn = await PostOpeningBalanceAsync(shareholder, cancellationToken);
        return Ok(new
        {
            message = "مانده اولیه سرمایه ثبت شد.",
            shareholderEquityTxnId = txn.ShareholderEquityTxnID,
            journalEntryId = txn.JournalEntryId,
        });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("people.shareholders.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var shareholder = await _db.Shareholders
            .FirstOrDefaultAsync(s => s.ShareholderID == id && s.IsDeleted != true, cancellationToken);

        if (shareholder is null)
        {
            return NotFound(new { message = "سهام‌دار یافت نشد." });
        }

        shareholder.IsDeleted = true;
        shareholder.IsActive = false;
        shareholder.DeletedAt = DateTime.Now;
        shareholder.DeletedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "سهام‌دار با موفقیت حذف شد." });
    }

    private async Task<ShareholderEquityTxn> PostOpeningBalanceAsync(
        Shareholder shareholder,
        CancellationToken cancellationToken)
    {
        var baseCurrency = await _currency.GetBaseCurrencyAsync(cancellationToken);
        var amount = shareholder.InitialBalance;
        var txn = new ShareholderEquityTxn
        {
            TxnType = ShareholderEquityTxnType.OpeningBalance,
            ShareholderId = shareholder.ShareholderID,
            TxnDate = DateTime.Today,
            CurrencyId = baseCurrency.CurrencyID,
            BaseCurrencyId = baseCurrency.CurrencyID,
            BaseUnitsPerUnitAtTransaction = 1,
            Amount = amount,
            AmountInBaseCurrency = amount,
            SettlementMode = EquitySettlementMode.Cash,
            Description = $"مانده اولیه سرمایه — {shareholder.FirstName} {shareholder.LastName}".Trim(),
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };

        _db.ShareholderEquityTxns.Add(txn);
        await _db.SaveChangesAsync(cancellationToken);

        var journal = await _equity.PostTxnAsync(txn, ResolveCurrentUserId(), cancellationToken);
        txn.JournalEntryId = journal.JournalEntryID;
        await _db.SaveChangesAsync(cancellationToken);
        return txn;
    }

    private static IQueryable<Shareholder> ApplyOrdering(
        IQueryable<Shareholder> query,
        List<DataTableOrder>? orders)
    {
        if (orders is null || orders.Count == 0)
        {
            return query.OrderByDescending(s => s.CreatedAt);
        }

        IOrderedQueryable<Shareholder>? ordered = null;
        foreach (var order in orders)
        {
            if (!OrderColumns.TryGetValue(order.Column, out var column))
            {
                continue;
            }

            var descending = string.Equals(order.Dir, "desc", StringComparison.OrdinalIgnoreCase);

            ordered = column switch
            {
                "FullName" when ordered is null => descending
                    ? query.OrderByDescending(s => s.LastName).ThenByDescending(s => s.FirstName)
                    : query.OrderBy(s => s.LastName).ThenBy(s => s.FirstName),
                "FullName" => descending
                    ? ordered!.ThenByDescending(s => s.LastName).ThenByDescending(s => s.FirstName)
                    : ordered!.ThenBy(s => s.LastName).ThenBy(s => s.FirstName),
                nameof(Shareholder.ProfitShare) when ordered is null => descending
                    ? query.OrderByDescending(s => s.ProfitShare)
                    : query.OrderBy(s => s.ProfitShare),
                nameof(Shareholder.ProfitShare) => descending
                    ? ordered!.ThenByDescending(s => s.ProfitShare)
                    : ordered!.ThenBy(s => s.ProfitShare),
                nameof(Shareholder.LossShare) when ordered is null => descending
                    ? query.OrderByDescending(s => s.LossShare)
                    : query.OrderBy(s => s.LossShare),
                nameof(Shareholder.LossShare) => descending
                    ? ordered!.ThenByDescending(s => s.LossShare)
                    : ordered!.ThenBy(s => s.LossShare),
                nameof(Shareholder.InitialBalance) when ordered is null => descending
                    ? query.OrderByDescending(s => s.InitialBalance)
                    : query.OrderBy(s => s.InitialBalance),
                nameof(Shareholder.InitialBalance) => descending
                    ? ordered!.ThenByDescending(s => s.InitialBalance)
                    : ordered!.ThenBy(s => s.InitialBalance),
                nameof(Shareholder.IsActive) when ordered is null => descending
                    ? query.OrderByDescending(s => s.IsActive)
                    : query.OrderBy(s => s.IsActive),
                nameof(Shareholder.IsActive) => descending
                    ? ordered!.ThenByDescending(s => s.IsActive)
                    : ordered!.ThenBy(s => s.IsActive),
                _ => ordered,
            };
        }

        return ordered ?? query.OrderByDescending(s => s.CreatedAt);
    }

    private int? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public DataTableSearch? Search { get; set; }
        public List<DataTableOrder>? Order { get; set; }
    }

    public class DataTableSearch
    {
        public string? Value { get; set; }
        public bool Regex { get; set; }
    }

    public class DataTableOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; } = "asc";
    }

    public class ShareholderTableRow
    {
        public int RowNumber { get; set; }
        public int ShareholderId { get; set; }
        public PersonTitle Title { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public decimal InitialBalance { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal ProfitShare { get; set; }
        public decimal LossShare { get; set; }
        public bool IsActive { get; set; }
        public int? AccountId { get; set; }
        public string? AccountCode { get; set; }
        public bool HasOpeningBalance { get; set; }
    }

    public class SaveShareholderRequest
    {
        public PersonTitle Title { get; set; } = PersonTitle.Mr;

        [Required(ErrorMessage = "نام الزامی است.")]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "نام خانوادگی الزامی است.")]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public decimal InitialBalance { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public decimal ProfitShare { get; set; }

        public decimal LossShare { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
