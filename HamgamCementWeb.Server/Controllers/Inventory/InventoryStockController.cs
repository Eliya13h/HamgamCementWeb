using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Controllers.Transport;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Inventory;

[ApiController]
[Route("api/inventory/stocks")]
[Authorize]
public class InventoryStockController : InventoryControllerBase
{
    private readonly IMeaurmentConversionService _conversion;

    public InventoryStockController(AppDbContext db, IMeaurmentConversionService conversion) : base(db)
    {
        _conversion = conversion;
    }

    [HttpPost("datatable")]
    [HasPermission("inventory.stock.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.InventoryStocks
            .AsNoTracking()
            .Where(s => s.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(s =>
                s.Product.Name.Contains(searchValue) ||
                s.Product.Code.Contains(searchValue) ||
                s.Warehouse.Name.Contains(searchValue));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .Skip(start)
            .Take(length)
            .Select(s => new
            {
                inventoryStockId = s.InventoryStockID,
                warehouseId = s.WarehouseId,
                warehouseName = s.Warehouse.Name,
                productId = s.ProductId,
                productCode = s.Product.Code,
                productName = s.Product.Name,
                quantityInBase = s.QuantityInBase,
                baseMeaurmentId = s.Product.BaseMeaurmentId,
                defaultMeaurmentId = s.Product.DefaultMeaurmentId,
                defaultMeaurmentName = s.Product.DefaultMeaurment != null
                    ? s.Product.DefaultMeaurment.Name
                    : null,
            })
            .ToListAsync(cancellationToken);

        var data = new List<object>();
        foreach (var row in rows)
        {
            decimal displayQty = row.quantityInBase;
            string displayUnit;

            if (row.defaultMeaurmentId.HasValue)
            {
                var unit = await _conversion.GetMeaurmentAsync(row.defaultMeaurmentId.Value, cancellationToken);
                displayQty = _conversion.FromBaseQuantity(row.quantityInBase, unit);
                displayUnit = unit.Name;
            }
            else
            {
                var baseUnit = await _conversion.GetBaseUnitAsync(row.baseMeaurmentId, cancellationToken);
                displayUnit = baseUnit.Name;
            }

            data.Add(new
            {
                rowNumber = start + data.Count + 1,
                row.inventoryStockId,
                row.warehouseId,
                row.warehouseName,
                row.productId,
                row.productCode,
                row.productName,
                row.quantityInBase,
                displayQuantity = displayQty,
                displayUnit,
            });
        }

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal,
            recordsFiltered,
            data,
        });
    }

    // لات‌های موجود یک کالا در انبار — شامل رهگیری بچ تولید
    [HttpGet("lots")]
    [HasPermission("inventory.stock.view")]
    public async Task<IActionResult> Lots(
        [FromQuery] int warehouseId,
        [FromQuery] int productId,
        CancellationToken cancellationToken)
    {
        if (warehouseId <= 0 || productId <= 0)
        {
            return BadRequest(new { message = "انبار و کالا الزامی است." });
        }

        var lots = await Db.InventoryLots
            .AsNoTracking()
            .Where(l =>
                l.WarehouseId == warehouseId &&
                l.ProductId == productId &&
                l.IsDeleted != true &&
                l.RemainingQuantityInBase > 0)
            .OrderBy(l => l.ReceiptSequence)
            .Select(l => new
            {
                inventoryLotId = l.InventoryLotID,
                lotCode = l.LotCode,
                remainingQuantityInBase = l.RemainingQuantityInBase,
                receivedQuantityInBase = l.ReceivedQuantityInBase,
                unitCost = l.UnitCost,
                receivedAt = l.ReceivedAt,
                productionBatchId = l.ProductionBatchId,
                productionBatchNumber = l.ProductionBatchId != null
                    ? Db.ProductionBatches
                        .Where(b => b.ProductionBatchID == l.ProductionBatchId)
                        .Select(b => b.BatchNumber)
                        .FirstOrDefault()
                    : null,
                purchaseInvoiceId = l.PurchaseInvoiceId,
            })
            .ToListAsync(cancellationToken);

        return Ok(lots);
    }
}
