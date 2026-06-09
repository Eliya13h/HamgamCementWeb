using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Data.Models.People;
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
        public DbSet<Department> Departments { get; set; }

        //finance tables

        public DbSet<Currency> Currencies { get; set; }
        public DbSet<CurrencyExchangeRate> CurrencyExchangeRates { get; set; }
        public DbSet<CurrencyExchangeHistory> CurrencyExchangeHistories { get; set; }

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
        }
    }
}
