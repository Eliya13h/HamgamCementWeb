using HamgamTransport.Server.Data.Models.Finance;
using HamgamTransport.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Data.Seed;

// حذف نرم حساب‌ها و دسته‌های فروش/تولید/موجودی از کدینگ ترانسپورت (اگر گردش دفتر نداشته باشند)
public static class ManufacturingAccountsCleanupSeeder
{
    private static readonly string[] SystemCodesToRemove =
    [
        AccountSystemCode.Inventory,
        AccountSystemCode.InventoryRaw,
        AccountSystemCode.InventorySemi,
        AccountSystemCode.InventoryFg,
        AccountSystemCode.ProductSales,
        AccountSystemCode.CogsGroup,
        AccountSystemCode.Cogs,
        AccountSystemCode.InventoryAdjustment,
        AccountSystemCode.ProductionWage,
        AccountSystemCode.ProductionOverhead,
        AccountSystemCode.ProductionAncillary,
        AccountSystemCode.ProductionFixed,
    ];

    private static readonly string[] CodesToRemove =
    [
        "13", "131", "132", "133",
        "41", "411",
        "5", "51", "511", "52", "521",
        "612", "613", "614", "615",
    ];

    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;

        await SoftDeleteProductCategoriesAsync(db, now, cancellationToken);

        var candidates = await db.Accounts
            .Where(a => a.IsDeleted != true
                        && (
                            (a.SystemCode != null && SystemCodesToRemove.Contains(a.SystemCode))
                            || CodesToRemove.Contains(a.Code)
                            || a.Name == "فروش محصولات"
                            || a.Name == "فروش کالا"
                            || a.Name == "موجودی کالا"
                            || a.Name == "بهای تمام‌شده"
                        ))
            .OrderByDescending(a => a.Level)
            .ToListAsync(cancellationToken);

        foreach (var account in candidates)
        {
            var hasLines = await db.JournalLines.AnyAsync(
                l => l.AccountId == account.AccountID && l.IsDeleted != true,
                cancellationToken);
            if (hasLines)
            {
                continue;
            }

            var hasActiveChildren = await db.Accounts.AnyAsync(
                a => a.ParentAccountId == account.AccountID && a.IsDeleted != true,
                cancellationToken);
            if (hasActiveChildren)
            {
                continue;
            }

            account.IsDeleted = true;
            account.IsActive = false;
            account.DeletedAt = now;
            account.IsUpdated = true;
            account.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SoftDeleteProductCategoriesAsync(
        AppDbContext db,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var expenseCats = await db.ExpenseCategories
            .Where(c => c.Code == FinanceCategoryCode.ProductPurchase && c.IsDeleted != true)
            .ToListAsync(cancellationToken);
        foreach (var cat in expenseCats)
        {
            cat.IsDeleted = true;
            cat.IsActive = false;
            cat.DeletedAt = now;
            cat.IsUpdated = true;
            cat.UpdatedAt = now;
        }

        var revenueCats = await db.RevenueCategories
            .Where(c => c.Code == FinanceCategoryCode.ProductSale && c.IsDeleted != true)
            .ToListAsync(cancellationToken);
        foreach (var cat in revenueCats)
        {
            cat.IsDeleted = true;
            cat.IsActive = false;
            cat.DeletedAt = now;
            cat.IsUpdated = true;
            cat.UpdatedAt = now;
        }
    }
}
