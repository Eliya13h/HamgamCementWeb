using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.People;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.People;

[ApiController]
[Route("api/vehicle-owners")]
[Authorize]
public class VehicleOwnerController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IVehicleOwnerReadService _reads;

    public VehicleOwnerController(AppDbContext db, IVehicleOwnerReadService reads)
    {
        _db = db;
        _reads = reads;
    }

    [HttpPost("datatable")]
    [HasPermission("people.vehicle-owners.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reads.QueryDataTableAsync(
            new VehicleOwnerDataTableQuery
            {
                Start = request.Start,
                Length = request.Length,
                Search = request.Search?.Value,
                Order = request.Order?
                    .Select(o => new DataTableOrderItem { Column = o.Column, Dir = o.Dir })
                    .ToList(),
            },
            cancellationToken);

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Rows.Select(r => new
            {
                r.RowNumber,
                r.VehicleOwnerId,
                title = r.Title,
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
        var items = await _reads.ListActiveAsync(cancellationToken);
        return Ok(items.Select(v => new
        {
            value = v.Value,
            label = v.Label,
        }));
    }

    [HttpPost]
    [HasPermission("people.vehicle-owners.create")]
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
    [HasPermission("people.vehicle-owners.edit")]
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
    [HasPermission("people.vehicle-owners.delete")]
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
