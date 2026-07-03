using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.People;
using HamgamCementWeb.Server.Data.Models.Transport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Services;

/// <summary>
/// همگام‌سازی راننده پیش‌فرض وسیله نقلیه با وسیله پیش‌فرض راننده
/// </summary>
public static class VehicleDriverService
{
    public static async Task<IActionResult?> AssignDefaultDriverAsync(
        AppDbContext db,
        Vehicle vehicle,
        int? driverId,
        CancellationToken cancellationToken)
    {
        if (driverId is null or <= 0)
        {
            if (vehicle.DefaultDriverId is int previousDriverId)
            {
                var previousDriver = await db.Drivers
                    .FirstOrDefaultAsync(d => d.DriverID == previousDriverId && d.IsDeleted != true, cancellationToken);
                if (previousDriver?.DefaultVehicleId == vehicle.VehicleID)
                {
                    previousDriver.DefaultVehicleId = null;
                }
            }

            vehicle.DefaultDriverId = null;
            return null;
        }

        var driverExists = await db.Drivers
            .AnyAsync(d => d.DriverID == driverId && d.IsDeleted != true, cancellationToken);
        if (!driverExists)
        {
            return new NotFoundObjectResult(new { message = "راننده انتخاب‌شده یافت نشد." });
        }

        if (vehicle.DefaultDriverId is int oldDriverId && oldDriverId != driverId)
        {
            var oldDriver = await db.Drivers
                .FirstOrDefaultAsync(d => d.DriverID == oldDriverId && d.IsDeleted != true, cancellationToken);
            if (oldDriver?.DefaultVehicleId == vehicle.VehicleID)
            {
                oldDriver.DefaultVehicleId = null;
            }
        }

        var driver = await db.Drivers
            .FirstOrDefaultAsync(d => d.DriverID == driverId && d.IsDeleted != true, cancellationToken);
        if (driver is null)
        {
            return new NotFoundObjectResult(new { message = "راننده انتخاب‌شده یافت نشد." });
        }

        if (driver.DefaultVehicleId is int oldVehicleId && oldVehicleId != vehicle.VehicleID)
        {
            var oldVehicle = await db.Vehicles
                .FirstOrDefaultAsync(v => v.VehicleID == oldVehicleId && v.IsDeleted != true, cancellationToken);
            if (oldVehicle is not null)
            {
                oldVehicle.DefaultDriverId = null;
            }
        }

        vehicle.DefaultDriverId = driverId;
        driver.DefaultVehicleId = vehicle.VehicleID;

        return null;
    }
}
