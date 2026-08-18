using HamgamTransport.Server.Data.Models;
using HamgamTransport.Server.Data.Models.Finance;
using HamgamTransport.Server.Data.Models.People;
using HamgamTransport.Server.Data.Models.Transport;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Shareholder> Shareholders { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }

        public DbSet<Currency> Currencies { get; set; }
        public DbSet<CurrencyExchangeRate> CurrencyExchangeRates { get; set; }
        public DbSet<CurrencyExchangeHistory> CurrencyExchangeHistories { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Revenue> Revenues { get; set; }
        public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
        public DbSet<RevenueCategory> RevenueCategories { get; set; }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<JournalLine> JournalLines { get; set; }
        public DbSet<CashBox> CashBoxes { get; set; }
        public DbSet<CashBoxUser> CashBoxUsers { get; set; }
        public DbSet<CashShift> CashShifts { get; set; }
        public DbSet<CashShiftOpeningLine> CashShiftOpeningLines { get; set; }
        public DbSet<CashTransfer> CashTransfers { get; set; }
        public DbSet<CashTransferLine> CashTransferLines { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<PartySettlement> PartySettlements { get; set; }
        public DbSet<CurrencyExchangeTxn> CurrencyExchangeTxns { get; set; }
        public DbSet<FiscalYear> FiscalYears { get; set; }
        public DbSet<FiscalPeriod> FiscalPeriods { get; set; }
        public DbSet<CostCenter> CostCenters { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<DoubtfulDebtProvision> DoubtfulDebtProvisions { get; set; }
        public DbSet<RecurringJournalTemplate> RecurringJournalTemplates { get; set; }
        public DbSet<RecurringJournalTemplateLine> RecurringJournalTemplateLines { get; set; }
        public DbSet<ShareholderEquityTxn> ShareholderEquityTxns { get; set; }
        public DbSet<FixedAssetCategory> FixedAssetCategories { get; set; }
        public DbSet<FixedAsset> FixedAssets { get; set; }
        public DbSet<FixedAssetDepreciation> FixedAssetDepreciations { get; set; }

        public DbSet<GeneralSettings> GeneralSettings { get; set; }

        public DbSet<VehicleType> VehicleTypes { get; set; }
        public DbSet<VehicleOwner> VehicleOwners { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehiclePair> VehiclePairs { get; set; }
        public DbSet<OwnerShareAgreement> OwnerShareAgreements { get; set; }
        public DbSet<TripExpenseCategory> TripExpenseCategories { get; set; }
        public DbSet<TransportTrip> TransportTrips { get; set; }
        public DbSet<TripExpense> TripExpenses { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            modelBuilder.Entity<JournalLine>()
                .HasOne(l => l.CostCenter)
                .WithMany()
                .HasForeignKey(l => l.CostCenterId)
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

            modelBuilder.Entity<CurrencyExchangeTxn>()
                .HasOne(t => t.FromCurrency)
                .WithMany()
                .HasForeignKey(t => t.FromCurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CurrencyExchangeTxn>()
                .HasOne(t => t.ToCurrency)
                .WithMany()
                .HasForeignKey(t => t.ToCurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CurrencyExchangeTxn>()
                .HasOne(t => t.FromCashBox)
                .WithMany()
                .HasForeignKey(t => t.FromCashBoxId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CurrencyExchangeTxn>()
                .HasOne(t => t.FromBankAccount)
                .WithMany()
                .HasForeignKey(t => t.FromBankAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CurrencyExchangeTxn>()
                .HasOne(t => t.ToCashBox)
                .WithMany()
                .HasForeignKey(t => t.ToCashBoxId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CurrencyExchangeTxn>()
                .HasOne(t => t.ToBankAccount)
                .WithMany()
                .HasForeignKey(t => t.ToBankAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CurrencyExchangeTxn>()
                .HasOne(t => t.ExchangeHistoryFrom)
                .WithMany()
                .HasForeignKey(t => t.ExchangeHistoryFromId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CurrencyExchangeTxn>()
                .HasOne(t => t.ExchangeHistoryTo)
                .WithMany()
                .HasForeignKey(t => t.ExchangeHistoryToId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CurrencyExchangeTxn>()
                .HasOne(t => t.JournalEntry)
                .WithMany()
                .HasForeignKey(t => t.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PartySettlement>()
                .HasOne(s => s.Currency)
                .WithMany()
                .HasForeignKey(s => s.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PartySettlement>()
                .HasOne(s => s.CashBox)
                .WithMany()
                .HasForeignKey(s => s.CashBoxId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PartySettlement>()
                .HasOne(s => s.BankAccount)
                .WithMany()
                .HasForeignKey(s => s.BankAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PartySettlement>()
                .HasOne(s => s.JournalEntry)
                .WithMany()
                .HasForeignKey(s => s.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DoubtfulDebtProvision>()
                .HasOne(p => p.JournalEntry)
                .WithMany()
                .HasForeignKey(p => p.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RecurringJournalTemplateLine>()
                .HasOne(l => l.Template)
                .WithMany(t => t.Lines)
                .HasForeignKey(l => l.RecurringJournalTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecurringJournalTemplateLine>()
                .HasOne(l => l.Account)
                .WithMany()
                .HasForeignKey(l => l.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RecurringJournalTemplateLine>()
                .HasOne(l => l.CostCenter)
                .WithMany()
                .HasForeignKey(l => l.CostCenterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FiscalPeriod>()
                .HasIndex(p => new { p.SolarYear, p.Month })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<CostCenter>()
                .HasIndex(c => c.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

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

            modelBuilder.Entity<VehicleType>()
                .HasIndex(v => v.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<Vehicle>()
                .HasIndex(v => v.PlateNumber)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<Vehicle>()
                .HasIndex(v => v.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0 AND [Code] <> ''");

            modelBuilder.Entity<TripExpenseCategory>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehiclePair>()
                .HasIndex(v => v.Code)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<TransportTrip>()
                .HasIndex(t => t.TripNumber)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.VehiclePair)
                .WithMany()
                .HasForeignKey(v => v.VehiclePairId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<VehiclePair>()
                .HasOne(p => p.PrimaryVehicle)
                .WithMany()
                .HasForeignKey(p => p.PrimaryVehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehiclePair>()
                .HasOne(p => p.SecondaryVehicle)
                .WithMany()
                .HasForeignKey(p => p.SecondaryVehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TripExpense>()
                .HasOne(e => e.TransportTrip)
                .WithMany(t => t.Expenses)
                .HasForeignKey(e => e.TransportTripId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransportTrip>()
                .HasOne(t => t.Customer)
                .WithMany()
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransportTrip>()
                .HasOne(t => t.Currency)
                .WithMany()
                .HasForeignKey(t => t.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TripExpense>()
                .HasOne(e => e.Currency)
                .WithMany()
                .HasForeignKey(e => e.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TripExpense>()
                .HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.TripExpenseCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
