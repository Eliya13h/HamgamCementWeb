using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Data.Seed;

// همگام‌سازی اسکیمای جداول فرمول تولید بدون migration سراسری (اجتناب از تداخل با کارهای موازی)
public static class ProductionSchemaSeeder
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        // جداول جدید فرمول و خطوط هزینه بچ
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.ProductionFormulas', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ProductionFormulas (
                    ProductionFormulaID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Name NVARCHAR(200) NOT NULL,
                    ProductId INT NOT NULL,
                    MeaurmentId INT NOT NULL,
                    BaseQuantity DECIMAL(18,6) NOT NULL CONSTRAINT DF_ProductionFormulas_BaseQuantity DEFAULT(1),
                    Mode INT NOT NULL CONSTRAINT DF_ProductionFormulas_Mode DEFAULT(1),
                    IsDefault BIT NOT NULL CONSTRAINT DF_ProductionFormulas_IsDefault DEFAULT(0),
                    Notes NVARCHAR(2000) NULL,
                    IsActive BIT NULL,
                    IsDeleted BIT NULL,
                    CreatedAt DATETIME2 NULL,
                    CreatedBy INT NULL,
                    IsUpdated BIT NULL,
                    UpdatedAt DATETIME2 NULL,
                    UpdatedBy INT NULL,
                    DeletedAt DATETIME2 NULL,
                    DeletedBy INT NULL,
                    CONSTRAINT FK_ProductionFormulas_Product FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductID),
                    CONSTRAINT FK_ProductionFormulas_Meaurment FOREIGN KEY (MeaurmentId) REFERENCES dbo.Meaurments(MeaurmentID)
                );
                CREATE UNIQUE INDEX IX_ProductionFormulas_Product_Default
                    ON dbo.ProductionFormulas(ProductId)
                    WHERE IsDefault = 1 AND IsDeleted = 0;
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.ProductionFormulaMaterialLines', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ProductionFormulaMaterialLines (
                    ProductionFormulaMaterialLineID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    ProductionFormulaId INT NOT NULL,
                    ProductId INT NOT NULL,
                    MeaurmentId INT NOT NULL,
                    Quantity DECIMAL(18,6) NOT NULL,
                    DefaultWarehouseId INT NULL,
                    IsActive BIT NULL,
                    IsDeleted BIT NULL,
                    CreatedAt DATETIME2 NULL,
                    CreatedBy INT NULL,
                    IsUpdated BIT NULL,
                    UpdatedAt DATETIME2 NULL,
                    UpdatedBy INT NULL,
                    DeletedAt DATETIME2 NULL,
                    DeletedBy INT NULL,
                    CONSTRAINT FK_PFML_Formula FOREIGN KEY (ProductionFormulaId) REFERENCES dbo.ProductionFormulas(ProductionFormulaID) ON DELETE CASCADE,
                    CONSTRAINT FK_PFML_Product FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductID),
                    CONSTRAINT FK_PFML_Meaurment FOREIGN KEY (MeaurmentId) REFERENCES dbo.Meaurments(MeaurmentID),
                    CONSTRAINT FK_PFML_Warehouse FOREIGN KEY (DefaultWarehouseId) REFERENCES dbo.Warehouses(WarehouseID)
                );
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.ProductionFormulaCostLines', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ProductionFormulaCostLines (
                    ProductionFormulaCostLineID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    ProductionFormulaId INT NOT NULL,
                    CostType INT NOT NULL,
                    Description NVARCHAR(200) NULL,
                    AmountMode INT NOT NULL CONSTRAINT DF_PFCL_AmountMode DEFAULT(1),
                    Amount DECIMAL(18,4) NOT NULL,
                    AccountId INT NULL,
                    IsActive BIT NULL,
                    IsDeleted BIT NULL,
                    CreatedAt DATETIME2 NULL,
                    CreatedBy INT NULL,
                    IsUpdated BIT NULL,
                    UpdatedAt DATETIME2 NULL,
                    UpdatedBy INT NULL,
                    DeletedAt DATETIME2 NULL,
                    DeletedBy INT NULL,
                    CONSTRAINT FK_PFCL_Formula FOREIGN KEY (ProductionFormulaId) REFERENCES dbo.ProductionFormulas(ProductionFormulaID) ON DELETE CASCADE,
                    CONSTRAINT FK_PFCL_Account FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(AccountID)
                );
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.ProductionBatchCostLines', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ProductionBatchCostLines (
                    ProductionBatchCostLineID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    ProductionBatchId INT NOT NULL,
                    CostType INT NOT NULL,
                    Description NVARCHAR(200) NULL,
                    Amount DECIMAL(18,4) NOT NULL,
                    AccountId INT NULL,
                    IsActive BIT NULL,
                    IsDeleted BIT NULL,
                    CreatedAt DATETIME2 NULL,
                    CreatedBy INT NULL,
                    IsUpdated BIT NULL,
                    UpdatedAt DATETIME2 NULL,
                    UpdatedBy INT NULL,
                    DeletedAt DATETIME2 NULL,
                    DeletedBy INT NULL,
                    CONSTRAINT FK_PBCL_Batch FOREIGN KEY (ProductionBatchId) REFERENCES dbo.ProductionBatches(ProductionBatchID) ON DELETE CASCADE,
                    CONSTRAINT FK_PBCL_Account FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(AccountID)
                );
            END
            """, cancellationToken);

        // ستون‌های جدید روی ProductionBatches
        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.ProductionBatches', 'ProductionFormulaId') IS NULL
                ALTER TABLE dbo.ProductionBatches ADD ProductionFormulaId INT NULL;
            IF COL_LENGTH('dbo.ProductionBatches', 'ProductionPlanId') IS NULL
                ALTER TABLE dbo.ProductionBatches ADD ProductionPlanId INT NULL;
            IF COL_LENGTH('dbo.ProductionBatches', 'TotalConversionCostInBase') IS NULL
                ALTER TABLE dbo.ProductionBatches ADD TotalConversionCostInBase DECIMAL(18,4) NOT NULL CONSTRAINT DF_PB_TotalConversion DEFAULT(0);
            IF COL_LENGTH('dbo.ProductionBatches', 'TotalCostInBase') IS NULL
                ALTER TABLE dbo.ProductionBatches ADD TotalCostInBase DECIMAL(18,4) NOT NULL CONSTRAINT DF_PB_TotalCost DEFAULT(0);
            IF COL_LENGTH('dbo.ProductionBatches', 'JournalEntryId') IS NULL
                ALTER TABLE dbo.ProductionBatches ADD JournalEntryId INT NULL;
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.ProductionBatches', 'ProductionFormulaId') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProductionBatches_Formula')
            BEGIN
                ALTER TABLE dbo.ProductionBatches WITH NOCHECK
                ADD CONSTRAINT FK_ProductionBatches_Formula
                    FOREIGN KEY (ProductionFormulaId) REFERENCES dbo.ProductionFormulas(ProductionFormulaID);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.ProductionBatches', 'JournalEntryId') IS NOT NULL
               AND OBJECT_ID(N'dbo.JournalEntries', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProductionBatches_JournalEntry')
            BEGIN
                ALTER TABLE dbo.ProductionBatches WITH NOCHECK
                ADD CONSTRAINT FK_ProductionBatches_JournalEntry
                    FOREIGN KEY (JournalEntryId) REFERENCES dbo.JournalEntries(JournalEntryID);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.ProductionBatches', 'ProductionPlanId') IS NOT NULL
               AND OBJECT_ID(N'dbo.ProductionPlans', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProductionBatches_Plan')
            BEGIN
                ALTER TABLE dbo.ProductionBatches WITH NOCHECK
                ADD CONSTRAINT FK_ProductionBatches_Plan
                    FOREIGN KEY (ProductionPlanId) REFERENCES dbo.ProductionPlans(ProductionPlanID);
            END
            """, cancellationToken);
    }
}
