using HamgamTransport.Server.Data.Models.Finance;
using HamgamTransport.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Data.Seed;

// جداول دارایی ثابت + بذر دسته‌بندی‌های سیستمی بدون migration سراسری
public static class FixedAssetSchemaSeeder
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.FixedAssetCategories', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.FixedAssetCategories (
                    FixedAssetCategoryID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Name NVARCHAR(200) NOT NULL,
                    Code NVARCHAR(50) NULL,
                    Description NVARCHAR(1000) NULL,
                    IsSystem BIT NOT NULL CONSTRAINT DF_FixedAssetCategories_IsSystem DEFAULT(0),
                    AssetAccountId INT NULL,
                    AccumulatedDepreciationAccountId INT NULL,
                    DepreciationExpenseAccountId INT NULL,
                    DefaultUsefulLifeMonths INT NOT NULL CONSTRAINT DF_FixedAssetCategories_Life DEFAULT(60),
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
            IF OBJECT_ID(N'dbo.FixedAssets', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.FixedAssets (
                    FixedAssetID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Code NVARCHAR(50) NOT NULL,
                    Name NVARCHAR(300) NOT NULL,
                    FixedAssetCategoryId INT NOT NULL,
                    AcquisitionDate DATETIME2 NOT NULL,
                    SupplierId INT NULL,
                    CurrencyId INT NOT NULL,
                    BaseCurrencyId INT NOT NULL,
                    ExchangeHistoryId INT NULL,
                    BaseUnitsPerUnitAtTransaction DECIMAL(18,8) NOT NULL CONSTRAINT DF_FixedAssets_Rate DEFAULT(1),
                    CostAmount DECIMAL(18,4) NOT NULL,
                    CostAmountInBaseCurrency DECIMAL(18,4) NOT NULL,
                    SalvageValue DECIMAL(18,4) NOT NULL CONSTRAINT DF_FixedAssets_Salvage DEFAULT(0),
                    SalvageValueInBaseCurrency DECIMAL(18,4) NOT NULL CONSTRAINT DF_FixedAssets_SalvageBase DEFAULT(0),
                    UsefulLifeMonths INT NOT NULL,
                    DepreciationMethod INT NOT NULL CONSTRAINT DF_FixedAssets_DepMethod DEFAULT(1),
                    AccumulatedDepreciationInBaseCurrency DECIMAL(18,4) NOT NULL CONSTRAINT DF_FixedAssets_Accum DEFAULT(0),
                    Status INT NOT NULL CONSTRAINT DF_FixedAssets_Status DEFAULT(1),
                    Description NVARCHAR(2000) NULL,
                    AcquisitionJournalEntryId INT NULL,
                    DisposalDate DATETIME2 NULL,
                    DisposalAmount DECIMAL(18,4) NULL,
                    DisposalAmountInBaseCurrency DECIMAL(18,4) NULL,
                    DisposalJournalEntryId INT NULL,
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

                CREATE UNIQUE INDEX IX_FixedAssets_Code
                    ON dbo.FixedAssets(Code)
                    WHERE IsDeleted = 0;
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.FixedAssetDepreciations', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.FixedAssetDepreciations (
                    FixedAssetDepreciationID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    FixedAssetId INT NOT NULL,
                    PeriodSolarYear INT NOT NULL,
                    PeriodMonth INT NOT NULL,
                    DepreciationDate DATETIME2 NOT NULL,
                    Amount DECIMAL(18,4) NOT NULL,
                    AmountInBaseCurrency DECIMAL(18,4) NOT NULL,
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

                CREATE UNIQUE INDEX IX_FixedAssetDepreciations_Period
                    ON dbo.FixedAssetDepreciations(FixedAssetId, PeriodSolarYear, PeriodMonth)
                    WHERE IsDeleted = 0;
            END
            """, cancellationToken);

        await EnsureFkAsync(db, "FK_FixedAssetCategories_AssetAccount",
            "FixedAssetCategories", "AssetAccountId", "Accounts", "AccountID", cancellationToken);
        await EnsureFkAsync(db, "FK_FixedAssetCategories_AccumDepAccount",
            "FixedAssetCategories", "AccumulatedDepreciationAccountId", "Accounts", "AccountID", cancellationToken);
        await EnsureFkAsync(db, "FK_FixedAssetCategories_DepExpAccount",
            "FixedAssetCategories", "DepreciationExpenseAccountId", "Accounts", "AccountID", cancellationToken);
        await EnsureFkAsync(db, "FK_FixedAssets_Category",
            "FixedAssets", "FixedAssetCategoryId", "FixedAssetCategories", "FixedAssetCategoryID", cancellationToken);
        await EnsureFkAsync(db, "FK_FixedAssets_Supplier",
            "FixedAssets", "SupplierId", "Suppliers", "SupplierID", cancellationToken);
        await EnsureFkAsync(db, "FK_FixedAssets_Currency",
            "FixedAssets", "CurrencyId", "Currencies", "CurrencyID", cancellationToken);
        await EnsureFkAsync(db, "FK_FixedAssets_BaseCurrency",
            "FixedAssets", "BaseCurrencyId", "Currencies", "CurrencyID", cancellationToken);
        await EnsureFkAsync(db, "FK_FixedAssets_AcquireJournal",
            "FixedAssets", "AcquisitionJournalEntryId", "JournalEntries", "JournalEntryID", cancellationToken);
        await EnsureFkAsync(db, "FK_FixedAssets_DisposeJournal",
            "FixedAssets", "DisposalJournalEntryId", "JournalEntries", "JournalEntryID", cancellationToken);
        await EnsureFkAsync(db, "FK_FixedAssetDepreciations_Asset",
            "FixedAssetDepreciations", "FixedAssetId", "FixedAssets", "FixedAssetID", cancellationToken);
        await EnsureFkAsync(db, "FK_FixedAssetDepreciations_Journal",
            "FixedAssetDepreciations", "JournalEntryId", "JournalEntries", "JournalEntryID", cancellationToken);

        await SeedCategoriesAsync(db, cancellationToken);
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

    private static async Task SeedCategoriesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;

        async Task<int?> AccountId(string systemCode) =>
            await db.Accounts
                .Where(a => a.SystemCode == systemCode && a.IsDeleted != true)
                .Select(a => (int?)a.AccountID)
                .FirstOrDefaultAsync(cancellationToken);

        var accumId = await AccountId(AccountSystemCode.AccumulatedDepreciation);
        var depExpId = await AccountId(AccountSystemCode.DepreciationExpense);

        async Task Ensure(string code, string name, string assetSystemCode, int defaultLifeMonths)
        {
            var existing = await db.FixedAssetCategories
                .FirstOrDefaultAsync(c => c.Code == code && c.IsDeleted != true, cancellationToken);
            if (existing is not null)
            {
                if (existing.AssetAccountId is null or 0)
                {
                    existing.AssetAccountId = await AccountId(assetSystemCode);
                }

                if (existing.AccumulatedDepreciationAccountId is null or 0)
                {
                    existing.AccumulatedDepreciationAccountId = accumId;
                }

                if (existing.DepreciationExpenseAccountId is null or 0)
                {
                    existing.DepreciationExpenseAccountId = depExpId;
                }

                return;
            }

            db.FixedAssetCategories.Add(new FixedAssetCategory
            {
                Code = code,
                Name = name,
                IsSystem = true,
                AssetAccountId = await AccountId(assetSystemCode),
                AccumulatedDepreciationAccountId = accumId,
                DepreciationExpenseAccountId = depExpId,
                DefaultUsefulLifeMonths = defaultLifeMonths,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
            });
        }

        await Ensure("MACHINERY", "ماشین‌آلات و تجهیزات", AccountSystemCode.FixedAssetMachinery, 120);
        await Ensure("VEHICLES", "وسایل نقلیه", AccountSystemCode.FixedAssetVehicles, 60);
        await Ensure("FURNITURE", "اثاثیه و منصوبات", AccountSystemCode.FixedAssetFurniture, 60);
        await Ensure("BUILDINGS", "ساختمان", AccountSystemCode.FixedAssetBuildings, 240);
        await db.SaveChangesAsync(cancellationToken);
    }
}
