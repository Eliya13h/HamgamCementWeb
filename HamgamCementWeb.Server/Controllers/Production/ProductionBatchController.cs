using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Controllers.Transport;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Production;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Production;

[ApiController]
[Route("api/production/batches")]
[Authorize]
public class ProductionBatchController : ControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(ProductionBatch.BatchNumber),
        [2] = nameof(ProductionBatch.ProductionDate),
        [3] = nameof(ProductionBatch.Status),
    };

    private readonly AppDbContext _db;
    private readonly IMeaurmentConversionService _conversion;
    private readonly IProductionPostingService _posting;

    public ProductionBatchController(
        AppDbContext db,
        IMeaurmentConversionService conversion,
        IProductionPostingService posting)
    {
        _db = db;
        _conversion = conversion;
        _posting = posting;
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

        var query = _db.ProductionBatches
            .AsNoTracking()
            .Where(b => b.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(b =>
                b.BatchNumber.Contains(searchValue) ||
                b.OutputWarehouse.Name.Contains(searchValue) ||
                (b.Description != null && b.Description.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(ProductionBatch.ProductionDate), defaultDescending: true)
            .Skip(start)
            .Take(length)
            .Select(b => new
            {
                productionBatchId = b.ProductionBatchID,
                batchNumber = b.BatchNumber,
                productionDate = b.ProductionDate,
                outputWarehouseId = b.OutputWarehouseId,
                outputWarehouseName = b.OutputWarehouse.Name,
                status = (int)b.Status,
                isPosted = b.IsPosted,
                isTransferredToSales = b.IsTransferredToSales,
                fixedCost = b.FixedCost,
                variableCost = b.VariableCost,
                totalMaterialCostInBase = b.TotalMaterialCostInBase,
                inputLinesCount = b.InputLines.Count(x => x.IsDeleted != true),
                outputLinesCount = b.OutputLines.Count(x => x.IsDeleted != true),
                description = b.Description,
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select((r, i) => new
            {
                rowNumber = start + i + 1,
                r.productionBatchId,
                r.batchNumber,
                productionDate = r.productionDate.ToString("yyyy-MM-dd"),
                r.outputWarehouseId,
                r.outputWarehouseName,
                r.status,
                statusLabel = GetStatusLabel((ProductionBatchStatus)r.status),
                r.isPosted,
                r.isTransferredToSales,
                r.fixedCost,
                r.variableCost,
                r.totalMaterialCostInBase,
                r.inputLinesCount,
                r.outputLinesCount,
                r.description,
            }),
        });
    }

    // چرا بدون HasPermission: دراپ‌داون سند تولید در فاکتور خرید (ورود از تولید) استفاده می‌شود.
    [HttpGet("list")]
    public async Task<IActionResult> List(
        [FromQuery] bool? availableForSales,
        CancellationToken cancellationToken)
    {
        var query = _db.ProductionBatches
            .AsNoTracking()
            .Where(b => b.IsDeleted != true && b.IsPosted);

        if (availableForSales == true)
        {
            query = query.Where(b => !b.IsTransferredToSales);
        }

        var items = await query
            .OrderByDescending(b => b.ProductionDate)
            .Select(b => new
            {
                value = b.ProductionBatchID,
                label = b.BatchNumber,
                productionDate = b.ProductionDate,
                outputWarehouseId = b.OutputWarehouseId,
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [HasPermission("production.daily.view")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var batch = await _db.ProductionBatches
            .AsNoTracking()
            .Where(b => b.ProductionBatchID == id && b.IsDeleted != true)
            .Select(b => new
            {
                productionBatchId = b.ProductionBatchID,
                batchNumber = b.BatchNumber,
                productionDate = b.ProductionDate,
                outputWarehouseId = b.OutputWarehouseId,
                outputWarehouseName = b.OutputWarehouse.Name,
                status = (int)b.Status,
                isPosted = b.IsPosted,
                isTransferredToSales = b.IsTransferredToSales,
                fixedCost = b.FixedCost,
                variableCost = b.VariableCost,
                totalMaterialCostInBase = b.TotalMaterialCostInBase,
                description = b.Description,
                inputLines = b.InputLines
                    .Where(x => x.IsDeleted != true)
                    .Select(x => new
                    {
                        productionInputLineId = x.ProductionInputLineID,
                        warehouseId = x.WarehouseId,
                        warehouseName = x.Warehouse.Name,
                        productId = x.ProductId,
                        productName = x.Product.Name,
                        meaurmentId = x.MeaurmentId,
                        meaurmentName = x.Meaurment.Name,
                        quantity = x.Quantity,
                        quantityInBase = x.QuantityInBase,
                        materialCostInBase = x.MaterialCostInBase,
                    })
                    .ToList(),
                outputLines = b.OutputLines
                    .Where(x => x.IsDeleted != true)
                    .Select(x => new
                    {
                        productionOutputLineId = x.ProductionOutputLineID,
                        productId = x.ProductId,
                        productName = x.Product.Name,
                        meaurmentId = x.MeaurmentId,
                        meaurmentName = x.Meaurment.Name,
                        quantity = x.Quantity,
                        quantityInBase = x.QuantityInBase,
                        unitCostInBase = x.UnitCostInBase,
                        inventoryLotId = x.InventoryLotId,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (batch is null)
        {
            return NotFound(new { message = "سند تولید یافت نشد." });
        }

        return Ok(batch);
    }

    [HttpGet("{id:int}/trace")]
    [HasPermission("production.daily.view")]
    public async Task<IActionResult> Trace(int id, CancellationToken cancellationToken)
    {
        try
        {
            var trace = await _posting.GetTraceAsync(id, cancellationToken);
            return Ok(trace);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
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

        var validationError = await ValidateBatchRequestAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;

        var batch = new ProductionBatch
        {
            BatchNumber = $"TMP{DateTime.UtcNow.Ticks}",
            ProductionDate = request.ProductionDate,
            OutputWarehouseId = request.OutputWarehouseId,
            FixedCost = request.FixedCost,
            VariableCost = request.VariableCost,
            Description = request.Description?.Trim(),
            Status = ProductionBatchStatus.Draft,
            IsDeleted = false,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId,
        };

        await AddLinesAsync(batch, request, userId, now, cancellationToken);

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
            .FirstOrDefaultAsync(b => b.ProductionBatchID == id && b.IsDeleted != true, cancellationToken);

        if (batch is null)
        {
            return NotFound(new { message = "سند تولید یافت نشد." });
        }

        if (batch.IsPosted)
        {
            return BadRequest(new { message = "سند ثبت‌شده قابل ویرایش نیست." });
        }

        var validationError = await ValidateBatchRequestAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;

        batch.ProductionDate = request.ProductionDate;
        batch.OutputWarehouseId = request.OutputWarehouseId;
        batch.FixedCost = request.FixedCost;
        batch.VariableCost = request.VariableCost;
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

        await AddLinesAsync(batch, request, userId, now, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "سند تولید ویرایش شد." });
    }

    // چرا edit: ثبت نهایی (Post) تغییر وضعیت سند تولید است و به .edit نگاشت می‌شود.
    [HttpPost("{id:int}/post")]
    [HasPermission("production.daily.edit")]
    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _posting.PostBatchAsync(id, ResolveCurrentUserId(), cancellationToken);
            return Ok(new { message = "سند تولید ثبت نهایی شد. موجودی به‌روز شد." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // چرا edit: برگشت ثبت (Unpost) تغییر وضعیت سند تولید است و به .edit نگاشت می‌شود.
    [HttpPost("{id:int}/unpost")]
    [HasPermission("production.daily.edit")]
    public async Task<IActionResult> Unpost(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _posting.UnpostBatchAsync(id, ResolveCurrentUserId(), cancellationToken);
            return Ok(new { message = "ثبت سند تولید برگشت خورد. موجودی و مواد مصرفی بازگردانده شد." });
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

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "سند تولید حذف شد." });
    }

    private async Task<string?> ValidateBatchRequestAsync(
        SaveProductionBatchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.InputLines is null || request.InputLines.Count == 0)
        {
            return "حداقل یک ردیف مصرف وارد کنید.";
        }

        if (request.OutputLines is null || request.OutputLines.Count == 0)
        {
            return "حداقل یک ردیف تولید وارد کنید.";
        }

        var outputWarehouse = await _db.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.WarehouseID == request.OutputWarehouseId && w.IsDeleted != true, cancellationToken);

        if (outputWarehouse is null)
        {
            return "انبار مقصد یافت نشد.";
        }

        if (outputWarehouse.WarehouseType != WarehouseType.Processed)
        {
            return "انبار مقصد باید از نوع مواد پردازش‌شده باشد.";
        }

        foreach (var line in request.InputLines)
        {
            var warehouse = await _db.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.WarehouseID == line.WarehouseId && w.IsDeleted != true, cancellationToken);

            if (warehouse is null)
            {
                return "یکی از انبارهای مصرف یافت نشد.";
            }

            if (warehouse.WarehouseType is not (WarehouseType.RawMaterials or WarehouseType.SemiFinished))
            {
                return $"انبار «{warehouse.Name}» برای مصرف تولید مجاز نیست.";
            }
        }

        // اعتبارسنجی محصول و واحد هر ردیف (مصرف و تولید): محصول باید موجود و فعال باشد و واحد انتخابی
        // باید جزو واحدهای مجاز همان محصول (ProductMeaurment) باشد تا تبدیل به پایه معنادار باشد.
        var lineChecks = request.InputLines
            .Select(l => (l.ProductId, l.MeaurmentId))
            .Concat(request.OutputLines.Select(l => (l.ProductId, l.MeaurmentId)));

        foreach (var (productId, meaurmentId) in lineChecks)
        {
            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductID == productId && p.IsDeleted != true, cancellationToken);

            if (product is null)
            {
                return "یکی از محصولات انتخاب‌شده یافت نشد.";
            }

            if (product.IsActive != true)
            {
                return $"محصول «{product.Name}» غیرفعال است و قابل استفاده در تولید نیست.";
            }

            var meaurmentAllowed = await _db.ProductMeaurments
                .AnyAsync(
                    pm => pm.ProductId == productId &&
                          pm.MeaurmentId == meaurmentId &&
                          pm.IsDeleted != true,
                    cancellationToken);

            if (!meaurmentAllowed)
            {
                return $"واحد انتخاب‌شده برای محصول «{product.Name}» مجاز نیست.";
            }
        }

        return null;
    }

    private async Task AddLinesAsync(
        ProductionBatch batch,
        SaveProductionBatchRequest request,
        int? userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        foreach (var line in request.InputLines)
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

        foreach (var line in request.OutputLines)
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
    }

    public class SaveProductionBatchRequest
    {
        [Required]
        public DateTime ProductionDate { get; set; }

        [Range(1, int.MaxValue)]
        public int OutputWarehouseId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal FixedCost { get; set; }

        [Range(0, double.MaxValue)]
        public decimal VariableCost { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public List<SaveProductionInputLineRequest> InputLines { get; set; } = [];

        public List<SaveProductionOutputLineRequest> OutputLines { get; set; } = [];
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
}
