using System.ComponentModel.DataAnnotations;
using Dapper;
using HamgamTransport.Server.Controllers.Common;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.People;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Controllers.Transport;

[ApiController]
[Route("api/transport/drivers")]
[Authorize]
public class DriverController : TransportControllerBase
{
    private readonly ISqlConnectionFactory _sql;
    private readonly IAccountLookupService _accounts;

    public DriverController(AppDbContext db, ISqlConnectionFactory sql, IAccountLookupService accounts) : base(db)
    {
        _sql = sql;
        _accounts = accounts;
    }

    [HttpPost("datatable")]
    public async Task<IActionResult> DataTable([FromBody] DataTableRequest request, CancellationToken ct)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);
        var search = request.Search?.Value?.Trim();

        await using var conn = (System.Data.Common.DbConnection)await _sql.OpenAsync(ct);
        const string baseWhere = "WHERE d.IsDeleted = 0";
        var where = baseWhere;
        var p = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += " AND (d.Name LIKE @Search OR d.PhoneNumber LIKE @Search)";
            p.Add("Search", $"%{search}%");
        }

        var recordsTotal = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Drivers d WHERE d.IsDeleted = 0");
        var recordsFiltered = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(1) FROM Drivers d {where}", p);
        p.Add("Offset", start);
        p.Add("Fetch", length);

        var rows = (await conn.QueryAsync(
            $"""
             SELECT d.DriverId AS driverId, d.Name AS name, d.PhoneNumber AS phoneNumber,
                    d.LicenseNumber AS licenseNumber, d.VehicleOwnerId AS vehicleOwnerId,
                    vo.Name AS ownerName, d.DefaultProfitSharePercent AS defaultProfitSharePercent,
                    d.IsActive AS isActive
             FROM Drivers d
             LEFT JOIN VehicleOwners vo ON vo.VehicleOwnerId = d.VehicleOwnerId
             {where}
             ORDER BY d.Name OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
             """, p)).ToList();

        return Ok(new { request.Draw, recordsTotal, recordsFiltered, data = rows.Select((r, i) => { var d = (IDictionary<string, object>)r; return new { rowNumber = start + i + 1, driverId = d["driverId"], name = d["name"], phoneNumber = d["phoneNumber"], licenseNumber = d["licenseNumber"], vehicleOwnerId = d["vehicleOwnerId"], ownerName = d["ownerName"], defaultProfitSharePercent = d["defaultProfitSharePercent"], isActive = d["isActive"] }; }) });
    }

    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken ct) =>
        Ok(await Db.Drivers.AsNoTracking()
            .Where(d => d.IsDeleted != true && d.IsActive == true)
            .OrderBy(d => d.Name)
            .Select(d => new { value = d.DriverId, label = d.Name })
            .ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DriverRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var entity = new Driver
        {
            Name = request.Name.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty,
            LicenseNumber = request.LicenseNumber?.Trim() ?? string.Empty,
            VehicleOwnerId = request.VehicleOwnerId,
            DefaultProfitSharePercent = request.DefaultProfitSharePercent,
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };
        Db.Drivers.Add(entity);
        await Db.SaveChangesAsync(ct);
        var account = await _accounts.EnsureDriverAccountAsync(entity.DriverId, entity.Name, ct);
        entity.AccountId = account.AccountID;
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "راننده ثبت شد.", driverId = entity.DriverId });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] DriverRequest request, CancellationToken ct)
    {
        var entity = await Db.Drivers.FirstOrDefaultAsync(d => d.DriverId == id && d.IsDeleted != true, ct);
        if (entity is null) return NotFound(new { message = "یافت نشد." });
        entity.Name = request.Name.Trim();
        entity.PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
        entity.LicenseNumber = request.LicenseNumber?.Trim() ?? string.Empty;
        entity.VehicleOwnerId = request.VehicleOwnerId;
        entity.DefaultProfitSharePercent = request.DefaultProfitSharePercent;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = ResolveCurrentUserId();
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "به‌روزرسانی شد." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Db.Drivers.FirstOrDefaultAsync(d => d.DriverId == id && d.IsDeleted != true, ct);
        if (entity is null) return NotFound(new { message = "یافت نشد." });
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.DeletedBy = ResolveCurrentUserId();
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "حذف شد." });
    }
}

public class DriverRequest
{
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] public string? PhoneNumber { get; set; }
    [MaxLength(50)] public string? LicenseNumber { get; set; }
    public int? VehicleOwnerId { get; set; }
    public decimal? DefaultProfitSharePercent { get; set; }
    public bool IsActive { get; set; } = true;
}
