using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using HamgamTransport.Server.Data.Models.Transport;
using HamgamTransport.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Data.Seed;

/// <summary>
/// داده‌های پایه حمل‌ونقل: انواع وسیله، دسته هزینه سفر، حساب‌های سیستمی.
/// </summary>
public static class TransportSchemaSeeder
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await EnsureTransportAccountsAsync(db, cancellationToken);
        await EnsureVehicleTypesAsync(db, cancellationToken);
        await EnsureTripExpenseCategoriesAsync(db, cancellationToken);
        await BackfillVehicleAndPairCodesAsync(db, cancellationToken);
        await FixLegacyFreightModeAsync(db, cancellationToken);
    }

    private static async Task EnsureTransportAccountsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var payables = await db.Accounts
            .FirstOrDefaultAsync(a => a.SystemCode == AccountSystemCode.Payables && a.IsDeleted != true, cancellationToken);
        var revenues = await db.Accounts
            .FirstOrDefaultAsync(a => a.SystemCode == AccountSystemCode.Revenues && a.IsDeleted != true, cancellationToken);
        if (payables is null || revenues is null)
        {
            return;
        }

        await EnsureChildAccountAsync(db, AccountSystemCode.TransportRevenue, "درآمد حمل", revenues.AccountID, cancellationToken);
        await EnsureChildAccountAsync(db, AccountSystemCode.OwnerPayable, "بدهی مالکان وسیله", payables.AccountID, cancellationToken);
        await EnsureChildAccountAsync(db, AccountSystemCode.DriverPayable, "بدهی رانندگان", payables.AccountID, cancellationToken);
    }

    private static async Task EnsureChildAccountAsync(
        AppDbContext db,
        string systemCode,
        string name,
        int parentId,
        CancellationToken cancellationToken)
    {
        var exists = await db.Accounts.AnyAsync(
            a => a.SystemCode == systemCode && a.IsDeleted != true, cancellationToken);
        if (exists)
        {
            return;
        }

        var parent = await db.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountID == parentId, cancellationToken);
        if (parent is null)
        {
            return;
        }

        var codePrefix = parent.Code?.TrimEnd('0').TrimEnd('-') ?? parent.Code ?? "0";
        var siblingCount = await db.Accounts.CountAsync(
            a => a.ParentAccountId == parentId && a.IsDeleted != true, cancellationToken);

        db.Accounts.Add(new Account
        {
            Code = $"{codePrefix}-{(siblingCount + 1):D2}",
            Name = name,
            SystemCode = systemCode,
            ParentAccountId = parentId,
            Level = AccountLevel.Moein,
            AccountType = parent.AccountType,
            Nature = parent.Nature,
            IsPostable = true,
            IsSystem = true,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureVehicleTypesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var defaults = new (string Code, string Name, VehicleRole Role)[]
        {
            ("TRACTOR", "کشنده", VehicleRole.Primary),
            ("BUNKER", "بونکر", VehicleRole.Secondary),
            ("SINGLE", "تک وسیله", VehicleRole.Standalone),
            ("MISC", "متفرقه", VehicleRole.Miscellaneous),
        };

        foreach (var (code, name, role) in defaults)
        {
            var existing = await db.VehicleTypes.FirstOrDefaultAsync(
                v => v.Code == code && v.IsDeleted != true, cancellationToken);
            if (existing is not null)
            {
                existing.IsSystem = true;
                existing.Name = name;
                existing.DefaultRole = role;
                existing.IsActive = true;
                continue;
            }

            db.VehicleTypes.Add(new VehicleType
            {
                Code = code,
                Name = name,
                DefaultRole = role,
                IsSystem = true,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task BackfillVehicleAndPairCodesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var vehicles = await db.Vehicles
            .Where(v => v.IsDeleted != true && (v.Code == null || v.Code == ""))
            .ToListAsync(cancellationToken);
        foreach (var vehicle in vehicles)
        {
            vehicle.Code = TransportCodeHelper.Vehicle(vehicle.VehicleId);
        }

        var pairs = await db.VehiclePairs
            .Where(p => p.IsDeleted != true && (p.Code == null || p.Code == ""))
            .ToListAsync(cancellationToken);
        foreach (var pair in pairs)
        {
            pair.Code = TransportCodeHelper.Pair(pair.VehiclePairId);
        }

        if (vehicles.Count > 0 || pairs.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task EnsureTripExpenseCategoriesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var defaults = new (string Code, string Name)[]
        {
            ("FUEL", "سوخت"),
            ("TOLL", "عوارض"),
            ("MAINT", "تعمیرات"),
            ("OTHER", "سایر"),
        };

        foreach (var (code, name) in defaults)
        {
            var exists = await db.TripExpenseCategories.AnyAsync(
                c => c.Code == code && c.IsDeleted != true, cancellationToken);
            if (exists)
            {
                continue;
            }

            db.TripExpenseCategories.Add(new TripExpenseCategory
            {
                Code = code,
                Name = name,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    // مقدار پیش‌فرض migration برای FreightMode صفر بود؛ سفرهای قدیمی را وزنی می‌گذاریم
    private static async Task FixLegacyFreightModeAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var trips = await db.TransportTrips
            .Where(t => t.IsDeleted != true && (int)t.FreightMode == 0)
            .ToListAsync(cancellationToken);
        foreach (var trip in trips)
        {
            trip.FreightMode = FreightMode.WeightBased;
        }

        if (trips.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
