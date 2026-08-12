using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Data.Seed;

/// <summary>
/// یک‌بار حذف جداول و ستون‌های حمل‌ونقل از دیتابیس (SQL Server).
/// </summary>
public static class TransportRemovalSeeder
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await DropInvoiceFreightColumnsAsync(db, cancellationToken);
        await DropTransportTablesAsync(db, cancellationToken);
    }

    private static async Task DropInvoiceFreightColumnsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var freightColumns = new[]
        {
            "FreightMode",
            "FreightRatePerTon",
            "FreightWeightTon",
            "FreightAmount",
            "FreightAmountInBaseCurrency",
            "FreightVehicleId",
            "FreightFleetUnitId",
            "FreightTractorVehicleId",
            "FreightBunkerVehicleId",
            "FreightDriverId",
            "FreightAssistantDriverId",
            "FreightTransportRouteId",
            "FreightCarrierName",
            "TransportTripId",
            "FreightExpenseId",
            "FreightRevenueId",
            "FreightJournalEntryId",
        };

        foreach (var table in new[] { "PurchaseInvoices", "SaleInvoices" })
        {
            foreach (var column in freightColumns)
            {
                await DropColumnIfExistsAsync(db, table, column, cancellationToken);
            }
        }
    }

    private static async Task DropTransportTablesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var tables = new[]
        {
            "VehiclePartReplacements",
            "VehicleMaintenances",
            "TransportExpenses",
            "TransportInvoices",
            "TransportTrips",
            "FleetUnits",
            "Vehicles",
            "VehicleTypes",
            "TransportRoutes",
            "ExpensesCategories",
            "Drivers",
            "VehicleOwners",
        };

        foreach (var table in tables)
        {
            await db.Database.ExecuteSqlRawAsync(
                $"""
                IF OBJECT_ID(N'dbo.{table}', N'U') IS NOT NULL
                    DROP TABLE dbo.{table};
                """,
                cancellationToken);
        }
    }

    private static async Task DropColumnIfExistsAsync(
        AppDbContext db,
        string table,
        string column,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(
            $"""
            IF OBJECT_ID(N'dbo.{table}', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.{table}', N'{column}') IS NOT NULL
            BEGIN
                DECLARE @fkName NVARCHAR(256);
                DECLARE fk_cursor CURSOR LOCAL FAST_FORWARD FOR
                    SELECT fk.name
                    FROM sys.foreign_keys fk
                    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                    INNER JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
                    WHERE fk.parent_object_id = OBJECT_ID(N'dbo.{table}')
                      AND c.name = N'{column}';
                OPEN fk_cursor;
                FETCH NEXT FROM fk_cursor INTO @fkName;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    EXEC(N'ALTER TABLE dbo.{table} DROP CONSTRAINT [' + @fkName + N']');
                    FETCH NEXT FROM fk_cursor INTO @fkName;
                END
                CLOSE fk_cursor;
                DEALLOCATE fk_cursor;

                ALTER TABLE dbo.{table} DROP COLUMN [{column}];
            END
            """,
            cancellationToken);
    }
}
