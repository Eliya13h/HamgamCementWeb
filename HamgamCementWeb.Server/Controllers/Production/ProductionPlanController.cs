using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Controllers.Transport;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Production;

[ApiController]
[Route("api/production/plans")]
[Authorize]
public class ProductionPlanController : ControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(ProductionPlan.PlanDate),
        [2] = nameof(ProductionPlan.ProductId),
        [3] = nameof(ProductionPlan.PlannedQuantity),
    };

    private readonly AppDbContext _db;

    public ProductionPlanController(AppDbContext db)
    {
        _db = db;
    }

    private int? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    [HttpPost("datatable")]
    [HasPermission("production.plan.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = _db.ProductionPlans
            .AsNoTracking()
            .Where(p => p.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(p =>
                p.Product.Name.Contains(searchValue) ||
                (p.Notes != null && p.Notes.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(ProductionPlan.PlanDate), defaultDescending: true)
            .Skip(start)
            .Take(length)
            .Select(p => new
            {
                productionPlanId = p.ProductionPlanID,
                planDate = p.PlanDate,
                productId = p.ProductId,
                productName = p.Product.Name,
                productCode = p.Product.Code,
                meaurmentId = p.MeaurmentId,
                meaurmentName = p.Meaurment.Name,
                plannedQuantity = p.PlannedQuantity,
                notes = p.Notes,
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
                r.productionPlanId,
                planDate = r.planDate.ToString("yyyy-MM-dd"),
                r.productId,
                r.productName,
                r.productCode,
                r.meaurmentId,
                r.meaurmentName,
                r.plannedQuantity,
                r.notes,
            }),
        });
    }

    [HttpPost]
    [HasPermission("production.plan.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveProductionPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        _db.ProductionPlans.Add(new ProductionPlan
        {
            PlanDate = request.PlanDate,
            ProductId = request.ProductId,
            MeaurmentId = request.MeaurmentId,
            PlannedQuantity = request.PlannedQuantity,
            Notes = request.Notes?.Trim(),
            IsDeleted = false,
            IsActive = true,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        });

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "برنامه تولید ثبت شد." });
    }

    [HttpPut("{id:int}")]
    [HasPermission("production.plan.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveProductionPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var plan = await _db.ProductionPlans
            .FirstOrDefaultAsync(p => p.ProductionPlanID == id && p.IsDeleted != true, cancellationToken);

        if (plan is null)
        {
            return NotFound(new { message = "برنامه تولید یافت نشد." });
        }

        plan.PlanDate = request.PlanDate;
        plan.ProductId = request.ProductId;
        plan.MeaurmentId = request.MeaurmentId;
        plan.PlannedQuantity = request.PlannedQuantity;
        plan.Notes = request.Notes?.Trim();
        plan.IsUpdated = true;
        plan.UpdatedAt = DateTime.Now;
        plan.UpdatedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "برنامه تولید ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("production.plan.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var plan = await _db.ProductionPlans
            .FirstOrDefaultAsync(p => p.ProductionPlanID == id && p.IsDeleted != true, cancellationToken);

        if (plan is null)
        {
            return NotFound(new { message = "برنامه تولید یافت نشد." });
        }

        plan.IsDeleted = true;
        plan.IsActive = false;
        plan.DeletedAt = DateTime.Now;
        plan.DeletedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "برنامه تولید حذف شد." });
    }

    public class SaveProductionPlanRequest
    {
        [Required]
        public DateTime PlanDate { get; set; }

        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int MeaurmentId { get; set; }

        [Range(0.000001, double.MaxValue)]
        public decimal PlannedQuantity { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }
    }
}
