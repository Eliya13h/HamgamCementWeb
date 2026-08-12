using System.ComponentModel.DataAnnotations;
using HamgamTransport.Server.Authorization;
using HamgamTransport.Server.Controllers.Common;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Inventory;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Controllers.Inventory;

[ApiController]
[Route("api/inventory/transfers")]
[Authorize]
public class WarehouseTransferController : InventoryControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(WarehouseTransfer.Code),
        [2] = nameof(WarehouseTransfer.TransferDate),
        [3] = nameof(WarehouseTransfer.Status),
    };

    private readonly IMeaurmentConversionService _conversion;
    private readonly IFifoInventoryService _fifo;
    private readonly IOperationalGlService _gl;

    public WarehouseTransferController(
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
    [HasPermission("inventory.transfers.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.WarehouseTransfers
            .AsNoTracking()
            .Where(t => t.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(t =>
                t.Code.Contains(searchValue) ||
                t.FromWarehouse.Name.Contains(searchValue) ||
                t.ToWarehouse.Name.Contains(searchValue) ||
                (t.Notes != null && t.Notes.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(WarehouseTransfer.TransferDate), defaultDescending: true)
            .Skip(start)
            .Take(length)
            .Select(t => new
            {
                warehouseTransferId = t.WarehouseTransferID,
                code = t.Code,
                transferDate = t.TransferDate,
                fromWarehouseName = t.FromWarehouse.Name,
                toWarehouseName = t.ToWarehouse.Name,
                status = t.Status,
                totalCostInBaseCurrency = t.TotalCostInBaseCurrency,
                journalEntryId = t.JournalEntryId,
                linesCount = t.Lines.Count(l => l.IsDeleted != true),
                notes = t.Notes,
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
                r.warehouseTransferId,
                r.code,
                transferDate = r.transferDate.ToString("yyyy-MM-dd"),
                r.fromWarehouseName,
                r.toWarehouseName,
                status = r.status.ToString(),
                statusLabel = GetStatusLabel(r.status),
                r.totalCostInBaseCurrency,
                r.journalEntryId,
                r.linesCount,
                r.notes,
            }),
        });
    }

    [HttpGet("{id:int}")]
    [HasPermission("inventory.transfers.view")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var transfer = await Db.WarehouseTransfers
            .AsNoTracking()
            .Where(t => t.WarehouseTransferID == id && t.IsDeleted != true)
            .Select(t => new
            {
                warehouseTransferId = t.WarehouseTransferID,
                code = t.Code,
                transferDate = t.TransferDate,
                fromWarehouseId = t.FromWarehouseId,
                fromWarehouseName = t.FromWarehouse.Name,
                toWarehouseId = t.ToWarehouseId,
                toWarehouseName = t.ToWarehouse.Name,
                status = t.Status,
                isPosted = t.IsPosted,
                totalCostInBaseCurrency = t.TotalCostInBaseCurrency,
                journalEntryId = t.JournalEntryId,
                notes = t.Notes,
                lines = t.Lines
                    .Where(l => l.IsDeleted != true)
                    .Select(l => new
                    {
                        warehouseTransferLineId = l.WarehouseTransferLineID,
                        productId = l.ProductId,
                        productCode = l.Product.Code,
                        productName = l.Product.Name,
                        meaurmentId = l.MeaurmentId,
                        meaurmentName = l.Meaurment.Name,
                        quantity = l.Quantity,
                        quantityInBase = l.QuantityInBase,
                        unitCostInBase = l.UnitCostInBase,
                        lineCostInBase = l.LineCostInBase,
                        notes = l.Notes,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (transfer is null)
        {
            return NotFound(new { message = "سند انتقال یافت نشد." });
        }

        return Ok(transfer);
    }

    [HttpPost]
    [HasPermission("inventory.transfers.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveWarehouseTransferRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.FromWarehouseId == request.ToWarehouseId)
        {
            return BadRequest(new { message = "انبار مبدأ و مقصد نمی‌توانند یکسان باشند." });
        }

        var fromWarehouse = await Db.Warehouses
            .FirstOrDefaultAsync(w => w.WarehouseID == request.FromWarehouseId && w.IsDeleted != true, cancellationToken);
        var toWarehouse = await Db.Warehouses
            .FirstOrDefaultAsync(w => w.WarehouseID == request.ToWarehouseId && w.IsDeleted != true, cancellationToken);

        if (fromWarehouse is null || toWarehouse is null)
        {
            return BadRequest(new { message = "انبار مبدأ یا مقصد یافت نشد." });
        }

        if (request.Lines is null || request.Lines.Count == 0)
        {
            return BadRequest(new { message = "حداقل یک ردیف انتقال وارد کنید." });
        }

        var userId = ResolveCurrentUserId();
        var transfer = new WarehouseTransfer
        {
            Code = await GenerateCodeAsync(cancellationToken),
            TransferDate = request.TransferDate ?? DateTime.Now,
            FromWarehouseId = request.FromWarehouseId,
            ToWarehouseId = request.ToWarehouseId,
            Status = WarehouseTransferStatus.Draft,
            Notes = request.Notes?.Trim(),
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = userId,
        };

        foreach (var line in request.Lines)
        {
            if (line.Quantity <= 0)
            {
                return BadRequest(new { message = "مقدار هر ردیف باید بزرگ‌تر از صفر باشد." });
            }

            var qtyInBase = await _conversion.ToBaseAsync(line.Quantity, line.MeaurmentId, cancellationToken);
            transfer.Lines.Add(new WarehouseTransferLine
            {
                ProductId = line.ProductId,
                MeaurmentId = line.MeaurmentId,
                Quantity = line.Quantity,
                QuantityInBase = qtyInBase,
                Notes = line.Notes?.Trim(),
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreatedBy = userId,
            });
        }

        Db.WarehouseTransfers.Add(transfer);
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "سند انتقال با موفقیت ثبت شد.",
            warehouseTransferId = transfer.WarehouseTransferID,
            code = transfer.Code,
        });
    }

    [HttpPost("{id:int}/post")]
    [HasPermission("inventory.transfers.edit")]
    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        var transfer = await Db.WarehouseTransfers
            .Include(t => t.Lines)
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .FirstOrDefaultAsync(t => t.WarehouseTransferID == id && t.IsDeleted != true, cancellationToken);

        if (transfer is null)
        {
            return NotFound(new { message = "سند انتقال یافت نشد." });
        }

        if (transfer.IsPosted || transfer.Status == WarehouseTransferStatus.Posted)
        {
            return Conflict(new { message = "این سند قبلاً ثبت نهایی شده است." });
        }

        if (transfer.Status == WarehouseTransferStatus.Cancelled)
        {
            return Conflict(new { message = "سند لغو‌شده قابل ثبت نیست." });
        }

        var lines = transfer.Lines.Where(l => l.IsDeleted != true).ToList();
        if (lines.Count == 0)
        {
            return BadRequest(new { message = "سند انتقال ردیف فعالی ندارد." });
        }

        var userId = ResolveCurrentUserId();
        await using var tx = await Db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await _fifo.ValidateAvailableStockAsync(
                transfer.FromWarehouseId,
                lines.Select(l => new AllocateStockRequest
                {
                    ProductId = l.ProductId,
                    WarehouseId = transfer.FromWarehouseId,
                    QuantityInBase = l.QuantityInBase,
                }).ToList(),
                cancellationToken);

            decimal totalCost = 0;
            foreach (var line in lines)
            {
                var allocations = await _fifo.TransferAsync(
                    line.ProductId,
                    transfer.FromWarehouseId,
                    transfer.ToWarehouseId,
                    line.QuantityInBase,
                    transfer.TransferDate,
                    userId,
                    cancellationToken);

                var lineCost = allocations.Sum(a => a.LineCost);
                line.LineCostInBase = Math.Round(lineCost, 4);
                line.UnitCostInBase = line.QuantityInBase > 0
                    ? Math.Round(lineCost / line.QuantityInBase, 4)
                    : 0;
                line.IsUpdated = true;
                line.UpdatedAt = DateTime.Now;
                line.UpdatedBy = userId;
                totalCost += line.LineCostInBase;
            }

            transfer.TotalCostInBaseCurrency = Math.Round(totalCost, 4);

            var journal = await _gl.PostWarehouseTransferAsync(
                transfer,
                transfer.FromWarehouse,
                transfer.ToWarehouse,
                userId,
                cancellationToken);

            transfer.JournalEntryId = journal?.JournalEntryID;
            transfer.IsPosted = true;
            transfer.PostedAt = DateTime.Now;
            transfer.Status = WarehouseTransferStatus.Posted;
            transfer.IsUpdated = true;
            transfer.UpdatedAt = DateTime.Now;
            transfer.UpdatedBy = userId;

            await Db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return Ok(new
            {
                message = journal is null
                    ? "انتقال ثبت شد و موجودی به‌روزرسانی شد؛ چون بهای انتقال صفر است یا حساب مبدأ/مقصد یکسان است، سند دفتر ثبت نشد."
                    : "انتقال ثبت شد؛ موجودی و سند دابل‌انتری ایجاد شد.",
                journalEntryId = transfer.JournalEntryId,
                totalCostInBaseCurrency = transfer.TotalCostInBaseCurrency,
            });
        }
        catch (Exception)
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    [HttpDelete("{id:int}")]
    [HasPermission("inventory.transfers.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await Db.WarehouseTransfers
            .FirstOrDefaultAsync(t => t.WarehouseTransferID == id && t.IsDeleted != true, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "سند انتقال یافت نشد." });
        }

        if (entity.IsPosted || entity.Status == WarehouseTransferStatus.Posted)
        {
            return Conflict(new { message = "سند ثبت‌شده قابل حذف نیست." });
        }

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.DeletedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "سند انتقال حذف شد." });
    }

    private async Task<string> GenerateCodeAsync(CancellationToken cancellationToken)
    {
        var codes = await Db.WarehouseTransfers
            .IgnoreQueryFilters()
            .Where(t => t.Code != null && t.Code.StartsWith("HMTR"))
            .Select(t => t.Code)
            .ToListAsync(cancellationToken);

        var maxSequence = codes
            .Select(c => int.TryParse(c.Substring(4), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"HMTR{(maxSequence + 1):D5}";
    }

    private static string GetStatusLabel(WarehouseTransferStatus status) => status switch
    {
        WarehouseTransferStatus.Draft => "پیش‌نویس",
        WarehouseTransferStatus.Posted => "ثبت‌شده",
        WarehouseTransferStatus.Cancelled => "لغو شده",
        _ => status.ToString(),
    };

    public class SaveWarehouseTransferRequest
    {
        [Required]
        public int FromWarehouseId { get; set; }

        [Required]
        public int ToWarehouseId { get; set; }

        public DateTime? TransferDate { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public List<SaveWarehouseTransferLineRequest> Lines { get; set; } = [];
    }

    public class SaveWarehouseTransferLineRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public int MeaurmentId { get; set; }

        public decimal Quantity { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
