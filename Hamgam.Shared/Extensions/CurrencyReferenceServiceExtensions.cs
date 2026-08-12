using Hamgam.Shared.CurrencySync;
using Hamgam.Shared.Data;
using Hamgam.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hamgam.Shared.Extensions;

public static class CurrencyReferenceServiceExtensions
{
    public static IServiceCollection AddHamgamCurrencyReference(
        this IServiceCollection services,
        IConfiguration configuration,
        string systemCode)
    {
        services.Configure<CurrencySyncOptions>(configuration.GetSection(CurrencySyncOptions.SectionName));
        services.PostConfigure<CurrencySyncOptions>(options => options.SystemCode = systemCode);

        var referenceConnection = configuration.GetConnectionString("Reference")
            ?? throw new InvalidOperationException("Connection string 'Reference' is not configured.");

        services.AddDbContext<ReferenceDbContext>(options =>
            options.UseSqlServer(referenceConnection));

        services.AddScoped<ICurrencyReferenceSyncService, CurrencyReferenceSyncService>();

        return services;
    }
}
