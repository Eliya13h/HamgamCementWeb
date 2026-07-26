using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Data.Seed;

// همگام‌سازی جداول خطوط چندارزی صندوق بدون migration سراسری
public static class CashSchemaSeeder
{
    public const string DefaultCashBoxName = "صندوق اصلی";

    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.CashShiftOpeningLines', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CashShiftOpeningLines (
                    CashShiftOpeningLineID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    CashShiftId INT NOT NULL,
                    CurrencyId INT NOT NULL,
                    Amount DECIMAL(18,4) NOT NULL CONSTRAINT DF_CashShiftOpeningLines_Amount DEFAULT(0),
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

                CREATE INDEX IX_CashShiftOpeningLines_CashShiftId
                    ON dbo.CashShiftOpeningLines(CashShiftId)
                    WHERE IsDeleted = 0;
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.CashTransferLines', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CashTransferLines (
                    CashTransferLineID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    CashTransferId INT NOT NULL,
                    CurrencyId INT NOT NULL,
                    Amount DECIMAL(18,4) NOT NULL CONSTRAINT DF_CashTransferLines_Amount DEFAULT(0),
                    AmountInBaseCurrency DECIMAL(18,4) NOT NULL CONSTRAINT DF_CashTransferLines_AmountInBase DEFAULT(0),
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

                CREATE INDEX IX_CashTransferLines_CashTransferId
                    ON dbo.CashTransferLines(CashTransferId)
                    WHERE IsDeleted = 0;
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.CashShiftOpeningLines', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.CashShifts', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CashShiftOpeningLines_CashShift')
            BEGIN
                ALTER TABLE dbo.CashShiftOpeningLines WITH NOCHECK
                ADD CONSTRAINT FK_CashShiftOpeningLines_CashShift
                    FOREIGN KEY (CashShiftId) REFERENCES dbo.CashShifts(CashShiftID);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.CashShiftOpeningLines', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.Currencies', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CashShiftOpeningLines_Currency')
            BEGIN
                ALTER TABLE dbo.CashShiftOpeningLines WITH NOCHECK
                ADD CONSTRAINT FK_CashShiftOpeningLines_Currency
                    FOREIGN KEY (CurrencyId) REFERENCES dbo.Currencies(CurrencyID);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.CashTransferLines', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.CashTransfers', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CashTransferLines_CashTransfer')
            BEGIN
                ALTER TABLE dbo.CashTransferLines WITH NOCHECK
                ADD CONSTRAINT FK_CashTransferLines_CashTransfer
                    FOREIGN KEY (CashTransferId) REFERENCES dbo.CashTransfers(CashTransferID);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.CashTransferLines', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.Currencies', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CashTransferLines_Currency')
            BEGIN
                ALTER TABLE dbo.CashTransferLines WITH NOCHECK
                ADD CONSTRAINT FK_CashTransferLines_Currency
                    FOREIGN KEY (CurrencyId) REFERENCES dbo.Currencies(CurrencyID);
            END
            """, cancellationToken);
    }

    // صندوق اولیه «صندوق اصلی» با کاربر ۱
    public static async Task EnsureDefaultCashBoxAsync(
        AppDbContext db,
        ICashBoxService cashBoxes,
        CancellationToken cancellationToken = default)
    {
        var hasCoa = await db.Accounts
            .AsNoTracking()
            .AnyAsync(a => a.SystemCode == AccountSystemCode.CashBoxes && a.IsDeleted != true, cancellationToken);
        if (!hasCoa)
        {
            return;
        }

        var userId = await db.Users
            .AsNoTracking()
            .Where(u => u.UserID == 1 && u.IsDeleted != true)
            .Select(u => (int?)u.UserID)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await db.Users
                .AsNoTracking()
                .Where(u => u.IsDeleted != true)
                .OrderBy(u => u.UserID)
                .Select(u => (int?)u.UserID)
                .FirstOrDefaultAsync(cancellationToken);

        if (userId is null)
        {
            return;
        }

        var mainBox = await db.CashBoxes
            .FirstOrDefaultAsync(
                c => c.Name == DefaultCashBoxName && c.IsDeleted != true,
                cancellationToken);

        if (mainBox is null)
        {
            await cashBoxes.CreateAsync(
                code: null,
                name: DefaultCashBoxName,
                parentCashBoxId: null,
                userIds: [userId.Value],
                description: "صندوق پیش‌فرض سیستم",
                createdBy: userId.Value,
                cancellationToken);
            return;
        }

        var linked = await db.CashBoxUsers
            .AnyAsync(
                u => u.CashBoxId == mainBox.CashBoxID && u.UserId == userId.Value && u.IsDeleted != true,
                cancellationToken);
        if (linked)
        {
            return;
        }

        db.CashBoxUsers.Add(new CashBoxUser
        {
            CashBoxId = mainBox.CashBoxID,
            UserId = userId.Value,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = userId.Value,
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
