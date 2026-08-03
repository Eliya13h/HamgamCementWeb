using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Data.Seed;

// همگام‌سازی اسکیمای محصول (نوع محصول + حالت قیمت فروش) بدون migration سراسری
public static class ProductSchemaSeeder
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.Products', 'ProductKind') IS NULL
                ALTER TABLE dbo.Products ADD ProductKind INT NOT NULL
                    CONSTRAINT DF_Products_ProductKind DEFAULT(3);

            IF COL_LENGTH('dbo.Products', 'SalePriceMode') IS NULL
                ALTER TABLE dbo.Products ADD SalePriceMode INT NOT NULL
                    CONSTRAINT DF_Products_SalePriceMode DEFAULT(1);

            IF COL_LENGTH('dbo.Products', 'SaleProfitPercent') IS NULL
                ALTER TABLE dbo.Products ADD SaleProfitPercent DECIMAL(18,4) NOT NULL
                    CONSTRAINT DF_Products_SaleProfitPercent DEFAULT(0);
            """, cancellationToken);
    }
}
