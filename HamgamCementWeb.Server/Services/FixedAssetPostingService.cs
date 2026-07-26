using System.Globalization;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Services;

public interface IFixedAssetPostingService
{
    Task<JournalEntry> PostAcquisitionAsync(FixedAsset asset, int? userId, int? cashBoxId, CancellationToken cancellationToken = default);
    Task<JournalEntry> PostDepreciationAsync(FixedAssetDepreciation depreciation, FixedAsset asset, int? userId, CancellationToken cancellationToken = default);
    Task<JournalEntry> PostDisposalAsync(FixedAsset asset, int? userId, int? cashBoxId, CancellationToken cancellationToken = default);
    Task SoftDeleteAcquisitionAsync(int fixedAssetId, int? userId, CancellationToken cancellationToken = default);
    decimal CalculateMonthlyDepreciationInBase(FixedAsset asset);
}

public class FixedAssetPostingService : IFixedAssetPostingService
{
    private readonly AppDbContext _db;
    private readonly IJournalPostingService _journal;
    private readonly IAccountLookupService _accounts;
    private readonly ICashBalanceService _cashBalances;
    private readonly IOperationalGlService _gl;

    public FixedAssetPostingService(
        AppDbContext db,
        IJournalPostingService journal,
        IAccountLookupService accounts,
        ICashBalanceService cashBalances,
        IOperationalGlService gl)
    {
        _db = db;
        _journal = journal;
        _accounts = accounts;
        _cashBalances = cashBalances;
        _gl = gl;
    }

    public decimal CalculateMonthlyDepreciationInBase(FixedAsset asset)
    {
        if (asset.UsefulLifeMonths <= 0)
        {
            return 0;
        }

        var depreciable = asset.CostAmountInBaseCurrency - asset.SalvageValueInBaseCurrency;
        if (depreciable <= 0)
        {
            return 0;
        }

        return Math.Round(depreciable / asset.UsefulLifeMonths, 4, MidpointRounding.AwayFromZero);
    }

    public async Task<JournalEntry> PostAcquisitionAsync(
        FixedAsset asset,
        int? userId,
        int? cashBoxId,
        CancellationToken cancellationToken = default)
    {
        var category = await _db.FixedAssetCategories
            .FirstAsync(c => c.FixedAssetCategoryID == asset.FixedAssetCategoryId, cancellationToken);

        var assetAccountId = category.AssetAccountId
            ?? (await _accounts.GetBySystemCodeAsync(AccountSystemCode.FixedAssetMachinery, cancellationToken)).AccountID;

        var creditAccountId = await _gl.ResolveSettlementAccountIdAsync(cashBoxId, cancellationToken);
        if (asset.SupplierId is int supplierId)
        {
            var name = await _db.Suppliers
                .Where(s => s.SupplierID == supplierId)
                .Select(s => s.Name)
                .FirstAsync(cancellationToken);
            creditAccountId = (await _accounts.EnsureSupplierAccountAsync(supplierId, name, cancellationToken)).AccountID;
        }
        else if (cashBoxId is int boxId)
        {
            await _cashBalances.EnsureSufficientBalanceAsync(boxId, asset.CurrencyId, asset.CostAmount, cancellationToken);
        }

        var desc = $"خرید دارایی ثابت {asset.Code} — {asset.Name}";
        var lines = new List<JournalLineDraft>
        {
            new(assetAccountId, asset.CostAmount, 0, asset.CostAmountInBaseCurrency, 0, asset.CurrencyId, desc),
            new(creditAccountId, 0, asset.CostAmount, 0, asset.CostAmountInBaseCurrency, asset.CurrencyId, desc,
                CashBoxId: asset.SupplierId is null ? cashBoxId : null,
                PartyId: asset.SupplierId),
        };

        return await _journal.PostAsync(
            asset.AcquisitionDate,
            desc,
            JournalSource.FixedAssetAcquire,
            asset.FixedAssetID,
            asset.BaseCurrencyId,
            lines,
            userId,
            cancellationToken);
    }

    public async Task<JournalEntry> PostDepreciationAsync(
        FixedAssetDepreciation depreciation,
        FixedAsset asset,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var category = await _db.FixedAssetCategories
            .FirstAsync(c => c.FixedAssetCategoryID == asset.FixedAssetCategoryId, cancellationToken);

        var expenseAccountId = category.DepreciationExpenseAccountId
            ?? (await _accounts.GetBySystemCodeAsync(AccountSystemCode.DepreciationExpense, cancellationToken)).AccountID;
        var accumAccountId = category.AccumulatedDepreciationAccountId
            ?? (await _accounts.GetBySystemCodeAsync(AccountSystemCode.AccumulatedDepreciation, cancellationToken)).AccountID;

        var desc = $"استهلاک {asset.Code} — {depreciation.PeriodSolarYear}/{depreciation.PeriodMonth:00}";
        var amount = depreciation.AmountInBaseCurrency;
        var lines = new List<JournalLineDraft>
        {
            new(expenseAccountId, amount, 0, amount, 0, asset.BaseCurrencyId, desc),
            new(accumAccountId, 0, amount, 0, amount, asset.BaseCurrencyId, desc),
        };

        return await _journal.PostAsync(
            depreciation.DepreciationDate,
            desc,
            JournalSource.FixedAssetDepreciation,
            depreciation.FixedAssetDepreciationID,
            asset.BaseCurrencyId,
            lines,
            userId,
            cancellationToken);
    }

    public async Task<JournalEntry> PostDisposalAsync(
        FixedAsset asset,
        int? userId,
        int? cashBoxId,
        CancellationToken cancellationToken = default)
    {
        var category = await _db.FixedAssetCategories
            .FirstAsync(c => c.FixedAssetCategoryID == asset.FixedAssetCategoryId, cancellationToken);

        var assetAccountId = category.AssetAccountId
            ?? (await _accounts.GetBySystemCodeAsync(AccountSystemCode.FixedAssetMachinery, cancellationToken)).AccountID;
        var accumAccountId = category.AccumulatedDepreciationAccountId
            ?? (await _accounts.GetBySystemCodeAsync(AccountSystemCode.AccumulatedDepreciation, cancellationToken)).AccountID;

        var cost = asset.CostAmountInBaseCurrency;
        var accum = asset.AccumulatedDepreciationInBaseCurrency;
        var book = cost - accum;
        var proceeds = asset.DisposalAmountInBaseCurrency ?? 0m;
        var currencyId = asset.CurrencyId;
        var proceedsTxn = asset.DisposalAmount ?? 0m;

        var desc = $"فروش/اسقاط دارایی ثابت {asset.Code} — {asset.Name}";
        var lines = new List<JournalLineDraft>();

        // بستن استهلاک انباشته (فقط اگر مبلغ داشته باشد)
        if (accum > 0.01m)
        {
            lines.Add(new(accumAccountId, accum, 0, accum, 0, asset.BaseCurrencyId, desc));
        }

        // بستن بهای تمام‌شده دارایی
        lines.Add(new(assetAccountId, 0, cost, 0, cost, asset.BaseCurrencyId, desc));

        if (proceedsTxn > 0.01m)
        {
            var cashAccountId = await _gl.ResolveSettlementAccountIdAsync(cashBoxId, cancellationToken);
            lines.Add(new(
                cashAccountId,
                proceedsTxn,
                0,
                proceeds,
                0,
                currencyId,
                desc,
                CashBoxId: cashBoxId));
        }

        var gainLoss = Math.Round(proceeds - book, 4, MidpointRounding.AwayFromZero);
        if (gainLoss > 0.01m)
        {
            var gainAccountId = (await _accounts.GetBySystemCodeAsync(AccountSystemCode.FixedAssetDisposalGain, cancellationToken)).AccountID;
            lines.Add(new(gainAccountId, 0, gainLoss, 0, gainLoss, asset.BaseCurrencyId, desc));
        }
        else if (gainLoss < -0.01m)
        {
            var loss = Math.Abs(gainLoss);
            var lossAccountId = (await _accounts.GetBySystemCodeAsync(AccountSystemCode.FixedAssetDisposalLoss, cancellationToken)).AccountID;
            lines.Add(new(lossAccountId, loss, 0, loss, 0, asset.BaseCurrencyId, desc));
        }

        return await _journal.PostAsync(
            asset.DisposalDate ?? DateTime.Now.Date,
            desc,
            JournalSource.FixedAssetDispose,
            asset.FixedAssetID,
            asset.BaseCurrencyId,
            lines,
            userId,
            cancellationToken);
    }

    public Task SoftDeleteAcquisitionAsync(int fixedAssetId, int? userId, CancellationToken cancellationToken = default) =>
        _journal.SoftDeleteBySourceAsync(JournalSource.FixedAssetAcquire, fixedAssetId, userId, cancellationToken);

    public static (int Year, int Month) GetSolarPeriod(DateTime date)
    {
        var calendar = new PersianCalendar();
        return (calendar.GetYear(date), calendar.GetMonth(date));
    }
}
