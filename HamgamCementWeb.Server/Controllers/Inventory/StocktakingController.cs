using System.ComponentModel.DataAnnotations;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Controllers.Common;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Inventory;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Inventory;

[ApiController]
[Route("api/inventory/stocktakings")]
[Authorize]
public class StocktakingController : InventoryControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(Stocktaking.Code),
        [2] = nameof(Stocktaking.StocktakingDate),
        [3] = nameof(Stocktaking.Status),
    };

    private readonly IMeaurmentConversionService _conversion;
    private readonly IFifoInventoryService _fifo;
    private readonly IOperationalGlService _gl;

    public StocktakingController(
        AppDbContext db,
        IMeaurmentConversionService conversion,
        IFifoInventoryService fifo,
        IOperationalGlService gl) : base(db)
    {
        _conversion = conversion;
        _fifo = fifo;
        _gl = gl;
    }

    [HttpPost("datatable")]
    [HasPermission("inventory.stocktaking.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.Stocktakings
            .AsNoTracking()
            .Where(s => s.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(s =>
                s.Code.Contains(searchValue) ||
                s.Warehouse.Name.Contains(searchValue) ||
                (s.Notes != null && s.Notes.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(Stocktaking.StocktakingDate), defaultDescending: true)
            .Skip(start)
            .Take(length)
            .Select(s => new
            {
                stocktakingId = s.StocktakingID,
                code = s.Code,
                warehouseId = s.WarehouseId,
                warehouseName = s.Warehouse.Name,
                stocktakingDate = s.StocktakingDate,
                status = s.Status,
                journalEntryId = s.JournalEntryId,
                linesCount = s.Lines.Count(l => l.IsDeleted != true),
                notes = s.Notes,
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
                r.stocktakingId,
                r.code,
                r.warehouseId,
                r.warehouseName,
                stocktakingDate = r.stocktakingDate.ToString("yyyy-MM-dd"),
                status = r.status.ToString(),
                statusLabel = GetStatusLabel(r.status),
                r.journalEntryId,
                r.linesCount,
                r.notes,
            }),
        });
    }

    [HttpGet("{id:int}")]
    [HasPermission("inventory.stocktaking.view")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var stocktaking = await Db.Stocktakings
            .AsNoTracking()
            .Where(s => s.StocktakingID == id && s.IsDeleted != true)
            .Select(s => new
            {
                stocktakingId = s.StocktakingID,
                code = s.Code,
                warehouseId = s.WarehouseId,
                warehouseName = s.Warehouse.Name,
                stocktakingDate = s.StocktakingDate,
                status = s.Status,
                journalEntryId = s.JournalEntryId,
                notes = s.Notes,
                lines = s.Lines
                    .Where(l => l.IsDeleted != true)
                    .Select(l => new
                    {
                        stocktakingLineId = l.StocktakingLineID,
                        productId = l.ProductId,
                        productCode = l.Product.Code,
                        productName = l.Product.Name,
                        systemQuantityInBase = l.SystemQuantityInBase,
                        countedQuantity = l.CountedQuantity,
                        countedMeaurmentId = l.CountedMeaurmentId,
                        countedMeaurmentName = l.CountedMeaurment.Name,
                        countedQuantityInBase = l.CountedQuantityInBase,
                        differenceInBase = l.DifferenceInBase,
                        adjustmentCostInBase = l.AdjustmentCostInBase,
                        notes = l.Notes,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (stocktaking is null)
        {
            return NotFound(new { message = "سند انبارگردانی یافت نشد." });
        }

        return Ok(stocktaking);
    }

    [HttpPost]
    [HasPermission("inventory.stocktaking.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveStocktakingRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var warehouse = await Db.Warehouses
            .FirstOrDefaultAsync(w => w.WarehouseID == request.WarehouseId && w.IsDeleted != true, cancellationToken);
        if (warehouse is null)
        {
            return BadRequest(new { message = "انبار انتخاب‌شده یافت نشد." });
        }

        if (request.Lines is null || request.Lines.Count == 0)
        {
            return BadRequest(new { message = "حداقل یک ردیف شمارش وارد کنید." });
        }

        var code = await GenerateCodeAsync(cancellationToken);
        var stocktaking = new Stocktaking
        {
            Code = code,
            WarehouseId = request.WarehouseId,
            StocktakingDate = request.StocktakingDate ?? DateTime.Now,
            Status = StocktakingStatus.Draft,
            Notes = request.Notes?.Trim(),
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };

        foreach (var line in request.Lines)
        {
            var countedInBase = await _conversion.ToBaseAsync(
                line.CountedQuantity,
                line.CountedMeaurmentId,
                cancellationToken);

            var stock = await Db.InventoryStocks
                .FirstOrDefaultAsync(
                    s => s.WarehouseId == request.WarehouseId &&
                         s.ProductId == line.ProductId &&
                         s.IsDeleted != true,
                    cancellationToken);

            var systemQty = stock?.QuantityInBase ?? 0;

            stocktaking.Lines.Add(new StocktakingLine
            {
                ProductId = line.ProductId,
                SystemQuantityInBase = systemQty,
                CountedQuantity = line.CountedQuantity,
                CountedMeaurmentId = line.CountedMeaurmentId,
                CountedQuantityInBase = countedInBase,
                DifferenceInBase = countedInBase - systemQty,
                Notes = line.Notes?.Trim(),
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreatedBy = ResolveCurrentUserId(),
            });
        }

        Db.Stocktakings.Add(stocktaking);
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "سند انبارگردانی با موفقیت ثبت شد.",
            stocktakingId = stocktaking.StocktakingID,
            code = stocktaking.Code,
        });
    }

    // چرا edit: تأیید سند انبارگردانی تغییر وضعیت آن است و به .edit نگاشت می‌شود.
    [HttpPost("{id:int}/confirm")]
    [HasPermission("inventory.stocktaking.edit")]
    public async Task<IActionResult> Confirm(int id, CancellationToken cancellationToken)
    {
        var stocktaking = await Db.Stocktakings
            .Include(s => s.Lines)
            .Include(s => s.Warehouse)
            .FirstOrDefaultAsync(s => s.StocktakingID == id && s.IsDeleted != true, cancellationToken);

        if (stocktaking is null)
        {
            return NotFound(new { message = "سند انبارگردانی یافت نشد." });
        }

        if (stocktaking.Status == StocktakingStatus.Confirmed)
        {
            return Conflict(new { message = "این سند قبلاً تأیید شده است." });
        }

        if (stocktaking.Status == StocktakingStatus.Cancelled)
        {
            return Conflict(new { message = "سند لغو‌شده قابل تأیید نیست." });
        }

        var userId = ResolveCurrentUserId();
        await using var tx = await Db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // چرا AdjustToCountAsync: هم موجودی تجمیعی و هم مجموع Lotها را با مقدار شمارش‌شده هماهنگ می‌کند
            // تا FIFO پس از انبارگردانی از موجودی واقعی تخصیص دهد و ناسازگاری Stock/Lot رخ ندهد.
            foreach (var line in stocktaking.Lines.Where(l => l.IsDeleted != true))
            {
                var result = await _fifo.AdjustToCountAsync(
                    line.ProductId,
                    stocktaking.WarehouseId,
                    line.CountedQuantityInBase,
                    stocktaking.StocktakingDate,
                    userId,
                    cancellationToken);

                line.DifferenceInBase = result.DifferenceInBase;
                line.AdjustmentCostInBase = result.AdjustmentCostInBase;
                line.IsUpdated = true;
                line.UpdatedAt = DateTime.Now;
                line.UpdatedBy = userId;
            }

            var journal = await _gl.PostStocktakingAsync(
                stocktaking,
                stocktaking.Warehouse,
                userId,
                cancellationToken);

            stocktaking.JournalEntryId = journal?.JournalEntryID;
            stocktaking.Status = StocktakingStatus.Confirmed;
            stocktaking.IsUpdated = true;
            stocktaking.UpdatedAt = DateTime.Now;
            stocktaking.UpdatedBy = userId;

            await Db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return Ok(new
            {
                message = journal is null
                    ? "انبارگردانی تأیید شد و موجودی به‌روزرسانی شد؛ چون بهای تعدیل صفر است سند دفتر ثبت نشد."
                    : "انبارگردانی تأیید شد؛ موجودی و سند دابل‌انتری ثبت شد.",
                journalEntryId = stocktaking.JournalEntryId,
            });
        }
        catch (Exception)
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    [HttpDelete("{id:int}")]
    [HasPermission("inventory.stocktaking.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await Db.Stocktakings
            .FirstOrDefaultAsync(s => s.StocktakingID == id && s.IsDeleted != true, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "سند انبارگردانی یافت نشد." });
        }

        if (entity.Status == StocktakingStatus.Confirmed)
        {
            return Conflict(new { message = "سند تأییدشده قابل حذف نیست." });
        }

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.DeletedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "سند انبارگردانی حذف شد." });
    }

    // چرا Max به‌جای Count: با Count پس از حذف رکورد، شماره تکراری تولید می‌شد؛ استخراج عدد از آخرین کد
    // و افزودن یک واحد از تکرار جلوگیری می‌کند.
    private async Task<string> GenerateCodeAsync(CancellationToken cancellationToken)
    {
        var codes = await Db.Stocktakings
            .IgnoreQueryFilters()
            .Where(s => s.Code != null && s.Code.StartsWith("HMST"))
            .Select(s => s.Code)
            .ToListAsync(cancellationToken);

        var maxSequence = codes
            .Select(c => int.TryParse(c.Substring(4), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"HMST{(maxSequence + 1):D5}";
    }

    private static string GetStatusLabel(StocktakingStatus status) => status switch
    {
        StocktakingStatus.Draft => "پیش‌نویس",
        StocktakingStatus.Confirmed => "تأیید شده",
        StocktakingStatus.Cancelled => "لغو شده",
        _ => status.ToString(),
    };

    public class SaveStocktakingRequest
    {
        [Required]
        public int WarehouseId { get; set; }

        public DateTime? StocktakingDate { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public List<SaveStocktakingLineRequest> Lines { get; set; } = [];
    }

    public class SaveStocktakingLineRequest
    {
        [Required]
        public int ProductId { get; set; }

        public decimal CountedQuantity { get; set; }

        [Required]
        public int CountedMeaurmentId { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
