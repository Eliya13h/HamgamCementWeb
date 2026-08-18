using System.ComponentModel.DataAnnotations;
using Dapper;
using HamgamTransport.Server.Controllers.Common;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Transport;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Controllers.Transport;

[ApiController]
[Route("api/transport/vehicle-pairs")]
[Authorize]
public class VehiclePairController : TransportControllerBase
{
    private readonly ISqlConnectionFactory _sql;

    public VehiclePairController(AppDbContext db, ISqlConnectionFactory sql) : base(db)
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
        const string baseWhere = "WHERE p.IsDeleted = 0";
        var where = baseWhere;
        var p = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += " AND (p.Code LIKE @Search OR p.Name LIKE @Search)";
            p.Add("Search", $"%{search}%");
        }

        var recordsTotal = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM VehiclePairs p WHERE p.IsDeleted = 0");
        var recordsFiltered = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(1) FROM VehiclePairs p {where}", p);
        p.Add("Offset", start);
        p.Add("Fetch", length);

        var rows = (await conn.QueryAsync(
            $"""
             SELECT p.VehiclePairId AS vehiclePairId, p.Code AS code, p.Name AS name,
                    pv.PlateNumber AS primaryPlate, sv.PlateNumber AS secondaryPlate,
                    p.PrimarySharePercent AS primarySharePercent,
                    p.SecondarySharePercent AS secondarySharePercent,
                    p.IsActive AS isActive
             FROM VehiclePairs p
             LEFT JOIN Vehicles pv ON pv.VehicleId = p.PrimaryVehicleId
             LEFT JOIN Vehicles sv ON sv.VehicleId = p.SecondaryVehicleId
             {where}
             ORDER BY p.Code OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
             """, p)).ToList();

        return Ok(new { request.Draw, recordsTotal, recordsFiltered, data = rows.Select((r, i) => { var d = (IDictionary<string, object>)r; return new { rowNumber = start + i + 1, vehiclePairId = d["vehiclePairId"], code = d["code"], name = d["name"], primaryPlate = d["primaryPlate"], secondaryPlate = d["secondaryPlate"], primarySharePercent = d["primarySharePercent"], secondarySharePercent = d["secondarySharePercent"], isActive = d["isActive"] }; }) });
    }

    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken ct) =>
        Ok(await Db.VehiclePairs.AsNoTracking()
            .Where(p => p.IsDeleted != true && p.IsActive == true)
            .OrderBy(p => p.Code)
            .Select(p => new
            {
                value = p.VehiclePairId,
                label = p.Code + " — " + p.Name,
                primaryVehicleId = p.PrimaryVehicleId,
                secondaryVehicleId = p.SecondaryVehicleId,
                primarySharePercent = p.PrimarySharePercent,
                secondarySharePercent = p.SecondarySharePercent,
            })
            .ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VehiclePairRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (request.PrimaryVehicleId is int p1 && request.SecondaryVehicleId is int s1 && p1 == s1)
            return BadRequest(new { message = "کشنده و بونکر نمی‌توانند یک وسیله باشند." });

        var entity = new VehiclePair
        {
            Code = "TMP",
            Name = request.Name.Trim(),
            PrimaryVehicleId = request.PrimaryVehicleId,
            SecondaryVehicleId = request.SecondaryVehicleId,
            PrimarySharePercent = request.PrimarySharePercent,
            SecondarySharePercent = request.SecondarySharePercent,
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };
        Db.VehiclePairs.Add(entity);
        await Db.SaveChangesAsync(ct);
        entity.Code = TransportCodeHelper.Pair(entity.VehiclePairId);
        await Db.SaveChangesAsync(ct);
        await SyncPairVehiclesAsync(entity, ct);
        return Ok(new { message = "جفت ثبت شد.", vehiclePairId = entity.VehiclePairId, code = entity.Code });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] VehiclePairRequest request, CancellationToken ct)
    {
        var entity = await Db.VehiclePairs.FirstOrDefaultAsync(p => p.VehiclePairId == id && p.IsDeleted != true, ct);
        if (entity is null) return NotFound(new { message = "یافت نشد." });
        entity.Name = request.Name.Trim();
        entity.PrimaryVehicleId = request.PrimaryVehicleId;
        entity.SecondaryVehicleId = request.SecondaryVehicleId;
        entity.PrimarySharePercent = request.PrimarySharePercent;
        entity.SecondarySharePercent = request.SecondarySharePercent;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = ResolveCurrentUserId();
        await Db.SaveChangesAsync(ct);
        await SyncPairVehiclesAsync(entity, ct);
        return Ok(new { message = "به‌روزرسانی شد." });
    }

    [HttpPost("{id:int}/share-agreements")]
    public async Task<IActionResult> AddShareAgreement(int id, [FromBody] ShareAgreementRequest request, CancellationToken ct)
    {
        if (!await Db.VehiclePairs.AnyAsync(p => p.VehiclePairId == id && p.IsDeleted != true, ct))
            return NotFound(new { message = "جفت یافت نشد." });

        var agreement = new OwnerShareAgreement
        {
            VehiclePairId = id,
            PrimarySharePercent = request.PrimarySharePercent,
            SecondarySharePercent = request.SecondarySharePercent,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };
        Db.OwnerShareAgreements.Add(agreement);
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "قرارداد سهم ثبت شد.", ownerShareAgreementId = agreement.OwnerShareAgreementId });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Db.VehiclePairs.FirstOrDefaultAsync(p => p.VehiclePairId == id && p.IsDeleted != true, ct);
        if (entity is null) return NotFound(new { message = "یافت نشد." });
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.DeletedBy = ResolveCurrentUserId();
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "حذف شد." });
    }

    private async Task SyncPairVehiclesAsync(VehiclePair pair, CancellationToken ct)
    {
        var vehicles = await Db.Vehicles.Where(v => v.VehiclePairId == pair.VehiclePairId).ToListAsync(ct);
        foreach (var v in vehicles) v.VehiclePairId = null;

        if (pair.PrimaryVehicleId is int primaryId)
        {
            var primary = await Db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == primaryId, ct);
            if (primary is not null) { primary.VehiclePairId = pair.VehiclePairId; primary.RoleInPair = VehicleRole.Primary; }
        }
        if (pair.SecondaryVehicleId is int secondaryId)
        {
            var secondary = await Db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == secondaryId, ct);
            if (secondary is not null) { secondary.VehiclePairId = pair.VehiclePairId; secondary.RoleInPair = VehicleRole.Secondary; }
        }
        await Db.SaveChangesAsync(ct);
    }
}

public class VehiclePairRequest
{
    [MaxLength(50)] public string? Code { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    public int? PrimaryVehicleId { get; set; }
    public int? SecondaryVehicleId { get; set; }
    public decimal PrimarySharePercent { get; set; } = 60m;
    public decimal SecondarySharePercent { get; set; } = 40m;
    public bool IsActive { get; set; } = true;
}

public class ShareAgreementRequest
{
    public decimal PrimarySharePercent { get; set; }
    public decimal SecondarySharePercent { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
