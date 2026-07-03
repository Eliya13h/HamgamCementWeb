using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Inventory;
using HamgamCementWeb.Server.Data.Models.Product;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Services;

public record FifoAllocation(
    int InventoryLotId,
    string LotCode,
    decimal QuantityInBase,
    decimal UnitCost,
    decimal LineCost,
    int? PurchaseInvoiceId = null);

public class ReceiveStockRequest
{
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public decimal QuantityInBase { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public int? CreatedBy { get; set; }
    public int? PurchaseInvoiceId { get; set; }
    public int? PurchaseItemId { get; set; }
    public int? ProductionBatchId { get; set; }
}

public class AllocateStockRequest
{
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public decimal QuantityInBase { get; set; }
}

/// <summary>
/// مدیریت Lotها برای FIFO — آماده اتصال به خرید/فروش
/// </summary>
public interface IFifoInventoryService
{
    Task<InventoryLot> ReceiveAsync(ReceiveStockRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FifoAllocation>> PreviewAllocationAsync(
        AllocateStockRequest request,
        bool allowInsufficientStock = false,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FifoAllocation>> AllocateAndApplyAsync(
        AllocateStockRequest request,
        bool allowInsufficientStock = false,
        CancellationToken cancellationToken = default);
    Task ValidateAvailableStockAsync(
        int warehouseId,
        IReadOnlyList<AllocateStockRequest> lines,
        CancellationToken cancellationToken = default);
    Task ReturnFromLotAsync(
        int inventoryLotId,
        decimal quantityInBase,
        CancellationToken cancellationToken = default);
    Task RestoreToLotAsync(
        int inventoryLotId,
        decimal quantityInBase,
        CancellationToken cancellationToken = default);
}

public class FifoInventoryService : IFifoInventoryService
{
    private readonly AppDbContext _db;

    public FifoInventoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<InventoryLot> ReceiveAsync(
        ReceiveStockRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.QuantityInBase <= 0)
        {
            throw new InvalidOperationException("مقدار دریافت باید بزرگ‌تر از صفر باشد.");
        }

        var productExists = await _db.Products
            .AnyAsync(p => p.ProductID == request.ProductId && p.IsDeleted != true, cancellationToken);
        if (!productExists)
        {
            throw new InvalidOperationException("محصول یافت نشد.");
        }

        var sequence = await _db.InventoryLots
            .Where(l => l.IsDeleted != true)
            .Select(l => (long?)l.ReceiptSequence)
            .MaxAsync(cancellationToken) ?? 0;

        var lot = new InventoryLot
        {
            LotCode = "TEMP",
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            ReceivedAt = request.ReceivedAt ?? DateTime.Now,
            ReceiptSequence = sequence + 1,
            ReceivedQuantityInBase = request.QuantityInBase,
            RemainingQuantityInBase = request.QuantityInBase,
            UnitCost = request.UnitCost,
            PurchaseInvoiceId = request.PurchaseInvoiceId,
            PurchaseItemId = request.PurchaseItemId,
            ProductionBatchId = request.ProductionBatchId,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = request.CreatedBy,
        };

        _db.InventoryLots.Add(lot);
        await _db.SaveChangesAsync(cancellationToken);

        lot.LotCode = InventoryLotCodeHelper.ForLot(lot.InventoryLotID);
        await UpsertStockAsync(lot, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return lot;
    }

    public async Task<IReadOnlyList<FifoAllocation>> PreviewAllocationAsync(
        AllocateStockRequest request,
        bool allowInsufficientStock = false,
        CancellationToken cancellationToken = default)
    {
        return await BuildAllocationsAsync(request, apply: false, allowInsufficientStock, cancellationToken);
    }

    public async Task<IReadOnlyList<FifoAllocation>> AllocateAndApplyAsync(
        AllocateStockRequest request,
        bool allowInsufficientStock = false,
        CancellationToken cancellationToken = default)
    {
        return await BuildAllocationsAsync(request, apply: true, allowInsufficientStock, cancellationToken);
    }

    public async Task ValidateAvailableStockAsync(
        int warehouseId,
        IReadOnlyList<AllocateStockRequest> lines,
        CancellationToken cancellationToken = default)
    {
        foreach (var line in lines)
        {
            if (line.QuantityInBase <= 0)
            {
                continue;
            }

            var product = await _db.Products
                .AsNoTracking()
                .Where(p => p.ProductID == line.ProductId && p.IsDeleted != true)
                .Select(p => new { p.Name, p.Code })
                .FirstOrDefaultAsync(cancellationToken);

            var available = await _db.InventoryStocks
                .AsNoTracking()
                .Where(s =>
                    s.WarehouseId == warehouseId &&
                    s.ProductId == line.ProductId &&
                    s.IsDeleted != true)
                .Select(s => (decimal?)s.QuantityInBase)
                .FirstOrDefaultAsync(cancellationToken) ?? 0m;

            if (line.QuantityInBase > available + 0.000001m)
            {
                var label = product?.Name ?? product?.Code ?? line.ProductId.ToString();
                throw new InvalidOperationException($"موجودی کافی برای «{label}» در انبار انتخاب‌شده وجود ندارد.");
            }
        }
    }

    public async Task ReturnFromLotAsync(
        int inventoryLotId,
        decimal quantityInBase,
        CancellationToken cancellationToken = default)
    {
        if (quantityInBase <= 0)
        {
            throw new InvalidOperationException("مقدار برگشت باید بزرگ‌تر از صفر باشد.");
        }

        var lot = await _db.InventoryLots
            .FirstOrDefaultAsync(l => l.InventoryLotID == inventoryLotId && l.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("دسته موجودی یافت نشد.");

        if (lot.RemainingQuantityInBase < quantityInBase)
        {
            throw new InvalidOperationException("موجودی کافی در دسته خرید برای برگشت وجود ندارد.");
        }

        lot.RemainingQuantityInBase -= quantityInBase;
        lot.IsUpdated = true;
        lot.UpdatedAt = DateTime.Now;

        await ApplyStockReductionAsync(new AllocateStockRequest
        {
            ProductId = lot.ProductId,
            WarehouseId = lot.WarehouseId,
            QuantityInBase = quantityInBase,
        }, allowInsufficientStock: false, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreToLotAsync(
        int inventoryLotId,
        decimal quantityInBase,
        CancellationToken cancellationToken = default)
    {
        if (quantityInBase <= 0)
        {
            throw new InvalidOperationException("مقدار برگشت باید بزرگ‌تر از صفر باشد.");
        }

        var lot = await _db.InventoryLots
            .FirstOrDefaultAsync(l => l.InventoryLotID == inventoryLotId && l.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("دسته موجودی یافت نشد.");

        lot.RemainingQuantityInBase += quantityInBase;
        lot.IsUpdated = true;
        lot.UpdatedAt = DateTime.Now;

        var stock = await _db.InventoryStocks
            .FirstOrDefaultAsync(
                s => s.WarehouseId == lot.WarehouseId &&
                     s.ProductId == lot.ProductId &&
                     s.IsDeleted != true,
                cancellationToken);

        if (stock is null)
        {
            _db.InventoryStocks.Add(new InventoryStock
            {
                WarehouseId = lot.WarehouseId,
                ProductId = lot.ProductId,
                QuantityInBase = quantityInBase,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
            });
        }
        else
        {
            stock.QuantityInBase += quantityInBase;
            stock.IsUpdated = true;
            stock.UpdatedAt = DateTime.Now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<FifoAllocation>> BuildAllocationsAsync(
        AllocateStockRequest request,
        bool apply,
        bool allowInsufficientStock,
        CancellationToken cancellationToken)
    {
        if (request.QuantityInBase <= 0)
        {
            throw new InvalidOperationException("مقدار خروج باید بزرگ‌تر از صفر باشد.");
        }

        var lots = await _db.InventoryLots
            .Where(l =>
                l.IsDeleted != true &&
                l.ProductId == request.ProductId &&
                l.WarehouseId == request.WarehouseId &&
                l.RemainingQuantityInBase > 0)
            .OrderBy(l => l.ReceiptSequence)
            .ThenBy(l => l.ReceivedAt)
            .ToListAsync(cancellationToken);

        var remaining = request.QuantityInBase;
        var allocations = new List<FifoAllocation>();

        foreach (var lot in lots)
        {
            if (remaining <= 0)
            {
                break;
            }

            var take = Math.Min(lot.RemainingQuantityInBase, remaining);
            if (take <= 0)
            {
                continue;
            }

            allocations.Add(new FifoAllocation(
                lot.InventoryLotID,
                lot.LotCode,
                take,
                lot.UnitCost,
                take * lot.UnitCost,
                lot.PurchaseInvoiceId));

            if (apply)
            {
                lot.RemainingQuantityInBase -= take;
                lot.IsUpdated = true;
                lot.UpdatedAt = DateTime.Now;
            }

            remaining -= take;
        }

        if (remaining > 0 && !allowInsufficientStock)
        {
            throw new InvalidOperationException("موجودی کافی برای تخصیص FIFO وجود ندارد.");
        }

        if (apply)
        {
            await ApplyStockReductionAsync(request, allowInsufficientStock, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return allocations;
    }

    private async Task UpsertStockAsync(InventoryLot lot, CancellationToken cancellationToken)
    {
        var stock = await _db.InventoryStocks
            .FirstOrDefaultAsync(
                s => s.WarehouseId == lot.WarehouseId &&
                     s.ProductId == lot.ProductId &&
                     s.IsDeleted != true,
                cancellationToken);

        if (stock is null)
        {
            _db.InventoryStocks.Add(new InventoryStock
            {
                WarehouseId = lot.WarehouseId,
                ProductId = lot.ProductId,
                QuantityInBase = lot.ReceivedQuantityInBase,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreatedBy = lot.CreatedBy,
            });
        }
        else
        {
            stock.QuantityInBase += lot.ReceivedQuantityInBase;
            stock.IsUpdated = true;
            stock.UpdatedAt = DateTime.Now;
        }
    }

    private async Task ApplyStockReductionAsync(
        AllocateStockRequest request,
        bool allowInsufficientStock,
        CancellationToken cancellationToken)
    {
        var stock = await _db.InventoryStocks
            .FirstOrDefaultAsync(
                s => s.WarehouseId == request.WarehouseId &&
                     s.ProductId == request.ProductId &&
                     s.IsDeleted != true,
                cancellationToken);

        if (stock is null)
        {
            if (!allowInsufficientStock)
            {
                throw new InvalidOperationException("موجودی انبار با Lotها هم‌خوان نیست.");
            }

            _db.InventoryStocks.Add(new InventoryStock
            {
                WarehouseId = request.WarehouseId,
                ProductId = request.ProductId,
                QuantityInBase = -request.QuantityInBase,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
            });
            return;
        }

        if (!allowInsufficientStock && stock.QuantityInBase < request.QuantityInBase)
        {
            throw new InvalidOperationException("موجودی انبار با Lotها هم‌خوان نیست.");
        }

        stock.QuantityInBase -= request.QuantityInBase;
        stock.IsUpdated = true;
        stock.UpdatedAt = DateTime.Now;
    }
}
