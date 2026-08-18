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
            where += """
                 AND (v.Code LIKE @Search OR v.PlateNumber LIKE @Search OR v.ChassisNumber LIKE @Search
                      OR v.Model LIKE @Search OR vo.Name LIKE @Search OR vt.Name LIKE @Search)
                """;
            p.Add("Search", $"%{search}%");
        }

        var recordsTotal = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Vehicles v WHERE v.IsDeleted = 0");
        var recordsFiltered = await conn.ExecuteScalarAsync<int>(
            $"""
             SELECT COUNT(1) FROM Vehicles v
             INNER JOIN VehicleOwners vo ON vo.VehicleOwnerId = v.VehicleOwnerId
             INNER JOIN VehicleTypes vt ON vt.VehicleTypeId = v.VehicleTypeId
             {where}
             """, p);
        p.Add("Offset", start);
        p.Add("Fetch", length);

        var rows = (await conn.QueryAsync(
            $"""
             SELECT v.VehicleId AS vehicleId, v.Code AS code, v.PlateNumber AS plateNumber,
                    v.VehicleTypeId AS vehicleTypeId, vt.Name AS typeName, vt.Code AS typeCode,
                    v.VehicleOwnerId AS vehicleOwnerId, vo.Name AS ownerName,
                    v.ChassisNumber AS chassisNumber, v.Model AS model,
                    v.ManufactureYear AS manufactureYear, v.WeightTon AS weightTon, v.Volume AS volume,
                    v.DefaultIncomeSharePercent AS defaultIncomeSharePercent,
                    v.DefaultDriverId AS defaultDriverId, d.Name AS defaultDriverName,
                    v.RoleInPair AS roleInPair, v.IsActive AS isActive
             FROM Vehicles v
             INNER JOIN VehicleOwners vo ON vo.VehicleOwnerId = v.VehicleOwnerId
             INNER JOIN VehicleTypes vt ON vt.VehicleTypeId = v.VehicleTypeId
             LEFT JOIN Drivers d ON d.DriverId = v.DefaultDriverId
             {where}
             ORDER BY v.Code, v.PlateNumber OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
             """, p)).ToList();

        return Ok(new
        {
            request.Draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select((r, i) =>
            {
                var d = (IDictionary<string, object>)r;
                return new
                {
                    rowNumber = start + i + 1,
                    vehicleId = d["vehicleId"],
                    code = d["code"],
                    plateNumber = d["plateNumber"],
                    vehicleTypeId = d["vehicleTypeId"],
                    typeName = d["typeName"],
                    typeCode = d["typeCode"],
                    vehicleOwnerId = d["vehicleOwnerId"],
                    ownerName = d["ownerName"],
                    chassisNumber = d["chassisNumber"],
                    model = d["model"],
                    manufactureYear = d["manufactureYear"],
                    weightTon = d["weightTon"],
                    volume = d["volume"],
                    defaultIncomeSharePercent = d["defaultIncomeSharePercent"],
                    defaultDriverId = d["defaultDriverId"],
                    defaultDriverName = d["defaultDriverName"],
                    roleInPair = d["roleInPair"],
                    isActive = d["isActive"],
                };
            }),
        });
    }

    [HttpGet("options")]
    public async Task<IActionResult> Options([FromQuery] VehicleRole? role, [FromQuery] string? typeCode, CancellationToken ct)
    {
        var q = Db.Vehicles.AsNoTracking()
            .Include(v => v.VehicleType)
            .Where(v => v.IsDeleted != true && v.IsActive == true);
        if (role is not null) q = q.Where(v => v.RoleInPair == role);
        if (!string.IsNullOrWhiteSpace(typeCode))
        {
            q = q.Where(v => v.VehicleType != null && v.VehicleType.Code == typeCode);
        }

        var rows = await q.OrderBy(v => v.Code).ThenBy(v => v.PlateNumber)
            .Select(v => new
            {
                value = v.VehicleId,
                label = (v.Code == "" ? v.PlateNumber : v.Code + " — " + v.PlateNumber),
                role = (int)v.RoleInPair,
                typeCode = v.VehicleType != null ? v.VehicleType.Code : null,
                defaultDriverId = v.DefaultDriverId,
                defaultIncomeSharePercent = v.DefaultIncomeSharePercent,
                vehicleOwnerId = v.VehicleOwnerId,
            })
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VehicleRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var type = await Db.VehicleTypes.FirstOrDefaultAsync(
            t => t.VehicleTypeId == request.VehicleTypeId && t.IsDeleted != true, ct);
        if (type is null) return BadRequest(new { message = "نوع وسیله نامعتبر است." });

        var plate = request.PlateNumber.Trim();
        if (await Db.Vehicles.AnyAsync(v => v.PlateNumber == plate && v.IsDeleted != true, ct))
            return BadRequest(new { message = "پلاک تکراری است." });

        var error = ValidateByType(type, request);
        if (error is not null) return BadRequest(new { message = error });

        var userId = ResolveCurrentUserId();
        var cc = new CostCenter
        {
            Code = $"V-{plate}",
            Name = $"وسیله {plate}",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = userId,
        };
        Db.CostCenters.Add(cc);
        await Db.SaveChangesAsync(ct);

        var entity = MapRequest(new Vehicle(), request, type);
        entity.CostCenterId = cc.CostCenterID;
        entity.IsDeleted = false;
        entity.CreatedAt = DateTime.Now;
        entity.CreatedBy = userId;
        Db.Vehicles.Add(entity);
        await Db.SaveChangesAsync(ct);

        entity.Code = TransportCodeHelper.Vehicle(entity.VehicleId);
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "وسیله ثبت شد.", vehicleId = entity.VehicleId, code = entity.Code });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] VehicleRequest request, CancellationToken ct)
    {
        var entity = await Db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == id && v.IsDeleted != true, ct);
        if (entity is null) return NotFound(new { message = "یافت نشد." });

        var type = await Db.VehicleTypes.FirstOrDefaultAsync(
            t => t.VehicleTypeId == request.VehicleTypeId && t.IsDeleted != true, ct);
        if (type is null) return BadRequest(new { message = "نوع وسیله نامعتبر است." });

        var plate = request.PlateNumber.Trim();
        if (await Db.Vehicles.AnyAsync(v => v.VehicleId != id && v.PlateNumber == plate && v.IsDeleted != true, ct))
            return BadRequest(new { message = "پلاک تکراری است." });

        var error = ValidateByType(type, request);
        if (error is not null) return BadRequest(new { message = error });

        MapRequest(entity, request, type);
        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = ResolveCurrentUserId();
        if (string.IsNullOrWhiteSpace(entity.Code))
        {
            entity.Code = TransportCodeHelper.Vehicle(entity.VehicleId);
        }

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

    private static string? ValidateByType(VehicleType type, VehicleRequest request)
    {
        if (type.Code == "BUNKER")
        {
            if (request.DefaultDriverId is > 0)
            {
                return "بونکر راننده پیش‌فرض ندارد.";
            }
        }

        return null;
    }

    private static Vehicle MapRequest(Vehicle entity, VehicleRequest request, VehicleType type)
    {
        var isBunker = type.Code == "BUNKER";
        entity.PlateNumber = request.PlateNumber.Trim();
        entity.VehicleTypeId = request.VehicleTypeId;
        entity.VehicleOwnerId = request.VehicleOwnerId;
        entity.ChassisNumber = (request.ChassisNumber ?? string.Empty).Trim();
        entity.Model = (request.Model ?? string.Empty).Trim();
        entity.ManufactureYear = request.ManufactureYear;
        entity.WeightTon = isBunker ? request.WeightTon : null;
        entity.Volume = isBunker ? request.Volume : null;
        entity.DefaultIncomeSharePercent = request.DefaultIncomeSharePercent;
        entity.DefaultDriverId = isBunker ? null : request.DefaultDriverId;
        entity.RoleInPair = type.DefaultRole;
        entity.IsActive = request.IsActive;
        return entity;
    }
}

public class VehicleRequest
{
    [Required, MaxLength(30)] public string PlateNumber { get; set; } = string.Empty;
    public int VehicleTypeId { get; set; }
    public int VehicleOwnerId { get; set; }
    [MaxLength(80)] public string? ChassisNumber { get; set; }
    [MaxLength(80)] public string? Model { get; set; }
    public int? ManufactureYear { get; set; }
    public decimal? WeightTon { get; set; }
    public decimal? Volume { get; set; }
    public decimal? DefaultIncomeSharePercent { get; set; }
    public int? DefaultDriverId { get; set; }
    public bool IsActive { get; set; } = true;
}
