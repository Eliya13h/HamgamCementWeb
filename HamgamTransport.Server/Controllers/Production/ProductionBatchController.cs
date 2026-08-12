using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamTransport.Server.Authorization;
using HamgamTransport.Server.Controllers.Common;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Production;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Controllers.Production;

[ApiController]
[Route("api/production/batches")]
[Authorize]
public class ProductionBatchController : ControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = "BatchNumber",
        [3] = "ProductionDate",
        [5] = "Status",
        [6] = "TotalCostInBase",
    };

    private readonly AppDbContext _db;
    private readonly IMeaurmentConversionService _conversion;
    private readonly IProductionPostingService _posting;
    private readonly IProductionBatchReadService _batchRead;

    public ProductionBatchController(
        AppDbContext db,
        IMeaurmentConversionService conversion,
        IProductionPostingService posting,
        IProductionBatchReadService batchRead)
    {
        _db = db;
        _conversion = conversion;
        _posting = posting;
        _batchRead = batchRead;
    }

    private int? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string GetStatusLabel(ProductionBatchStatus status) => status switch
    {
        ProductionBatchStatus.Draft => "پیش‌نویس",
        ProductionBatchStatus.Posted => "ثبت‌شده",
        _ => status.ToString(),
    };

    [HttpPost("datatable")]
    [HasPermission("production.daily.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);
        var order = request.Order?.FirstOrDefault();
        var orderColumn = order is not null && OrderColumns.TryGetValue(order.Column, out var col)
            ? col
            : "ProductionDate";
        // پیش‌فرض: تاریخ نزولی (مثل قبل)
        var ascending = order is null
            ? false
            : !string.Equals(order.Dir, "desc", StringComparison.OrdinalIgnoreCase);

        var (recordsTotal, recordsFiltered, rows) = await _batchRead.GetDataTableAsync(
            start,
            length,
            request.Search?.Value,
            orderColumn,
            ascending,
            cancellationToken);

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select((r, i) => new
            {
                rowNumber = start + i + 1,
                productionBatchId = r.ProductionBatchId,
                batchNumber = r.BatchNumber,
                productionDate = r.ProductionDate.ToString("yyyy-MM-dd"),
                productionFormulaId = r.ProductionFormulaId,
                formulaName = r.FormulaName,
                productionPlanId = r.ProductionPlanId,
                planLabel = r.PlanLabel,
                outputWarehouseId = r.OutputWarehouseId,
                outputWarehouseName = r.OutputWarehouseName,
                status = r.Status,
                statusLabel = GetStatusLabel((ProductionBatchStatus)r.Status),
                isPosted = r.IsPosted,
                totalMaterialCostInBase = r.TotalMaterialCostInBase,
                totalConversionCostInBase = r.TotalConversionCostInBase,
                totalCostInBase = r.TotalCostInBase,
                inputLinesCount = r.InputLinesCount,
                outputLinesCount = r.OutputLinesCount,
                description = r.Description,
            }),
        });
    }

    [HttpGet("list")]
    [HasPermission("production.daily.view")]
    public async Task<IActionResult> List(
        [FromQuery] int start = 0,
        [FromQuery] int length = 100,
        CancellationToken cancellationToken = default)
    {
        var items = await _batchRead.GetListAsync(start, length, cancellationToken);
        return Ok(items.Select(b => new
        {
            value = b.Value,
            label = b.Label,
            productionDate = b.ProductionDate,
            outputWarehouseId = b.OutputWarehouseId,
        }));
    }

    [HttpGet("{id:int}")]
    [HasPermission("production.daily.view")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var batch = await _batchRead.GetByIdAsync(id, cancellationToken);
        if (batch is null)
        {
            return NotFound(new { message = "سند تولید یافت نشد." });
        }

        return Ok(new
        {
            productionBatchId = batch.ProductionBatchId,
            batchNumber = batch.BatchNumber,
            productionDate = batch.ProductionDate,
            productionFormulaId = batch.ProductionFormulaId,
            formulaName = batch.FormulaName,
            formulaMode = batch.FormulaMode,
            productionPlanId = batch.ProductionPlanId,
            planLabel = batch.PlanLabel,
            outputWarehouseId = batch.OutputWarehouseId,
            outputWarehouseName = batch.OutputWarehouseName,
            status = batch.Status,
            isPosted = batch.IsPosted,
            totalMaterialCostInBase = batch.TotalMaterialCostInBase,
            totalConversionCostInBase = batch.TotalConversionCostInBase,
            totalCostInBase = batch.TotalCostInBase,
            journalEntryId = batch.JournalEntryId,
            description = batch.Description,
            inputLines = batch.InputLines.Select(x => new
            {
                productionInputLineId = x.ProductionInputLineId,
                warehouseId = x.WarehouseId,
                warehouseName = x.WarehouseName,
                productId = x.ProductId,
                productName = x.ProductName,
                meaurmentId = x.MeaurmentId,
                meaurmentName = x.MeaurmentName,
                quantity = x.Quantity,
                quantityInBase = x.QuantityInBase,
                materialCostInBase = x.MaterialCostInBase,
            }),
            outputLines = batch.OutputLines.Select(x => new
            {
                productionOutputLineId = x.ProductionOutputLineId,
                productId = x.ProductId,
                productName = x.ProductName,
                meaurmentId = x.MeaurmentId,
                meaurmentName = x.MeaurmentName,
                quantity = x.Quantity,
                quantityInBase = x.QuantityInBase,
                unitCostInBase = x.UnitCostInBase,
                inventoryLotId = x.InventoryLotId,
            }),
            costLines = batch.CostLines.Select(x => new
            {
                productionBatchCostLineId = x.ProductionBatchCostLineId,
                costType = x.CostType,
                description = x.Description,
                amount = x.Amount,
                accountId = x.AccountId,
            }),
        });
    }

    [HttpGet("{id:int}/trace")]
    [HasPermission("production.daily.view")]
    public async Task<IActionResult> Trace(int id, CancellationToken cancellationToken)
    {
        var trace = await _batchRead.GetTraceAsync(id, cancellationToken);
        if (trace is null)
        {
            return NotFound(new { message = "سند تولید یافت نشد." });
        }

        return Ok(trace);
    }

    [HttpPost]
    [HasPermission("production.daily.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveProductionBatchRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var prepared = await PrepareBatchFromRequestAsync(request, cancellationToken);
        if (prepared.Error is not null)
        {
            return BadRequest(new { message = prepared.Error });
        }

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;
        var p = prepared.Payload!;

        var batch = new ProductionBatch
        {
            BatchNumber = $"TMP{DateTime.UtcNow.Ticks}",
            ProductionDate = request.ProductionDate,
            ProductionFormulaId = p.FormulaId,
            ProductionPlanId = p.ProductionPlanId,
            OutputWarehouseId = request.OutputWarehouseId,
            // Fixed/Variable فقط مشتق از CostLines برای گزارش‌های قدیمی
            FixedCost = p.FixedCost,
            VariableCost = p.VariableCost,
            Description = request.Description?.Trim(),
            Status = ProductionBatchStatus.Draft,
            IsDeleted = false,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId,
        };

        await AddLinesAsync(batch, p, userId, now, cancellationToken);

        _db.ProductionBatches.Add(batch);
        await _db.SaveChangesAsync(cancellationToken);

        batch.BatchNumber = ProductionCodeHelper.ForBatch(batch.ProductionBatchID);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "سند تولید ایجاد شد.", productionBatchId = batch.ProductionBatchID });
    }

    [HttpPut("{id:int}")]
    [HasPermission("production.daily.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveProductionBatchRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var batch = await _db.ProductionBatches
            .Include(b => b.InputLines.Where(x => x.IsDeleted != true))
            .Include(b => b.OutputLines.Where(x => x.IsDeleted != true))
            .Include(b => b.CostLines.Where(x => x.IsDeleted != true))
            .FirstOrDefaultAsync(b => b.ProductionBatchID == id && b.IsDeleted != true, cancellationToken);

        if (batch is null)
        {
            return NotFound(new { message = "سند تولید یافت نشد." });
        }

        if (batch.IsPosted)
        {
            return BadRequest(new { message = "سند ثبت‌شده قابل ویرایش نیست." });
        }

        var prepared = await PrepareBatchFromRequestAsync(request, cancellationToken);
        if (prepared.Error is not null)
        {
            return BadRequest(new { message = prepared.Error });
        }

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;
        var p = prepared.Payload!;

        batch.ProductionDate = request.ProductionDate;
        batch.ProductionFormulaId = p.FormulaId;
        batch.ProductionPlanId = p.ProductionPlanId;
        batch.OutputWarehouseId = request.OutputWarehouseId;
        // Fixed/Variable فقط مشتق از CostLines برای گزارش‌های قدیمی
        batch.FixedCost = p.FixedCost;
        batch.VariableCost = p.VariableCost;
        batch.Description = request.Description?.Trim();
        batch.IsUpdated = true;
        batch.UpdatedAt = now;
        batch.UpdatedBy = userId;

        foreach (var line in batch.InputLines.ToList())
        {
            line.IsDeleted = true;
            line.DeletedAt = now;
            line.DeletedBy = userId;
        }

        foreach (var line in batch.OutputLines.ToList())
        {
            line.IsDeleted = true;
            line.DeletedAt = now;
            line.DeletedBy = userId;
        }

        foreach (var line in batch.CostLines.ToList())
        {
            line.IsDeleted = true;
            line.DeletedAt = now;
            line.DeletedBy = userId;
        }

        await AddLinesAsync(batch, p, userId, now, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "سند تولید ویرایش شد." });
    }

    [HttpGet("{id:int}/preview-post")]
    [HasPermission("production.daily.view")]
    public async Task<IActionResult> PreviewPost(int id, CancellationToken cancellationToken)
    {
        try
        {
            var preview = await _posting.PreviewPostAsync(id, cancellationToken);
            return Ok(preview);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/post")]
    [HasPermission("production.daily.edit")]
    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _posting.PostBatchAsync(id, ResolveCurrentUserId(), cancellationToken);
            return Ok(new { message = "سند تولید ثبت نهایی شد. موجودی و بهای تمام‌شده به‌روز شد." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/unpost")]
    [HasPermission("production.daily.edit")]
    public async Task<IActionResult> Unpost(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _posting.UnpostBatchAsync(id, ResolveCurrentUserId(), cancellationToken);
            return Ok(new { message = "ثبت سند تولید برگشت خورد. موجودی، مواد و سند حسابداری بازگردانده شد." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [HasPermission("production.daily.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var batch = await _db.ProductionBatches
            .Include(b => b.InputLines)
            .Include(b => b.OutputLines)
            .Include(b => b.CostLines)
            .FirstOrDefaultAsync(b => b.ProductionBatchID == id && b.IsDeleted != true, cancellationToken);

        if (batch is null)
        {
            return NotFound(new { message = "سند تولید یافت نشد." });
        }

        if (batch.IsPosted)
        {
            return BadRequest(new { message = "سند ثبت‌شده قابل حذف نیست." });
        }

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;

        batch.IsDeleted = true;
        batch.IsActive = false;
        batch.DeletedAt = now;
        batch.DeletedBy = userId;

        foreach (var line in batch.InputLines)
        {
            line.IsDeleted = true;
            line.DeletedAt = now;
            line.DeletedBy = userId;
        }

        foreach (var line in batch.OutputLines)
        {
            line.IsDeleted = true;
            line.DeletedAt = now;
            line.DeletedBy = userId;
        }

        foreach (var line in batch.CostLines)
        {
            line.IsDeleted = true;
            line.DeletedAt = now;
            line.DeletedBy = userId;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "سند تولید حذف شد." });
    }

    private sealed class PreparedBatch
    {
        public int FormulaId { get; init; }
        public int? ProductionPlanId { get; init; }
        // مشتق از CostLines — برای سازگاری گزارش
        public decimal FixedCost { get; init; }
        public decimal VariableCost { get; init; }
        public List<SaveProductionInputLineRequest> InputLines { get; init; } = [];
        public List<SaveProductionOutputLineRequest> OutputLines { get; init; } = [];
        public List<SaveProductionCostLineRequest> CostLines { get; init; } = [];
    }

    private async Task<(string? Error, PreparedBatch? Payload)> PrepareBatchFromRequestAsync(
        SaveProductionBatchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ProductionFormulaId <= 0)
        {
            return ("انتخاب فرمول ساخت الزامی است.", null);
        }

        var formula = await _db.ProductionFormulas
            .AsNoTracking()
            .Include(f => f.MaterialLines.Where(x => x.IsDeleted != true))
            .Include(f => f.CostLines.Where(x => x.IsDeleted != true))
            .FirstOrDefaultAsync(
                f => f.ProductionFormulaID == request.ProductionFormulaId && f.IsDeleted != true,
                cancellationToken);

        if (formula is null)
        {
            return ("فرمول ساخت یافت نشد.", null);
        }

        if (request.ProducedQuantity <= 0)
        {
            return ("مقدار تولید باید بزرگ‌تر از صفر باشد.", null);
        }

        int? productionPlanId = null;
        if (request.ProductionPlanId is > 0)
        {
            var plan = await _db.ProductionPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.ProductionPlanID == request.ProductionPlanId && p.IsDeleted != true,
                    cancellationToken);
            if (plan is null)
            {
                return ("برنامه تولید یافت نشد.", null);
            }

            if (plan.ProductId != formula.ProductId)
            {
                return ("محصول برنامه تولید با محصول فرمول ساخت یکسان نیست.", null);
            }

            productionPlanId = plan.ProductionPlanID;
        }

        var outputWarehouse = await _db.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.WarehouseID == request.OutputWarehouseId && w.IsDeleted != true, cancellationToken);

        if (outputWarehouse is null)
        {
            return ("انبار مقصد یافت نشد.", null);
        }

        if (outputWarehouse.WarehouseType != WarehouseType.Processed)
        {
            return ("انبار مقصد باید از نوع مواد پردازش‌شده باشد.", null);
        }

        var scale = request.ProducedQuantity / formula.BaseQuantity;
        List<SaveProductionInputLineRequest> inputs;
        List<SaveProductionCostLineRequest> costs;

        if (formula.Mode == ProductionFormulaMode.Fixed)
        {
            // فرمول ثابت: مواد و هزینه از فرمول مقیاس می‌شوند؛ انبار از درخواست یا پیش‌فرض فرمول
            var requestInputsByProduct = (request.InputLines ?? [])
                .GroupBy(l => l.ProductId)
                .ToDictionary(g => g.Key, g => g.First());

            inputs = [];
            foreach (var mat in formula.MaterialLines)
            {
                requestInputsByProduct.TryGetValue(mat.ProductId, out var fromReq);
                var warehouseId = fromReq?.WarehouseId > 0
                    ? fromReq.WarehouseId
                    : mat.DefaultWarehouseId ?? 0;

                if (warehouseId <= 0)
                {
                    return ($"انبار مصرف برای ماده «محصول #{mat.ProductId}» مشخص نشده است.", null);
                }

                inputs.Add(new SaveProductionInputLineRequest
                {
                    WarehouseId = warehouseId,
                    ProductId = mat.ProductId,
                    MeaurmentId = mat.MeaurmentId,
                    Quantity = mat.Quantity * scale,
                });
            }

            costs = formula.CostLines.Select(c => new SaveProductionCostLineRequest
            {
                CostType = c.CostType,
                Description = c.Description,
                Amount = c.AmountMode == ProductionCostAmountMode.Flat ? c.Amount : c.Amount * scale,
                AccountId = c.AccountId,
            }).ToList();
        }
        else
        {
            if (request.InputLines is null || request.InputLines.Count == 0)
            {
                return ("حداقل یک ردیف مصرف وارد کنید.", null);
            }

            inputs = request.InputLines;
            costs = request.CostLines ?? [];
        }

        if (inputs.Count == 0)
        {
            return ("فرمول ماده ندارد؛ ابتدا مواد فرمول را تعریف کنید.", null);
        }

        foreach (var line in inputs)
        {
            var warehouse = await _db.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.WarehouseID == line.WarehouseId && w.IsDeleted != true, cancellationToken);

            if (warehouse is null)
            {
                return ("یکی از انبارهای مصرف یافت نشد.", null);
            }

            if (warehouse.WarehouseType is not (WarehouseType.RawMaterials or WarehouseType.SemiFinished))
            {
                return ($"انبار «{warehouse.Name}» برای مصرف تولید مجاز نیست.", null);
            }

            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductID == line.ProductId && p.IsDeleted != true, cancellationToken);
            if (product is null)
            {
                return ("یکی از محصولات مصرفی یافت نشد.", null);
            }

            var meaurmentAllowed = await _db.ProductMeaurments.AnyAsync(
                pm => pm.ProductId == line.ProductId &&
                      pm.MeaurmentId == line.MeaurmentId &&
                      pm.IsDeleted != true,
                cancellationToken);
            if (!meaurmentAllowed)
            {
                return ($"واحد انتخاب‌شده برای محصول «{product.Name}» مجاز نیست.", null);
            }
        }

        var outputs = new List<SaveProductionOutputLineRequest>
        {
            new()
            {
                ProductId = formula.ProductId,
                MeaurmentId = formula.MeaurmentId,
                Quantity = request.ProducedQuantity,
            },
        };

        var fixedCost = costs.Where(c => c.CostType == ProductionCostType.Fixed).Sum(c => c.Amount);
        var variableCost = costs.Where(c => c.CostType != ProductionCostType.Fixed).Sum(c => c.Amount);

        return (null, new PreparedBatch
        {
            FormulaId = formula.ProductionFormulaID,
            ProductionPlanId = productionPlanId,
            FixedCost = fixedCost,
            VariableCost = variableCost,
            InputLines = inputs,
            OutputLines = outputs,
            CostLines = costs,
        });
    }

    private async Task AddLinesAsync(
        ProductionBatch batch,
        PreparedBatch prepared,
        int? userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        foreach (var line in prepared.InputLines)
        {
            var quantityInBase = await _conversion.ToBaseAsync(line.Quantity, line.MeaurmentId, cancellationToken);
            batch.InputLines.Add(new ProductionInputLine
            {
                WarehouseId = line.WarehouseId,
                ProductId = line.ProductId,
                MeaurmentId = line.MeaurmentId,
                Quantity = line.Quantity,
                QuantityInBase = quantityInBase,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = userId,
            });
        }

        foreach (var line in prepared.OutputLines)
        {
            var quantityInBase = await _conversion.ToBaseAsync(line.Quantity, line.MeaurmentId, cancellationToken);
            batch.OutputLines.Add(new ProductionOutputLine
            {
                ProductId = line.ProductId,
                MeaurmentId = line.MeaurmentId,
                Quantity = line.Quantity,
                QuantityInBase = quantityInBase,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = userId,
            });
        }

        foreach (var line in prepared.CostLines)
        {
            batch.CostLines.Add(new ProductionBatchCostLine
            {
                CostType = line.CostType,
                Description = line.Description?.Trim(),
                Amount = line.Amount,
                AccountId = line.AccountId is > 0 ? line.AccountId : null,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = userId,
            });
        }
    }

    public class SaveProductionBatchRequest
    {
        [Required]
        public DateTime ProductionDate { get; set; }

        [Range(1, int.MaxValue)]
        public int ProductionFormulaId { get; set; }

        // لینک اختیاری به برنامه تولید
        public int? ProductionPlanId { get; set; }

        [Range(1, int.MaxValue)]
        public int OutputWarehouseId { get; set; }

        // مقدار تولید محصول خروجی فرمول
        [Range(0.000001, double.MaxValue)]
        public decimal ProducedQuantity { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        // در فرمول متغیر الزامی؛ در ثابت فقط برای تعیین انبار مصرف استفاده می‌شود
        public List<SaveProductionInputLineRequest>? InputLines { get; set; }

        public List<SaveProductionCostLineRequest>? CostLines { get; set; }
    }

    public class SaveProductionInputLineRequest
    {
        [Range(1, int.MaxValue)]
        public int WarehouseId { get; set; }

        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int MeaurmentId { get; set; }

        [Range(0.000001, double.MaxValue)]
        public decimal Quantity { get; set; }
    }

    public class SaveProductionOutputLineRequest
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int MeaurmentId { get; set; }

        [Range(0.000001, double.MaxValue)]
        public decimal Quantity { get; set; }
    }

    public class SaveProductionCostLineRequest
    {
        public ProductionCostType CostType { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        public int? AccountId { get; set; }
    }
}
