using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Inventory;
using HamgamCementWeb.Server.Data.Models.Production;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Services;

public record ProductionTraceLot(
    int InventoryLotId,
    string LotCode,
    int ProductId,
    string ProductName,
    decimal QuantityInBase,
    decimal UnitCost,
    int? PurchaseInvoiceId,
    string? PurchaseInvoiceNumber);

public record ProductionTracePurchase(
    int PurchaseInvoiceId,
    string InvoiceNumber,
    DateTime InvoiceDate,
    decimal TotalAmount,
    bool IsPosted);

public record ProductionTraceResult(
    int ProductionBatchId,
    string BatchNumber,
    DateTime ProductionDate,
    string OutputWarehouseName,
    decimal TotalMaterialCostInBase,
    decimal FixedCost,
    decimal VariableCost,
    bool IsTransferredToSales,
    IReadOnlyList<object> InputLines,
    IReadOnlyList<object> OutputLines,
    IReadOnlyList<ProductionTracePurchase> PurchaseInvoices,
    IReadOnlyList<ProductionTraceLot> InventoryLots);

public interface IProductionPostingService
{
    Task PostBatchAsync(int productionBatchId, int? userId, CancellationToken cancellationToken = default);
    Task<ProductionTraceResult> GetTraceAsync(int productionBatchId, CancellationToken cancellationToken = default);
}

public class ProductionPostingService : IProductionPostingService
{
    private readonly AppDbContext _db;
    private readonly IMeaurmentConversionService _conversion;
    private readonly IFifoInventoryService _fifo;

    public ProductionPostingService(
        AppDbContext db,
        IMeaurmentConversionService conversion,
        IFifoInventoryService fifo)
    {
        _db = db;
        _conversion = conversion;
        _fifo = fifo;
    }

    public async Task PostBatchAsync(int productionBatchId, int? userId, CancellationToken cancellationToken = default)
    {
        var batch = await _db.ProductionBatches
            .Include(b => b.InputLines.Where(x => x.IsDeleted != true))
            .Include(b => b.OutputLines.Where(x => x.IsDeleted != true))
            .Include(b => b.OutputWarehouse)
            .FirstOrDefaultAsync(b => b.ProductionBatchID == productionBatchId && b.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("سند تولید یافت نشد.");

        if (batch.IsPosted)
        {
            throw new InvalidOperationException("این سند تولید قبلاً ثبت نهایی شده است.");
        }

        if (batch.InputLines.Count == 0 || batch.OutputLines.Count == 0)
        {
            throw new InvalidOperationException("سند تولید باید حداقل یک ردیف مصرف و یک ردیف تولید داشته باشد.");
        }

        if (batch.OutputWarehouse.WarehouseType != WarehouseType.Processed)
        {
            throw new InvalidOperationException("انبار مقصد باید از نوع مواد پردازش‌شده باشد.");
        }

        decimal totalMaterialCost = 0;
        var now = DateTime.Now;

        foreach (var line in batch.InputLines)
        {
            var warehouse = await _db.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.WarehouseID == line.WarehouseId && w.IsDeleted != true, cancellationToken)
                ?? throw new InvalidOperationException("انبار مصرف یافت نشد.");

            if (warehouse.WarehouseType is not (WarehouseType.RawMaterials or WarehouseType.SemiFinished))
            {
                throw new InvalidOperationException($"انبار «{warehouse.Name}» برای مصرف تولید مجاز نیست. فقط مواد خام و نیمه‌خام.");
            }

            line.QuantityInBase = await _conversion.ToBaseAsync(line.Quantity, line.MeaurmentId, cancellationToken);
            if (line.QuantityInBase <= 0)
            {
                throw new InvalidOperationException("مقدار مصرف باید بزرگ‌تر از صفر باشد.");
            }

            var allocations = await _fifo.AllocateAndApplyAsync(new AllocateStockRequest
            {
                ProductId = line.ProductId,
                WarehouseId = line.WarehouseId,
                QuantityInBase = line.QuantityInBase,
            }, allowInsufficientStock: false, cancellationToken);

            line.MaterialCostInBase = allocations.Sum(a => a.LineCost);
            totalMaterialCost += line.MaterialCostInBase;
            line.IsUpdated = true;
            line.UpdatedAt = now;
            line.UpdatedBy = userId;
        }

        var totalOutputBase = batch.OutputLines.Sum(o => o.QuantityInBase);
        if (totalOutputBase <= 0)
        {
            foreach (var line in batch.OutputLines)
            {
                line.QuantityInBase = await _conversion.ToBaseAsync(line.Quantity, line.MeaurmentId, cancellationToken);
            }

            totalOutputBase = batch.OutputLines.Sum(o => o.QuantityInBase);
        }

        if (totalOutputBase <= 0)
        {
            throw new InvalidOperationException("مجموع مقدار تولید باید بزرگ‌تر از صفر باشد.");
        }

        var totalProductionCost = totalMaterialCost + batch.FixedCost + batch.VariableCost;

        foreach (var line in batch.OutputLines)
        {
            if (line.QuantityInBase <= 0)
            {
                line.QuantityInBase = await _conversion.ToBaseAsync(line.Quantity, line.MeaurmentId, cancellationToken);
            }

            if (line.QuantityInBase <= 0)
            {
                throw new InvalidOperationException("مقدار تولید باید بزرگ‌تر از صفر باشد.");
            }

            var share = line.QuantityInBase / totalOutputBase;
            var lineCost = totalProductionCost * share;
            line.UnitCostInBase = lineCost / line.QuantityInBase;

            var lot = await _fifo.ReceiveAsync(new ReceiveStockRequest
            {
                ProductId = line.ProductId,
                WarehouseId = batch.OutputWarehouseId,
                QuantityInBase = line.QuantityInBase,
                UnitCost = line.UnitCostInBase,
                ReceivedAt = batch.ProductionDate,
                CreatedBy = userId,
                ProductionBatchId = batch.ProductionBatchID,
            }, cancellationToken);

            line.InventoryLotId = lot.InventoryLotID;
            line.IsUpdated = true;
            line.UpdatedAt = now;
            line.UpdatedBy = userId;
        }

        batch.TotalMaterialCostInBase = totalMaterialCost;
        batch.Status = ProductionBatchStatus.Posted;
        batch.IsPosted = true;
        batch.PostedAt = now;
        batch.IsUpdated = true;
        batch.UpdatedAt = now;
        batch.UpdatedBy = userId;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductionTraceResult> GetTraceAsync(int productionBatchId, CancellationToken cancellationToken = default)
    {
        var batch = await _db.ProductionBatches
            .AsNoTracking()
            .Where(b => b.ProductionBatchID == productionBatchId && b.IsDeleted != true)
            .Select(b => new
            {
                b.ProductionBatchID,
                b.BatchNumber,
                b.ProductionDate,
                OutputWarehouseName = b.OutputWarehouse.Name,
                b.TotalMaterialCostInBase,
                b.FixedCost,
                b.VariableCost,
                b.IsTransferredToSales,
                InputLines = b.InputLines
                    .Where(x => x.IsDeleted != true)
                    .Select(x => new
                    {
                        x.ProductionInputLineID,
                        x.WarehouseId,
                        warehouseName = x.Warehouse.Name,
                        x.ProductId,
                        productName = x.Product.Name,
                        x.Quantity,
                        x.QuantityInBase,
                        meaurmentName = x.Meaurment.Name,
                        x.MaterialCostInBase,
                    })
                    .ToList(),
                OutputLines = b.OutputLines
                    .Where(x => x.IsDeleted != true)
                    .Select(x => new
                    {
                        x.ProductionOutputLineID,
                        x.ProductId,
                        productName = x.Product.Name,
                        x.Quantity,
                        x.QuantityInBase,
                        meaurmentName = x.Meaurment.Name,
                        x.UnitCostInBase,
                        x.InventoryLotId,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("سند تولید یافت نشد.");

        var purchaseInvoices = await _db.PurchaseInvoices
            .AsNoTracking()
            .Where(i => i.ProductionBatchId == productionBatchId && i.IsDeleted != true)
            .Select(i => new ProductionTracePurchase(
                i.PurchaseInvoiceID,
                i.InvoiceNumber,
                i.InvoiceDate,
                i.TotalAmount,
                i.IsPosted))
            .ToListAsync(cancellationToken);

        var lotsRaw = await _db.InventoryLots
            .AsNoTracking()
            .Where(l => l.ProductionBatchId == productionBatchId && l.IsDeleted != true)
            .Select(l => new
            {
                l.InventoryLotID,
                l.LotCode,
                l.ProductId,
                ProductName = l.Product.Name,
                l.RemainingQuantityInBase,
                l.UnitCost,
                l.PurchaseInvoiceId,
            })
            .ToListAsync(cancellationToken);

        var purchaseIds = lotsRaw
            .Where(l => l.PurchaseInvoiceId.HasValue)
            .Select(l => l.PurchaseInvoiceId!.Value)
            .Distinct()
            .ToList();

        var invoiceNumbers = purchaseIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.PurchaseInvoices
                .AsNoTracking()
                .Where(i => purchaseIds.Contains(i.PurchaseInvoiceID))
                .ToDictionaryAsync(i => i.PurchaseInvoiceID, i => i.InvoiceNumber, cancellationToken);

        var lots = lotsRaw
            .Select(l => new ProductionTraceLot(
                l.InventoryLotID,
                l.LotCode,
                l.ProductId,
                l.ProductName,
                l.RemainingQuantityInBase,
                l.UnitCost,
                l.PurchaseInvoiceId,
                l.PurchaseInvoiceId.HasValue && invoiceNumbers.TryGetValue(l.PurchaseInvoiceId.Value, out var num)
                    ? num
                    : null))
            .ToList();

        return new ProductionTraceResult(
            batch.ProductionBatchID,
            batch.BatchNumber,
            batch.ProductionDate,
            batch.OutputWarehouseName,
            batch.TotalMaterialCostInBase,
            batch.FixedCost,
            batch.VariableCost,
            batch.IsTransferredToSales,
            batch.InputLines.Cast<object>().ToList(),
            batch.OutputLines.Cast<object>().ToList(),
            purchaseInvoices,
            lots);
    }
}
