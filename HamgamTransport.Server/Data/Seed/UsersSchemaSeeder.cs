using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Data.Seed;

// همگام‌سازی اسکیمای کاربران (شماره کارت) بدون migration سراسری
public static class UsersSchemaSeeder
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.Users', 'CardNumber') IS NULL
                ALTER TABLE dbo.Users ADD CardNumber NVARCHAR(50) NOT NULL
                    CONSTRAINT DF_Users_CardNumber DEFAULT('');
            """, cancellationToken);
    }
}
