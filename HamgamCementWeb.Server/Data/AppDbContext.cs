using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Data.Models.Inventory;
using HamgamCementWeb.Server.Data.Models.Invoice;
using HamgamCementWeb.Server.Data.Models.People;
using HamgamCementWeb.Server.Data.Models.Product;
using HamgamCementWeb.Server.Data.Models.Production;
using HamgamCementWeb.Server.Data.Models.Transport;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace HamgamCementWeb.Server.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Shareholder> Shareholders { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<VehicleOwner> VehicleOwners { get; set; }

        //finance tables

        public DbSet<Currency> Currencies { get; set; }
        public DbSet<CurrencyExchangeRate> CurrencyExchangeRates { get; set; }
        public DbSet<CurrencyExchangeHistory> CurrencyExchangeHistories { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Revenue> Revenues { get; set; }
        public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
        public DbSet<RevenueCategory> RevenueCategories { get; set; }

        //invoice tables

        public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
        public DbSet<PurchaseItem> PurchaseItems { get; set; }
        public DbSet<SaleInvoice> SaleInvoices { get; set; }
        public DbSet<SalesItem> SalesItems { get; set; }
        public DbSet<SaleItemLotAllocation> SaleItemLotAllocations { get; set; }

        //transport tables

        public DbSet<VehicleType> VehicleTypes { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<TransportRoute> TransportRoutes { get; set; }
        public DbSet<TransportTrip> TransportTrips { get; set; }
        public DbSet<ExpensesCategory> ExpensesCategories { get; set; }
        public DbSet<TransportInvoice> TransportInvoices { get; set; }
        public DbSet<TransportExpense> TransportExpenses { get; set; }
        public DbSet<VehicleMaintenance> VehicleMaintenances { get; set; }
        public DbSet<VehiclePartReplacement> VehiclePartReplacements { get; set; }

        //product tables

        public DbSet<Meaurment> Meaurments { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductMeaurment> ProductMeaurments { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }

        //inventory tables

        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<InventoryStock> InventoryStocks { get; set; }
        public DbSet<InventoryLot> InventoryLots { get; set; }
        public DbSet<Stocktaking> Stocktakings { get; set; }
        public DbSet<StocktakingLine> StocktakingLines { get; set; }

        //production tables

        public DbSet<ProductionBatch> ProductionBatches { get; set; }
        public DbSet<ProductionInputLine> ProductionInputLines { get; set; }
        public DbSet<ProductionOutputLine> ProductionOutputLines { get; set; }
        public DbSet<ProductionPlan> ProductionPlans { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Employee)
                .WithOne(e => e.User)
                .HasForeignKey<User>(u => u.EmployeeId);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);

            modelBuilder.Entity<UserPermission>()
                .HasIndex(up => new { up.UserId, up.PermissionKey })
                .IsUnique();

            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.User)
                .WithMany(u => u.Permissions)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId);

            modelBuilder.Entity<Currency>()
                .HasIndex(c => c.CurrencyCode)
                .IsUnique();

            modelBuilder.Entity<CurrencyExchangeRate>()
                .HasIndex(r => r.CurrencyID)
                .IsUnique();

            modelBuilder.Entity<CurrencyExchangeRate>()
                .HasOne(r => r.Currency)
                .WithMany(c => c.ExchangeRates)
                .HasForeignKey(r => r.CurrencyID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CurrencyExchangeRate>()
                .HasOne(r => r.BaseCurrency)
                .WithMany()
                .HasForeignKey(r => r.BaseCurrencyID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CurrencyExchangeRate>()
                .HasOne(r => r.SourceHistory)
                .WithMany()
                .HasForeignKey(r => r.SourceHistoryID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CurrencyExchangeHistory>()
                .HasIndex(h => new { h.CurrencyID, h.EffectiveFrom });

            modelBuilder.Entity<CurrencyExchangeHistory>()
                .HasIndex(h => new { h.CurrencyID, h.EffectiveTo });

            modelBuilder.Entity<CurrencyExchangeHistory>()
                .HasOne(h => h.Currency)
                .WithMany(c => c.ExchangeHistories)
                .HasForeignKey(h => h.CurrencyID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CurrencyExchangeHistory>()
                .HasOne(h => h.BaseCurrency)
                .WithMany()
                .HasForeignKey(h => h.BaseCurrencyID)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- حمل و نقل ----------

            // کد و پلاک وسیله نقلیه باید یکتا باشند
            modelBuilder.Entity<Vehicle>()
                .HasIndex(v => v.Code)
                .IsUnique();

            modelBuilder.Entity<Vehicle>()
                .HasIndex(v => v.PlateNumber)
                .IsUnique();

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.DefaultDriver)
                .WithMany()
                .HasForeignKey(v => v.DefaultDriverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.Owner)
                .WithMany()
                .HasForeignKey(v => v.VehicleOwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Driver>()
                .HasOne(d => d.DefaultVehicle)
                .WithMany()
                .HasForeignKey(d => d.DefaultVehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransportRoute>()
                .HasIndex(r => r.Code)
                .IsUnique();

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.VehicleType)
                .WithMany(t => t.Vehicles)
                .HasForeignKey(v => v.VehicleTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransportTrip>()
                .HasIndex(t => t.TripNumber)
                .IsUnique();

            modelBuilder.Entity<TransportTrip>()
                .HasOne(t => t.Vehicle)
                .WithMany()
                .HasForeignKey(t => t.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransportTrip>()
                .HasOne(t => t.Route)
                .WithMany(r => r.Trips)
                .HasForeignKey(t => t.TransportRouteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransportTrip>()
                .HasOne(t => t.Driver)
                .WithMany()
                .HasForeignKey(t => t.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransportInvoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique();

            modelBuilder.Entity<TransportInvoice>()
                .HasOne(i => i.Vehicle)
                .WithMany()
                .HasForeignKey(i => i.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransportInvoice>()
                .HasOne(i => i.Trip)
                .WithMany()
                .HasForeignKey(i => i.TransportTripId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransportExpense>()
                .HasOne(e => e.Invoice)
                .WithMany(i => i.Expenses)
                .HasForeignKey(e => e.TransportInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransportExpense>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Expenses)
                .HasForeignKey(e => e.ExpensesCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransportExpense>()
                .HasOne(e => e.Currency)
                .WithMany()
                .HasForeignKey(e => e.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehicleMaintenance>()
                .HasOne(m => m.Vehicle)
                .WithMany()
                .HasForeignKey(m => m.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehiclePartReplacement>()
                .HasOne(p => p.Vehicle)
                .WithMany()
                .HasForeignKey(p => p.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- محصولات و انبار ----------

            modelBuilder.Entity<Meaurment>()
                .HasIndex(m => m.Name)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0 AND [IsBaseUnit] = 1");

            modelBuilder.Entity<Meaurment>()
                .HasIndex(m => new { m.BaseMeaurmentId, m.Name })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0 AND [IsBaseUnit] = 0");

            modelBuilder.Entity<Meaurment>()
                .HasOne(m => m.BaseMeaurment)
                .WithMany(b => b.DerivedUnits)
                .HasForeignKey(m => m.BaseMeaurmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<Product>()
                .HasOne(p => p.BaseMeaurment)
                .WithMany()
                .HasForeignKey(p => p.BaseMeaurmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.DefaultMeaurment)
                .WithMany()
                .HasForeignKey(p => p.DefaultMeaurmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductMeaurment>()
                .HasIndex(pm => new { pm.ProductId, pm.MeaurmentId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<ProductMeaurment>()
                .HasOne(pm => pm.Product)
                .WithMany(p => p.ProductMeaurments)
                .HasForeignKey(pm => pm.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductMeaurment>()
                .HasOne(pm => pm.Meaurment)
                .WithMany(m => m.ProductMeaurments)
                .HasForeignKey(pm => pm.MeaurmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductCategory>()
                .HasIndex(pc => new { pc.ProductId, pc.CategoryId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<ProductCategory>()
                .HasOne(pc => pc.Product)
                .WithMany(p => p.ProductCategories)
                .HasForeignKey(pc => pc.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductCategory>()
                .HasOne(pc => pc.Category)
                .WithMany(c => c.ProductCategories)
                .HasForeignKey(pc => pc.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryStock>()
                .HasIndex(s => new { s.WarehouseId, s.ProductId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<InventoryStock>()
                .HasOne(s => s.Warehouse)
                .WithMany(w => w.Stocks)
                .HasForeignKey(s => s.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryStock>()
                .HasOne(s => s.Product)
                .WithMany()
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Warehouse>()
                .HasOne(w => w.CapacityMeaurment)
                .WithMany()
                .HasForeignKey(w => w.CapacityMeaurmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryLot>()
                .HasIndex(l => l.LotCode)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<InventoryLot>()
                .HasIndex(l => new { l.ProductId, l.WarehouseId, l.ReceiptSequence });

            modelBuilder.Entity<InventoryLot>()
                .HasOne(l => l.Product)
                .WithMany()
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryLot>()
                .HasOne(l => l.Warehouse)
                .WithMany(w => w.Lots)
                .HasForeignKey(l => l.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Stocktaking>()
                .HasIndex(s => s.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<Stocktaking>()
                .HasOne(s => s.Warehouse)
                .WithMany(w => w.Stocktakings)
                .HasForeignKey(s => s.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StocktakingLine>()
                .HasOne(l => l.Stocktaking)
                .WithMany(s => s.Lines)
                .HasForeignKey(l => l.StocktakingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StocktakingLine>()
                .HasOne(l => l.Product)
                .WithMany()
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StocktakingLine>()
                .HasOne(l => l.CountedMeaurment)
                .WithMany()
                .HasForeignKey(l => l.CountedMeaurmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- مالی: مصارف و درآمد — Restrict برای جلوگیری از multiple cascade paths به Currencies ----------

            modelBuilder.Entity<Expense>()
                .HasOne(e => e.Supplier)
                .WithMany()
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Expense>()
                .HasOne(e => e.Currency)
                .WithMany()
                .HasForeignKey(e => e.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Expense>()
                .HasOne(e => e.BaseCurrency)
                .WithMany()
                .HasForeignKey(e => e.BaseCurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Expense>()
                .HasOne(e => e.ExchangeHistory)
                .WithMany()
                .HasForeignKey(e => e.ExchangeHistoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Expense>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Expenses)
                .HasForeignKey(e => e.ExpenseCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExpenseCategory>()
                .HasIndex(c => c.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0 AND [Code] IS NOT NULL");

            modelBuilder.Entity<Revenue>()
                .HasOne(r => r.Customer)
                .WithMany()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Revenue>()
                .HasOne(r => r.Currency)
                .WithMany()
                .HasForeignKey(r => r.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Revenue>()
                .HasOne(r => r.BaseCurrency)
                .WithMany()
                .HasForeignKey(r => r.BaseCurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Revenue>()
                .HasOne(r => r.ExchangeHistory)
                .WithMany()
                .HasForeignKey(r => r.ExchangeHistoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Revenue>()
                .HasOne(r => r.Category)
                .WithMany(c => c.Revenues)
                .HasForeignKey(r => r.RevenueCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RevenueCategory>()
                .HasIndex(c => c.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0 AND [Code] IS NOT NULL");

            // ---------- فاکتور خرید و فروش ----------

            modelBuilder.Entity<PurchaseInvoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<PurchaseInvoice>()
                .HasOne(i => i.Supplier)
                .WithMany()
                .HasForeignKey(i => i.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseInvoice>()
                .HasOne(i => i.Warehouse)
                .WithMany()
                .HasForeignKey(i => i.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseInvoice>()
                .HasOne(i => i.Currency)
                .WithMany()
                .HasForeignKey(i => i.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseInvoice>()
                .HasOne(i => i.BaseCurrency)
                .WithMany()
                .HasForeignKey(i => i.BaseCurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseInvoice>()
                .HasOne(i => i.ExchangeHistory)
                .WithMany()
                .HasForeignKey(i => i.ExchangeHistoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PurchaseInvoice>()
                .HasOne(i => i.Expense)
                .WithOne(e => e.PurchaseInvoice)
                .HasForeignKey<PurchaseInvoice>(i => i.ExpenseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseInvoice>()
                .HasOne(i => i.ReferencePurchaseInvoice)
                .WithMany(i => i.ReturnDocuments)
                .HasForeignKey(i => i.ReferencePurchaseInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseInvoice>()
                .HasOne(i => i.ProductionBatch)
                .WithMany()
                .HasForeignKey(i => i.ProductionBatchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseItem>()
                .HasOne(i => i.Invoice)
                .WithMany(p => p.Items)
                .HasForeignKey(i => i.PurchaseInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PurchaseItem>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseItem>()
                .HasOne(i => i.InventoryLot)
                .WithMany()
                .HasForeignKey(i => i.InventoryLotId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseItem>()
                .HasOne(i => i.ReferencePurchaseItem)
                .WithMany()
                .HasForeignKey(i => i.ReferencePurchaseItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleInvoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<SaleInvoice>()
                .HasOne(i => i.Customer)
                .WithMany()
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleInvoice>()
                .HasOne(i => i.Warehouse)
                .WithMany()
                .HasForeignKey(i => i.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleInvoice>()
                .HasOne(i => i.Currency)
                .WithMany()
                .HasForeignKey(i => i.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleInvoice>()
                .HasOne(i => i.BaseCurrency)
                .WithMany()
                .HasForeignKey(i => i.BaseCurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleInvoice>()
                .HasOne(i => i.ExchangeHistory)
                .WithMany()
                .HasForeignKey(i => i.ExchangeHistoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SaleInvoice>()
                .HasOne(i => i.Revenue)
                .WithOne(r => r.SaleInvoice)
                .HasForeignKey<SaleInvoice>(i => i.RevenueId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleInvoice>()
                .HasOne(i => i.ReferenceSaleInvoice)
                .WithMany(i => i.ReturnDocuments)
                .HasForeignKey(i => i.ReferenceSaleInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesItem>()
                .HasOne(i => i.Invoice)
                .WithMany(s => s.Items)
                .HasForeignKey(i => i.SaleInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalesItem>()
                .HasOne(i => i.ReferenceSalesItem)
                .WithMany()
                .HasForeignKey(i => i.ReferenceSalesItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleItemLotAllocation>()
                .HasOne(a => a.SalesItem)
                .WithMany(i => i.LotAllocations)
                .HasForeignKey(a => a.SalesItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SaleItemLotAllocation>()
                .HasOne(a => a.InventoryLot)
                .WithMany()
                .HasForeignKey(a => a.InventoryLotId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseInvoice>()
                .HasIndex(i => i.ExpenseId)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0 AND [ExpenseId] IS NOT NULL");

            modelBuilder.Entity<SaleInvoice>()
                .HasIndex(i => i.RevenueId)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0 AND [RevenueId] IS NOT NULL");

            // ---------- تولید ----------

            modelBuilder.Entity<ProductionBatch>()
                .HasIndex(b => b.BatchNumber)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<ProductionBatch>()
                .HasOne(b => b.OutputWarehouse)
                .WithMany()
                .HasForeignKey(b => b.OutputWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionInputLine>()
                .HasOne(l => l.Batch)
                .WithMany(b => b.InputLines)
                .HasForeignKey(l => l.ProductionBatchId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductionInputLine>()
                .HasOne(l => l.Warehouse)
                .WithMany()
                .HasForeignKey(l => l.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionInputLine>()
                .HasOne(l => l.Product)
                .WithMany()
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionInputLine>()
                .HasOne(l => l.Meaurment)
                .WithMany()
                .HasForeignKey(l => l.MeaurmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionOutputLine>()
                .HasOne(l => l.Batch)
                .WithMany(b => b.OutputLines)
                .HasForeignKey(l => l.ProductionBatchId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductionOutputLine>()
                .HasOne(l => l.Product)
                .WithMany()
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionOutputLine>()
                .HasOne(l => l.Meaurment)
                .WithMany()
                .HasForeignKey(l => l.MeaurmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionPlan>()
                .HasOne(p => p.Product)
                .WithMany()
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionPlan>()
                .HasOne(p => p.Meaurment)
                .WithMany()
                .HasForeignKey(p => p.MeaurmentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
