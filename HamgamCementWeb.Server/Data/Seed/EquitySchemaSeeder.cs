using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Data.Seed;

// همگام‌سازی جداول/ستون‌های حقوق صاحبان سهام بدون migration سراسری
public static class EquitySchemaSeeder
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.Shareholders', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.Shareholders', N'AccountId') IS NULL
            BEGIN
                ALTER TABLE dbo.Shareholders ADD AccountId INT NULL;
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.Shareholders', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.Accounts', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.Shareholders', N'AccountId') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Shareholders_Account')
            BEGIN
                ALTER TABLE dbo.Shareholders WITH NOCHECK
                ADD CONSTRAINT FK_Shareholders_Account
                    FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(AccountID);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.FiscalYears', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.FiscalYears', N'EquityAllocationJournalEntryId') IS NULL
            BEGIN
                ALTER TABLE dbo.FiscalYears ADD EquityAllocationJournalEntryId INT NULL;
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.FiscalYears', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.JournalEntries', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.FiscalYears', N'EquityAllocationJournalEntryId') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FiscalYears_EquityAllocationJournalEntry')
            BEGIN
                ALTER TABLE dbo.FiscalYears WITH NOCHECK
                ADD CONSTRAINT FK_FiscalYears_EquityAllocationJournalEntry
                    FOREIGN KEY (EquityAllocationJournalEntryId) REFERENCES dbo.JournalEntries(JournalEntryID);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.ShareholderEquityTxns', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ShareholderEquityTxns (
                    ShareholderEquityTxnID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    TxnType INT NOT NULL,
                    ShareholderId INT NOT NULL,
                    TxnDate DATETIME2 NOT NULL,
                    CurrencyId INT NOT NULL,
                    BaseCurrencyId INT NOT NULL,
                    ExchangeHistoryId INT NULL,
                    BaseUnitsPerUnitAtTransaction DECIMAL(18,8) NOT NULL CONSTRAINT DF_ShareholderEquityTxns_Rate DEFAULT(1),
                    Amount DECIMAL(18,4) NOT NULL,
                    AmountInBaseCurrency DECIMAL(18,4) NOT NULL,
                    CashBoxId INT NULL,
                    SettlementMode INT NOT NULL CONSTRAINT DF_ShareholderEquityTxns_Settlement DEFAULT(1),
                    Description NVARCHAR(2000) NULL,
                    JournalEntryId INT NULL,
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

                CREATE INDEX IX_ShareholderEquityTxns_ShareholderId
                    ON dbo.ShareholderEquityTxns(ShareholderId)
                    WHERE IsDeleted = 0;

                CREATE INDEX IX_ShareholderEquityTxns_TxnDate
                    ON dbo.ShareholderEquityTxns(TxnDate)
                    WHERE IsDeleted = 0;
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.ShareholderEquityTxns', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.Shareholders', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ShareholderEquityTxns_Shareholder')
            BEGIN
                ALTER TABLE dbo.ShareholderEquityTxns WITH NOCHECK
                ADD CONSTRAINT FK_ShareholderEquityTxns_Shareholder
                    FOREIGN KEY (ShareholderId) REFERENCES dbo.Shareholders(ShareholderID);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.ShareholderEquityTxns', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.CashBoxes', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ShareholderEquityTxns_CashBox')
            BEGIN
                ALTER TABLE dbo.ShareholderEquityTxns WITH NOCHECK
                ADD CONSTRAINT FK_ShareholderEquityTxns_CashBox
                    FOREIGN KEY (CashBoxId) REFERENCES dbo.CashBoxes(CashBoxID);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.ShareholderEquityTxns', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.JournalEntries', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ShareholderEquityTxns_JournalEntry')
            BEGIN
                ALTER TABLE dbo.ShareholderEquityTxns WITH NOCHECK
                ADD CONSTRAINT FK_ShareholderEquityTxns_JournalEntry
                    FOREIGN KEY (JournalEntryId) REFERENCES dbo.JournalEntries(JournalEntryID);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.ShareholderEquityTxns', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.Currencies', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ShareholderEquityTxns_Currency')
            BEGIN
                ALTER TABLE dbo.ShareholderEquityTxns WITH NOCHECK
                ADD CONSTRAINT FK_ShareholderEquityTxns_Currency
                    FOREIGN KEY (CurrencyId) REFERENCES dbo.Currencies(CurrencyID);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.ShareholderEquityTxns', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.Currencies', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ShareholderEquityTxns_BaseCurrency')
            BEGIN
                ALTER TABLE dbo.ShareholderEquityTxns WITH NOCHECK
                ADD CONSTRAINT FK_ShareholderEquityTxns_BaseCurrency
                    FOREIGN KEY (BaseCurrencyId) REFERENCES dbo.Currencies(CurrencyID);
            END
            """, cancellationToken);
    }
}
