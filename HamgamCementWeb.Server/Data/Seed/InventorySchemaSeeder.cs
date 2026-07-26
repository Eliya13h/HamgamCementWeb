using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Data.Seed;

// همگام‌سازی اسکیمای انبار (انبارگردانی دابل‌انتری + انتقال بین انبار) بدون migration سراسری
public static class InventorySchemaSeeder
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        // ستون‌های دابل‌انتری روی انبارگردانی
        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.Stocktakings', 'JournalEntryId') IS NULL
                ALTER TABLE dbo.Stocktakings ADD JournalEntryId INT NULL;

            IF COL_LENGTH('dbo.StocktakingLines', 'AdjustmentCostInBase') IS NULL
                ALTER TABLE dbo.StocktakingLines ADD AdjustmentCostInBase DECIMAL(18,4) NOT NULL
                    CONSTRAINT DF_StocktakingLines_AdjustmentCost DEFAULT(0);
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.Stocktakings', 'JournalEntryId') IS NOT NULL
               AND OBJECT_ID(N'dbo.JournalEntries', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Stocktakings_JournalEntry')
            BEGIN
                ALTER TABLE dbo.Stocktakings WITH NOCHECK
                ADD CONSTRAINT FK_Stocktakings_JournalEntry
                    FOREIGN KEY (JournalEntryId) REFERENCES dbo.JournalEntries(JournalEntryID);
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.WarehouseTransfers', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.WarehouseTransfers (
                    WarehouseTransferID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Code NVARCHAR(50) NOT NULL,
                    TransferDate DATETIME2 NOT NULL,
                    FromWarehouseId INT NOT NULL,
                    ToWarehouseId INT NOT NULL,
                    Status INT NOT NULL CONSTRAINT DF_WarehouseTransfers_Status DEFAULT(1),
                    IsPosted BIT NOT NULL CONSTRAINT DF_WarehouseTransfers_IsPosted DEFAULT(0),
                    PostedAt DATETIME2 NULL,
                    TotalCostInBaseCurrency DECIMAL(18,4) NOT NULL CONSTRAINT DF_WarehouseTransfers_TotalCost DEFAULT(0),
                    JournalEntryId INT NULL,
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
                    CONSTRAINT FK_WarehouseTransfers_FromWarehouse
                        FOREIGN KEY (FromWarehouseId) REFERENCES dbo.Warehouses(WarehouseID),
                    CONSTRAINT FK_WarehouseTransfers_ToWarehouse
                        FOREIGN KEY (ToWarehouseId) REFERENCES dbo.Warehouses(WarehouseID)
                );
                CREATE UNIQUE INDEX IX_WarehouseTransfers_Code
                    ON dbo.WarehouseTransfers(Code)
                    WHERE IsDeleted = 0;
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.WarehouseTransferLines', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.WarehouseTransferLines (
                    WarehouseTransferLineID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    WarehouseTransferId INT NOT NULL,
                    ProductId INT NOT NULL,
                    MeaurmentId INT NOT NULL,
                    Quantity DECIMAL(18,6) NOT NULL,
                    QuantityInBase DECIMAL(18,6) NOT NULL,
                    UnitCostInBase DECIMAL(18,4) NOT NULL CONSTRAINT DF_WTL_UnitCost DEFAULT(0),
                    LineCostInBase DECIMAL(18,4) NOT NULL CONSTRAINT DF_WTL_LineCost DEFAULT(0),
                    Notes NVARCHAR(500) NULL,
                    IsActive BIT NULL,
                    IsDeleted BIT NULL,
                    CreatedAt DATETIME2 NULL,
                    CreatedBy INT NULL,
                    IsUpdated BIT NULL,
                    UpdatedAt DATETIME2 NULL,
                    UpdatedBy INT NULL,
                    DeletedAt DATETIME2 NULL,
                    DeletedBy INT NULL,
                    CONSTRAINT FK_WTL_Transfer FOREIGN KEY (WarehouseTransferId)
                        REFERENCES dbo.WarehouseTransfers(WarehouseTransferID) ON DELETE CASCADE,
                    CONSTRAINT FK_WTL_Product FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductID),
                    CONSTRAINT FK_WTL_Meaurment FOREIGN KEY (MeaurmentId) REFERENCES dbo.Meaurments(MeaurmentID)
                );
            END
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('dbo.WarehouseTransfers', 'JournalEntryId') IS NOT NULL
               AND OBJECT_ID(N'dbo.JournalEntries', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_WarehouseTransfers_JournalEntry')
            BEGIN
                ALTER TABLE dbo.WarehouseTransfers WITH NOCHECK
                ADD CONSTRAINT FK_WarehouseTransfers_JournalEntry
                    FOREIGN KEY (JournalEntryId) REFERENCES dbo.JournalEntries(JournalEntryID);
            END
            """, cancellationToken);
    }
}
