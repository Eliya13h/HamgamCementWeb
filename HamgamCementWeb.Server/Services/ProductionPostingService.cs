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
    decimal RemainingQuantityInBase,
    decimal UnitCost,
    int? PurchaseInvoiceId,
    string? PurchaseInvoiceNumber);

public record ProductionTraceSale(
    int SaleInvoiceId,
    string InvoiceNumber,
    DateTime InvoiceDate,
    decimal QuantityInBase,
    int InventoryLotId,
    string LotCode);

public record ProductionTraceConsumedLot(
    int ProductionInputLineId,
    int ProductId,
    string ProductName,
    int InventoryLotId,
    string LotCode,
    decimal QuantityInBase,
    decimal UnitCostInBase,
    decimal LineCostInBase);

public record ProductionTraceResult(
    int ProductionBatchId,
    string BatchNumber,
    DateTime ProductionDate,
    string OutputWarehouseName,
    decimal TotalMaterialCostInBase,
    decimal TotalConversionCostInBase,
    decimal TotalCostInBase,
    decimal FixedCost,
    decimal VariableCost,
    int? JournalEntryId,
    IReadOnlyList<object> InputLines,
    IReadOnlyList<object> OutputLines,
    IReadOnlyList<object> CostLines,
    IReadOnlyList<ProductionTraceLot> InventoryLots,
    IReadOnlyList<ProductionTraceConsumedLot> ConsumedLots,
    IReadOnlyList<ProductionTraceSale> Sales);

public record ProductionPostPreviewInputLine(
    int ProductId,
    string ProductName,
    int WarehouseId,
    string WarehouseName,
    string MeaurmentName,
    decimal Quantity,
    decimal QuantityInBase,
    decimal EstimatedMaterialCostInBase,
    decimal AvailableQuantityInBase,
    bool HasEnoughStock);

public record ProductionPostPreviewOutputLine(
    int ProductId,
    string ProductName,
    string MeaurmentName,
    decimal Quantity,
    decimal QuantityInBase);

public record ProductionPostPreviewCostLine(
    int CostType,
    string? Description,
    decimal Amount);

public record ProductionPostPreviewResult(
    int ProductionBatchId,
    string BatchNumber,
    string OutputWarehouseName,
    bool CanPost,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<ProductionPostPreviewInputLine> InputLines,
    IReadOnlyList<ProductionPostPreviewOutputLine> OutputLines,
    IReadOnlyList<ProductionPostPreviewCostLine> CostLines,
    decimal EstimatedMaterialCostInBase,
    decimal ConversionCostInBase,
    decimal EstimatedTotalCostInBase);

public interface IProductionPostingService
{
    Task PostBatchAsync(int productionBatchId, int? userId, CancellationToken cancellationToken = default);
    Task UnpostBatchAsync(int productionBatchId, int? userId, CancellationToken cancellationToken = default);
    Task<ProductionTraceResult> GetTraceAsync(int productionBatchId, CancellationToken cancellationToken = default);
    Task<ProductionPostPreviewResult> PreviewPostAsync(int productionBatchId, CancellationToken cancellationToken = default);
}

public class ProductionPostingService : IProductionPostingService
{
    private readonly AppDbContext _db;
    private readonly IMeaurmentConversionService _conversion;
    private readonly IFifoInventoryService _fifo;
    private readonly IJournalPostingService _journal;
    private readonly IAccountLookupService _accounts;

    public ProductionPostingService(
        AppDbContext db,
        IMeaurmentConversionService conversion,
        IFifoInventoryService fifo,
        IJournalPostingService journal,
        IAccountLookupService accounts)
    {
        _db = db;
        _conversion = conversion;
        _fifo = fifo;
        _journal = journal;
        _accounts = accounts;
    }

    public static string CostTypeSystemCode(ProductionCostType costType) => costType switch
    {
        ProductionCostType.DirectWage => AccountSystemCode.ProductionWage,
        ProductionCostType.Overhead => AccountSystemCode.ProductionOverhead,
        ProductionCostType.Ancillary => AccountSystemCode.ProductionAncillary,
        ProductionCostType.Fixed => AccountSystemCode.ProductionFixed,
        _ => AccountSystemCode.OperatingExpense,
    };

    // چرا تراکنش: مصرف FIFO + ساخت Lot + سند دفتر باید اتمیک باشد.
    public async Task PostBatchAsync(int productionBatchId, int? userId, CancellationToken cancellationToken = default)
    {
        var ownsTransaction = _db.Database.CurrentTransaction is null;
        await using var tx = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await PostBatchCoreAsync(productionBatchId, userId, cancellationToken);

        if (tx is not null)
        {
            await tx.CommitAsync(cancellationToken);
        }
    }

    public async Task<ProductionPostPreviewResult> PreviewPostAsync(
        int productionBatchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await _db.ProductionBatches
            .AsNoTracking()
            .Include(b => b.InputLines)
                .ThenInclude(l => l.Product)
            .Include(b => b.InputLines)
                .ThenInclude(l => l.Warehouse)
            .Include(b => b.InputLines)
                .ThenInclude(l => l.Meaurment)
            .Include(b => b.OutputLines)
                .ThenInclude(l => l.Product)
            .Include(b => b.OutputLines)
                .ThenInclude(l => l.Meaurment)
            .Include(b => b.CostLines)
            .Include(b => b.OutputWarehouse)
            .FirstOrDefaultAsync(b => b.ProductionBatchID == productionBatchId && b.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("سند تولید یافت نشد.");

        if (batch.IsPosted)
        {
            throw new InvalidOperationException("این سند تولید قبلاً ثبت نهایی شده است.");
        }

        var inputLines = batch.InputLines.Where(x => x.IsDeleted != true).ToList();
        var outputLines = batch.OutputLines.Where(x => x.IsDeleted != true).ToList();
        var costLines = batch.CostLines.Where(x => x.IsDeleted != true).ToList();

        var warnings = new List<string>();
        if (inputLines.Count == 0 || outputLines.Count == 0)
        {
            warnings.Add("سند تولید باید حداقل یک ردیف مصرف و یک ردیف تولید داشته باشد.");
        }

        if (batch.OutputWarehouse.WarehouseType != WarehouseType.Processed)
        {
            warnings.Add("انبار مقصد باید از نوع مواد پردازش‌شده باشد.");
        }

        var previewInputs = new List<ProductionPostPreviewInputLine>();
        decimal estimatedMaterial = 0;

        foreach (var line in inputLines)
        {
            var qtyInBase = await _conversion.ToBaseAsync(line.Quantity, line.MeaurmentId, cancellationToken);
            var available = await _db.InventoryStocks
                .AsNoTracking()
                .Where(s =>
                    s.IsDeleted != true &&
                    s.ProductId == line.ProductId &&
                    s.WarehouseId == line.WarehouseId)
                .Select(s => (decimal?)s.QuantityInBase)
                .FirstOrDefaultAsync(cancellationToken) ?? 0m;

            decimal estimatedCost = 0;
            var hasEnough = available + 0.000001m >= qtyInBase;
            if (qtyInBase > 0)
            {
                try
                {
                    var allocations = await _fifo.PreviewAllocationAsync(
                        new AllocateStockRequest
                        {
                            ProductId = line.ProductId,
                            WarehouseId = line.WarehouseId,
                            QuantityInBase = qtyInBase,
                        },
                        allowInsufficientStock: true,
                        cancellationToken);
                    estimatedCost = allocations.Sum(a => a.LineCost);
                }
                catch (InvalidOperationException ex)
                {
                    warnings.Add($"{line.Product.Name}: {ex.Message}");
                    hasEnough = false;
                }
            }

            if (!hasEnough)
            {
                warnings.Add(
                    $"موجودی ناکافی برای «{line.Product.Name}» در انبار «{line.Warehouse.Name}» " +
                    $"(نیاز: {qtyInBase:N4}، موجود: {available:N4}).");
            }

            estimatedMaterial += estimatedCost;
            previewInputs.Add(new ProductionPostPreviewInputLine(
                line.ProductId,
                line.Product.Name,
                line.WarehouseId,
                line.Warehouse.Name,
                line.Meaurment.Name,
                line.Quantity,
                qtyInBase,
                estimatedCost,
                available,
                hasEnough));
        }

        var conversionCost = costLines.Sum(c => c.Amount);
        // سازگاری با اسناد قدیمی بدون CostLines
        if (conversionCost <= 0 && (batch.FixedCost > 0 || batch.VariableCost > 0))
        {
            conversionCost = batch.FixedCost + batch.VariableCost;
        }

        var outputs = new List<ProductionPostPreviewOutputLine>();
        foreach (var line in outputLines)
        {
            var qtyInBase = await _conversion.ToBaseAsync(line.Quantity, line.MeaurmentId, cancellationToken);
            outputs.Add(new ProductionPostPreviewOutputLine(
                line.ProductId,
                line.Product.Name,
                line.Meaurment.Name,
                line.Quantity,
                qtyInBase));
        }

        var canPost = warnings.Count == 0 && previewInputs.All(x => x.HasEnoughStock);

        return new ProductionPostPreviewResult(
            batch.ProductionBatchID,
            batch.BatchNumber,
            batch.OutputWarehouse.Name,
            canPost,
            warnings,
            previewInputs,
            outputs,
            costLines.Select(c => new ProductionPostPreviewCostLine(
                (int)c.CostType,
                c.Description,
                c.Amount)).ToList(),
            estimatedMaterial,
            conversionCost,
            estimatedMaterial + conversionCost);
    }

    private async Task PostBatchCoreAsync(int productionBatchId, int? userId, CancellationToken cancellationToken = default)
    {
        var batch = await _db.ProductionBatches
            .Include(b => b.InputLines.Where(x => x.IsDeleted != true))
            .Include(b => b.OutputLines.Where(x => x.IsDeleted != true))
            .Include(b => b.CostLines.Where(x => x.IsDeleted != true))
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
        var materialByWarehouseType = new Dictionary<WarehouseType, decimal>();
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
            materialByWarehouseType.TryGetValue(warehouse.WarehouseType, out var typeSum);
            materialByWarehouseType[warehouse.WarehouseType] = typeSum + line.MaterialCostInBase;

            line.IsUpdated = true;
            line.UpdatedAt = now;
            line.UpdatedBy = userId;

            foreach (var allocation in allocations)
            {
                _db.ProductionInputLotAllocations.Add(new ProductionInputLotAllocation
                {
                    ProductionInputLineId = line.ProductionInputLineID,
                    InventoryLotId = allocation.InventoryLotId,
                    QuantityInBase = allocation.QuantityInBase,
                    UnitCostInBase = allocation.UnitCost,
                    LineCostInBase = allocation.LineCost,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    CreatedBy = userId,
                });
            }
        }

        foreach (var line in batch.OutputLines)
        {
            line.QuantityInBase = await _conversion.ToBaseAsync(line.Quantity, line.MeaurmentId, cancellationToken);
            if (line.QuantityInBase <= 0)
            {
                throw new InvalidOperationException("مقدار تولید باید بزرگ‌تر از صفر باشد.");
            }
        }

        var totalOutputBase = batch.OutputLines.Sum(o => o.QuantityInBase);
        if (totalOutputBase <= 0)
        {
            throw new InvalidOperationException("مجموع مقدار تولید باید بزرگ‌تر از صفر باشد.");
        }

        var conversionCost = batch.CostLines.Sum(c => c.Amount);
        // سازگاری با اسناد قدیمی بدون CostLines
        if (conversionCost <= 0 && (batch.FixedCost > 0 || batch.VariableCost > 0))
        {
            conversionCost = batch.FixedCost + batch.VariableCost;
        }

        var totalProductionCost = totalMaterialCost + conversionCost;

        foreach (var line in batch.OutputLines)
        {
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
        batch.TotalConversionCostInBase = conversionCost;
        batch.TotalCostInBase = totalProductionCost;
        batch.FixedCost = batch.CostLines
            .Where(c => c.CostType == ProductionCostType.Fixed)
            .Sum(c => c.Amount);
        batch.VariableCost = batch.CostLines
            .Where(c => c.CostType != ProductionCostType.Fixed)
            .Sum(c => c.Amount);
        if (batch.CostLines.Count == 0)
        {
            // مقادیر Fixed/Variable قبلی حفظ می‌شوند
        }

        batch.Status = ProductionBatchStatus.Posted;
        batch.IsPosted = true;
        batch.PostedAt = now;
        batch.IsUpdated = true;
        batch.UpdatedAt = now;
        batch.UpdatedBy = userId;

        await _db.SaveChangesAsync(cancellationToken);

        if (totalProductionCost > 0)
        {
            var entry = await PostProductionJournalAsync(
                batch,
                totalMaterialCost,
                materialByWarehouseType,
                conversionCost,
                totalProductionCost,
                userId,
                cancellationToken);
            batch.JournalEntryId = entry.JournalEntryID;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<Data.Models.Finance.JournalEntry> PostProductionJournalAsync(
        ProductionBatch batch,
        decimal totalMaterialCost,
        Dictionary<WarehouseType, decimal> materialByWarehouseType,
        decimal conversionCost,
        decimal totalProductionCost,
        int? userId,
        CancellationToken cancellationToken)
    {
        var fgAccountId = await _accounts.ResolveInventoryAccountIdAsync(WarehouseType.Processed, cancellationToken);
        var baseCurrencyId = await ResolveBaseCurrencyIdAsync(cancellationToken);

        var lines = new List<JournalLineDraft>
        {
            new(fgAccountId, totalProductionCost, 0, totalProductionCost, 0, baseCurrencyId, $"تولید {batch.BatchNumber}"),
        };

        foreach (var (warehouseType, amount) in materialByWarehouseType.Where(x => x.Value > 0))
        {
            var accountId = await _accounts.ResolveInventoryAccountIdAsync(warehouseType, cancellationToken);
            var label = warehouseType == WarehouseType.SemiFinished ? "مصرف نیمه‌ساخته" : "مصرف مواد اولیه";
            lines.Add(new(accountId, 0, amount, 0, amount, baseCurrencyId, label));
        }

        if (batch.CostLines.Count > 0)
        {
            foreach (var costLine in batch.CostLines.Where(c => c.Amount > 0))
            {
                var accountId = costLine.AccountId is > 0
                    ? costLine.AccountId.Value
                    : (await _accounts.GetBySystemCodeAsync(CostTypeSystemCode(costLine.CostType), cancellationToken)).AccountID;

                lines.Add(new(
                    accountId,
                    0,
                    costLine.Amount,
                    0,
                    costLine.Amount,
                    baseCurrencyId,
                    costLine.Description ?? costLine.CostType.ToString()));
            }
        }
        else if (conversionCost > 0)
        {
            // اسناد قدیمی: Fixed+Variable روی OPEX
            var opex = await _accounts.GetBySystemCodeAsync(AccountSystemCode.OperatingExpense, cancellationToken);
            lines.Add(new(opex.AccountID, 0, conversionCost, 0, conversionCost, baseCurrencyId, "هزینه ساخت ثابت/متغیر"));
        }

        // اگر فقط مواد بود و جمع اعتبار کمتر از بدهکار شد (نباید رخ دهد) — اعتبارسنجی Journal انجام می‌دهد
        _ = totalMaterialCost;

        return await _journal.PostAsync(
            batch.ProductionDate,
            $"تولید {batch.BatchNumber}",
            JournalSource.Production,
            batch.ProductionBatchID,
            baseCurrencyId,
            lines,
            userId,
            cancellationToken);
    }

    private async Task<int> ResolveBaseCurrencyIdAsync(CancellationToken cancellationToken)
    {
        var baseCurrencyId = await _db.Currencies
            .Where(c => c.IsBaseCurrency && c.IsDeleted != true)
            .Select(c => c.CurrencyID)
            .FirstOrDefaultAsync(cancellationToken);
        if (baseCurrencyId != 0)
        {
            return baseCurrencyId;
        }

        return await _db.Currencies
            .Where(c => c.IsDeleted != true)
            .Select(c => c.CurrencyID)
            .FirstAsync(cancellationToken);
    }

    public async Task UnpostBatchAsync(int productionBatchId, int? userId, CancellationToken cancellationToken = default)
    {
        var ownsTransaction = _db.Database.CurrentTransaction is null;
        await using var tx = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await UnpostBatchCoreAsync(productionBatchId, userId, cancellationToken);

        if (tx is not null)
        {
            await tx.CommitAsync(cancellationToken);
        }
    }

    private async Task UnpostBatchCoreAsync(int productionBatchId, int? userId, CancellationToken cancellationToken)
    {
        var batch = await _db.ProductionBatches
            .Include(b => b.InputLines.Where(x => x.IsDeleted != true))
            .Include(b => b.OutputLines.Where(x => x.IsDeleted != true))
            .FirstOrDefaultAsync(b => b.ProductionBatchID == productionBatchId && b.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("سند تولید یافت نشد.");

        if (!batch.IsPosted)
        {
            throw new InvalidOperationException("این سند تولید ثبت نهایی نشده است.");
        }

        var now = DateTime.Now;

        foreach (var line in batch.OutputLines)
        {
            if (line.InventoryLotId is not int lotId)
            {
                continue;
            }

            var lot = await _db.InventoryLots
                .FirstOrDefaultAsync(l => l.InventoryLotID == lotId && l.IsDeleted != true, cancellationToken);
            if (lot is null)
            {
                continue;
            }

            if (lot.RemainingQuantityInBase != lot.ReceivedQuantityInBase)
            {
                throw new InvalidOperationException(
                    "بخشی از محصول تولیدی فروش رفته یا مصرف شده است؛ برگشت ممکن نیست.");
            }

            var stock = await _db.InventoryStocks
                .FirstOrDefaultAsync(
                    s => s.WarehouseId == lot.WarehouseId && s.ProductId == lot.ProductId && s.IsDeleted != true,
                    cancellationToken);
            if (stock is not null)
            {
                stock.QuantityInBase -= lot.RemainingQuantityInBase;
                stock.IsUpdated = true;
                stock.UpdatedAt = now;
                stock.UpdatedBy = userId;
            }

            lot.RemainingQuantityInBase = 0;
            lot.IsDeleted = true;
            lot.DeletedAt = now;
            lot.DeletedBy = userId;

            line.InventoryLotId = null;
            line.UnitCostInBase = 0;
            line.IsUpdated = true;
            line.UpdatedAt = now;
            line.UpdatedBy = userId;
        }

        var inputLineIds = batch.InputLines.Select(l => l.ProductionInputLineID).ToList();
        var allocations = await _db.ProductionInputLotAllocations
            .Where(a => inputLineIds.Contains(a.ProductionInputLineId) && a.IsDeleted != true)
            .ToListAsync(cancellationToken);

        foreach (var allocation in allocations)
        {
            await _fifo.RestoreToLotAsync(allocation.InventoryLotId, allocation.QuantityInBase, cancellationToken);

            allocation.IsDeleted = true;
            allocation.IsActive = false;
            allocation.DeletedAt = now;
            allocation.DeletedBy = userId;
        }

        foreach (var line in batch.InputLines)
        {
            line.MaterialCostInBase = 0;
            line.IsUpdated = true;
            line.UpdatedAt = now;
            line.UpdatedBy = userId;
        }

        await _journal.ReverseBySourceAsync(JournalSource.Production, productionBatchId, userId, cancellationToken: cancellationToken);

        batch.IsPosted = false;
        batch.PostedAt = null;
        batch.Status = ProductionBatchStatus.Draft;
        batch.TotalMaterialCostInBase = 0;
        batch.TotalConversionCostInBase = 0;
        batch.TotalCostInBase = 0;
        batch.JournalEntryId = null;
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
                b.TotalConversionCostInBase,
                b.TotalCostInBase,
                b.FixedCost,
                b.VariableCost,
                b.JournalEntryId,
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
                CostLines = b.CostLines
                    .Where(x => x.IsDeleted != true)
                    .Select(x => new
                    {
                        x.ProductionBatchCostLineID,
                        costType = (int)x.CostType,
                        x.Description,
                        x.Amount,
                        x.AccountId,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("سند تولید یافت نشد.");

        var lotsRaw = await _db.InventoryLots
            .AsNoTracking()
            .Where(l => l.ProductionBatchId == productionBatchId && l.IsDeleted != true)
            .Select(l => new
            {
                l.InventoryLotID,
                l.LotCode,
                l.ProductId,
                ProductName = l.Product.Name,
                l.ReceivedQuantityInBase,
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
                l.ReceivedQuantityInBase,
                l.RemainingQuantityInBase,
                l.UnitCost,
                l.PurchaseInvoiceId,
                l.PurchaseInvoiceId.HasValue && invoiceNumbers.TryGetValue(l.PurchaseInvoiceId.Value, out var num)
                    ? num
                    : null))
            .ToList();

        var inputLineIds = await _db.ProductionInputLines
            .AsNoTracking()
            .Where(l => l.ProductionBatchId == productionBatchId && l.IsDeleted != true)
            .Select(l => l.ProductionInputLineID)
            .ToListAsync(cancellationToken);

        var consumedLots = await _db.ProductionInputLotAllocations
            .AsNoTracking()
            .Where(a => inputLineIds.Contains(a.ProductionInputLineId) && a.IsDeleted != true)
            .Select(a => new ProductionTraceConsumedLot(
                a.ProductionInputLineId,
                a.InputLine.ProductId,
                a.InputLine.Product.Name,
                a.InventoryLotId,
                a.InventoryLot.LotCode,
                a.QuantityInBase,
                a.UnitCostInBase,
                a.LineCostInBase))
            .ToListAsync(cancellationToken);

        var lotIds = lotsRaw.Select(l => l.InventoryLotID).ToList();
        var sales = lotIds.Count == 0
            ? []
            : await _db.SaleItemLotAllocations
                .AsNoTracking()
                .Where(a => lotIds.Contains(a.InventoryLotId) && a.IsDeleted != true)
                .Select(a => new ProductionTraceSale(
                    a.SalesItem.SaleInvoiceId,
                    a.SalesItem.Invoice.InvoiceNumber,
                    a.SalesItem.Invoice.InvoiceDate,
                    a.QuantityInBase,
                    a.InventoryLotId,
                    a.InventoryLot.LotCode))
                .ToListAsync(cancellationToken);

        return new ProductionTraceResult(
            batch.ProductionBatchID,
            batch.BatchNumber,
            batch.ProductionDate,
            batch.OutputWarehouseName,
            batch.TotalMaterialCostInBase,
            batch.TotalConversionCostInBase,
            batch.TotalCostInBase > 0
                ? batch.TotalCostInBase
                : batch.TotalMaterialCostInBase + batch.FixedCost + batch.VariableCost,
            batch.FixedCost,
            batch.VariableCost,
            batch.JournalEntryId,
            batch.InputLines.Cast<object>().ToList(),
            batch.OutputLines.Cast<object>().ToList(),
            batch.CostLines.Cast<object>().ToList(),
            lots,
            consumedLots,
            sales);
    }
}
