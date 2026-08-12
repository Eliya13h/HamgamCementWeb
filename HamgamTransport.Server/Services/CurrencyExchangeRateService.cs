using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Services;

public interface ICurrencyExchangeRateService
{
    Task<int> ApplyRateChangeAsync(
        int currencyId,
        int baseCurrencyId,
        decimal newRate,
        string? changeReason,
        DateTime effectiveFrom,
        int? userId,
        CancellationToken cancellationToken = default);
}

public class CurrencyExchangeRateService : ICurrencyExchangeRateService
{
    private readonly AppDbContext _db;

    public CurrencyExchangeRateService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> ApplyRateChangeAsync(
        int currencyId,
        int baseCurrencyId,
        decimal newRate,
        string? changeReason,
        DateTime effectiveFrom,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var openHistories = await _db.CurrencyExchangeHistories
            .Where(h => h.CurrencyID == currencyId && h.EffectiveTo == null && h.IsDeleted != true)
            .OrderByDescending(h => h.EffectiveFrom)
            .ToListAsync(cancellationToken);

        var latestOpen = openHistories.FirstOrDefault();
        decimal? previousRate = latestOpen?.BaseUnitsPerUnit;

        // همه دوره‌های باز را می‌بندیم تا فقط نرخ جدید «جاری» بماند
        foreach (var openHistory in openHistories)
        {
            openHistory.EffectiveTo = effectiveFrom;
            openHistory.UpdatedAt = DateTime.Now;
            openHistory.IsUpdated = true;
            openHistory.UpdatedBy = userId;
        }

        var history = new CurrencyExchangeHistory
        {
            CurrencyID = currencyId,
            BaseCurrencyID = baseCurrencyId,
            BaseUnitsPerUnit = newRate,
            PreviousBaseUnitsPerUnit = previousRate,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = null,
            ChangeReason = changeReason?.Trim(),
            CreatedBy = userId,
            CreatedAt = DateTime.Now,
            IsActive = true,
            IsDeleted = false,
        };

        _db.CurrencyExchangeHistories.Add(history);
        await _db.SaveChangesAsync(cancellationToken);

        var currentRate = await _db.CurrencyExchangeRates
            .FirstOrDefaultAsync(r => r.CurrencyID == currencyId, cancellationToken);

        if (currentRate is null)
        {
            currentRate = new CurrencyExchangeRate
            {
                CurrencyID = currencyId,
                BaseCurrencyID = baseCurrencyId,
                CreatedBy = userId,
                CreatedAt = DateTime.Now,
                IsActive = true,
                IsDeleted = false,
            };
            _db.CurrencyExchangeRates.Add(currentRate);
        }

        currentRate.BaseCurrencyID = baseCurrencyId;
        currentRate.BaseUnitsPerUnit = newRate;
        currentRate.EffectiveFrom = effectiveFrom;
        currentRate.SourceHistoryID = history.HistoryID;
        currentRate.UpdatedAt = DateTime.Now;
        currentRate.IsUpdated = true;
        currentRate.UpdatedBy = userId;

        await _db.SaveChangesAsync(cancellationToken);

        return history.HistoryID;
    }
}
