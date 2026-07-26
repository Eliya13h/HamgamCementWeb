using HamgamCementWeb.Server.Data.Models;
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
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<SalaryPayment> SalaryPayments { get; set; }
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

        // دفترکل و صندوق
        public DbSet<Account> Accounts { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<JournalLine> JournalLines { get; set; }
        public DbSet<CashBox> CashBoxes { get; set; }
        public DbSet<CashBoxUser> CashBoxUsers { get; set; }
        public DbSet<CashShift> CashShifts { get; set; }
        public DbSet<CashShiftOpeningLine> CashShiftOpeningLines { get; set; }
        public DbSet<CashTransfer> CashTransfers { get; set; }
        public DbSet<CashTransferLine> CashTransferLines { get; set; }
        public DbSet<FiscalYear> FiscalYears { get; set; }
        public DbSet<ShareholderEquityTxn> ShareholderEquityTxns { get; set; }
        public DbSet<FixedAssetCategory> FixedAssetCategories { get; set; }
        public DbSet<FixedAsset> FixedAssets { get; set; }
        public DbSet<FixedAssetDepreciation> FixedAssetDepreciations { get; set; }

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
        public DbSet<WarehouseTransfer> WarehouseTransfers { get; set; }
        public DbSet<WarehouseTransferLine> WarehouseTransferLines { get; set; }

        //production tables

        public DbSet<ProductionBatch> ProductionBatches { get; set; }
        public DbSet<ProductionInputLine> ProductionInputLines { get; set; }
        public DbSet<ProductionOutputLine> ProductionOutputLines { get; set; }
        public DbSet<ProductionInputLotAllocation> ProductionInputLotAllocations { get; set; }
        public DbSet<ProductionPlan> ProductionPlans { get; set; }
        public DbSet<ProductionFormula> ProductionFormulas { get; set; }
        public DbSet<ProductionFormulaMaterialLine> ProductionFormulaMaterialLines { get; set; }
        public DbSet<ProductionFormulaCostLine> ProductionFormulaCostLines { get; set; }
        public DbSet<ProductionBatchCostLine> ProductionBatchCostLines { get; set; }

        public DbSet<GeneralSettings> GeneralSettings { get; set; }

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

            // هر کارمند در هر روز فقط یک ردیف حضور
            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new { a.EmployeeId, a.Date })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // هر کارمند در هر ماه فقط یک فیش حقوق
            modelBuilder.Entity<SalaryPayment>()
                .HasIndex(s => new { s.EmployeeId, s.Year, s.Month })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<SalaryPayment>()
                .HasOne(s => s.Employee)
                .WithMany()
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalaryPayment>()
                .HasOne(s => s.CashBox)
                .WithMany()
                .HasForeignKey(s => s.CashBoxId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalaryPayment>()
                .HasOne(s => s.JournalEntry)
                .WithMany()
                .HasForeignKey(s => s.JournalEntryId)
                .OnDelete(DeleteBehavior.SetNull);

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

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.FixedAsset)
                .WithMany()
                .HasForeignKey(v => v.FixedAssetId)
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
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransportTrip>()
                .HasOne(t => t.Driver)
                .WithMany()
                .HasForeignKey(t => t.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransportTrip>()
                .HasOne(t => t.PurchaseInvoice)
                .WithMany()
                .HasForeignKey(t => t.PurchaseInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransportTrip>()
                .HasOne(t => t.SaleInvoice)
                .WithMany()
                .HasForeignKey(t => t.SaleInvoiceId)
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

            // اسنپ‌شات ارز پایه و تاریخچه نرخ برای ردیف مصرف حمل‌ونقل — Restrict برای جلوگیری از مسیر cascade چندگانه
            modelBuilder.Entity<TransportExpense>()
                .HasOne<Currency>()
                .WithMany()
                .HasForeignKey(e => e.BaseCurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransportExpense>()
                .HasOne<CurrencyExchangeHistory>()
                .WithMany()
                .HasForeignKey(e => e.ExchangeHistoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // لینک فاکتور مصارف حمل‌ونقل به رکورد مصرف حسابداری
            modelBuilder.Entity<TransportInvoice>()
                .HasOne(i => i.Expense)
                .WithMany()
                .HasForeignKey(i => i.ExpenseId)
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

            modelBuilder.Entity<Stocktaking>()
                .HasOne(s => s.JournalEntry)
                .WithMany()
                .HasForeignKey(s => s.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseTransfer>()
                .HasIndex(t => t.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<WarehouseTransfer>()
                .HasOne(t => t.FromWarehouse)
                .WithMany()
                .HasForeignKey(t => t.FromWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseTransfer>()
                .HasOne(t => t.ToWarehouse)
                .WithMany()
                .HasForeignKey(t => t.ToWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseTransfer>()
                .HasOne(t => t.JournalEntry)
                .WithMany()
                .HasForeignKey(t => t.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseTransferLine>()
                .HasOne(l => l.WarehouseTransfer)
                .WithMany(t => t.Lines)
                .HasForeignKey(l => l.WarehouseTransferId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WarehouseTransferLine>()
                .HasOne(l => l.Product)
                .WithMany()
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseTransferLine>()
                .HasOne(l => l.Meaurment)
                .WithMany()
                .HasForeignKey(l => l.MeaurmentId)
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
                .HasOne(i => i.FreightVehicle)
                .WithMany()
                .HasForeignKey(i => i.FreightVehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseInvoice>()
                .HasOne(i => i.TransportTrip)
                .WithMany()
                .HasForeignKey(i => i.TransportTripId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseInvoice>()
                .HasOne(i => i.FreightExpense)
                .WithMany()
                .HasForeignKey(i => i.FreightExpenseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseInvoice>()
                .HasOne(i => i.FreightJournalEntry)
                .WithMany()
                .HasForeignKey(i => i.FreightJournalEntryId)
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
                .HasOne(i => i.FreightVehicle)
                .WithMany()
                .HasForeignKey(i => i.FreightVehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleInvoice>()
                .HasOne(i => i.TransportTrip)
                .WithMany()
                .HasForeignKey(i => i.TransportTripId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleInvoice>()
                .HasOne(i => i.FreightRevenue)
                .WithMany()
                .HasForeignKey(i => i.FreightRevenueId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleInvoice>()
                .HasOne(i => i.FreightJournalEntry)
                .WithMany()
                .HasForeignKey(i => i.FreightJournalEntryId)
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

            // ---------- دفترکل و صندوق ----------

            modelBuilder.Entity<Account>()
                .HasIndex(a => a.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<Account>()
                .HasIndex(a => a.SystemCode)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0 AND [SystemCode] IS NOT NULL");

            modelBuilder.Entity<Account>()
                .HasOne(a => a.ParentAccount)
                .WithMany(a => a.Children)
                .HasForeignKey(a => a.ParentAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<JournalEntry>()
                .HasIndex(e => e.EntryNumber)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<JournalEntry>()
                .HasOne(e => e.BaseCurrency)
                .WithMany()
                .HasForeignKey(e => e.BaseCurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<JournalLine>()
                .HasOne(l => l.JournalEntry)
                .WithMany(e => e.Lines)
                .HasForeignKey(l => l.JournalEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<JournalLine>()
                .HasOne(l => l.Account)
                .WithMany()
                .HasForeignKey(l => l.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<JournalLine>()
                .HasOne(l => l.Currency)
                .WithMany()
                .HasForeignKey(l => l.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<JournalLine>()
                .HasOne(l => l.CashBox)
                .WithMany()
                .HasForeignKey(l => l.CashBoxId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Expense>()
                .HasOne(e => e.JournalEntry)
                .WithMany()
                .HasForeignKey(e => e.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Revenue>()
                .HasOne(r => r.JournalEntry)
                .WithMany()
                .HasForeignKey(r => r.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseInvoice>()
                .HasOne(i => i.JournalEntry)
                .WithMany()
                .HasForeignKey(i => i.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleInvoice>()
                .HasOne(i => i.JournalEntry)
                .WithMany()
                .HasForeignKey(i => i.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExpenseCategory>()
                .HasOne(c => c.Account)
                .WithMany()
                .HasForeignKey(c => c.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RevenueCategory>()
                .HasOne(c => c.Account)
                .WithMany()
                .HasForeignKey(c => c.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- دارایی‌های ثابت ----------
            modelBuilder.Entity<FixedAssetCategory>()
                .HasOne(c => c.AssetAccount)
                .WithMany()
                .HasForeignKey(c => c.AssetAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FixedAssetCategory>()
                .HasOne(c => c.AccumulatedDepreciationAccount)
                .WithMany()
                .HasForeignKey(c => c.AccumulatedDepreciationAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FixedAssetCategory>()
                .HasOne(c => c.DepreciationExpenseAccount)
                .WithMany()
                .HasForeignKey(c => c.DepreciationExpenseAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FixedAsset>()
                .HasIndex(a => a.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<FixedAsset>()
                .HasOne(a => a.Category)
                .WithMany(c => c.Assets)
                .HasForeignKey(a => a.FixedAssetCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FixedAsset>()
                .HasOne(a => a.Supplier)
                .WithMany()
                .HasForeignKey(a => a.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FixedAsset>()
                .HasOne(a => a.Currency)
                .WithMany()
                .HasForeignKey(a => a.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FixedAsset>()
                .HasOne(a => a.BaseCurrency)
                .WithMany()
                .HasForeignKey(a => a.BaseCurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FixedAsset>()
                .HasOne(a => a.ExchangeHistory)
                .WithMany()
                .HasForeignKey(a => a.ExchangeHistoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<FixedAsset>()
                .HasOne(a => a.AcquisitionJournalEntry)
                .WithMany()
                .HasForeignKey(a => a.AcquisitionJournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FixedAsset>()
                .HasOne(a => a.DisposalJournalEntry)
                .WithMany()
                .HasForeignKey(a => a.DisposalJournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FixedAssetDepreciation>()
                .HasIndex(d => new { d.FixedAssetId, d.PeriodSolarYear, d.PeriodMonth })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<FixedAssetDepreciation>()
                .HasOne(d => d.FixedAsset)
                .WithMany(a => a.Depreciations)
                .HasForeignKey(d => d.FixedAssetId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FixedAssetDepreciation>()
                .HasOne(d => d.JournalEntry)
                .WithMany()
                .HasForeignKey(d => d.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashBox>()
                .HasIndex(c => c.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<CashBox>()
                .HasOne(c => c.ParentCashBox)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentCashBoxId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashBox>()
                .HasOne(c => c.Account)
                .WithMany()
                .HasForeignKey(c => c.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashBoxUser>()
                .HasIndex(u => new { u.CashBoxId, u.UserId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<CashBoxUser>()
                .HasOne(u => u.CashBox)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CashBoxId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CashBoxUser>()
                .HasOne(u => u.User)
                .WithMany()
                .HasForeignKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashShift>()
                .HasOne(s => s.CashBox)
                .WithMany(c => c.Shifts)
                .HasForeignKey(s => s.CashBoxId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashShift>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashShift>()
                .HasOne(s => s.CashTransfer)
                .WithMany()
                .HasForeignKey(s => s.CashTransferId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashTransfer>()
                .HasOne(t => t.FromCashBox)
                .WithMany()
                .HasForeignKey(t => t.FromCashBoxId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashTransfer>()
                .HasOne(t => t.ToCashBox)
                .WithMany()
                .HasForeignKey(t => t.ToCashBoxId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashTransfer>()
                .HasOne(t => t.JournalEntry)
                .WithMany()
                .HasForeignKey(t => t.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashShiftOpeningLine>()
                .HasOne(l => l.CashShift)
                .WithMany(s => s.OpeningLines)
                .HasForeignKey(l => l.CashShiftId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CashShiftOpeningLine>()
                .HasOne(l => l.Currency)
                .WithMany()
                .HasForeignKey(l => l.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashTransferLine>()
                .HasOne(l => l.CashTransfer)
                .WithMany(t => t.Lines)
                .HasForeignKey(l => l.CashTransferId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CashTransferLine>()
                .HasOne(l => l.Currency)
                .WithMany()
                .HasForeignKey(l => l.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- سال مالی ----------
            modelBuilder.Entity<FiscalYear>()
                .HasIndex(y => y.SolarYear)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<FiscalYear>()
                .HasOne(y => y.ClosedByUser)
                .WithMany()
                .HasForeignKey(y => y.ClosedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FiscalYear>()
                .HasOne(y => y.ClosingJournalEntry)
                .WithMany()
                .HasForeignKey(y => y.ClosingJournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FiscalYear>()
                .HasOne(y => y.EquityAllocationJournalEntry)
                .WithMany()
                .HasForeignKey(y => y.EquityAllocationJournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Shareholder>()
                .HasOne(s => s.Account)
                .WithMany()
                .HasForeignKey(s => s.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShareholderEquityTxn>()
                .HasOne(t => t.Shareholder)
                .WithMany()
                .HasForeignKey(t => t.ShareholderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShareholderEquityTxn>()
                .HasOne(t => t.CashBox)
                .WithMany()
                .HasForeignKey(t => t.CashBoxId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShareholderEquityTxn>()
                .HasOne(t => t.JournalEntry)
                .WithMany()
                .HasForeignKey(t => t.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShareholderEquityTxn>()
                .HasOne(t => t.Currency)
                .WithMany()
                .HasForeignKey(t => t.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShareholderEquityTxn>()
                .HasOne(t => t.BaseCurrency)
                .WithMany()
                .HasForeignKey(t => t.BaseCurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

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

            modelBuilder.Entity<ProductionBatch>()
                .HasOne(b => b.Formula)
                .WithMany()
                .HasForeignKey(b => b.ProductionFormulaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionBatch>()
                .HasOne(b => b.Plan)
                .WithMany()
                .HasForeignKey(b => b.ProductionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionBatch>()
                .HasOne(b => b.JournalEntry)
                .WithMany()
                .HasForeignKey(b => b.JournalEntryId)
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

            // رابطه‌ی رسمی Lot تولیدشده با ردیف خروجی (پیش‌تر بدون relation بود) — آیتم ۶.۴
            modelBuilder.Entity<ProductionOutputLine>()
                .HasOne<InventoryLot>()
                .WithMany()
                .HasForeignKey(l => l.InventoryLotId)
                .OnDelete(DeleteBehavior.Restrict);

            // تخصیص FIFO مصرف تولید — Restrict برای جلوگیری از مسیر cascade چندگانه
            modelBuilder.Entity<ProductionInputLotAllocation>()
                .HasOne(a => a.InputLine)
                .WithMany()
                .HasForeignKey(a => a.ProductionInputLineId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionInputLotAllocation>()
                .HasOne(a => a.InventoryLot)
                .WithMany()
                .HasForeignKey(a => a.InventoryLotId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionBatchCostLine>()
                .HasOne(l => l.Batch)
                .WithMany(b => b.CostLines)
                .HasForeignKey(l => l.ProductionBatchId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductionBatchCostLine>()
                .HasOne(l => l.Account)
                .WithMany()
                .HasForeignKey(l => l.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionFormula>()
                .HasOne(f => f.Product)
                .WithMany()
                .HasForeignKey(f => f.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionFormula>()
                .HasOne(f => f.Meaurment)
                .WithMany()
                .HasForeignKey(f => f.MeaurmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // حداکثر یک فرمول پیش‌فرض فعال برای هر محصول
            modelBuilder.Entity<ProductionFormula>()
                .HasIndex(f => f.ProductId)
                .IsUnique()
                .HasFilter("[IsDefault] = 1 AND [IsDeleted] = 0");

            modelBuilder.Entity<ProductionFormulaMaterialLine>()
                .HasOne(l => l.Formula)
                .WithMany(f => f.MaterialLines)
                .HasForeignKey(l => l.ProductionFormulaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductionFormulaMaterialLine>()
                .HasOne(l => l.Product)
                .WithMany()
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionFormulaMaterialLine>()
                .HasOne(l => l.Meaurment)
                .WithMany()
                .HasForeignKey(l => l.MeaurmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionFormulaMaterialLine>()
                .HasOne(l => l.DefaultWarehouse)
                .WithMany()
                .HasForeignKey(l => l.DefaultWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionFormulaCostLine>()
                .HasOne(l => l.Formula)
                .WithMany(f => f.CostLines)
                .HasForeignKey(l => l.ProductionFormulaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductionFormulaCostLine>()
                .HasOne(l => l.Account)
                .WithMany()
                .HasForeignKey(l => l.AccountId)
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
