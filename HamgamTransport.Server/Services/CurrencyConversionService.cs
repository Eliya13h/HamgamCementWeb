using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HamgamTransport.Server.Services;

public record CurrencySnapshot(
    int CurrencyId,
    int BaseCurrencyId,
    int? ExchangeHistoryId,
    decimal BaseUnitsPerUnit,
    bool IsBaseCurrency);

public interface ICurrencyConversionService
{
    Task<Currency> GetBaseCurrencyAsync(CancellationToken cancellationToken = default);
    Task<CurrencySnapshot> GetSnapshotAsync(int currencyId, DateTime date, CancellationToken cancellationToken = default);
    decimal ConvertToBase(decimal amount, CurrencySnapshot snapshot);
    decimal ConvertFromBase(decimal amountInBase, CurrencySnapshot snapshot);
}

public class CurrencyConversionService : ICurrencyConversionService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CurrencyConversionService> _logger;

    public CurrencyConversionService(AppDbContext db, ILogger<CurrencyConversionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Currency> GetBaseCurrencyAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Currencies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IsBaseCurrency && c.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("ارز پایه تعریف نشده است.");
    }

    public async Task<CurrencySnapshot> GetSnapshotAsync(
        int currencyId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var currency = await _db.Currencies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CurrencyID == currencyId && c.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("ارز یافت نشد.");

        var baseCurrency = await GetBaseCurrencyAsync(cancellationToken);

        if (currency.IsBaseCurrency)
        {
            return new CurrencySnapshot(
                currency.CurrencyID,
                baseCurrency.CurrencyID,
                null,
                1m,
                true);
        }

        var history = await _db.CurrencyExchangeHistories
            .AsNoTracking()
            .Where(h =>
                h.CurrencyID == currencyId &&
                h.IsDeleted != true &&
                h.EffectiveFrom <= date &&
                (h.EffectiveTo == null || h.EffectiveTo > date))
            .OrderByDescending(h => h.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        if (history is null)
        {
            var currentRate = await _db.CurrencyExchangeRates
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.CurrencyID == currencyId, cancellationToken);

            if (currentRate is null)
            {
                throw new InvalidOperationException($"نرخ ارز «{currency.Name}» در تاریخ {date:yyyy/MM/dd} یافت نشد.");
            }

            // چرا هشدار: تاریخچه نرخ برای این تاریخ وجود ندارد و به‌ناچار از نرخ جاری استفاده می‌شود؛ این fallback
            // می‌تواند ارزش‌گذاری معاملات گذشته را نادرست کند، پس برای ردیابی شفاف لاگ می‌شود (رفتار تغییر نمی‌کند).
            _logger.LogWarning(
                "نرخ تاریخی برای ارز {CurrencyName} (شناسه {CurrencyId}) در تاریخ {Date:yyyy-MM-dd} یافت نشد؛ از نرخ جاری ({Rate}) استفاده شد.",
                currency.Name,
                currency.CurrencyID,
                date,
                currentRate.BaseUnitsPerUnit);

            return new CurrencySnapshot(
                currency.CurrencyID,
                baseCurrency.CurrencyID,
                currentRate.SourceHistoryID,
                currentRate.BaseUnitsPerUnit,
                false);
        }

        return new CurrencySnapshot(
            currency.CurrencyID,
            baseCurrency.CurrencyID,
            history.HistoryID,
            history.BaseUnitsPerUnit,
            false);
    }

    public decimal ConvertToBase(decimal amount, CurrencySnapshot snapshot)
    {
        return snapshot.IsBaseCurrency ? amount : amount * snapshot.BaseUnitsPerUnit;
    }

    public decimal ConvertFromBase(decimal amountInBase, CurrencySnapshot snapshot)
    {
        if (snapshot.IsBaseCurrency)
        {
            return amountInBase;
        }

        if (snapshot.BaseUnitsPerUnit <= 0)
        {
            throw new InvalidOperationException("نرخ ارز نامعتبر است.");
        }

        return amountInBase / snapshot.BaseUnitsPerUnit;
    }
}
