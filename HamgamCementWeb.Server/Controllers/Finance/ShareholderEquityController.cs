using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/equity-txns")]
[Authorize]
public class ShareholderEquityController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IShareholderEquityPostingService _equity;
    private readonly ICurrencyConversionService _currency;
    private readonly ICashBoxService _cashBoxes;
    private readonly IJournalPostingService _journal;

    public ShareholderEquityController(
        AppDbContext db,
        IShareholderEquityPostingService equity,
        ICurrencyConversionService currency,
        ICashBoxService cashBoxes,
        IJournalPostingService journal)
    {
        _db = db;
        _equity = equity;
        _currency = currency;
        _cashBoxes = cashBoxes;
        _journal = journal;
    }

    [HttpGet("cash-box-options")]
    [HasPermission("accounting.equity.view")]
    public async Task<IActionResult> CashBoxOptions(CancellationToken cancellationToken)
    {
        var items = await _db.CashBoxes
            .AsNoTracking()
            .Where(c => c.IsDeleted != true && c.IsActive == true)
            .OrderBy(c => c.Code)
            .Select(c => new { value = c.CashBoxID, label = c.Code + " — " + c.Name })
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("datatable")]
    [HasPermission("accounting.equity.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var draw = request.Draw;
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var baseQuery = _db.ShareholderEquityTxns
            .AsNoTracking()
            .Where(t => t.IsDeleted != true);

        var recordsTotal = await baseQuery.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            baseQuery = baseQuery.Where(t =>
                (t.Description != null && t.Description.Contains(searchValue))
                || _db.Shareholders.Any(s =>
                    s.ShareholderID == t.ShareholderId
                    && (s.FirstName.Contains(searchValue) || s.LastName.Contains(searchValue))));
        }

        var recordsFiltered = await baseQuery.CountAsync(cancellationToken);

        var orderCol = request.Order?.FirstOrDefault()?.Column ?? 1;
        var descending = string.Equals(
            request.Order?.FirstOrDefault()?.Dir,
            "desc",
            StringComparison.OrdinalIgnoreCase);

        IQueryable<ShareholderEquityTxn> ordered = orderCol switch
        {
            3 when descending => baseQuery.OrderByDescending(t => t.TxnType),
            3 => baseQuery.OrderBy(t => t.TxnType),
            4 when descending => baseQuery.OrderByDescending(t => t.Amount),
            4 => baseQuery.OrderBy(t => t.Amount),
            5 when descending => baseQuery.OrderByDescending(t => t.SettlementMode),
            5 => baseQuery.OrderBy(t => t.SettlementMode),
            _ when descending => baseQuery.OrderByDescending(t => t.TxnDate),
            _ => baseQuery.OrderBy(t => t.TxnDate),
        };

        var page = await ordered
            .Skip(start)
            .Take(length)
            .Select(t => new
            {
                t.ShareholderEquityTxnID,
                t.TxnDate,
                t.TxnType,
                t.ShareholderId,
                t.Amount,
                t.AmountInBaseCurrency,
                t.SettlementMode,
                t.CashBoxId,
                t.Description,
                t.JournalEntryId,
                t.CurrencyId,
                ShareholderName = _db.Shareholders
                    .Where(s => s.ShareholderID == t.ShareholderId)
                    .Select(s => (s.FirstName + " " + s.LastName).Trim())
                    .FirstOrDefault() ?? string.Empty,
            })
            .ToListAsync(cancellationToken);

        var data = page.Select((r, i) => new
        {
            rowNumber = start + i + 1,
            shareholderEquityTxnId = r.ShareholderEquityTxnID,
            txnDate = r.TxnDate,
            txnType = (int)r.TxnType,
            txnTypeLabel = r.TxnType switch
            {
                ShareholderEquityTxnType.CapitalContribution => "آورده سرمایه",
                ShareholderEquityTxnType.CapitalWithdrawal => "برداشت سرمایه",
                ShareholderEquityTxnType.ProfitDistribution => "توزیع سود",
                _ => "مانده اولیه",
            },
            shareholderId = r.ShareholderId,
            shareholderName = r.ShareholderName,
            amount = r.Amount,
            amountInBaseCurrency = r.AmountInBaseCurrency,
            settlementMode = (int)r.SettlementMode,
            settlementModeLabel = r.SettlementMode == EquitySettlementMode.Payable ? "پرداختنی" : "نقدی",
            cashBoxId = r.CashBoxId,
            description = r.Description,
            journalEntryId = r.JournalEntryId,
            currencyId = r.CurrencyId,
        });

        return Ok(new { draw, recordsTotal, recordsFiltered, data });
    }

    [HttpPost]
    [HasPermission("accounting.equity.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveEquityTxnRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "مبلغ باید بزرگ‌تر از صفر باشد." });
        }

        if (request.TxnType is ShareholderEquityTxnType.OpeningBalance)
        {
            return BadRequest(new { message = "مانده اولیه از صفحه سهامداران ثبت می‌شود." });
        }

        var shareholderExists = await _db.Shareholders
            .AnyAsync(s => s.ShareholderID == request.ShareholderId && s.IsDeleted != true, cancellationToken);
        if (!shareholderExists)
        {
            return BadRequest(new { message = "سهام‌دار یافت نشد." });
        }

        var userId = ResolveCurrentUserId();
        var txnDate = request.TxnDate?.Date ?? DateTime.Today;
        var snapshot = await _currency.GetSnapshotAsync(request.CurrencyId, txnDate, cancellationToken);
        var amountInBase = _currency.ConvertToBase(request.Amount, snapshot);

        var settlement = request.SettlementMode ?? EquitySettlementMode.Cash;
        int? cashBoxId = request.CashBoxId;
        if (request.TxnType is ShareholderEquityTxnType.CapitalContribution
            or ShareholderEquityTxnType.CapitalWithdrawal
            || (request.TxnType == ShareholderEquityTxnType.ProfitDistribution
                && settlement == EquitySettlementMode.Cash))
        {
            cashBoxId ??= await _cashBoxes.ResolveUserCashBoxIdAsync(userId, cancellationToken);
            if (cashBoxId is null)
            {
                return BadRequest(new { message = "صندوق برای تسویه نقدی مشخص نشده است." });
            }
        }
        else
        {
            cashBoxId = null;
        }

        var txn = new ShareholderEquityTxn
        {
            TxnType = request.TxnType,
            ShareholderId = request.ShareholderId,
            TxnDate = txnDate,
            CurrencyId = snapshot.CurrencyId,
            BaseCurrencyId = snapshot.BaseCurrencyId,
            ExchangeHistoryId = snapshot.ExchangeHistoryId,
            BaseUnitsPerUnitAtTransaction = snapshot.BaseUnitsPerUnit,
            Amount = request.Amount,
            AmountInBaseCurrency = amountInBase,
            CashBoxId = cashBoxId,
            SettlementMode = settlement,
            Description = request.Description?.Trim(),
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = userId,
        };

        _db.ShareholderEquityTxns.Add(txn);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var journal = await _equity.PostTxnAsync(txn, userId, cancellationToken);
            txn.JournalEntryId = journal.JournalEntryID;
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                message = "سند سرمایه با موفقیت ثبت شد.",
                shareholderEquityTxnId = txn.ShareholderEquityTxnID,
                journalEntryId = journal.JournalEntryID,
            });
        }
        catch
        {
            txn.IsDeleted = true;
            txn.IsActive = false;
            txn.DeletedAt = DateTime.Now;
            txn.DeletedBy = userId;
            await _db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    [HttpDelete("{id:int}")]
    [HasPermission("accounting.equity.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var txn = await _db.ShareholderEquityTxns
            .FirstOrDefaultAsync(t => t.ShareholderEquityTxnID == id && t.IsDeleted != true, cancellationToken);

        if (txn is null)
        {
            return NotFound(new { message = "سند یافت نشد." });
        }

        var userId = ResolveCurrentUserId();
        var source = txn.TxnType switch
        {
            ShareholderEquityTxnType.CapitalContribution => JournalSource.EquityCapitalContribution,
            ShareholderEquityTxnType.CapitalWithdrawal => JournalSource.EquityCapitalWithdrawal,
            ShareholderEquityTxnType.ProfitDistribution => JournalSource.EquityProfitDistribution,
            ShareholderEquityTxnType.OpeningBalance => JournalSource.EquityOpeningBalance,
            _ => JournalSource.Manual,
        };

        await _journal.ReverseBySourceAsync(source, txn.ShareholderEquityTxnID, userId, cancellationToken: cancellationToken);

        txn.IsDeleted = true;
        txn.IsActive = false;
        txn.DeletedAt = DateTime.Now;
        txn.DeletedBy = userId;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "سند سرمایه حذف شد." });
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

    public class SaveEquityTxnRequest
    {
        public ShareholderEquityTxnType TxnType { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "سهام‌دار الزامی است.")]
        public int ShareholderId { get; set; }

        public DateTime? TxnDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ارز الزامی است.")]
        public int CurrencyId { get; set; }

        [Range(0.0001, double.MaxValue, ErrorMessage = "مبلغ نامعتبر است.")]
        public decimal Amount { get; set; }

        public int? CashBoxId { get; set; }

        public EquitySettlementMode? SettlementMode { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }
    }
}
