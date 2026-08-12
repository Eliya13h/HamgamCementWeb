using Hamgam.Shared.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Hamgam.Shared.Data;

public class ReferenceDbContext : DbContext
{
    public ReferenceDbContext(DbContextOptions<ReferenceDbContext> options)
        : base(options)
    {
    }

    public DbSet<ReferenceCurrency> Currencies { get; set; }
    public DbSet<ReferenceCurrencyExchangeRate> CurrencyExchangeRates { get; set; }
    public DbSet<ReferenceCurrencyExchangeHistory> CurrencyExchangeHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReferenceCurrency>(entity =>
        {
            entity.HasIndex(c => c.CurrencyCode).IsUnique();
            entity.Property(c => c.OriginSystem).HasMaxLength(20);
        });

        modelBuilder.Entity<ReferenceCurrencyExchangeRate>()
            .HasIndex(r => r.CurrencyID)
            .IsUnique();

        modelBuilder.Entity<ReferenceCurrencyExchangeRate>()
            .HasOne(r => r.Currency)
            .WithMany(c => c.ExchangeRates)
            .HasForeignKey(r => r.CurrencyID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReferenceCurrencyExchangeRate>()
            .HasOne(r => r.BaseCurrency)
            .WithMany()
            .HasForeignKey(r => r.BaseCurrencyID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReferenceCurrencyExchangeRate>()
            .HasOne(r => r.SourceHistory)
            .WithMany()
            .HasForeignKey(r => r.SourceHistoryID)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ReferenceCurrencyExchangeHistory>()
            .HasIndex(h => new { h.CurrencyID, h.EffectiveFrom });

        modelBuilder.Entity<ReferenceCurrencyExchangeHistory>()
            .HasIndex(h => new { h.CurrencyID, h.EffectiveTo });

        modelBuilder.Entity<ReferenceCurrencyExchangeHistory>()
            .HasOne(h => h.Currency)
            .WithMany(c => c.ExchangeHistories)
            .HasForeignKey(h => h.CurrencyID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReferenceCurrencyExchangeHistory>()
            .HasOne(h => h.BaseCurrency)
            .WithMany()
            .HasForeignKey(h => h.BaseCurrencyID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
