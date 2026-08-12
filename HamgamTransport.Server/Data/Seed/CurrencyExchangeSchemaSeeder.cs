using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Data.Seed;

// همگام‌سازی جدول خرید/فروش ارز بدون migration سراسری
public static class CurrencyExchangeSchemaSeeder
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.CurrencyExchangeTxns', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CurrencyExchangeTxns (
                    CurrencyExchangeTxnID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    ExchangeDate DATETIME2 NOT NULL,
                    FromCurrencyId INT NOT NULL,
                    FromAmount DECIMAL(18,4) NOT NULL CONSTRAINT DF_CurrencyExchangeTxns_FromAmount DEFAULT(0),
                    FromAmountInBaseCurrency DECIMAL(18,4) NOT NULL CONSTRAINT DF_CurrencyExchangeTxns_FromBase DEFAULT(0),
                    ToCurrencyId INT NOT NULL,
                    ToAmount DECIMAL(18,4) NOT NULL CONSTRAINT DF_CurrencyExchangeTxns_ToAmount DEFAULT(0),
                    ToAmountInBaseCurrency DECIMAL(18,4) NOT NULL CONSTRAINT DF_CurrencyExchangeTxns_ToBase DEFAULT(0),
                    DealRate DECIMAL(18,8) NOT NULL CONSTRAINT DF_CurrencyExchangeTxns_DealRate DEFAULT(0),
                    RecognizeFxDifference BIT NOT NULL CONSTRAINT DF_CurrencyExchangeTxns_RecognizeFx DEFAULT(0),
                    SystemFromBaseUnitsPerUnit DECIMAL(18,8) NOT NULL CONSTRAINT DF_CurrencyExchangeTxns_SysFrom DEFAULT(0),
                    SystemToBaseUnitsPerUnit DECIMAL(18,8) NOT NULL CONSTRAINT DF_CurrencyExchangeTxns_SysTo DEFAULT(0),
                    FxDifferenceInBaseCurrency DECIMAL(18,4) NOT NULL CONSTRAINT DF_CurrencyExchangeTxns_FxDiff DEFAULT(0),
                    FromCashBoxId INT NULL,
                    FromBankAccountId INT NULL,
                    ToCashBoxId INT NULL,
                    ToBankAccountId INT NULL,
                    ExchangeHistoryFromId INT NULL,
                    ExchangeHistoryToId INT NULL,
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

                CREATE INDEX IX_CurrencyExchangeTxns_ExchangeDate
                    ON dbo.CurrencyExchangeTxns(ExchangeDate)
                    WHERE IsDeleted = 0;
            END
            """, cancellationToken);

        await EnsureFkAsync(db, "FK_CurrencyExchangeTxns_FromCurrency",
            "CurrencyExchangeTxns", "FromCurrencyId", "Currencies", "CurrencyID", cancellationToken);
        await EnsureFkAsync(db, "FK_CurrencyExchangeTxns_ToCurrency",
            "CurrencyExchangeTxns", "ToCurrencyId", "Currencies", "CurrencyID", cancellationToken);
        await EnsureFkAsync(db, "FK_CurrencyExchangeTxns_FromCashBox",
            "CurrencyExchangeTxns", "FromCashBoxId", "CashBoxes", "CashBoxID", cancellationToken);
        await EnsureFkAsync(db, "FK_CurrencyExchangeTxns_FromBank",
            "CurrencyExchangeTxns", "FromBankAccountId", "BankAccounts", "BankAccountID", cancellationToken);
        await EnsureFkAsync(db, "FK_CurrencyExchangeTxns_ToCashBox",
            "CurrencyExchangeTxns", "ToCashBoxId", "CashBoxes", "CashBoxID", cancellationToken);
        await EnsureFkAsync(db, "FK_CurrencyExchangeTxns_ToBank",
            "CurrencyExchangeTxns", "ToBankAccountId", "BankAccounts", "BankAccountID", cancellationToken);
        await EnsureFkAsync(db, "FK_CurrencyExchangeTxns_HistFrom",
            "CurrencyExchangeTxns", "ExchangeHistoryFromId", "CurrencyExchangeHistories", "HistoryID", cancellationToken);
        await EnsureFkAsync(db, "FK_CurrencyExchangeTxns_HistTo",
            "CurrencyExchangeTxns", "ExchangeHistoryToId", "CurrencyExchangeHistories", "HistoryID", cancellationToken);
        await EnsureFkAsync(db, "FK_CurrencyExchangeTxns_Journal",
            "CurrencyExchangeTxns", "JournalEntryId", "JournalEntries", "JournalEntryID", cancellationToken);
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
