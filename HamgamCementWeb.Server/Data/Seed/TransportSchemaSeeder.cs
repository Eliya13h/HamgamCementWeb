using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Data.Seed;

// همگام‌سازی اسکیمای حمل‌ونقل — اتصال تعمیرات و تعویض قطعه به حسابداری
public static class TransportSchemaSeeder
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.VehicleMaintenances', 'ExpenseId') IS NULL
                ALTER TABLE dbo.VehicleMaintenances ADD ExpenseId INT NULL;

            IF COL_LENGTH('dbo.VehicleMaintenances', 'JournalEntryId') IS NULL
                ALTER TABLE dbo.VehicleMaintenances ADD JournalEntryId INT NULL;
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.VehicleMaintenances', 'ExpenseId') IS NOT NULL
               AND OBJECT_ID(N'dbo.Expenses', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_VehicleMaintenances_Expense')
            BEGIN
                ALTER TABLE dbo.VehicleMaintenances WITH NOCHECK
                ADD CONSTRAINT FK_VehicleMaintenances_Expense
                    FOREIGN KEY (ExpenseId) REFERENCES dbo.Expenses(ExpenseID);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.VehicleMaintenances', 'JournalEntryId') IS NOT NULL
               AND OBJECT_ID(N'dbo.JournalEntries', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_VehicleMaintenances_JournalEntry')
            BEGIN
                ALTER TABLE dbo.VehicleMaintenances WITH NOCHECK
                ADD CONSTRAINT FK_VehicleMaintenances_JournalEntry
                    FOREIGN KEY (JournalEntryId) REFERENCES dbo.JournalEntries(JournalEntryID);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.VehiclePartReplacements', 'ExpenseId') IS NULL
                ALTER TABLE dbo.VehiclePartReplacements ADD ExpenseId INT NULL;

            IF COL_LENGTH('dbo.VehiclePartReplacements', 'JournalEntryId') IS NULL
                ALTER TABLE dbo.VehiclePartReplacements ADD JournalEntryId INT NULL;
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.VehiclePartReplacements', 'ExpenseId') IS NOT NULL
               AND OBJECT_ID(N'dbo.Expenses', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_VehiclePartReplacements_Expense')
            BEGIN
                ALTER TABLE dbo.VehiclePartReplacements WITH NOCHECK
                ADD CONSTRAINT FK_VehiclePartReplacements_Expense
                    FOREIGN KEY (ExpenseId) REFERENCES dbo.Expenses(ExpenseID);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.VehiclePartReplacements', 'JournalEntryId') IS NOT NULL
               AND OBJECT_ID(N'dbo.JournalEntries', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_VehiclePartReplacements_JournalEntry')
            BEGIN
                ALTER TABLE dbo.VehiclePartReplacements WITH NOCHECK
                ADD CONSTRAINT FK_VehiclePartReplacements_JournalEntry
                    FOREIGN KEY (JournalEntryId) REFERENCES dbo.JournalEntries(JournalEntryID);
            END
            """, cancellationToken);
    }
}
