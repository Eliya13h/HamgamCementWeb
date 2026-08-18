using HamgamTransport.Server.Data.Models;
using HamgamTransport.Server.Data.Models.People;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Data.Seed;

/// <summary>
/// داده‌های پایه برای توسعه — اگر جدول خالی باشد یک رکورد پیش‌فرض می‌سازد.
/// </summary>
public static class DataSeeder
{
    public const string DefaultRoleName = "مدیر سیستم";
    public const string DefaultUserName = "admin";
    public const string DefaultPassword = "admin";
    public const string DefaultEmail = "admin@hamgam.local";
    public const string DefaultShareholderFirstName = "صاحب";
    public const string DefaultShareholderLastName = "امتیاز";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        var role = await EnsureRoleAsync(db, cancellationToken);
        await EnsureUserAsync(db, passwordHasher, role, cancellationToken);

        var financeCategories = scope.ServiceProvider.GetRequiredService<IFinanceCategoryService>();
        await financeCategories.EnsureSystemCategoriesAsync(cancellationToken);
        await ChartOfAccountsSeeder.EnsureAsync(db, cancellationToken);
        await ManufacturingAccountsCleanupSeeder.EnsureAsync(db, cancellationToken);
        await ManufacturingModuleCleanupSeeder.EnsureAsync(db, cancellationToken);
        await TransportSchemaSeeder.EnsureAsync(db, cancellationToken);
    }

    private static async Task<Role> EnsureRoleAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var role = await db.Roles
            .FirstOrDefaultAsync(r => r.IsDeleted != true, cancellationToken);

        if (role is not null)
        {
            return role;
        }

        role = new Role
        {
            Name = DefaultRoleName,
            Description = "دسترسی کامل مدیریت سیستم",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);

        return role;
    }

    private static async Task EnsureUserAsync(
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        Role role,
        CancellationToken cancellationToken)
    {
        var exists = await db.Users
            .AnyAsync(u => u.IsDeleted != true, cancellationToken);

        if (exists)
        {
            return;
        }

        var user = new User
        {
            Title = PersonTitle.Mr,
            FullName = "مدیر سیستم",
            Email = DefaultEmail,
            UserName = DefaultUserName,
            PasswordHash = passwordHasher.HashPassword(null!, DefaultPassword),
            RoleId = role.RoleID,
            HasFullAccess = true,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
    }
}
