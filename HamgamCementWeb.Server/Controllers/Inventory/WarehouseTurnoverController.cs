using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HamgamCementWeb.Server.Controllers.Inventory;

[ApiController]
[Route("api/inventory/turnover")]
[Authorize]
public class WarehouseTurnoverController : InventoryControllerBase
{
    private readonly IWarehouseTurnoverService _turnover;

    public WarehouseTurnoverController(AppDbContext db, IWarehouseTurnoverService turnover) : base(db)
    {
        _turnover = turnover;
    }

    [HttpPost("datatable")]
    public async Task<IActionResult> DataTable(
        [FromBody] WarehouseTurnoverDataTableRequest request,
        CancellationToken cancellationToken)
    {
        if (request.WarehouseId is not > 0)
        {
            return Ok(new
            {
                draw = request.Draw,
                recordsTotal = 0,
                recordsFiltered = 0,
                data = Array.Empty<object>(),
            });
        }

        var (recordsTotal, recordsFiltered, rows) = await _turnover.GetDataTableAsync(request, cancellationToken);

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal,
            recordsFiltered,
            data = rows,
        });
    }
}
