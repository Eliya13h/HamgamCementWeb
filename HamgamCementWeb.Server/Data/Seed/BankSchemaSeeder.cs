using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Data.Seed;

// همگام‌سازی جداول حساب بانکی و تسویه طرف‌حساب بدون migration سراسری
public static class BankSchemaSeeder
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.BankAccounts', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.BankAccounts (
                    BankAccountID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Code NVARCHAR(30) NOT NULL,
                    Name NVARCHAR(200) NOT NULL,
                    AccountNumber NVARCHAR(50) NULL,
                    AccountId INT NOT NULL,
                    CurrencyId INT NULL,
                    Description NVARCHAR(1000) NULL,
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

                CREATE UNIQUE INDEX IX_BankAccounts_Code
                    ON dbo.BankAccounts(Code)
                    WHERE IsDeleted = 0;
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.PartySettlements', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.PartySettlements (
                    PartySettlementID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    PartyType INT NOT NULL,
                    PartyId INT NOT NULL,
                    SettlementDate DATETIME2 NOT NULL,
                    CurrencyId INT NOT NULL,
                    Amount DECIMAL(18,4) NOT NULL CONSTRAINT DF_PartySettlements_Amount DEFAULT(0),
                    AmountInBaseCurrency DECIMAL(18,4) NOT NULL CONSTRAINT DF_PartySettlements_AmountBase DEFAULT(0),
                    CashBoxId INT NULL,
                    BankAccountId INT NULL,
                    SaleInvoiceId INT NULL,
                    PurchaseInvoiceId INT NULL,
                    Description NVARCHAR(1000) NULL,
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

                CREATE INDEX IX_PartySettlements_Party
                    ON dbo.PartySettlements(PartyType, PartyId)
                    WHERE IsDeleted = 0;

                CREATE INDEX IX_PartySettlements_SettlementDate
                    ON dbo.PartySettlements(SettlementDate)
                    WHERE IsDeleted = 0;
            END
            """, cancellationToken);

        await EnsureFkAsync(db, "FK_BankAccounts_Account",
            "BankAccounts", "AccountId", "Accounts", "AccountID", cancellationToken);
        await EnsureFkAsync(db, "FK_BankAccounts_Currency",
            "BankAccounts", "CurrencyId", "Currencies", "CurrencyID", cancellationToken);

        await EnsureFkAsync(db, "FK_PartySettlements_Currency",
            "PartySettlements", "CurrencyId", "Currencies", "CurrencyID", cancellationToken);
        await EnsureFkAsync(db, "FK_PartySettlements_CashBox",
            "PartySettlements", "CashBoxId", "CashBoxes", "CashBoxID", cancellationToken);
        await EnsureFkAsync(db, "FK_PartySettlements_BankAccount",
            "PartySettlements", "BankAccountId", "BankAccounts", "BankAccountID", cancellationToken);
        await EnsureFkAsync(db, "FK_PartySettlements_SaleInvoice",
            "PartySettlements", "SaleInvoiceId", "SaleInvoices", "SaleInvoiceID", cancellationToken);
        await EnsureFkAsync(db, "FK_PartySettlements_PurchaseInvoice",
            "PartySettlements", "PurchaseInvoiceId", "PurchaseInvoices", "PurchaseInvoiceID", cancellationToken);
        await EnsureFkAsync(db, "FK_PartySettlements_JournalEntry",
            "PartySettlements", "JournalEntryId", "JournalEntries", "JournalEntryID", cancellationToken);
    }

    private static async Task EnsureFkAsync(
        AppDbContext db,
        string fkName,
        string table,
        string column,
        string refTable,
        string refColumn,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync($"""
            IF OBJECT_ID(N'dbo.{table}', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.{refTable}', N'U') IS NOT NULL
               AND COL_LENGTH('dbo.{table}', '{column}') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'{fkName}')
            BEGIN
                ALTER TABLE dbo.{table} WITH NOCHECK
                ADD CONSTRAINT {fkName}
                    FOREIGN KEY ({column}) REFERENCES dbo.{refTable}({refColumn});
            END
            """, cancellationToken);
    }
}
