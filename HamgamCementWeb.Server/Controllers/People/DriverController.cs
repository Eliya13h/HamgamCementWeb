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
[Route("api/drivers")]
[Authorize]
public class DriverController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDriverReadService _reads;

    public DriverController(AppDbContext db, IDriverReadService reads)
    {
        _db = db;
        _reads = reads;
    }

    [HttpPost("datatable")]
    [HasPermission("people.drivers.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reads.QueryDataTableAsync(
            new DriverDataTableQuery
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
                r.DriverId,
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
        return Ok(items.Select(d => new
        {
            value = d.Value,
            label = d.Label,
            defaultVehicleId = d.DefaultVehicleId,
        }));
    }

    [HttpPost]
    [HasPermission("people.drivers.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveDriverRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var driver = new Driver
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

        _db.Drivers.Add(driver);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(Update),
            new { id = driver.DriverID },
            new { message = "راننده با موفقیت ایجاد شد." });
    }

    [HttpPut("{id:int}")]
    [HasPermission("people.drivers.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveDriverRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var driver = await _db.Drivers
            .FirstOrDefaultAsync(d => d.DriverID == id && d.IsDeleted != true, cancellationToken);

        if (driver is null)
        {
            return NotFound(new { message = "راننده یافت نشد." });
        }

        driver.Title = request.Title;
        driver.Name = request.Name.Trim();
        driver.FatherName = request.FatherName.Trim();
        driver.Family = request.Family.Trim();
        driver.NationalCode = request.NationalCode.Trim();
        driver.Mobile = request.Mobile.Trim();
        driver.Address = request.Address.Trim();
        driver.DefaultShare = request.DefaultShare;
        driver.IsActive = request.IsActive;
        driver.UpdatedAt = DateTime.Now;
        driver.IsUpdated = true;
        driver.UpdatedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "راننده با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("people.drivers.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .FirstOrDefaultAsync(d => d.DriverID == id && d.IsDeleted != true, cancellationToken);

        if (driver is null)
        {
            return NotFound(new { message = "راننده یافت نشد." });
        }

        driver.IsDeleted = true;
        driver.IsActive = false;
        driver.DeletedAt = DateTime.Now;
        driver.DeletedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "راننده با موفقیت حذف شد." });
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

    public class SaveDriverRequest
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
