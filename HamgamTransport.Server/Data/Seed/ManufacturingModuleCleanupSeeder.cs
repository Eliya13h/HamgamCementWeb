using Microsoft.EntityFrameworkCore;



namespace HamgamTransport.Server.Data.Seed;



/// <summary>

/// یک‌بار حذف جداول ماژول‌های غیرحسابداری/غیرترانسپورت از دیتابیس.

/// </summary>

public static class ManufacturingModuleCleanupSeeder

{

    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)

    {

        await DropTablesAsync(db, cancellationToken);

    }



    private static async Task DropTablesAsync(AppDbContext db, CancellationToken cancellationToken)

    {

        var tables = new[]

        {

            "SaleItemLotAllocations",

            "SalesItems",

            "PurchaseItems",

            "SaleInvoices",

            "PurchaseInvoices",

            "InvoiceInstallments",

            "StocktakingLines",

            "Stocktakings",

            "WarehouseTransferLines",

            "WarehouseTransfers",

            "InventoryLots",

            "InventoryStocks",

            "Warehouses",

            "ProductCategories",

            "ProductMeaurments",

            "Products",

            "Categories",

            "Meaurments",

            "ProductionInputLotAllocations",

            "ProductionBatchCostLines",

            "ProductionInputLines",

            "ProductionOutputLines",

            "ProductionFormulaMaterialLines",

            "ProductionFormulaCostLines",

            "ProductionCostCategoryDepartments",

            "ProductionBatches",

            "ProductionFormulas",

            "ProductionPlans",

            "ProductionCostCategories",

            "Attendances",

            "SalaryPayments",

            "Employees",

            "Departments",

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



        // حذف FKهای قدیمی از PartySettlements در صورت باقی‌ماندن ستون‌ها

        await db.Database.ExecuteSqlRawAsync(

            """

            IF COL_LENGTH('dbo.PartySettlements', 'SaleInvoiceId') IS NOT NULL

                ALTER TABLE dbo.PartySettlements DROP COLUMN SaleInvoiceId;

            IF COL_LENGTH('dbo.PartySettlements', 'PurchaseInvoiceId') IS NOT NULL

                ALTER TABLE dbo.PartySettlements DROP COLUMN PurchaseInvoiceId;

            IF COL_LENGTH('dbo.PartySettlements', 'InstallmentId') IS NOT NULL

                ALTER TABLE dbo.PartySettlements DROP COLUMN InstallmentId;

            IF COL_LENGTH('dbo.Users', 'EmployeeId') IS NOT NULL

                ALTER TABLE dbo.Users DROP COLUMN EmployeeId;

            IF COL_LENGTH('dbo.Revenues', 'ProfitInBaseCurrency') IS NOT NULL

                ALTER TABLE dbo.Revenues DROP COLUMN ProfitInBaseCurrency;

            """,

            cancellationToken);

    }

}

