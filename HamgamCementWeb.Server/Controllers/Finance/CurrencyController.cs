using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Services;
using Hamgam.Shared.CurrencySync;
using Hamgam.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HamgamCementWeb.Server.Controllers.Finance;

[ApiController]
[Route("api/currencies")]
[Authorize]
public class CurrencyController : ControllerBase
{
    private static readonly Dictionary<int, string> CurrencyOrderColumns = new()
    {
        [1] = nameof(Currency.Name),
        [2] = nameof(Currency.Symbol),
        [3] = nameof(Currency.CurrencyCode),
        [4] = nameof(Currency.IsBaseCurrency),
        [5] = nameof(Currency.DecimalPlaces),
        [6] = nameof(Currency.IsActive),
    };

    private static readonly Dictionary<int, string> HistoryOrderColumns = new()
    {
        [1] = "CurrencyName",
        [2] = nameof(CurrencyExchangeHistory.BaseUnitsPerUnit),
        [3] = nameof(CurrencyExchangeHistory.PreviousBaseUnitsPerUnit),
        [4] = nameof(CurrencyExchangeHistory.EffectiveFrom),
        [5] = nameof(CurrencyExchangeHistory.EffectiveTo),
    };

    private readonly AppDbContext _db;
    private readonly ICurrencyConversionService _currency;
    private readonly ICurrencyExchangeRateService _exchangeRates;
    private readonly ICurrencyReferenceSyncService _currencySync;
    private readonly CurrencySyncOptions _syncOptions;

    public CurrencyController(
        AppDbContext db,
        ICurrencyConversionService currency,
        ICurrencyExchangeRateService exchangeRates,
        ICurrencyReferenceSyncService currencySync,
        IOptions<CurrencySyncOptions> syncOptions)
    {
        _db = db;
        _currency = currency;
        _exchangeRates = exchangeRates;
        _currencySync = currencySync;
        _syncOptions = syncOptions.Value;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        await _currencySync.SyncFromReferenceToLocalAsync(cancellationToken);

        var items = await _db.Currencies
            .AsNoTracking()
            .Where(c => c.IsDeleted != true && c.IsActive == true)
            .OrderBy(c => c.IsBaseCurrency ? 0 : 1)
            .ThenBy(c => c.Name)
            .Select(c => new
            {
                c.CurrencyID,
                c.Name,
                c.Symbol,
                c.CurrencyCode,
                c.IsBaseCurrency,
                c.DecimalPlaces,
                c.UseInBothSystems,
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("base")]
    public async Task<IActionResult> GetBase(CancellationToken cancellationToken)
    {
        var baseCurrency = await _db.Currencies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IsBaseCurrency && c.IsDeleted != true, cancellationToken);

        if (baseCurrency is null)
        {
            return NotFound(new { message = "ارز پایه تعریف نشده است." });
        }

        return Ok(new
        {
            baseCurrency.CurrencyID,
            baseCurrency.Name,
            baseCurrency.Symbol,
            baseCurrency.CurrencyCode,
        });
    }

    [HttpGet("current-rates")]
    public async Task<IActionResult> CurrentRates(CancellationToken cancellationToken)
    {
        var baseCurrency = await _currency.GetBaseCurrencyAsync(cancellationToken);
        var rates = await _db.CurrencyExchangeRates
            .AsNoTracking()
            .Where(r => r.IsDeleted != true)
            .Select(r => new
            {
                currencyId = r.CurrencyID,
                baseUnitsPerUnit = r.BaseUnitsPerUnit,
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            baseCurrencyId = baseCurrency.CurrencyID,
            rates,
        });
    }

    [HttpGet("rate-at")]
    public async Task<IActionResult> GetRateAt(
        [FromQuery] int currencyId,
        [FromQuery] DateTime? date,
        CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await _currency.GetSnapshotAsync(currencyId, date ?? DateTime.Now, cancellationToken);
            var baseCurrency = await _currency.GetBaseCurrencyAsync(cancellationToken);

            return Ok(new
            {
                currencyId = snapshot.CurrencyId,
                baseCurrencyId = snapshot.BaseCurrencyId,
                baseCurrencyName = baseCurrency.Name,
                exchangeHistoryId = snapshot.ExchangeHistoryId,
                baseUnitsPerUnit = snapshot.BaseUnitsPerUnit,
                isBaseCurrency = snapshot.IsBaseCurrency,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("datatable")]
    [HasPermission("currencies.list.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        await _currencySync.SyncFromReferenceToLocalAsync(cancellationToken);

        var draw = request.Draw;
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = _db.Currencies
            .AsNoTracking()
            .Where(c => c.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(c =>
                c.Name.Contains(searchValue) ||
                c.Symbol.Contains(searchValue) ||
                c.CurrencyCode.Contains(searchValue) ||
                (c.Description != null && c.Description.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);
        var orderedQuery = ApplyCurrencyOrdering(query, request.Order);

        var rows = await orderedQuery
            .Skip(start)
            .Take(length)
            .Select(c => new CurrencyTableRow
            {
                CurrencyId = c.CurrencyID,
                Name = c.Name,
                Symbol = c.Symbol,
                CurrencyCode = c.CurrencyCode,
                Description = c.Description,
                IsBaseCurrency = c.IsBaseCurrency,
                DecimalPlaces = c.DecimalPlaces,
                IsActive = c.IsActive == true,
                UseInBothSystems = c.UseInBothSystems,
                CurrentRate = _db.CurrencyExchangeRates
                    .Where(r => r.CurrencyID == c.CurrencyID)
                    .Select(r => (decimal?)r.BaseUnitsPerUnit)
                    .FirstOrDefault(),
                RateEffectiveFrom = _db.CurrencyExchangeRates
                    .Where(r => r.CurrencyID == c.CurrencyID)
                    .Select(r => (DateTime?)r.EffectiveFrom)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].RowNumber = start + i + 1;
        }

        return Ok(new
        {
            draw,
            recordsTotal,
            recordsFiltered,
            data = rows,
        });
    }

    // چرا currencies.exchange: تاریخچه‌ی نوسانات در صفحه‌ی «نوسانات» (currencies.exchange) نمایش داده می‌شود.
    [HttpPost("exchange-history/datatable")]
    [HasPermission("currencies.exchange.view")]
    public async Task<IActionResult> ExchangeHistoryDataTable(
        [FromBody] ExchangeHistoryDataTableRequest request,
        CancellationToken cancellationToken)
    {
        var draw = request.Draw;
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = _db.CurrencyExchangeHistories
            .AsNoTracking()
            .Where(h => h.IsDeleted != true);

        if (request.CurrencyId is > 0)
        {
            query = query.Where(h => h.CurrencyID == request.CurrencyId);
        }

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(h =>
                h.Currency!.Name.Contains(searchValue) ||
                h.Currency.CurrencyCode.Contains(searchValue) ||
                (h.ChangeReason != null && h.ChangeReason.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);
        var orderedQuery = ApplyHistoryOrdering(query, request.Order);

        var rows = await orderedQuery
            .Skip(start)
            .Take(length)
            .Select(h => new ExchangeHistoryTableRow
            {
                HistoryId = h.HistoryID,
                CurrencyId = h.CurrencyID,
                CurrencyName = h.Currency!.Name,
                CurrencyCode = h.Currency.CurrencyCode,
                BaseCurrencyId = h.BaseCurrencyID,
                BaseCurrencyName = h.BaseCurrency!.Name,
                BaseCurrencyCode = h.BaseCurrency.CurrencyCode,
                BaseUnitsPerUnit = h.BaseUnitsPerUnit,
                PreviousBaseUnitsPerUnit = h.PreviousBaseUnitsPerUnit,
                EffectiveFrom = h.EffectiveFrom,
                EffectiveTo = h.EffectiveTo,
                ChangeReason = h.ChangeReason,
            })
            .ToListAsync(cancellationToken);

        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].RowNumber = start + i + 1;
        }

        return Ok(new
        {
            draw,
            recordsTotal,
            recordsFiltered,
            data = rows,
        });
    }

    [HttpPost]
    [HasPermission("currencies.list.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveCurrencyRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var code = request.CurrencyCode.Trim().ToUpperInvariant();
        if (await _db.Currencies.AnyAsync(
                c => c.CurrencyCode == code && c.IsDeleted != true,
                cancellationToken))
        {
            return BadRequest(new { message = "کد ارز تکراری است." });
        }

        if (request.IsBaseCurrency)
        {
            await ClearBaseCurrencyFlagsAsync(cancellationToken);
        }
        else if (!await _db.Currencies.AnyAsync(c => c.IsBaseCurrency && c.IsDeleted != true, cancellationToken))
        {
            return BadRequest(new { message = "ابتدا یک ارز پایه تعریف کنید." });
        }

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;

        var currency = new Currency
        {
            Name = request.Name.Trim(),
            Symbol = request.Symbol.Trim(),
            CurrencyCode = code,
            Description = request.Description?.Trim(),
            DecimalPlaces = request.DecimalPlaces,
            IsBaseCurrency = request.IsBaseCurrency,
            UseInBothSystems = request.UseInBothSystems,
            OriginSystem = _syncOptions.SystemCode,
            CreatedBy = userId,
            CreatedAt = now,
            IsActive = request.IsActive,
            IsDeleted = false,
        };

        _db.Currencies.Add(currency);
        await _db.SaveChangesAsync(cancellationToken);

        if (!request.IsBaseCurrency && request.BaseUnitsPerUnit is > 0)
        {
            var baseCurrency = await GetBaseCurrencyEntityAsync(cancellationToken);
            if (baseCurrency is null)
            {
                return BadRequest(new { message = "ارز پایه یافت نشد." });
            }

            await ApplyExchangeRateAsync(
                currency.CurrencyID,
                baseCurrency.CurrencyID,
                request.BaseUnitsPerUnit.Value,
                request.ChangeReason,
                now,
                userId,
                cancellationToken);
        }

        await _currencySync.PushLocalCurrencyToReferenceAsync(currency.CurrencyCode, cancellationToken);

        return CreatedAtAction(
            nameof(Update),
            new { id = currency.CurrencyID },
            new { message = "ارز با موفقیت ایجاد شد." });
    }

    [HttpPut("{id:int}")]
    [HasPermission("currencies.list.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveCurrencyRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var currency = await _db.Currencies
            .FirstOrDefaultAsync(c => c.CurrencyID == id && c.IsDeleted != true, cancellationToken);

        if (currency is null)
        {
            return NotFound(new { message = "ارز یافت نشد." });
        }

        var code = request.CurrencyCode.Trim().ToUpperInvariant();
        if (await _db.Currencies.AnyAsync(
                c => c.CurrencyCode == code && c.CurrencyID != id && c.IsDeleted != true,
                cancellationToken))
        {
            return BadRequest(new { message = "کد ارز تکراری است." });
        }

        if (request.IsBaseCurrency && !currency.IsBaseCurrency)
        {
            await ClearBaseCurrencyFlagsAsync(cancellationToken);
        }
        else if (!request.IsBaseCurrency && currency.IsBaseCurrency)
        {
            return BadRequest(new { message = "ارز پایه را نمی‌توان به ارز عادی تبدیل کرد. ابتدا ارز پایه دیگری تعیین کنید." });
        }

        currency.Name = request.Name.Trim();
        currency.Symbol = request.Symbol.Trim();
        currency.CurrencyCode = code;
        currency.Description = request.Description?.Trim();
        currency.DecimalPlaces = request.DecimalPlaces;
        currency.IsBaseCurrency = request.IsBaseCurrency;
        currency.IsActive = request.IsActive;
        currency.UseInBothSystems = request.UseInBothSystems;
        currency.UpdatedAt = DateTime.Now;
        currency.IsUpdated = true;
        currency.UpdatedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);

        await _currencySync.PushLocalCurrencyToReferenceAsync(currency.CurrencyCode, cancellationToken);

        return Ok(new { message = "ارز با موفقیت ویرایش شد." });
    }

    // چرا setBase: عملیات تعیین ارز پایه، extra action صفحه‌ی ارزهاست.
    [HttpPut("{id:int}/set-base")]
    [HasPermission("currencies.list.setBase")]
    public async Task<IActionResult> SetBase(int id, CancellationToken cancellationToken)
    {
        var currency = await _db.Currencies
            .FirstOrDefaultAsync(c => c.CurrencyID == id && c.IsDeleted != true, cancellationToken);

        if (currency is null)
        {
            return NotFound(new { message = "ارز یافت نشد." });
        }

        if (currency.IsBaseCurrency)
        {
            return Ok(new { message = "این ارز از قبل ارز پایه است." });
        }

        var userId = ResolveCurrentUserId();

        // چرا خواندن قبل از تغییر: برای بازمحاسبه نرخ سایر ارزها به نرخ ارز پایه‌ی جدید نسبت به پایه‌ی قدیم (rNew)
        // و نرخ فعلی هر ارز نسبت به پایه‌ی قدیم (rX) نیاز داریم؛ این مقادیر باید پیش از هر تغییری خوانده شوند.
        var newBaseRate = await _db.CurrencyExchangeRates
            .AsNoTracking()
            .Where(r => r.CurrencyID == id)
            .Select(r => (decimal?)r.BaseUnitsPerUnit)
            .FirstOrDefaultAsync(cancellationToken);

        // بدون نرخ معتبر ارز جدید نسبت به پایه‌ی فعلی، بازمحاسبه ممکن نیست؛ برای جلوگیری از خرابی داده، ابتدا نرخ لازم است.
        if (newBaseRate is not > 0)
        {
            return BadRequest(new { message = "برای تعیین این ارز به‌عنوان پایه، ابتدا باید نرخ آن نسبت به ارز پایه‌ی فعلی ثبت شود." });
        }

        var rNew = newBaseRate.Value;

        var oldBase = await _db.Currencies
            .FirstOrDefaultAsync(c => c.IsBaseCurrency && c.IsDeleted != true, cancellationToken);
        var oldBaseId = oldBase?.CurrencyID;

        // نرخ فعلی سایر ارزهای فعال نسبت به پایه‌ی قدیم (به‌جز ارز پایه‌ی جدید که نرخش حذف می‌شود).
        var otherRates = await _db.CurrencyExchangeRates
            .AsNoTracking()
            .Where(r => r.CurrencyID != id)
            .Join(
                _db.Currencies.Where(c => c.IsDeleted != true && c.IsActive == true),
                r => r.CurrencyID,
                c => c.CurrencyID,
                (r, c) => new { r.CurrencyID, r.BaseUnitsPerUnit })
            .ToListAsync(cancellationToken);

        // چرا تراکنش: تغییر ارز پایه شامل حذف نرخ‌ها، بستن تاریخچه و بازمحاسبه‌ی نرخ همه‌ی ارزها با چند SaveChanges است؛
        // در صورت خطای میانی باید کل عملیات برگردد تا نرخ‌ها ناسازگار (بخشی قدیم/بخشی جدید) نمانند.
        await using var setBaseTransaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        await ClearBaseCurrencyFlagsAsync(cancellationToken);

        currency.IsBaseCurrency = true;
        currency.UpdatedAt = DateTime.Now;
        currency.IsUpdated = true;
        currency.UpdatedBy = userId;

        // ارز پایه نباید در جدول نرخ ردیف داشته باشد؛ نرخ قبلی ارز پایه‌ی جدید حذف و تاریخچه‌ی باز آن بسته می‌شود.
        var rates = await _db.CurrencyExchangeRates
            .Where(r => r.CurrencyID == id)
            .ToListAsync(cancellationToken);
        _db.CurrencyExchangeRates.RemoveRange(rates);

        var openHistories = await _db.CurrencyExchangeHistories
            .Where(h => h.CurrencyID == id && h.EffectiveTo == null)
            .ToListAsync(cancellationToken);
        var now = DateTime.Now;
        foreach (var history in openHistories)
        {
            history.EffectiveTo = now;
            history.UpdatedAt = now;
            history.IsUpdated = true;
            history.UpdatedBy = userId;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // چرا بازمحاسبه: پس از تغییر پایه، نرخ همه‌ی ارزها باید نسبت به پایه‌ی جدید بیان شود.
        // نرخ ارز X نسبت به پایه‌ی جدید = (نرخ X نسبت به پایه‌ی قدیم) ÷ rNew؛ و نرخ پایه‌ی قدیم نسبت به پایه‌ی جدید = 1 ÷ rNew.
        var reason = $"بازمحاسبه نرخ به‌دلیل تغییر ارز پایه به «{currency.Name}»";
        var effectiveFrom = DateTime.Now;

        if (oldBaseId is int oldId)
        {
            await _exchangeRates.ApplyRateChangeAsync(
                oldId, id, 1m / rNew, reason, effectiveFrom, userId, cancellationToken);
        }

        foreach (var rate in otherRates)
        {
            if (rate.CurrencyID == oldBaseId)
            {
                continue;
            }

            await _exchangeRates.ApplyRateChangeAsync(
                rate.CurrencyID, id, rate.BaseUnitsPerUnit / rNew, reason, effectiveFrom, userId, cancellationToken);
        }

        await setBaseTransaction.CommitAsync(cancellationToken);

        await _currencySync.PushLocalCurrencyToReferenceAsync(currency.CurrencyCode, cancellationToken);

        return Ok(new { message = "ارز پایه با موفقیت تغییر کرد و نرخ سایر ارزها بازمحاسبه شد." });
    }

    // چرا setRate: ثبت نرخ ارز، extra action صفحه‌ی ارزهاست.
    [HttpPost("{id:int}/exchange-rate")]
    [HasPermission("currencies.list.setRate")]
    public async Task<IActionResult> UpdateExchangeRate(
        int id,
        [FromBody] UpdateExchangeRateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var currency = await _db.Currencies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CurrencyID == id && c.IsDeleted != true, cancellationToken);

        if (currency is null)
        {
            return NotFound(new { message = "ارز یافت نشد." });
        }

        if (currency.IsBaseCurrency)
        {
            return BadRequest(new { message = "برای ارز پایه نرخ تبدیل ثبت نمی‌شود." });
        }

        var baseCurrency = await GetBaseCurrencyEntityAsync(cancellationToken);
        if (baseCurrency is null)
        {
            return BadRequest(new { message = "ارز پایه تعریف نشده است." });
        }

        var effectiveFrom = request.EffectiveFrom ?? DateTime.Now;
        await ApplyExchangeRateAsync(
            id,
            baseCurrency.CurrencyID,
            request.BaseUnitsPerUnit,
            request.ChangeReason,
            effectiveFrom,
            ResolveCurrentUserId(),
            cancellationToken);

        await _currencySync.PushLocalCurrencyToReferenceAsync(currency.CurrencyCode, cancellationToken);

        return Ok(new { message = "نرخ ارز با موفقیت ثبت شد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("currencies.list.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var currency = await _db.Currencies
            .FirstOrDefaultAsync(c => c.CurrencyID == id && c.IsDeleted != true, cancellationToken);

        if (currency is null)
        {
            return NotFound(new { message = "ارز یافت نشد." });
        }

        if (currency.IsBaseCurrency)
        {
            return BadRequest(new { message = "ارز پایه قابل حذف نیست." });
        }

        var hasRates = await _db.CurrencyExchangeRates.AnyAsync(r => r.CurrencyID == id, cancellationToken);
        if (hasRates)
        {
            return BadRequest(new { message = "این ارز نرخ تبدیل دارد و قابل حذف نیست." });
        }

        currency.IsDeleted = true;
        currency.IsActive = false;
        currency.DeletedAt = DateTime.Now;
        currency.DeletedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);

        await _currencySync.PushLocalCurrencyToReferenceAsync(currency.CurrencyCode, cancellationToken);

        return Ok(new { message = "ارز با موفقیت حذف شد." });
    }

    private async Task ApplyExchangeRateAsync(
        int currencyId,
        int baseCurrencyId,
        decimal newRate,
        string? changeReason,
        DateTime effectiveFrom,
        int? userId,
        CancellationToken cancellationToken)
    {
        await _exchangeRates.ApplyRateChangeAsync(
            currencyId,
            baseCurrencyId,
            newRate,
            changeReason,
            effectiveFrom,
            userId,
            cancellationToken);
    }

    private async Task ClearBaseCurrencyFlagsAsync(CancellationToken cancellationToken)
    {
        var baseCurrencies = await _db.Currencies
            .Where(c => c.IsBaseCurrency && c.IsDeleted != true)
            .ToListAsync(cancellationToken);

        foreach (var item in baseCurrencies)
        {
            item.IsBaseCurrency = false;
            item.UpdatedAt = DateTime.Now;
            item.IsUpdated = true;
            item.UpdatedBy = ResolveCurrentUserId();
        }
    }

    private async Task<Currency?> GetBaseCurrencyEntityAsync(CancellationToken cancellationToken)
    {
        return await _db.Currencies
            .FirstOrDefaultAsync(c => c.IsBaseCurrency && c.IsDeleted != true, cancellationToken);
    }

    private static IQueryable<Currency> ApplyCurrencyOrdering(
        IQueryable<Currency> query,
        List<DataTableOrder>? orders)
    {
        if (orders is null || orders.Count == 0)
        {
            return query.OrderByDescending(c => c.IsBaseCurrency).ThenBy(c => c.Name);
        }

        IOrderedQueryable<Currency>? ordered = null;
        foreach (var order in orders)
        {
            if (!CurrencyOrderColumns.TryGetValue(order.Column, out var column))
            {
                continue;
            }

            var descending = string.Equals(order.Dir, "desc", StringComparison.OrdinalIgnoreCase);

            ordered = column switch
            {
                nameof(Currency.Name) when ordered is null => descending
                    ? query.OrderByDescending(c => c.Name)
                    : query.OrderBy(c => c.Name),
                nameof(Currency.Name) => descending
                    ? ordered!.ThenByDescending(c => c.Name)
                    : ordered!.ThenBy(c => c.Name),
                nameof(Currency.Symbol) when ordered is null => descending
                    ? query.OrderByDescending(c => c.Symbol)
                    : query.OrderBy(c => c.Symbol),
                nameof(Currency.Symbol) => descending
                    ? ordered!.ThenByDescending(c => c.Symbol)
                    : ordered!.ThenBy(c => c.Symbol),
                nameof(Currency.CurrencyCode) when ordered is null => descending
                    ? query.OrderByDescending(c => c.CurrencyCode)
                    : query.OrderBy(c => c.CurrencyCode),
                nameof(Currency.CurrencyCode) => descending
                    ? ordered!.ThenByDescending(c => c.CurrencyCode)
                    : ordered!.ThenBy(c => c.CurrencyCode),
                nameof(Currency.IsBaseCurrency) when ordered is null => descending
                    ? query.OrderByDescending(c => c.IsBaseCurrency)
                    : query.OrderBy(c => c.IsBaseCurrency),
                nameof(Currency.IsBaseCurrency) => descending
                    ? ordered!.ThenByDescending(c => c.IsBaseCurrency)
                    : ordered!.ThenBy(c => c.IsBaseCurrency),
                nameof(Currency.DecimalPlaces) when ordered is null => descending
                    ? query.OrderByDescending(c => c.DecimalPlaces)
                    : query.OrderBy(c => c.DecimalPlaces),
                nameof(Currency.DecimalPlaces) => descending
                    ? ordered!.ThenByDescending(c => c.DecimalPlaces)
                    : ordered!.ThenBy(c => c.DecimalPlaces),
                nameof(Currency.IsActive) when ordered is null => descending
                    ? query.OrderByDescending(c => c.IsActive)
                    : query.OrderBy(c => c.IsActive),
                nameof(Currency.IsActive) => descending
                    ? ordered!.ThenByDescending(c => c.IsActive)
                    : ordered!.ThenBy(c => c.IsActive),
                _ => ordered,
            };
        }

        return ordered ?? query.OrderByDescending(c => c.IsBaseCurrency).ThenBy(c => c.Name);
    }

    private static IQueryable<CurrencyExchangeHistory> ApplyHistoryOrdering(
        IQueryable<CurrencyExchangeHistory> query,
        List<DataTableOrder>? orders)
    {
        if (orders is null || orders.Count == 0)
        {
            return query.OrderByDescending(h => h.EffectiveFrom);
        }

        IOrderedQueryable<CurrencyExchangeHistory>? ordered = null;
        foreach (var order in orders)
        {
            if (!HistoryOrderColumns.TryGetValue(order.Column, out var column))
            {
                continue;
            }

            var descending = string.Equals(order.Dir, "desc", StringComparison.OrdinalIgnoreCase);

            ordered = column switch
            {
                "CurrencyName" when ordered is null => descending
                    ? query.OrderByDescending(h => h.Currency!.Name)
                    : query.OrderBy(h => h.Currency!.Name),
                "CurrencyName" => descending
                    ? ordered!.ThenByDescending(h => h.Currency!.Name)
                    : ordered!.ThenBy(c => c.Currency!.Name),
                nameof(CurrencyExchangeHistory.BaseUnitsPerUnit) when ordered is null => descending
                    ? query.OrderByDescending(h => h.BaseUnitsPerUnit)
                    : query.OrderBy(h => h.BaseUnitsPerUnit),
                nameof(CurrencyExchangeHistory.BaseUnitsPerUnit) => descending
                    ? ordered!.ThenByDescending(h => h.BaseUnitsPerUnit)
                    : ordered!.ThenBy(h => h.BaseUnitsPerUnit),
                nameof(CurrencyExchangeHistory.PreviousBaseUnitsPerUnit) when ordered is null => descending
                    ? query.OrderByDescending(h => h.PreviousBaseUnitsPerUnit)
                    : query.OrderBy(h => h.PreviousBaseUnitsPerUnit),
                nameof(CurrencyExchangeHistory.PreviousBaseUnitsPerUnit) => descending
                    ? ordered!.ThenByDescending(h => h.PreviousBaseUnitsPerUnit)
                    : ordered!.ThenBy(h => h.PreviousBaseUnitsPerUnit),
                nameof(CurrencyExchangeHistory.EffectiveFrom) when ordered is null => descending
                    ? query.OrderByDescending(h => h.EffectiveFrom)
                    : query.OrderBy(h => h.EffectiveFrom),
                nameof(CurrencyExchangeHistory.EffectiveFrom) => descending
                    ? ordered!.ThenByDescending(h => h.EffectiveFrom)
                    : ordered!.ThenBy(h => h.EffectiveFrom),
                nameof(CurrencyExchangeHistory.EffectiveTo) when ordered is null => descending
                    ? query.OrderByDescending(h => h.EffectiveTo)
                    : query.OrderBy(h => h.EffectiveTo),
                nameof(CurrencyExchangeHistory.EffectiveTo) => descending
                    ? ordered!.ThenByDescending(h => h.EffectiveTo)
                    : ordered!.ThenBy(h => h.EffectiveTo),
                _ => ordered,
            };
        }

        return ordered ?? query.OrderByDescending(h => h.EffectiveFrom);
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

    public class ExchangeHistoryDataTableRequest : DataTableRequest
    {
        public int? CurrencyId { get; set; }
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

    public class CurrencyTableRow
    {
        public int RowNumber { get; set; }
        public int CurrencyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsBaseCurrency { get; set; }
        public byte DecimalPlaces { get; set; }
        public bool IsActive { get; set; }
        public bool UseInBothSystems { get; set; }
        public decimal? CurrentRate { get; set; }
        public DateTime? RateEffectiveFrom { get; set; }
    }

    public class ExchangeHistoryTableRow
    {
        public int RowNumber { get; set; }
        public int HistoryId { get; set; }
        public int CurrencyId { get; set; }
        public string CurrencyName { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public int BaseCurrencyId { get; set; }
        public string BaseCurrencyName { get; set; } = string.Empty;
        public string BaseCurrencyCode { get; set; } = string.Empty;
        public decimal BaseUnitsPerUnit { get; set; }
        public decimal? PreviousBaseUnitsPerUnit { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string? ChangeReason { get; set; }
    }

    public class SaveCurrencyRequest
    {
        [Required(ErrorMessage = "نام ارز الزامی است.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "نماد ارز الزامی است.")]
        [MaxLength(10)]
        public string Symbol { get; set; } = string.Empty;

        [Required(ErrorMessage = "کد ارز الزامی است.")]
        [MaxLength(3)]
        public string CurrencyCode { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public byte DecimalPlaces { get; set; }

        public bool IsBaseCurrency { get; set; }

        public bool UseInBothSystems { get; set; }

        public bool IsActive { get; set; } = true;

        public decimal? BaseUnitsPerUnit { get; set; }

        [MaxLength(500)]
        public string? ChangeReason { get; set; }
    }

    public class UpdateExchangeRateRequest
    {
        [Range(0.00000001, double.MaxValue, ErrorMessage = "نرخ باید بزرگ‌تر از صفر باشد.")]
        public decimal BaseUnitsPerUnit { get; set; }

        [MaxLength(500)]
        public string? ChangeReason { get; set; }

        public DateTime? EffectiveFrom { get; set; }
    }
}
