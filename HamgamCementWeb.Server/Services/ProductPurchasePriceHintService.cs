using HamgamCementWeb.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Services;

public enum ProductPurchasePriceSource
{
    None = 0,
    WeightedAverageStock = 1,
    LastLot = 2,
    LastPurchaseInvoice = 3,
}

public sealed record ProductPurchasePriceHint(
    decimal? UnitCostInBase,
    ProductPurchasePriceSource Source);

public interface IProductPurchasePriceHintService
{
    Task<ProductPurchasePriceHint> GetHintAsync(
        int productId,
        int? warehouseId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, ProductPurchasePriceHint>> GetHintsAsync(
        IEnumerable<int> productIds,
        int? warehouseId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// پیشنهاد لحظه‌ای قیمت خرید از میانگین موزون لات‌های موجود، آخرین لات، یا آخرین فاکتور خرید.
/// روی Product ذخیره نمی‌شود.
/// </summary>
public sealed class ProductPurchasePriceHintService(AppDbContext db) : IProductPurchasePriceHintService
{
    public async Task<ProductPurchasePriceHint> GetHintAsync(
        int productId,
        int? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        var map = await GetHintsAsync([productId], warehouseId, cancellationToken);
        return map.TryGetValue(productId, out var hint)
            ? hint
            : new ProductPurchasePriceHint(null, ProductPurchasePriceSource.None);
    }

    public async Task<IReadOnlyDictionary<int, ProductPurchasePriceHint>> GetHintsAsync(
        IEnumerable<int> productIds,
        int? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        var ids = productIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, ProductPurchasePriceHint>();
        }

        var result = ids.ToDictionary(
            id => id,
            _ => new ProductPurchasePriceHint(null, ProductPurchasePriceSource.None));

        var stockLotsQuery = db.InventoryLots
            .AsNoTracking()
            .Where(l =>
                ids.Contains(l.ProductId) &&
                l.IsDeleted != true &&
                l.RemainingQuantityInBase > 0);

        if (warehouseId is > 0)
        {
            stockLotsQuery = stockLotsQuery.Where(l => l.WarehouseId == warehouseId.Value);
        }

        var stockLots = await stockLotsQuery
            .Select(l => new
            {
                l.ProductId,
                l.RemainingQuantityInBase,
                l.UnitCost,
            })
            .ToListAsync(cancellationToken);

        foreach (var group in stockLots.GroupBy(l => l.ProductId))
        {
            var totalQty = group.Sum(x => x.RemainingQuantityInBase);
            if (totalQty <= 0)
            {
                continue;
            }

            var avg = group.Sum(x => x.RemainingQuantityInBase * x.UnitCost) / totalQty;
            result[group.Key] = new ProductPurchasePriceHint(
                Math.Round(avg, 4, MidpointRounding.AwayFromZero),
                ProductPurchasePriceSource.WeightedAverageStock);
        }

        var missingAfterStock = result
            .Where(kv => kv.Value.UnitCostInBase is null)
            .Select(kv => kv.Key)
            .ToList();

        if (missingAfterStock.Count > 0)
        {
            var lotRows = await db.InventoryLots
                .AsNoTracking()
                .Where(l =>
                    missingAfterStock.Contains(l.ProductId) &&
                    l.IsDeleted != true)
                .Select(l => new
                {
                    l.ProductId,
                    l.ReceiptSequence,
                    l.UnitCost,
                })
                .ToListAsync(cancellationToken);

            foreach (var group in lotRows.GroupBy(l => l.ProductId))
            {
                var last = group.OrderByDescending(x => x.ReceiptSequence).First();
                result[group.Key] = new ProductPurchasePriceHint(
                    last.UnitCost,
                    ProductPurchasePriceSource.LastLot);
            }
        }

        var stillMissing = result
            .Where(kv => kv.Value.UnitCostInBase is null)
            .Select(kv => kv.Key)
            .ToList();

        if (stillMissing.Count > 0)
        {
            var purchaseRows = await db.PurchaseItems
                .AsNoTracking()
                .Where(i =>
                    stillMissing.Contains(i.ProductId) &&
                    i.IsDeleted != true &&
                    i.QuantityInBase > 0 &&
                    i.Invoice.IsDeleted != true &&
                    i.Invoice.IsPosted &&
                    i.Invoice.DocumentType == InvoiceDocumentType.Invoice)
                .Select(i => new
                {
                    i.ProductId,
                    i.Invoice.InvoiceDate,
                    i.PurchaseItemID,
                    UnitCost = i.LineTotalInBaseCurrency / i.QuantityInBase,
                })
                .ToListAsync(cancellationToken);

            foreach (var group in purchaseRows.GroupBy(i => i.ProductId))
            {
                var last = group
                    .OrderByDescending(x => x.InvoiceDate)
                    .ThenByDescending(x => x.PurchaseItemID)
                    .First();
                result[group.Key] = new ProductPurchasePriceHint(
                    Math.Round(last.UnitCost, 4, MidpointRounding.AwayFromZero),
                    ProductPurchasePriceSource.LastPurchaseInvoice);
            }
        }

        return result;
    }
}
