using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.People;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.People;

[ApiController]
[Route("api/vehicle-owners")]
[Authorize]
public class VehicleOwnerController : ControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = "FullName",
        [2] = nameof(VehicleOwner.NationalCode),
        [3] = nameof(VehicleOwner.Mobile),
        [4] = nameof(VehicleOwner.DefaultShare),
        [5] = nameof(VehicleOwner.IsActive),
    };

    private readonly AppDbContext _db;

    public VehicleOwnerController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("datatable")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var draw = request.Draw;
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = _db.VehicleOwners
            .AsNoTracking()
            .Where(v => v.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(v =>
                v.Name.Contains(searchValue) ||
                v.Family.Contains(searchValue) ||
                v.NationalCode.Contains(searchValue) ||
                v.Mobile.Contains(searchValue));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var orderedQuery = ApplyOrdering(query, request.Order);
        var rows = await orderedQuery
            .Skip(start)
            .Take(length)
            .Select(v => new VehicleOwnerTableRow
            {
                VehicleOwnerId = v.VehicleOwnerID,
                Title = v.Title,
                Name = v.Name,
                FatherName = v.FatherName,
                Family = v.Family,
                NationalCode = v.NationalCode,
                Mobile = v.Mobile,
                Address = v.Address,
                DefaultShare = v.DefaultShare,
                IsActive = v.IsActive == true,
            })
            .ToListAsync(cancellationToken);

        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].RowNumber = start + i + 1;
            rows[i].FullName = $"{rows[i].Name} {rows[i].Family}".Trim();
        }

        return Ok(new
        {
            draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select(r => new
            {
                r.RowNumber,
                r.VehicleOwnerId,
                title = (int)r.Title,
                r.Name,
                r.FatherName,
                r.Family,
                r.FullName,
                r.NationalCode,
                r.Mobile,
                r.Address,
                r.DefaultShare,
                r.IsActive,
            }),
        });
    }

    [HttpGet("list")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await _db.VehicleOwners
            .AsNoTracking()
            .Where(v => v.IsDeleted != true && v.IsActive == true)
            .OrderBy(v => v.Name)
            .ThenBy(v => v.Family)
            .Select(v => new
            {
                value = v.VehicleOwnerID,
                label = v.Name + " " + v.Family,
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveVehicleOwnerRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var vehicleOwner = new VehicleOwner
        {
            Title = request.Title,
            Name = request.Name.Trim(),
            FatherName = request.FatherName.Trim(),
            Family = request.Family.Trim(),
            NationalCode = request.NationalCode.Trim(),
            Mobile = request.Mobile.Trim(),
            Address = request.Address.Trim(),
            DefaultShare = request.DefaultShare,
            CreatedBy = ResolveCurrentUserId(),
            CreatedAt = DateTime.Now,
            IsActive = request.IsActive,
            IsDeleted = false,
        };

        _db.VehicleOwners.Add(vehicleOwner);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(Update),
            new { id = vehicleOwner.VehicleOwnerID },
            new { message = "موتردار با موفقیت ایجاد شد." });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveVehicleOwnerRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var vehicleOwner = await _db.VehicleOwners
            .FirstOrDefaultAsync(v => v.VehicleOwnerID == id && v.IsDeleted != true, cancellationToken);

        if (vehicleOwner is null)
        {
            return NotFound(new { message = "موتردار یافت نشد." });
        }

        vehicleOwner.Title = request.Title;
        vehicleOwner.Name = request.Name.Trim();
        vehicleOwner.FatherName = request.FatherName.Trim();
        vehicleOwner.Family = request.Family.Trim();
        vehicleOwner.NationalCode = request.NationalCode.Trim();
        vehicleOwner.Mobile = request.Mobile.Trim();
        vehicleOwner.Address = request.Address.Trim();
        vehicleOwner.DefaultShare = request.DefaultShare;
        vehicleOwner.IsActive = request.IsActive;
        vehicleOwner.UpdatedAt = DateTime.Now;
        vehicleOwner.IsUpdated = true;
        vehicleOwner.UpdatedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "موتردار با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var vehicleOwner = await _db.VehicleOwners
            .FirstOrDefaultAsync(v => v.VehicleOwnerID == id && v.IsDeleted != true, cancellationToken);

        if (vehicleOwner is null)
        {
            return NotFound(new { message = "موتردار یافت نشد." });
        }

        vehicleOwner.IsDeleted = true;
        vehicleOwner.IsActive = false;
        vehicleOwner.DeletedAt = DateTime.Now;
        vehicleOwner.DeletedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "موتردار با موفقیت حذف شد." });
    }

    private static IQueryable<VehicleOwner> ApplyOrdering(
        IQueryable<VehicleOwner> query,
        List<DataTableOrder>? orders)
    {
        if (orders is null || orders.Count == 0)
        {
            return query.OrderByDescending(v => v.CreatedAt);
        }

        IOrderedQueryable<VehicleOwner>? ordered = null;
        foreach (var order in orders)
        {
            if (!OrderColumns.TryGetValue(order.Column, out var column))
            {
                continue;
            }

            var descending = string.Equals(order.Dir, "desc", StringComparison.OrdinalIgnoreCase);

            ordered = column switch
            {
                "FullName" when ordered is null => descending
                    ? query.OrderByDescending(v => v.Family).ThenByDescending(v => v.Name)
                    : query.OrderBy(v => v.Family).ThenBy(v => v.Name),
                "FullName" => descending
                    ? ordered!.ThenByDescending(v => v.Family).ThenByDescending(v => v.Name)
                    : ordered!.ThenBy(v => v.Family).ThenBy(v => v.Name),
                nameof(VehicleOwner.NationalCode) when ordered is null => descending
                    ? query.OrderByDescending(v => v.NationalCode)
                    : query.OrderBy(v => v.NationalCode),
                nameof(VehicleOwner.NationalCode) => descending
                    ? ordered!.ThenByDescending(v => v.NationalCode)
                    : ordered!.ThenBy(v => v.NationalCode),
                nameof(VehicleOwner.Mobile) when ordered is null => descending
                    ? query.OrderByDescending(v => v.Mobile)
                    : query.OrderBy(v => v.Mobile),
                nameof(VehicleOwner.Mobile) => descending
                    ? ordered!.ThenByDescending(v => v.Mobile)
                    : ordered!.ThenBy(v => v.Mobile),
                nameof(VehicleOwner.DefaultShare) when ordered is null => descending
                    ? query.OrderByDescending(v => v.DefaultShare)
                    : query.OrderBy(v => v.DefaultShare),
                nameof(VehicleOwner.DefaultShare) => descending
                    ? ordered!.ThenByDescending(v => v.DefaultShare)
                    : ordered!.ThenBy(v => v.DefaultShare),
                nameof(VehicleOwner.IsActive) when ordered is null => descending
                    ? query.OrderByDescending(v => v.IsActive)
                    : query.OrderBy(v => v.IsActive),
                nameof(VehicleOwner.IsActive) => descending
                    ? ordered!.ThenByDescending(v => v.IsActive)
                    : ordered!.ThenBy(v => v.IsActive),
                _ => ordered,
            };
        }

        return ordered ?? query.OrderByDescending(v => v.CreatedAt);
    }

    private int? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public DataTableSearch? Search { get; set; }
        public List<DataTableOrder>? Order { get; set; }
    }

    public class DataTableSearch
    {
        public string? Value { get; set; }
        public bool Regex { get; set; }
    }

    public class DataTableOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; } = "asc";
    }

    public class VehicleOwnerTableRow
    {
        public int RowNumber { get; set; }
        public int VehicleOwnerId { get; set; }
        public PersonTitle Title { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FatherName { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal DefaultShare { get; set; }
        public bool IsActive { get; set; }
    }

    public class SaveVehicleOwnerRequest
    {
        public PersonTitle Title { get; set; } = PersonTitle.Mr;

        [Required(ErrorMessage = "نام الزامی است.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FatherName { get; set; } = string.Empty;

        [Required(ErrorMessage = "نام خانوادگی الزامی است.")]
        [MaxLength(100)]
        public string Family { get; set; } = string.Empty;

        [MaxLength(20)]
        public string NationalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "موبایل الزامی است.")]
        [MaxLength(20)]
        public string Mobile { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        public decimal DefaultShare { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
