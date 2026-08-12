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
[Route("api/production/plans")]
[Authorize]
public class ProductionPlanController : ControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [2] = "PlanDate",
        [3] = "PlannedQuantity",
    };

    private readonly AppDbContext _db;
    private readonly IProductionPlanReadService _planRead;

    public ProductionPlanController(AppDbContext db, IProductionPlanReadService planRead)
    {
        _db = db;
        _planRead = planRead;
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
        var order = request.Order?.FirstOrDefault();
        var orderColumn = order is not null && OrderColumns.TryGetValue(order.Column, out var col)
            ? col
            : "PlanDate";
        // پیش‌فرض: تاریخ برنامه نزولی
        var ascending = order is null
            ? false
            : !string.Equals(order.Dir, "desc", StringComparison.OrdinalIgnoreCase);

        var (recordsTotal, recordsFiltered, rows) = await _planRead.GetDataTableAsync(
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
                productionPlanId = r.ProductionPlanId,
                planDate = r.PlanDate.ToString("yyyy-MM-dd"),
                productId = r.ProductId,
                productName = r.ProductName,
                productCode = r.ProductCode,
                meaurmentId = r.MeaurmentId,
                meaurmentName = r.MeaurmentName,
                plannedQuantity = r.PlannedQuantity,
                notes = r.Notes,
                linkedBatchesCount = r.LinkedBatchesCount,
                postedBatchesCount = r.PostedBatchesCount,
                statusLabel = r.PostedBatchesCount > 0
                    ? "تولید شده"
                    : r.LinkedBatchesCount > 0
                        ? "در حال تولید"
                        : "برنامه‌ریزی",
            }),
        });
    }

    [HttpGet("list")]
    [HasPermission("production.plan.view")]
    public async Task<IActionResult> List(
        [FromQuery] int? productId,
        [FromQuery] int start = 0,
        [FromQuery] int length = 100,
        CancellationToken cancellationToken = default)
    {
        var items = await _planRead.GetListAsync(productId, start, length, cancellationToken);
        return Ok(items.Select(p => new
        {
            value = p.Value,
            label = p.Label,
            productId = p.ProductId,
            meaurmentId = p.MeaurmentId,
            plannedQuantity = p.PlannedQuantity,
            planDate = p.PlanDate,
            defaultFormulaId = p.DefaultFormulaId,
        }));
    }

    [HttpGet("{id:int}")]
    [HasPermission("production.plan.view")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var plan = await _planRead.GetByIdAsync(id, cancellationToken);
        if (plan is null)
        {
            return NotFound(new { message = "برنامه تولید یافت نشد." });
        }

        return Ok(new
        {
            productionPlanId = plan.ProductionPlanId,
            planDate = plan.PlanDate,
            productId = plan.ProductId,
            productName = plan.ProductName,
            meaurmentId = plan.MeaurmentId,
            meaurmentName = plan.MeaurmentName,
            plannedQuantity = plan.PlannedQuantity,
            notes = plan.Notes,
            defaultFormulaId = plan.DefaultFormulaId,
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
