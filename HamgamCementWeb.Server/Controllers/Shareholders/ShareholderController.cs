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
    private readonly AppDbContext _db;
    private readonly IAccountLookupService _accounts;
    private readonly IShareholderEquityPostingService _equity;
    private readonly ICurrencyConversionService _currency;
    private readonly IShareholderReadService _reads;

    public ShareholderController(
        AppDbContext db,
        IAccountLookupService accounts,
        IShareholderEquityPostingService equity,
        ICurrencyConversionService currency,
        IShareholderReadService reads)
    {
        _db = db;
        _accounts = accounts;
        _equity = equity;
        _currency = currency;
        _reads = reads;
    }

    [HttpGet("options")]
    [HasPermission("people.shareholders.view")]
    public async Task<IActionResult> Options(CancellationToken cancellationToken)
    {
        var rows = await _reads.ListActiveOptionsAsync(cancellationToken);
        return Ok(rows.Select(s => new
        {
            value = s.Value,
            label = s.Label,
            profitShare = s.ProfitShare,
            lossShare = s.LossShare,
            accountId = s.AccountId,
        }));
    }

    [HttpPost("datatable")]
    [HasPermission("people.shareholders.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reads.QueryDataTableAsync(
            new ShareholderDataTableQuery
            {
                Start = request.Start,
                Length = request.Length,
                Search = request.Search?.Value,
                Order = request.Order?
                    .Select(o => new DataTableOrderItem { Column = o.Column, Dir = o.Dir })
                    .ToList(),
            },
            cancellationToken);

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Rows.Select(r => new
            {
                r.RowNumber,
                r.ShareholderId,
                title = r.Title,
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
