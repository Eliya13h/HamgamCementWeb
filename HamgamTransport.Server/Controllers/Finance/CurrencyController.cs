using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamTransport.Server.Authorization;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using HamgamTransport.Server.Services;
using Hamgam.Shared.CurrencySync;
using Hamgam.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HamgamTransport.Server.Controllers.Finance;

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
            return NotFound(new { message = "Ø§Ø±Ø² Ù¾Ø§ÛŒÙ‡ ØªØ¹Ø±ÛŒÙ Ù†Ø´Ø¯Ù‡ Ø§Ø³Øª." });
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

    // Ú†Ø±Ø§ currencies.exchange: ØªØ§Ø±ÛŒØ®Ú†Ù‡â€ŒÛŒ Ù†ÙˆØ³Ø§Ù†Ø§Øª Ø¯Ø± ØµÙØ­Ù‡â€ŒÛŒ Â«Ù†ÙˆØ³Ø§Ù†Ø§ØªÂ» (currencies.exchange) Ù†Ù…Ø§ÛŒØ´ Ø¯Ø§Ø¯Ù‡ Ù…ÛŒâ€ŒØ´ÙˆØ¯.
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
            return BadRequest(new { message = "Ú©Ø¯ Ø§Ø±Ø² ØªÚ©Ø±Ø§Ø±ÛŒ Ø§Ø³Øª." });
        }

        if (request.IsBaseCurrency)
        {
            await ClearBaseCurrencyFlagsAsync(cancellationToken);
        }
        else if (!await _db.Currencies.AnyAsync(c => c.IsBaseCurrency && c.IsDeleted != true, cancellationToken))
        {
            return BadRequest(new { message = "Ø§Ø¨ØªØ¯Ø§ ÛŒÚ© Ø§Ø±Ø² Ù¾Ø§ÛŒÙ‡ ØªØ¹Ø±ÛŒÙ Ú©Ù†ÛŒØ¯." });
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
                return BadRequest(new { message = "Ø§Ø±Ø² Ù¾Ø§ÛŒÙ‡ ÛŒØ§ÙØª Ù†Ø´Ø¯." });
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
            new { message = "Ø§Ø±Ø² Ø¨Ø§ Ù…ÙˆÙÙ‚ÛŒØª Ø§ÛŒØ¬Ø§Ø¯ Ø´Ø¯." });
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
            return NotFound(new { message = "Ø§Ø±Ø² ÛŒØ§ÙØª Ù†Ø´Ø¯." });
        }

        var code = request.CurrencyCode.Trim().ToUpperInvariant();
        if (await _db.Currencies.AnyAsync(
                c => c.CurrencyCode == code && c.CurrencyID != id && c.IsDeleted != true,
                cancellationToken))
        {
            return BadRequest(new { message = "Ú©Ø¯ Ø§Ø±Ø² ØªÚ©Ø±Ø§Ø±ÛŒ Ø§Ø³Øª." });
        }

        if (request.IsBaseCurrency && !currency.IsBaseCurrency)
        {
            await ClearBaseCurrencyFlagsAsync(cancellationToken);
        }
        else if (!request.IsBaseCurrency && currency.IsBaseCurrency)
        {
            return BadRequest(new { message = "Ø§Ø±Ø² Ù¾Ø§ÛŒÙ‡ Ø±Ø§ Ù†Ù…ÛŒâ€ŒØªÙˆØ§Ù† Ø¨Ù‡ Ø§Ø±Ø² Ø¹Ø§Ø¯ÛŒ ØªØ¨Ø¯ÛŒÙ„ Ú©Ø±Ø¯. Ø§Ø¨ØªØ¯Ø§ Ø§Ø±Ø² Ù¾Ø§ÛŒÙ‡ Ø¯ÛŒÚ¯Ø±ÛŒ ØªØ¹ÛŒÛŒÙ† Ú©Ù†ÛŒØ¯." });
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

        return Ok(new { message = "Ø§Ø±Ø² Ø¨Ø§ Ù…ÙˆÙÙ‚ÛŒØª ÙˆÛŒØ±Ø§ÛŒØ´ Ø´Ø¯." });
    }

    // Ú†Ø±Ø§ setBase: Ø¹Ù…Ù„ÛŒØ§Øª ØªØ¹ÛŒÛŒÙ† Ø§Ø±Ø² Ù¾Ø§ÛŒÙ‡ØŒ extra action ØµÙØ­Ù‡â€ŒÛŒ Ø§Ø±Ø²Ù‡Ø§Ø³Øª.
    [HttpPut("{id:int}/set-base")]
    [HasPermission("currencies.list.setBase")]
    public async Task<IActionResult> SetBase(int id, CancellationToken cancellationToken)
    {
        var currency = await _db.Currencies
            .FirstOrDefaultAsync(c => c.CurrencyID == id && c.IsDeleted != true, cancellationToken);

        if (currency is null)
        {
            return NotFound(new { message = "Ø§Ø±Ø² ÛŒØ§ÙØª Ù†Ø´Ø¯." });
        }

        if (currency.IsBaseCurrency)
        {
            return Ok(new { message = "Ø§ÛŒÙ† Ø§Ø±Ø² Ø§Ø² Ù‚Ø¨Ù„ Ø§Ø±Ø² Ù¾Ø§ÛŒÙ‡ Ø§Ø³Øª." });
        }

        var userId = ResolveCurrentUserId();

        // Ú†Ø±Ø§ Ø®ÙˆØ§Ù†Ø¯Ù† Ù‚Ø¨Ù„ Ø§Ø² ØªØºÛŒÛŒØ±: Ø¨Ø±Ø§ÛŒ Ø¨Ø§Ø²Ù…Ø­Ø§Ø³Ø¨Ù‡ Ù†Ø±Ø® Ø³Ø§ÛŒØ± Ø§Ø±Ø²Ù‡Ø§ Ø¨Ù‡ Ù†Ø±Ø® Ø§Ø±Ø² Ù¾Ø§ÛŒÙ‡â€ŒÛŒ Ø¬Ø¯ÛŒØ¯ Ù†Ø³Ø¨Øª Ø¨Ù‡ Ù¾Ø§ÛŒÙ‡â€ŒÛŒ Ù‚Ø¯ÛŒÙ… (rNew)
        // Ùˆ Ù†Ø±Ø® ÙØ¹Ù„ÛŒ Ù‡Ø± Ø§Ø±Ø² Ù†Ø³Ø¨Øª Ø¨Ù‡ Ù¾Ø§ÛŒÙ‡â€ŒÛŒ Ù‚Ø¯ÛŒÙ… (rX) Ù†ÛŒØ§Ø² Ø¯Ø§Ø±ÛŒÙ…Ø› Ø§ÛŒÙ† Ù…Ù‚Ø§Ø¯ÛŒØ± Ø¨Ø§ÛŒØ¯ Ù¾ÛŒØ´ Ø§Ø² Ù‡Ø± ØªØºÛŒÛŒØ±ÛŒ Ø®ÙˆØ§Ù†Ø¯Ù‡ Ø´ÙˆÙ†Ø¯.
        var newBaseRate = await _db.CurrencyExchangeRates
            .AsNoTracking()
            .Where(r => r.CurrencyID == id)
            .Select(r => (decimal?)r.BaseUnitsPerUnit)
            .FirstOrDefaultAsync(cancellationToken);

        // Ø¨Ø¯ÙˆÙ† Ù†Ø±Ø® Ù…Ø¹ØªØ¨Ø± Ø§Ø±Ø² Ø¬Ø¯ÛŒØ¯ Ù†Ø³Ø¨Øª Ø¨Ù‡ Ù¾Ø§ÛŒÙ‡â€ŒÛŒ ÙØ¹Ù„ÛŒØŒ Ø¨Ø§Ø²Ù…Ø­Ø§Ø³Ø¨Ù‡ Ù…Ù…Ú©Ù† Ù†ÛŒØ³ØªØ› Ø¨Ø±Ø§ÛŒ Ø¬Ù„ÙˆÚ¯ÛŒØ±ÛŒ Ø§Ø² Ø®Ø±Ø§Ø¨ÛŒ Ø¯Ø§Ø¯Ù‡ØŒ Ø§Ø¨ØªØ¯Ø§ Ù†Ø±Ø® Ù„Ø§Ø²Ù… Ø§Ø³Øª.
        if (newBaseRate is not > 0)
        {
            return BadRequest(new { message = "Ø¨Ø±Ø§ÛŒ ØªØ¹ÛŒÛŒÙ† Ø§ÛŒÙ† Ø§Ø±Ø² Ø¨Ù‡â€ŒØ¹Ù†ÙˆØ§Ù† Ù¾Ø§ÛŒÙ‡ØŒ Ø§Ø¨ØªØ¯Ø§ Ø¨Ø§ÛŒØ¯ Ù†Ø±Ø® Ø¢Ù† Ù†Ø³Ø¨Øª Ø¨Ù‡ Ø§Ø±Ø² Ù¾Ø§ÛŒÙ‡â€ŒÛŒ ÙØ¹Ù„ÛŒ Ø«Ø¨Øª Ø´ÙˆØ¯." });
        }

        var rNew = newBaseRate.Value;

        var oldBase = await _db.Currencies
            .FirstOrDefaultAsync(c => c.IsBaseCurrency && c.IsDeleted != true, cancellationToken);
        var oldBaseId = oldBase?.CurrencyID;

        // Ù†Ø±Ø® ÙØ¹Ù„ÛŒ Ø³Ø§ÛŒØ± Ø§Ø±Ø²Ù‡Ø§ÛŒ ÙØ¹Ø§Ù„ Ù†Ø³Ø¨Øª Ø¨Ù‡ Ù¾Ø§ÛŒÙ‡â€ŒÛŒ Ù‚Ø¯ÛŒÙ… (Ø¨Ù‡â€ŒØ¬Ø² Ø§Ø±Ø² Ù¾Ø§ÛŒÙ‡â€ŒÛŒ Ø¬Ø¯ÛŒØ¯ Ú©Ù‡ Ù†Ø±Ø®Ø´ Ø­Ø°Ù Ù…ÛŒâ€ŒØ´ÙˆØ¯).
        var otherRates = await _db.CurrencyExchangeRates
            .AsNoTracking()
            .Where(r => r.CurrencyID != id)
            .Join(
                _db.Currencies.Where(c => c.IsDeleted != true && c.IsActive == true),
                r => r.CurrencyID,
                c => c.CurrencyID,
                (r, c) => new { r.CurrencyID, r.BaseUnitsPerUnit })
            .ToListAsync(cancellationToken);

        // Ú†Ø±Ø§ ØªØ±Ø§Ú©Ù†Ø´: ØªØºÛŒÛŒØ± Ø§Ø±Ø² Ù¾Ø§ÛŒÙ‡ Ø´Ø§Ù…Ù„ Ø­Ø°Ù Ù†Ø±Ø®â€ŒÙ‡Ø§ØŒ Ø¨Ø³ØªÙ† ØªØ§Ø±ÛŒØ®Ú†Ù‡ Ùˆ Ø¨Ø§Ø²Ù…Ø­Ø§Ø³Ø¨Ù‡â€ŒÛŒ Ù†Ø±Ø® Ù‡Ù…Ù‡â€ŒÛŒ Ø§Ø±Ø²Ù‡Ø§ Ø¨Ø§ Ú†Ù†Ø¯ SaveChanges Ø§Ø³ØªØ›
        // Ø¯Ø± ØµÙˆØ±Øª Ø®Ø·Ø§ÛŒ Ù…ÛŒØ§Ù†ÛŒ Ø¨Ø§ÛŒØ¯ Ú©Ù„ Ø¹Ù…Ù„ÛŒØ§Øª Ø¨Ø±Ú¯Ø±Ø¯Ø¯ ØªØ§ Ù†Ø±Ø®â€ŒÙ‡Ø§ Ù†Ø§Ø³Ø§Ø²Ú¯Ø§Ø± (Ø¨Ø®Ø´ÛŒ Ù‚Ø¯ÛŒÙ…/Ø¨Ø®Ø´ÛŒ Ø¬Ø¯ÛŒØ¯) Ù†Ù…Ø§Ù†Ù†Ø¯.
        await using var setBaseTransaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        await ClearBaseCurrencyFlagsAsync(cancellationToken);

        currency.IsBaseCurrency = true;
        currency.UpdatedAt = DateTime.Now;
        currency.IsUpdated = true;
        currency.UpdatedBy = userId;

        // Ø§Ø±Ø² Ù¾Ø§ÛŒÙ‡ Ù†Ø¨Ø§ÛŒØ¯ Ø¯Ø± Ø¬Ø¯ÙˆÙ„ Ù†Ø±Ø® Ø±Ø¯ÛŒÙ Ø¯Ø§Ø´ØªÙ‡ Ø¨Ø§Ø´Ø¯Ø› Ù†Ø±Ø® Ù‚Ø¨Ù„ÛŒ Ø§Ø±Ø² Ù¾Ø§ÛŒÙ‡â€ŒÛŒ Ø¬Ø¯ÛŒØ¯ Ø­Ø°Ù Ùˆ ØªØ§Ø±ÛŒØ®Ú†Ù‡â€ŒÛŒ Ø¨Ø§Ø² Ø¢Ù† Ø¨Ø³ØªÙ‡ Ù…ÛŒâ€ŒØ´ÙˆØ¯.
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

        // Ú†Ø±Ø§ Ø¨Ø§Ø²Ù…Ø­Ø§Ø³Ø¨Ù‡: Ù¾Ø³ Ø§Ø² ØªØºÛŒÛŒØ± Ù¾Ø§ÛŒÙ‡ØŒ Ù†Ø±Ø® Ù‡Ù…Ù‡â€ŒÛŒ Ø§Ø±Ø²Ù‡Ø§ Ø¨Ø§ÛŒØ¯ Ù†Ø³Ø¨Øª Ø¨Ù‡ Ù¾Ø§ÛŒÙ‡â€ŒÛŒ Ø¬Ø¯ÛŒØ¯ Ø¨ÛŒØ§Ù† Ø´ÙˆØ¯.
        // Ù†Ø±Ø® Ø§Ø±Ø² X Ù†Ø³Ø¨Øª Ø¨Ù‡ Ù¾Ø§ÛŒÙ‡â€ŒÛŒ Ø¬Ø¯ÛŒØ¯ = (Ù†Ø±Ø® X Ù†Ø³Ø¨Øª Ø¨Ù‡ Ù¾Ø§ÛŒÙ‡â€ŒÛŒ Ù‚Ø¯ÛŒÙ…) Ã· rNewØ› Ùˆ Ù†Ø±Ø® Ù¾Ø§ÛŒÙ‡â€ŒÛŒ Ù‚Ø¯ÛŒÙ… Ù†Ø³Ø¨Øª Ø¨Ù‡ Ù¾Ø§ÛŒÙ‡â€ŒÛŒ Ø¬Ø¯ÛŒØ¯ = 1 Ã· rNew.
        var reason = $"Ø¨Ø§Ø²Ù…Ø­Ø§Ø³Ø¨Ù‡ Ù†Ø±Ø® Ø¨Ù‡â€ŒØ¯Ù„ÛŒÙ„ ØªØºÛŒÛŒØ± Ø§Ø±Ø² Ù¾Ø§ÛŒÙ‡ Ø¨Ù‡ Â«{currency.Name}Â»";
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

        return Ok(new { message = "Ø§Ø±Ø² Ù¾Ø§ÛŒÙ‡ Ø¨Ø§ Ù…ÙˆÙÙ‚ÛŒØª ØªØºÛŒÛŒØ± Ú©Ø±Ø¯ Ùˆ Ù†Ø±Ø® Ø³Ø§ÛŒØ± Ø§Ø±Ø²Ù‡Ø§ Ø¨Ø§Ø²Ù…Ø­Ø§Ø³Ø¨Ù‡ Ø´Ø¯." });
    }

    // Ú†Ø±Ø§ setRate: Ø«Ø¨Øª Ù†Ø±Ø® Ø§Ø±Ø²ØŒ extra action ØµÙØ­Ù‡â€ŒÛŒ Ø§Ø±Ø²Ù‡Ø§Ø³Øª.
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
            return NotFound(new { message = "Ø§Ø±Ø² ÛŒØ§ÙØª Ù†Ø´Ø¯." });
        }

        if (currency.IsBaseCurrency)
        {
            return BadRequest(new { message = "Ø¨Ø±Ø§ÛŒ Ø§Ø±Ø² Ù¾Ø§ÛŒÙ‡ Ù†Ø±Ø® ØªØ¨Ø¯ÛŒÙ„ Ø«Ø¨Øª Ù†Ù…ÛŒâ€ŒØ´ÙˆØ¯." });
        }

        var baseCurrency = await GetBaseCurrencyEntityAsync(cancellationToken);
        if (baseCurrency is null)
        {
            return BadRequest(new { message = "Ø§Ø±Ø² Ù¾Ø§ÛŒÙ‡ ØªØ¹Ø±ÛŒÙ Ù†Ø´Ø¯Ù‡ Ø§Ø³Øª." });
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

        return Ok(new { message = "Ù†Ø±Ø® Ø§Ø±Ø² Ø¨Ø§ Ù…ÙˆÙÙ‚ÛŒØª Ø«Ø¨Øª Ø´Ø¯." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("currencies.list.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var currency = await _db.Currencies
            .FirstOrDefaultAsync(c => c.CurrencyID == id && c.IsDeleted != true, cancellationToken);

        if (currency is null)
        {
            return NotFound(new { message = "Ø§Ø±Ø² ÛŒØ§ÙØª Ù†Ø´Ø¯." });
        }

        if (currency.IsBaseCurrency)
        {
            return BadRequest(new { message = "Ø§Ø±Ø² Ù¾Ø§ÛŒÙ‡ Ù‚Ø§Ø¨Ù„ Ø­Ø°Ù Ù†ÛŒØ³Øª." });
        }

        var hasRates = await _db.CurrencyExchangeRates.AnyAsync(r => r.CurrencyID == id, cancellationToken);
        if (hasRates)
        {
            return BadRequest(new { message = "Ø§ÛŒÙ† Ø§Ø±Ø² Ù†Ø±Ø® ØªØ¨Ø¯ÛŒÙ„ Ø¯Ø§Ø±Ø¯ Ùˆ Ù‚Ø§Ø¨Ù„ Ø­Ø°Ù Ù†ÛŒØ³Øª." });
        }

        currency.IsDeleted = true;
        currency.IsActive = false;
        currency.DeletedAt = DateTime.Now;
        currency.DeletedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);

        await _currencySync.PushLocalCurrencyToReferenceAsync(currency.CurrencyCode, cancellationToken);

        return Ok(new { message = "Ø§Ø±Ø² Ø¨Ø§ Ù…ÙˆÙÙ‚ÛŒØª Ø­Ø°Ù Ø´Ø¯." });
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
        [Required(ErrorMessage = "Ù†Ø§Ù… Ø§Ø±Ø² Ø§Ù„Ø²Ø§Ù…ÛŒ Ø§Ø³Øª.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ù†Ù…Ø§Ø¯ Ø§Ø±Ø² Ø§Ù„Ø²Ø§Ù…ÛŒ Ø§Ø³Øª.")]
        [MaxLength(10)]
        public string Symbol { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ú©Ø¯ Ø§Ø±Ø² Ø§Ù„Ø²Ø§Ù…ÛŒ Ø§Ø³Øª.")]
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
        [Range(0.00000001, double.MaxValue, ErrorMessage = "Ù†Ø±Ø® Ø¨Ø§ÛŒØ¯ Ø¨Ø²Ø±Ú¯â€ŒØªØ± Ø§Ø² ØµÙØ± Ø¨Ø§Ø´Ø¯.")]
        public decimal BaseUnitsPerUnit { get; set; }

        [MaxLength(500)]
        public string? ChangeReason { get; set; }

        public DateTime? EffectiveFrom { get; set; }
    }
}

