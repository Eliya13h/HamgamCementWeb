using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using HamgamTransport.Server.Data.Models.People;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Services;

public sealed record PartnerDistributableResult(
    int ShareholderId,
    DateTime AsOf,
    int SolarYear,
    DateTime FiscalYearStart,
    decimal ProfitSharePercent,
    decimal YtdNetIncomeInBase,
    decimal PartnerShareOfYtdInBase,
    decimal PriorDistributionsInBase,
    decimal AvailableInBase);

public interface IShareholderEquityPostingService
{
    Task<JournalEntry> PostTxnAsync(ShareholderEquityTxn txn, int? userId, CancellationToken cancellationToken = default);
    Task<JournalEntry?> PostYearAllocationAsync(
        FiscalYear year,
        decimal netIncomeInBase,
        int baseCurrencyId,
        int? userId,
        CancellationToken cancellationToken = default);
    Task ValidateSharePercentagesAsync(CancellationToken cancellationToken = default);

    // سهم سود قابل‌برداشت سهام‌دار تا تاریخ — برای UI و تفکیک خودکار
    Task<PartnerDistributableResult> GetDistributableAsync(
        int shareholderId,
        DateTime asOf,
        int? excludeTxnId = null,
        CancellationToken cancellationToken = default);
}

public class ShareholderEquityPostingService : IShareholderEquityPostingService
{
    private readonly AppDbContext _db;
    private readonly IJournalPostingService _journal;
    private readonly IAccountLookupService _accounts;
    private readonly ICashBalanceService _cashBalances;
    private readonly IFinanceStatementService _statements;

    public ShareholderEquityPostingService(
        AppDbContext db,
        IJournalPostingService journal,
        IAccountLookupService accounts,
        ICashBalanceService cashBalances,
        IFinanceStatementService statements)
    {
        _db = db;
        _journal = journal;
        _accounts = accounts;
        _cashBalances = cashBalances;
        _statements = statements;
    }

    public async Task ValidateSharePercentagesAsync(CancellationToken cancellationToken = default)
    {
        var shareholders = await _db.Shareholders
            .AsNoTracking()
            .Where(s => s.IsDeleted != true && s.IsActive == true)
            .Select(s => new { s.ProfitShare, s.LossShare })
            .ToListAsync(cancellationToken);

        if (shareholders.Count == 0)
        {
            throw new InvalidOperationException("هیچ سهامدار فعالی برای تخصیص سود/زیان تعریف نشده است.");
        }

        var profitSum = shareholders.Sum(s => s.ProfitShare);
        var lossSum = shareholders.Sum(s => s.LossShare);
        if (Math.Abs(profitSum - 100m) > 0.01m)
        {
            throw new InvalidOperationException(
                $"مجموع سهم سود سهامداران فعال باید ۱۰۰ باشد (الان {profitSum:N2}).");
        }

        if (Math.Abs(lossSum - 100m) > 0.01m)
        {
            throw new InvalidOperationException(
                $"مجموع سهم زیان سهامداران فعال باید ۱۰۰ باشد (الان {lossSum:N2}).");
        }
    }

    public async Task<PartnerDistributableResult> GetDistributableAsync(
        int shareholderId,
        DateTime asOf,
        int? excludeTxnId = null,
        CancellationToken cancellationToken = default)
    {
        var shareholder = await _db.Shareholders
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ShareholderID == shareholderId && s.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("سهام‌دار یافت نشد.");

        var asOfDate = asOf.Date;
        var solarYear = JalaliDateHelper.GetSolarYear(asOfDate);
        var fiscalYear = await _db.FiscalYears
            .AsNoTracking()
            .FirstOrDefaultAsync(y => y.SolarYear == solarYear && y.IsDeleted != true, cancellationToken);

        var (yearStart, yearEnd) = fiscalYear is not null
            ? (fiscalYear.StartDate.Date, fiscalYear.EndDate.Date)
            : JalaliDateHelper.GetSolarYearRange(solarYear);

        if (asOfDate < yearStart)
        {
            asOfDate = yearStart;
        }

        var rangeEnd = asOfDate > yearEnd ? yearEnd : asOfDate;
        var ytdNetIncome = await _statements.GetNetIncomeInBaseAsync(yearStart, rangeEnd, cancellationToken);
        var partnerShare = Math.Round(ytdNetIncome * shareholder.ProfitShare / 100m, 4, MidpointRounding.AwayFromZero);

        var priorRows = await _db.ShareholderEquityTxns
            .AsNoTracking()
            .Where(t =>
                t.IsDeleted != true
                && t.ShareholderId == shareholderId
                && t.TxnType == ShareholderEquityTxnType.ProfitDistribution
                && t.TxnDate >= yearStart
                && t.TxnDate <= rangeEnd.AddDays(1).AddTicks(-1)
                && (excludeTxnId == null || t.ShareholderEquityTxnID != excludeTxnId.Value))
            .Select(t => new { t.AmountInBaseCurrency, t.ProfitPortionInBase, t.CapitalPortionInBase })
            .ToListAsync(cancellationToken);

        // اسناد قدیمی بدون تفکیک: کل مبلغ توزیع سود محسوب می‌شود
        var priorDistributions = priorRows.Sum(t =>
            t.ProfitPortionInBase + t.CapitalPortionInBase >= 0.01m
                ? t.ProfitPortionInBase
                : t.AmountInBaseCurrency);

        var available = Math.Max(0m, partnerShare - priorDistributions);

        return new PartnerDistributableResult(
            shareholderId,
            asOfDate,
            solarYear,
            yearStart,
            shareholder.ProfitShare,
            ytdNetIncome,
            partnerShare,
            priorDistributions,
            available);
    }

    public async Task<JournalEntry> PostTxnAsync(
        ShareholderEquityTxn txn,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var shareholder = await _db.Shareholders
            .FirstOrDefaultAsync(s => s.ShareholderID == txn.ShareholderId && s.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("سهام‌دار یافت نشد.");

        var capitalAccount = await EnsureShareholderCapitalAsync(shareholder, cancellationToken);
        var amount = txn.Amount;
        var amountBase = txn.AmountInBaseCurrency;
        var desc = string.IsNullOrWhiteSpace(txn.Description)
            ? TxnTypeLabel(txn.TxnType)
            : txn.Description.Trim();

        if (txn.TxnType == ShareholderEquityTxnType.ProfitDistribution)
        {
            var split = await ResolveDistributionSplitAsync(txn, amount, amountBase, cancellationToken);
            txn.ProfitPortionInBase = split.ProfitPortionInBase;
            txn.CapitalPortionInBase = split.CapitalPortionInBase;
            desc = AppendSplitNote(desc, split.ProfitPortionInBase, split.CapitalPortionInBase);
            txn.Description = desc;
        }
        else
        {
            txn.ProfitPortionInBase = 0;
            txn.CapitalPortionInBase = 0;
        }

        List<JournalLineDraft> lines = txn.TxnType switch
        {
            ShareholderEquityTxnType.CapitalContribution => await BuildContributionLinesAsync(
                txn, capitalAccount.AccountID, amount, amountBase, desc, cancellationToken),
            ShareholderEquityTxnType.CapitalWithdrawal => await BuildWithdrawalLinesAsync(
                txn, capitalAccount.AccountID, amount, amountBase, desc, cancellationToken),
            ShareholderEquityTxnType.ProfitDistribution => await BuildDistributionLinesAsync(
                txn, capitalAccount.AccountID, amount, amountBase, desc, cancellationToken),
            ShareholderEquityTxnType.OpeningBalance => await BuildOpeningLinesAsync(
                capitalAccount.AccountID, amount, amountBase, txn.CurrencyId, desc, cancellationToken),
            _ => throw new InvalidOperationException("نوع سند سرمایه نامعتبر است."),
        };

        var source = txn.TxnType switch
        {
            ShareholderEquityTxnType.CapitalContribution => JournalSource.EquityCapitalContribution,
            ShareholderEquityTxnType.CapitalWithdrawal => JournalSource.EquityCapitalWithdrawal,
            ShareholderEquityTxnType.ProfitDistribution => JournalSource.EquityProfitDistribution,
            ShareholderEquityTxnType.OpeningBalance => JournalSource.EquityOpeningBalance,
            _ => JournalSource.Manual,
        };

        return await _journal.PostAsync(
            txn.TxnDate,
            desc,
            source,
            txn.ShareholderEquityTxnID,
            txn.BaseCurrencyId,
            lines,
            userId,
            cancellationToken);
    }

    public async Task<JournalEntry?> PostYearAllocationAsync(
        FiscalYear year,
        decimal netIncomeInBase,
        int baseCurrencyId,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        if (Math.Abs(netIncomeInBase) < 0.01m)
        {
            return null;
        }

        await ValidateSharePercentagesAsync(cancellationToken);

        var shareholders = await _db.Shareholders
            .Where(s => s.IsDeleted != true && s.IsActive == true)
            .OrderBy(s => s.ShareholderID)
            .ToListAsync(cancellationToken);

        var retained = await _accounts.ResolveRetainedEarningsPostableAsync(cancellationToken);
        var isProfit = netIncomeInBase > 0;
        var total = Math.Abs(netIncomeInBase);
        var drafts = new List<JournalLineDraft>();
        decimal allocated = 0;

        for (var i = 0; i < shareholders.Count; i++)
        {
            var sh = shareholders[i];
            var pct = isProfit ? sh.ProfitShare : sh.LossShare;
            var shareAmount = i == shareholders.Count - 1
                ? total - allocated
                : Math.Round(total * pct / 100m, 4, MidpointRounding.AwayFromZero);
            allocated += shareAmount;

            if (shareAmount < 0.01m)
            {
                continue;
            }

            var capital = await EnsureShareholderCapitalAsync(sh, cancellationToken);
            var name = $"{sh.FirstName} {sh.LastName}".Trim();

            if (isProfit)
            {
                // Dr RE / Cr Capital
                drafts.Add(new JournalLineDraft(
                    capital.AccountID,
                    0,
                    shareAmount,
                    0,
                    shareAmount,
                    baseCurrencyId,
                    $"تخصیص سود سال {year.SolarYear} — {name}"));
            }
            else
            {
                // Dr Capital / Cr RE
                drafts.Add(new JournalLineDraft(
                    capital.AccountID,
                    shareAmount,
                    0,
                    shareAmount,
                    0,
                    baseCurrencyId,
                    $"تخصیص زیان سال {year.SolarYear} — {name}"));
            }
        }

        if (drafts.Count == 0)
        {
            return null;
        }

        var capitalSide = drafts.Sum(d => isProfit ? d.CreditInBaseCurrency : d.DebitInBaseCurrency);
        if (isProfit)
        {
            drafts.Insert(0, new JournalLineDraft(
                retained.AccountID,
                capitalSide,
                0,
                capitalSide,
                0,
                baseCurrencyId,
                $"تخصیص سود سال {year.SolarYear} به سرمایه سهامداران"));
        }
        else
        {
            drafts.Insert(0, new JournalLineDraft(
                retained.AccountID,
                0,
                capitalSide,
                0,
                capitalSide,
                baseCurrencyId,
                $"تخصیص زیان سال {year.SolarYear} از سرمایه سهامداران"));
        }

        return await _journal.PostAsync(
            year.EndDate.Date,
            $"تخصیص {(isProfit ? "سود" : "زیان")} سال مالی {year.SolarYear} به سهامداران",
            JournalSource.EquityYearAllocation,
            year.FiscalYearID,
            baseCurrencyId,
            drafts,
            userId,
            cancellationToken);
    }

    private async Task<(decimal ProfitPortionInBase, decimal CapitalPortionInBase)> ResolveDistributionSplitAsync(
        ShareholderEquityTxn txn,
        decimal amount,
        decimal amountBase,
        CancellationToken cancellationToken)
    {
        var distributable = await GetDistributableAsync(
            txn.ShareholderId,
            txn.TxnDate,
            excludeTxnId: txn.ShareholderEquityTxnID,
            cancellationToken);

        var profitBase = Math.Min(amountBase, distributable.AvailableInBase);
        profitBase = Math.Round(profitBase, 4, MidpointRounding.AwayFromZero);
        if (profitBase < 0.01m)
        {
            profitBase = 0;
        }

        if (profitBase > amountBase)
        {
            profitBase = amountBase;
        }

        var capitalBase = Math.Round(amountBase - profitBase, 4, MidpointRounding.AwayFromZero);
        if (capitalBase < 0)
        {
            capitalBase = 0;
            profitBase = amountBase;
        }

        _ = amount; // مبلغ ارزی در خطوط ژورنال با نسبت پایه محاسبه می‌شود
        return (profitBase, capitalBase);
    }

    private async Task<List<JournalLineDraft>> BuildContributionLinesAsync(
        ShareholderEquityTxn txn,
        int capitalAccountId,
        decimal amount,
        decimal amountBase,
        string desc,
        CancellationToken cancellationToken)
    {
        var cashAccountId = await ResolveCashAccountIdAsync(txn.CashBoxId, cancellationToken);
        return
        [
            new(cashAccountId, amount, 0, amountBase, 0, txn.CurrencyId, desc, CashBoxId: txn.CashBoxId),
            new(capitalAccountId, 0, amount, 0, amountBase, txn.CurrencyId, desc),
        ];
    }

    private async Task<List<JournalLineDraft>> BuildWithdrawalLinesAsync(
        ShareholderEquityTxn txn,
        int capitalAccountId,
        decimal amount,
        decimal amountBase,
        string desc,
        CancellationToken cancellationToken)
    {
        if (txn.CashBoxId is int cashBoxId)
        {
            await _cashBalances.EnsureSufficientBalanceAsync(cashBoxId, txn.CurrencyId, amount, cancellationToken);
        }

        var cashAccountId = await ResolveCashAccountIdAsync(txn.CashBoxId, cancellationToken);
        return
        [
            new(capitalAccountId, amount, 0, amountBase, 0, txn.CurrencyId, desc),
            new(cashAccountId, 0, amount, 0, amountBase, txn.CurrencyId, desc, CashBoxId: txn.CashBoxId),
        ];
    }

    private async Task<List<JournalLineDraft>> BuildDistributionLinesAsync(
        ShareholderEquityTxn txn,
        int capitalAccountId,
        decimal amount,
        decimal amountBase,
        string desc,
        CancellationToken cancellationToken)
    {
        var profitBase = txn.ProfitPortionInBase;
        var capitalBase = txn.CapitalPortionInBase;

        // اگر به‌هر دلیل هنوز ست نشده، کل را سود فرض کن
        if (profitBase + capitalBase < 0.01m && amountBase >= 0.01m)
        {
            profitBase = amountBase;
            capitalBase = 0;
        }

        var (profitAmount, capitalAmount) = SplitTxnCurrencyAmounts(amount, amountBase, profitBase, capitalBase);

        if (txn.SettlementMode == EquitySettlementMode.Cash && txn.CashBoxId is int cashBoxId)
        {
            await _cashBalances.EnsureSufficientBalanceAsync(cashBoxId, txn.CurrencyId, amount, cancellationToken);
        }

        var creditAccountId = txn.SettlementMode == EquitySettlementMode.Payable
            ? (await _accounts.GetBySystemCodeAsync(AccountSystemCode.DividendPayable, cancellationToken)).AccountID
            : await ResolveCashAccountIdAsync(txn.CashBoxId, cancellationToken);

        var retained = await _accounts.ResolveRetainedEarningsPostableAsync(cancellationToken);
        var lines = new List<JournalLineDraft>();

        if (profitBase >= 0.01m)
        {
            lines.Add(new(
                retained.AccountID,
                profitAmount,
                0,
                profitBase,
                0,
                txn.CurrencyId,
                desc));
        }

        if (capitalBase >= 0.01m)
        {
            lines.Add(new(
                capitalAccountId,
                capitalAmount,
                0,
                capitalBase,
                0,
                txn.CurrencyId,
                desc));
        }

        lines.Add(new(
            creditAccountId,
            0,
            amount,
            0,
            amountBase,
            txn.CurrencyId,
            desc,
            CashBoxId: txn.SettlementMode == EquitySettlementMode.Cash ? txn.CashBoxId : null));

        return lines;
    }

    private async Task<List<JournalLineDraft>> BuildOpeningLinesAsync(
        int capitalAccountId,
        decimal amount,
        decimal amountBase,
        int currencyId,
        string desc,
        CancellationToken cancellationToken)
    {
        var opening = await _accounts.GetBySystemCodeAsync(AccountSystemCode.EquityOpening, cancellationToken);
        return
        [
            new(opening.AccountID, amount, 0, amountBase, 0, currencyId, desc),
            new(capitalAccountId, 0, amount, 0, amountBase, currencyId, desc),
        ];
    }

    private async Task<Account> EnsureShareholderCapitalAsync(
        Shareholder shareholder,
        CancellationToken cancellationToken)
    {
        var name = $"{shareholder.FirstName} {shareholder.LastName}".Trim();
        var account = await _accounts.EnsureShareholderAccountAsync(
            shareholder.ShareholderID,
            name,
            cancellationToken);

        if (shareholder.AccountId != account.AccountID)
        {
            shareholder.AccountId = account.AccountID;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return account;
    }

    private async Task<int> ResolveCashAccountIdAsync(int? cashBoxId, CancellationToken cancellationToken)
    {
        if (cashBoxId is int id)
        {
            var box = await _db.CashBoxes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CashBoxID == id && c.IsDeleted != true, cancellationToken);
            if (box is not null)
            {
                return box.AccountId;
            }
        }

        throw new InvalidOperationException("صندوق برای تسویه نقدی مشخص نشده یا یافت نشد.");
    }

    private static (decimal ProfitAmount, decimal CapitalAmount) SplitTxnCurrencyAmounts(
        decimal amount,
        decimal amountBase,
        decimal profitBase,
        decimal capitalBase)
    {
        if (amountBase < 0.01m)
        {
            return (0, 0);
        }

        var profitAmount = Math.Round(amount * profitBase / amountBase, 4, MidpointRounding.AwayFromZero);
        if (profitAmount > amount)
        {
            profitAmount = amount;
        }

        var capitalAmount = Math.Round(amount - profitAmount, 4, MidpointRounding.AwayFromZero);
        if (capitalBase < 0.01m)
        {
            capitalAmount = 0;
            profitAmount = amount;
        }
        else if (profitBase < 0.01m)
        {
            profitAmount = 0;
            capitalAmount = amount;
        }

        return (profitAmount, capitalAmount);
    }

    private static string AppendSplitNote(string desc, decimal profitBase, decimal capitalBase)
    {
        if (capitalBase < 0.01m)
        {
            return desc;
        }

        var note = profitBase >= 0.01m
            ? $"تفکیک خودکار: توزیع سود {profitBase:N2} + برداشت سرمایه {capitalBase:N2} (پایه)"
            : $"تفکیک خودکار: کل مبلغ از سرمایه ({capitalBase:N2} پایه)";

        if (desc.Contains("تفکیک خودکار", StringComparison.Ordinal))
        {
            return desc;
        }

        return string.IsNullOrWhiteSpace(desc) ? note : $"{desc} — {note}";
    }

    private static string TxnTypeLabel(ShareholderEquityTxnType type) => type switch
    {
        ShareholderEquityTxnType.CapitalContribution => "آورده سرمایه",
        ShareholderEquityTxnType.CapitalWithdrawal => "برداشت سرمایه",
        ShareholderEquityTxnType.ProfitDistribution => "توزیع سود",
        ShareholderEquityTxnType.OpeningBalance => "مانده اولیه سرمایه",
        _ => "سند سرمایه",
    };
}
