using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Data.Seed;

// همگام‌سازی جدول سال مالی بدون migration سراسری
public static class FiscalYearSchemaSeeder
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.FiscalYears', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.FiscalYears (
                    FiscalYearID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    SolarYear INT NOT NULL,
                    StartDate DATETIME2 NOT NULL,
                    EndDate DATETIME2 NOT NULL,
                    Status INT NOT NULL CONSTRAINT DF_FiscalYears_Status DEFAULT(1),
                    ClosedAt DATETIME2 NULL,
                    ClosedByUserId INT NULL,
                    ClosingJournalEntryId INT NULL,
                    NetIncomeInBaseCurrency DECIMAL(18,4) NOT NULL CONSTRAINT DF_FiscalYears_NetIncome DEFAULT(0),
                    IsActive BIT NULL,
                    IsDeleted BIT NULL,
                    IsUpdated BIT NULL,
                    CreatedAt DATETIME2 NULL,
                    UpdatedAt DATETIME2 NULL,
                    DeletedAt DATETIME2 NULL,
                    CreatedBy INT NULL,
                    UpdatedBy INT NULL,
                    DeletedBy INT NULL
                );

                CREATE UNIQUE INDEX IX_FiscalYears_SolarYear
                    ON dbo.FiscalYears(SolarYear)
                    WHERE IsDeleted = 0;
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.FiscalYears', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FiscalYears_ClosedByUser')
            BEGIN
                ALTER TABLE dbo.FiscalYears WITH NOCHECK
                ADD CONSTRAINT FK_FiscalYears_ClosedByUser
                    FOREIGN KEY (ClosedByUserId) REFERENCES dbo.Users(UserID);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.FiscalYears', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.JournalEntries', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FiscalYears_ClosingJournalEntry')
            BEGIN
                ALTER TABLE dbo.FiscalYears WITH NOCHECK
                ADD CONSTRAINT FK_FiscalYears_ClosingJournalEntry
                    FOREIGN KEY (ClosingJournalEntryId) REFERENCES dbo.JournalEntries(JournalEntryID);
            END
            """, cancellationToken);
    }
}
