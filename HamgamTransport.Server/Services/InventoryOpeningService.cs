using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Services;

public record InventoryOpeningLine(int ProductId, decimal QuantityInBase, decimal UnitCost);

public interface IInventoryOpeningService
{
    Task<JournalEntry> PostOpeningAsync(
        int warehouseId,
        IReadOnlyList<InventoryOpeningLine> lines,
        DateTime? openingDate,
        int? userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// موجودی اول دوره — ایجاد Lot و سند دوطرفه (بدهکار موجودی / بستانکار افتتاحیه).
/// </summary>
public class InventoryOpeningService : IInventoryOpeningService
{
    private readonly AppDbContext _db;
    private readonly IFifoInventoryService _fifo;
    private readonly IJournalPostingService _journal;
    private readonly IAccountLookupService _accounts;
    private readonly ICurrencyConversionService _currencies;

    public InventoryOpeningService(
        AppDbContext db,
        IFifoInventoryService fifo,
        IJournalPostingService journal,
        IAccountLookupService accounts,
        ICurrencyConversionService currencies)
    {
        _db = db;
        _fifo = fifo;
        _journal = journal;
        _accounts = accounts;
        _currencies = currencies;
    }

    public async Task<JournalEntry> PostOpeningAsync(
        int warehouseId,
        IReadOnlyList<InventoryOpeningLine> lines,
        DateTime? openingDate,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        if (lines is null || lines.Count == 0)
        {
            throw new InvalidOperationException("حداقل یک ردیف موجودی اول دوره الزامی است.");
        }

        var warehouse = await _db.Warehouses
            .FirstOrDefaultAsync(w => w.WarehouseID == warehouseId && w.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("انبار یافت نشد.");

        var openingIds = await _db.JournalEntries
            .Where(e => e.IsDeleted != true
                        && e.IsPosted
                        && e.Source == JournalSource.InventoryOpening
                        && e.SourceId == warehouseId)
            .Select(e => e.JournalEntryID)
            .ToListAsync(cancellationToken);

        if (openingIds.Count > 0)
        {
            var reversedIds = await _db.JournalEntries
                .Where(e => e.IsDeleted != true
                            && e.Source == JournalSource.ManualReversal
                            && e.SourceId != null
                            && openingIds.Contains(e.SourceId.Value))
                .Select(e => e.SourceId!.Value)
                .ToListAsync(cancellationToken);

            if (openingIds.Any(id => !reversedIds.Contains(id)))
            {
                throw new InvalidOperationException("موجودی اول‌دوره این انبار قبلاً در دفتر ثبت شده است.");
            }
        }

        var date = (openingDate ?? DateTime.Today).Date;
        var inventoryAccountId = await _accounts.ResolveInventoryAccountIdAsync(warehouse.WarehouseType, cancellationToken);
        var openingAccount = await _accounts.GetBySystemCodeAsync(AccountSystemCode.EquityOpening, cancellationToken);
        var baseCurrency = await _currencies.GetBaseCurrencyAsync(cancellationToken);

        foreach (var line in lines)
        {
            if (line.ProductId <= 0 || line.QuantityInBase <= 0 || line.UnitCost < 0)
            {
                throw new InvalidOperationException("ردیف موجودی اول دوره نامعتبر است.");
            }
        }

        var totalCost = lines.Sum(l => Math.Round(l.QuantityInBase * l.UnitCost, 4));
        if (totalCost <= 0)
        {
            throw new InvalidOperationException("جمع بهای موجودی اول دوره باید بزرگ‌تر از صفر باشد.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var line in lines)
        {
            await _fifo.ReceiveAsync(new ReceiveStockRequest
            {
                ProductId = line.ProductId,
                WarehouseId = warehouseId,
                QuantityInBase = line.QuantityInBase,
                UnitCost = line.UnitCost,
                ReceivedAt = date,
                CreatedBy = userId,
            }, cancellationToken);
        }

        var journalLines = new List<JournalLineDraft>
        {
            new(inventoryAccountId, totalCost, 0, totalCost, 0, baseCurrency.CurrencyID,
                $"موجودی اول دوره — {warehouse.Name}"),
            new(openingAccount.AccountID, 0, totalCost, 0, totalCost, baseCurrency.CurrencyID,
                $"طرف مقابل موجودی اول دوره — {warehouse.Name}"),
        };

        var entry = await _journal.PostAsync(
            date,
            $"موجودی اول دوره — {warehouse.Name}",
            JournalSource.InventoryOpening,
            warehouseId,
            baseCurrency.CurrencyID,
            journalLines,
            userId,
            cancellationToken);

        await tx.CommitAsync(cancellationToken);
        return entry;
    }
}
