using System.ComponentModel.DataAnnotations;
using HamgamTransport.Server.Authorization;
using HamgamTransport.Server.Controllers.Common;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HamgamTransport.Server.Controllers.Inventory;

[ApiController]
[Route("api/inventory/opening")]
[Authorize]
public class InventoryOpeningController : InventoryControllerBase
{
    private readonly IInventoryOpeningService _opening;

    public InventoryOpeningController(AppDbContext db, IInventoryOpeningService opening) : base(db)
    {
        _opening = opening;
    }

    [HttpPost]
    [HasPermission("inventory.opening.create")]
    public async Task<IActionResult> PostOpening(
        [FromBody] PostInventoryOpeningRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.Lines is null || request.Lines.Count == 0)
        {
            return BadRequest(new { message = "حداقل یک ردیف موجودی اول دوره الزامی است." });
        }

        try
        {
            var lines = request.Lines
                .Select(l => new InventoryOpeningLine(l.ProductId, l.QuantityInBase, l.UnitCost))
                .ToList();

            var entry = await _opening.PostOpeningAsync(
                request.WarehouseId,
                lines,
                request.Date,
                ResolveCurrentUserId(),
                cancellationToken);

            return Ok(new
            {
                message = "موجودی اول دوره با موفقیت ثبت شد.",
                journalEntryId = entry.JournalEntryID,
                entryNumber = entry.EntryNumber,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public class PostInventoryOpeningRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "انتخاب انبار الزامی است.")]
        public int WarehouseId { get; set; }

        public DateTime? Date { get; set; }

        [MinLength(1, ErrorMessage = "حداقل یک ردیف موجودی اول دوره الزامی است.")]
        public List<InventoryOpeningLineRequest> Lines { get; set; } = [];
    }

    public class InventoryOpeningLineRequest
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Range(0.0001, double.MaxValue)]
        public decimal QuantityInBase { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitCost { get; set; }
    }
}
