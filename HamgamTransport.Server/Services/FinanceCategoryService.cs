using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Services;

public static class FinanceCategoryCode
{
    public const string ProductPurchase = "PRODUCT_PURCHASE";
    public const string ProductSale = "PRODUCT_SALE";
    public const string MiscellaneousExpense = "MISC_EXPENSE";
    public const string MiscellaneousRevenue = "MISC_REVENUE";
}

public interface IFinanceCategoryService
{
    Task EnsureSystemCategoriesAsync(CancellationToken cancellationToken = default);
    Task<int> GetExpenseCategoryIdAsync(string code, CancellationToken cancellationToken = default);
    Task<int> GetRevenueCategoryIdAsync(string code, CancellationToken cancellationToken = default);
}

public class FinanceCategoryService : IFinanceCategoryService
{
    private readonly AppDbContext _db;

    public FinanceCategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task EnsureSystemCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureExpenseCategoryAsync(
            FinanceCategoryCode.ProductPurchase,
            "خرید محصولات",
            "مصرف ناشی از خرید کالا",
            cancellationToken);
        await EnsureExpenseCategoryAsync(
            FinanceCategoryCode.MiscellaneousExpense,
            "متفرقه",
            "مصارف متفرقه",
            cancellationToken);
        await EnsureRevenueCategoryAsync(
            FinanceCategoryCode.ProductSale,
            "فروش محصولات",
            "درآمد ناشی از فروش کالا",
            cancellationToken);
        await EnsureRevenueCategoryAsync(
            FinanceCategoryCode.MiscellaneousRevenue,
            "متفرقه",
            "عواید متفرقه",
            cancellationToken);
    }

    public async Task<int> GetExpenseCategoryIdAsync(string code, CancellationToken cancellationToken = default)
    {
        var id = await _db.ExpenseCategories
            .AsNoTracking()
            .Where(c => c.Code == code && c.IsDeleted != true)
            .Select(c => c.ExpenseCategoryID)
            .FirstOrDefaultAsync(cancellationToken);

        if (id == 0)
        {
            throw new InvalidOperationException($"دسته‌بندی مصرف «{code}» یافت نشد.");
        }

        return id;
    }

    public async Task<int> GetRevenueCategoryIdAsync(string code, CancellationToken cancellationToken = default)
    {
        var id = await _db.RevenueCategories
            .AsNoTracking()
            .Where(c => c.Code == code && c.IsDeleted != true)
            .Select(c => c.RevenueCategoryID)
            .FirstOrDefaultAsync(cancellationToken);

        if (id == 0)
        {
            throw new InvalidOperationException($"دسته‌بندی عاید «{code}» یافت نشد.");
        }

        return id;
    }

    private async Task EnsureExpenseCategoryAsync(
        string code,
        string name,
        string? description,
        CancellationToken cancellationToken)
    {
        var exists = await _db.ExpenseCategories
            .AnyAsync(c => c.Code == code && c.IsDeleted != true, cancellationToken);
        if (exists)
        {
            return;
        }

        _db.ExpenseCategories.Add(new ExpenseCategory
        {
            Code = code,
            Name = name,
            Description = description,
            IsSystem = true,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureRevenueCategoryAsync(
        string code,
        string name,
        string? description,
        CancellationToken cancellationToken)
    {
        var exists = await _db.RevenueCategories
            .AnyAsync(c => c.Code == code && c.IsDeleted != true, cancellationToken);
        if (exists)
        {
            return;
        }

        _db.RevenueCategories.Add(new RevenueCategory
        {
            Code = code,
            Name = name,
            Description = description,
            IsSystem = true,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
