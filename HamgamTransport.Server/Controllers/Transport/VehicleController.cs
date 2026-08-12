using System.ComponentModel.DataAnnotations;
using Dapper;
using HamgamTransport.Server.Controllers.Common;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using HamgamTransport.Server.Data.Models.Transport;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Controllers.Transport;

[ApiController]
[Route("api/transport/vehicles")]
[Authorize]
public class VehicleController : TransportControllerBase
{
    private readonly ISqlConnectionFactory _sql;

    public VehicleController(AppDbContext db, ISqlConnectionFactory sql) : base(db)
    {
        _sql = sql;
    }

    [HttpPost("datatable")]
    public async Task<IActionResult> DataTable([FromBody] DataTableRequest request, CancellationToken ct)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);
        var search = request.Search?.Value?.Trim();

        await using var conn = (System.Data.Common.DbConnection)await _sql.OpenAsync(ct);
        const string baseWhere = "WHERE v.IsDeleted = 0";
        var where = baseWhere;
        var p = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += " AND (v.PlateNumber LIKE @Search OR vo.Name LIKE @Search OR vt.Name LIKE @Search)";
            p.Add("Search", $"%{search}%");
        }

        var recordsTotal = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Vehicles v WHERE v.IsDeleted = 0");
        var recordsFiltered = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM Vehicles v INNER JOIN VehicleOwners vo ON vo.VehicleOwnerId = v.VehicleOwnerId INNER JOIN VehicleTypes vt ON vt.VehicleTypeId = v.VehicleTypeId {where}", p);
        p.Add("Offset", start);
        p.Add("Fetch", length);

        var rows = (await conn.QueryAsync(
            $"""
             SELECT v.VehicleId AS vehicleId, v.PlateNumber AS plateNumber,
                    vt.Name AS typeName, vo.Name AS ownerName,
                    v.RoleInPair AS roleInPair, v.VehiclePairId AS vehiclePairId,
                    v.IsActive AS isActive
             FROM Vehicles v
             INNER JOIN VehicleOwners vo ON vo.VehicleOwnerId = v.VehicleOwnerId
             INNER JOIN VehicleTypes vt ON vt.VehicleTypeId = v.VehicleTypeId
             {where}
             ORDER BY v.PlateNumber OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
             """, p)).ToList();

        return Ok(new { request.Draw, recordsTotal, recordsFiltered, data = rows.Select((r, i) => { var d = (IDictionary<string, object>)r; return new { rowNumber = start + i + 1, vehicleId = d["vehicleId"], plateNumber = d["plateNumber"], typeName = d["typeName"], ownerName = d["ownerName"], roleInPair = d["roleInPair"], vehiclePairId = d["vehiclePairId"], isActive = d["isActive"] }; }) });
    }

    [HttpGet("options")]
    public async Task<IActionResult> Options([FromQuery] VehicleRole? role, CancellationToken ct)
    {
        var q = Db.Vehicles.AsNoTracking().Where(v => v.IsDeleted != true && v.IsActive == true);
        if (role is not null) q = q.Where(v => v.RoleInPair == role);
        var rows = await q.OrderBy(v => v.PlateNumber)
            .Select(v => new { value = v.VehicleId, label = v.PlateNumber, role = (int)v.RoleInPair })
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VehicleRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var plate = request.PlateNumber.Trim();
        if (await Db.Vehicles.AnyAsync(v => v.PlateNumber == plate && v.IsDeleted != true, ct))
            return BadRequest(new { message = "پلاک تکراری است." });

        var cc = new CostCenter
        {
            Code = $"V-{plate}",
            Name = $"وسیله {plate}",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };
        Db.CostCenters.Add(cc);
        await Db.SaveChangesAsync(ct);

        var entity = new Vehicle
        {
            PlateNumber = plate,
            VehicleTypeId = request.VehicleTypeId,
            VehicleOwnerId = request.VehicleOwnerId,
            CostCenterId = cc.CostCenterID,
            VehiclePairId = request.VehiclePairId,
            RoleInPair = request.RoleInPair,
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };
        Db.Vehicles.Add(entity);
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "وسیله ثبت شد.", vehicleId = entity.VehicleId });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] VehicleRequest request, CancellationToken ct)
    {
        var entity = await Db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == id && v.IsDeleted != true, ct);
        if (entity is null) return NotFound(new { message = "یافت نشد." });
        entity.VehicleTypeId = request.VehicleTypeId;
        entity.VehicleOwnerId = request.VehicleOwnerId;
        entity.VehiclePairId = request.VehiclePairId;
        entity.RoleInPair = request.RoleInPair;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = ResolveCurrentUserId();
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "به‌روزرسانی شد." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == id && v.IsDeleted != true, ct);
        if (entity is null) return NotFound(new { message = "یافت نشد." });
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.DeletedBy = ResolveCurrentUserId();
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "حذف شد." });
    }
}

public class VehicleRequest
{
    [Required, MaxLength(30)] public string PlateNumber { get; set; } = string.Empty;
    public int VehicleTypeId { get; set; }
    public int VehicleOwnerId { get; set; }
    public int? VehiclePairId { get; set; }
    public VehicleRole RoleInPair { get; set; } = VehicleRole.Primary;
    public bool IsActive { get; set; } = true;
}
