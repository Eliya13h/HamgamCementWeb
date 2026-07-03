using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.People;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.People;

[ApiController]
[Route("api/drivers")]
[Authorize]
public class DriverController : ControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = "FullName",
        [2] = nameof(Driver.NationalCode),
        [3] = nameof(Driver.Mobile),
        [4] = nameof(Driver.DefaultShare),
        [5] = nameof(Driver.IsActive),
    };

    private readonly AppDbContext _db;

    public DriverController(AppDbContext db)
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

        var query = _db.Drivers
            .AsNoTracking()
            .Where(d => d.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(d =>
                d.Name.Contains(searchValue) ||
                d.Family.Contains(searchValue) ||
                d.NationalCode.Contains(searchValue) ||
                d.Mobile.Contains(searchValue));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var orderedQuery = ApplyOrdering(query, request.Order);
        var rows = await orderedQuery
            .Skip(start)
            .Take(length)
            .Select(d => new DriverTableRow
            {
                DriverId = d.DriverID,
                Title = d.Title,
                Name = d.Name,
                FatherName = d.FatherName,
                Family = d.Family,
                NationalCode = d.NationalCode,
                Mobile = d.Mobile,
                Address = d.Address,
                DefaultShare = d.DefaultShare,
                IsActive = d.IsActive == true,
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
                r.DriverId,
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
        var items = await _db.Drivers
            .AsNoTracking()
            .Where(d => d.IsDeleted != true && d.IsActive == true)
            .OrderBy(d => d.Name)
            .ThenBy(d => d.Family)
            .Select(d => new
            {
                value = d.DriverID,
                label = d.Name + " " + d.Family,
                defaultVehicleId = d.DefaultVehicleId,
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost]
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

    private static IQueryable<Driver> ApplyOrdering(
        IQueryable<Driver> query,
        List<DataTableOrder>? orders)
    {
        if (orders is null || orders.Count == 0)
        {
            return query.OrderByDescending(d => d.CreatedAt);
        }

        IOrderedQueryable<Driver>? ordered = null;
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
                    ? query.OrderByDescending(d => d.Family).ThenByDescending(d => d.Name)
                    : query.OrderBy(d => d.Family).ThenBy(d => d.Name),
                "FullName" => descending
                    ? ordered!.ThenByDescending(d => d.Family).ThenByDescending(d => d.Name)
                    : ordered!.ThenBy(d => d.Family).ThenBy(d => d.Name),
                nameof(Driver.NationalCode) when ordered is null => descending
                    ? query.OrderByDescending(d => d.NationalCode)
                    : query.OrderBy(d => d.NationalCode),
                nameof(Driver.NationalCode) => descending
                    ? ordered!.ThenByDescending(d => d.NationalCode)
                    : ordered!.ThenBy(d => d.NationalCode),
                nameof(Driver.Mobile) when ordered is null => descending
                    ? query.OrderByDescending(d => d.Mobile)
                    : query.OrderBy(d => d.Mobile),
                nameof(Driver.Mobile) => descending
                    ? ordered!.ThenByDescending(d => d.Mobile)
                    : ordered!.ThenBy(d => d.Mobile),
                nameof(Driver.DefaultShare) when ordered is null => descending
                    ? query.OrderByDescending(d => d.DefaultShare)
                    : query.OrderBy(d => d.DefaultShare),
                nameof(Driver.DefaultShare) => descending
                    ? ordered!.ThenByDescending(d => d.DefaultShare)
                    : ordered!.ThenBy(d => d.DefaultShare),
                nameof(Driver.IsActive) when ordered is null => descending
                    ? query.OrderByDescending(d => d.IsActive)
                    : query.OrderBy(d => d.IsActive),
                nameof(Driver.IsActive) => descending
                    ? ordered!.ThenByDescending(d => d.IsActive)
                    : ordered!.ThenBy(d => d.IsActive),
                _ => ordered,
            };
        }

        return ordered ?? query.OrderByDescending(d => d.CreatedAt);
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

    public class DriverTableRow
    {
        public int RowNumber { get; set; }
        public int DriverId { get; set; }
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
