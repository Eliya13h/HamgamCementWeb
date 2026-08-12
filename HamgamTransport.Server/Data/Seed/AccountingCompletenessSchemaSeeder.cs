using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Data.Seed;

// همگام‌سازی اسکیمای تکمیل حسابداری بدون migration سراسری
public static class AccountingCompletenessSchemaSeeder
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await EnsureColumnAsync(db, "JournalLines", "CostCenterId", "INT NULL", cancellationToken);
        await EnsureColumnAsync(db, "CashBoxes", "IsPettyCash", "BIT NOT NULL CONSTRAINT DF_CashBoxes_IsPettyCash DEFAULT(0)", cancellationToken);
        await EnsureColumnAsync(db, "CashBoxes", "CeilingAmountInBase", "DECIMAL(18,4) NOT NULL CONSTRAINT DF_CashBoxes_Ceiling DEFAULT(0)", cancellationToken);
        await EnsureColumnAsync(db, "PartySettlements", "InstallmentId", "INT NULL", cancellationToken);
        await EnsureColumnAsync(db, "GeneralSettings", "DefaultTaxPercent", "DECIMAL(18,4) NOT NULL CONSTRAINT DF_GeneralSettings_DefaultTaxPercent DEFAULT(0)", cancellationToken);

        foreach (var table in new[] { "SaleInvoices", "PurchaseInvoices" })
        {
            await EnsureColumnAsync(db, table, "SubTotalAmount", "DECIMAL(18,4) NOT NULL CONSTRAINT DF_" + table + "_SubTotal DEFAULT(0)", cancellationToken);
            await EnsureColumnAsync(db, table, "SubTotalAmountInBaseCurrency", "DECIMAL(18,4) NOT NULL CONSTRAINT DF_" + table + "_SubTotalBase DEFAULT(0)", cancellationToken);
            await EnsureColumnAsync(db, table, "TaxPercent", "DECIMAL(18,4) NOT NULL CONSTRAINT DF_" + table + "_TaxPercent DEFAULT(0)", cancellationToken);
            await EnsureColumnAsync(db, table, "TaxAmount", "DECIMAL(18,4) NOT NULL CONSTRAINT DF_" + table + "_TaxAmount DEFAULT(0)", cancellationToken);
            await EnsureColumnAsync(db, table, "TaxAmountInBaseCurrency", "DECIMAL(18,4) NOT NULL CONSTRAINT DF_" + table + "_TaxAmountBase DEFAULT(0)", cancellationToken);
            await EnsureColumnAsync(db, table, "PaymentTermDays", "INT NOT NULL CONSTRAINT DF_" + table + "_PaymentTermDays DEFAULT(0)", cancellationToken);
            await EnsureColumnAsync(db, table, "DueDate", "DATETIME2 NULL", cancellationToken);
        }

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.CostCenters', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CostCenters (
                    CostCenterID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Code NVARCHAR(30) NOT NULL,
                    Name NVARCHAR(200) NOT NULL,
                    Description NVARCHAR(500) NULL,
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
                CREATE UNIQUE INDEX IX_CostCenters_Code ON dbo.CostCenters(Code) WHERE IsDeleted = 0;
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.Attachments', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Attachments (
                    AttachmentID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    EntityType NVARCHAR(50) NOT NULL,
                    EntityId INT NOT NULL,
                    FileName NVARCHAR(260) NOT NULL,
                    StoredFileName NVARCHAR(260) NOT NULL,
                    RelativePath NVARCHAR(500) NOT NULL,
                    ContentType NVARCHAR(120) NULL,
                    SizeBytes BIGINT NOT NULL CONSTRAINT DF_Attachments_Size DEFAULT(0),
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
                CREATE INDEX IX_Attachments_Entity ON dbo.Attachments(EntityType, EntityId) WHERE IsDeleted = 0;
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.InvoiceInstallments', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.InvoiceInstallments (
                    InvoiceInstallmentID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    InvoiceKind INT NOT NULL,
                    InvoiceId INT NOT NULL,
                    InstallmentNo INT NOT NULL,
                    DueDate DATETIME2 NOT NULL,
                    Amount DECIMAL(18,4) NOT NULL CONSTRAINT DF_InvoiceInstallments_Amount DEFAULT(0),
                    PaidAmount DECIMAL(18,4) NOT NULL CONSTRAINT DF_InvoiceInstallments_Paid DEFAULT(0),
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
                CREATE INDEX IX_InvoiceInstallments_Invoice ON dbo.InvoiceInstallments(InvoiceKind, InvoiceId) WHERE IsDeleted = 0;
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.FiscalPeriods', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.FiscalPeriods (
                    FiscalPeriodID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    SolarYear INT NOT NULL,
                    Month INT NOT NULL,
                    Status INT NOT NULL CONSTRAINT DF_FiscalPeriods_Status DEFAULT(1),
                    ClosedAt DATETIME2 NULL,
                    ClosedByUserId INT NULL,
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
                CREATE UNIQUE INDEX IX_FiscalPeriods_YearMonth ON dbo.FiscalPeriods(SolarYear, Month) WHERE IsDeleted = 0;
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.DoubtfulDebtProvisions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.DoubtfulDebtProvisions (
                    DoubtfulDebtProvisionID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    ProvisionDate DATETIME2 NOT NULL,
                    AmountInBaseCurrency DECIMAL(18,4) NOT NULL CONSTRAINT DF_DoubtfulDebtProvisions_Amount DEFAULT(0),
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
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.RecurringJournalTemplates', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.RecurringJournalTemplates (
                    RecurringJournalTemplateID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Code NVARCHAR(30) NOT NULL,
                    Name NVARCHAR(200) NOT NULL,
                    Description NVARCHAR(500) NULL,
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
                CREATE UNIQUE INDEX IX_RecurringJournalTemplates_Code ON dbo.RecurringJournalTemplates(Code) WHERE IsDeleted = 0;
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.RecurringJournalTemplateLines', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.RecurringJournalTemplateLines (
                    RecurringJournalTemplateLineID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    RecurringJournalTemplateId INT NOT NULL,
                    [LineNo] INT NOT NULL,
                    AccountId INT NOT NULL,
                    Description NVARCHAR(500) NULL,
                    DebitInBaseCurrency DECIMAL(18,4) NOT NULL CONSTRAINT DF_RJTL_Debit DEFAULT(0),
                    CreditInBaseCurrency DECIMAL(18,4) NOT NULL CONSTRAINT DF_RJTL_Credit DEFAULT(0),
                    CostCenterId INT NULL,
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
            END
            """, cancellationToken);

        await EnsureFkAsync(db, "FK_JournalLines_CostCenter", "JournalLines", "CostCenterId", "CostCenters", "CostCenterID", cancellationToken);
        await EnsureFkAsync(db, "FK_PartySettlements_Installment", "PartySettlements", "InstallmentId", "InvoiceInstallments", "InvoiceInstallmentID", cancellationToken);
        await EnsureFkAsync(db, "FK_DoubtfulDebtProvisions_Journal", "DoubtfulDebtProvisions", "JournalEntryId", "JournalEntries", "JournalEntryID", cancellationToken);
        await EnsureFkAsync(db, "FK_RJTL_Template", "RecurringJournalTemplateLines", "RecurringJournalTemplateId", "RecurringJournalTemplates", "RecurringJournalTemplateID", cancellationToken);
        await EnsureFkAsync(db, "FK_RJTL_Account", "RecurringJournalTemplateLines", "AccountId", "Accounts", "AccountID", cancellationToken);
        await EnsureFkAsync(db, "FK_RJTL_CostCenter", "RecurringJournalTemplateLines", "CostCenterId", "CostCenters", "CostCenterID", cancellationToken);
    }

    private static async Task EnsureColumnAsync(
        AppDbContext db,
        string table,
        string column,
        string sqlType,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync($"""
            IF OBJECT_ID(N'dbo.{table}', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.{table}', N'{column}') IS NULL
            BEGIN
                ALTER TABLE dbo.{table} ADD {column} {sqlType};
            END
            """, cancellationToken);
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
               AND COL_LENGTH(N'dbo.{table}', N'{column}') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'{fkName}')
            BEGIN
                ALTER TABLE dbo.{table} WITH NOCHECK
                ADD CONSTRAINT {fkName}
                    FOREIGN KEY ({column}) REFERENCES dbo.{refTable}({refColumn});
            END
            """, cancellationToken);
    }
}
