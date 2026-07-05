using System.ComponentModel.DataAnnotations;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Transport;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Transport;

[ApiController]
[Route("api/transport/routes")]
[Authorize]
public class TransportRouteController : TransportControllerBase
{
    // اصلاح شد: کلید تکراری [2] باعث می‌شد ستون «نام» قابل مرتب‌سازی نباشد؛ کلیدهای متمایز و ترتیبی شدند.
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(TransportRoute.Code),
        [2] = nameof(TransportRoute.Name),
        [3] = nameof(TransportRoute.Origin),
        [4] = nameof(TransportRoute.Destination),
        [5] = nameof(TransportRoute.DistanceKm),
        [6] = nameof(TransportRoute.EstimatedDays),
        [7] = nameof(TransportRoute.IsActive),
    };

    public TransportRouteController(AppDbContext db) : base(db)
    {
    }

    [HttpPost("datatable")]
    [HasPermission("transport.routes.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.TransportRoutes
            .AsNoTracking()
            .Where(r => r.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(r =>
                r.Code.Contains(searchValue) ||
                r.Name.Contains(searchValue) ||
                r.Origin.Contains(searchValue) ||
                r.Destination.Contains(searchValue) ||
                (r.OriginCountry != null && r.OriginCountry.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(TransportRoute.CreatedAt))
            .Skip(start)
            .Take(length)
            .Select(r => new
            {
                transportRouteId = r.TransportRouteID,
                code = r.Code,
                name = r.Name,
                origin = r.Origin,
                originCountry = r.OriginCountry,
                destination = r.Destination,
                distanceKm = r.DistanceKm,
                estimatedDays = r.EstimatedDays,
                description = r.Description,
                isActive = r.IsActive == true,
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
                r.transportRouteId,
                r.code,
                r.name,
                r.origin,
                r.originCountry,
                r.destination,
                r.distanceKm,
                r.estimatedDays,
                r.description,
                r.isActive,
            }),
        });
    }

    // لیست مسیرها برای دراپ‌داون‌ها
    [HttpGet("list")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await Db.TransportRoutes
            .AsNoTracking()
            .Where(r => r.IsDeleted != true && r.IsActive == true)
            .OrderBy(r => r.Name)
            .Select(r => new { value = r.TransportRouteID, label = r.Code + " — " + r.Name })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost]
    [HasPermission("transport.routes.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveRouteRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var route = new TransportRoute
        {
            Code = $"TMP{DateTime.UtcNow.Ticks}",
            Name = request.Name.Trim(),
            Origin = request.Origin.Trim(),
            OriginCountry = request.OriginCountry?.Trim(),
            Destination = request.Destination.Trim(),
            DistanceKm = request.DistanceKm,
            EstimatedDays = request.EstimatedDays,
            Description = request.Description?.Trim(),
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };

        Db.TransportRoutes.Add(route);
        await Db.SaveChangesAsync(cancellationToken);

        route.Code = TransportCodeHelper.ForRoute(route.TransportRouteID);
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "مسیر با موفقیت ایجاد شد." });
    }

    [HttpPut("{id:int}")]
    [HasPermission("transport.routes.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveRouteRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var route = await Db.TransportRoutes
            .FirstOrDefaultAsync(r => r.TransportRouteID == id && r.IsDeleted != true, cancellationToken);
        if (route is null)
        {
            return NotFound(new { message = "مسیر یافت نشد." });
        }

        route.Name = request.Name.Trim();
        route.Origin = request.Origin.Trim();
        route.OriginCountry = request.OriginCountry?.Trim();
        route.Destination = request.Destination.Trim();
        route.DistanceKm = request.DistanceKm;
        route.EstimatedDays = request.EstimatedDays;
        route.Description = request.Description?.Trim();
        route.IsActive = request.IsActive;
        route.IsUpdated = true;
        route.UpdatedAt = DateTime.Now;
        route.UpdatedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "مسیر با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("transport.routes.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var route = await Db.TransportRoutes
            .FirstOrDefaultAsync(r => r.TransportRouteID == id && r.IsDeleted != true, cancellationToken);
        if (route is null)
        {
            return NotFound(new { message = "مسیر یافت نشد." });
        }

        route.IsDeleted = true;
        route.IsActive = false;
        route.DeletedAt = DateTime.Now;
        route.DeletedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "مسیر با موفقیت حذف شد." });
    }

    public class SaveRouteRequest
    {
        [Required(ErrorMessage = "نام مسیر الزامی است.")]
        [MaxLength(300)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "مبدأ الزامی است.")]
        [MaxLength(200)]
        public string Origin { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? OriginCountry { get; set; }

        [Required(ErrorMessage = "مقصد الزامی است.")]
        [MaxLength(200)]
        public string Destination { get; set; } = string.Empty;

        public decimal? DistanceKm { get; set; }

        public int? EstimatedDays { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
