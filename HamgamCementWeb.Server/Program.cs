using System.Text.Json;
using HamgamCementWeb.Server;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Seed;
using HamgamCementWeb.Server.Services;
using Hamgam.Shared.CurrencySync;
using Hamgam.Shared.Extensions;
using Hamgam.Shared.Services;
using AppUser = HamgamCementWeb.Server.Data.Models.People.User;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stimulsoft.Base;

// کلید پیش‌فرض لایسنس Stimulsoft؛ در صورت وجود مقدار در تنظیمات (Stimulsoft:LicenseKey) از آن استفاده می‌شود
const string DefaultStiLicenseKey =
    "6vJhGtLLLz2GNviWmUTrhSqnOItdDwjBylQzQcAOiHkO46nMQvol4ASeg91in+mGJLnn2KMIpg3eSXQSgaFOm15+0l" +
    "hekKip+wRGMwXsKpHAkTvorOFqnpF9rchcYoxHXtjNDLiDHZGTIWq6D/2q4k/eiJm9fV6FdaJIUbWGS3whFWRLPHWC" +
    "BsWnalqTdZlP9knjaWclfjmUKf2Ksc5btMD6pmR7ZHQfHXfdgYK7tLR1rqtxYxBzOPq3LIBvd3spkQhKb07LTZQoyQ" +
    "3vmRSMALmJSS6ovIS59XPS+oSm8wgvuRFqE1im111GROa7Ww3tNJTA45lkbXX+SocdwXvEZyaaq61Uc1dBg+4uFRxv" +
    "yRWvX5WDmJz1X0VLIbHpcIjdEDJUvVAN7Z+FW5xKsV5ySPs8aegsY9ndn4DmoZ1kWvzUaz+E1mxMbOd3tyaNnmVhPZ" +
    "eIBILmKJGN0BwnnI5fu6JHMM/9QR2tMO1Z4pIwae4P92gKBrt0MqhvnU1Q6kIaPPuG2XBIvAWykVeH2a9EP6064e11" +
    "PFCBX4gEpJ3XFD0peE5+ddZh+h495qUc1H2B";

var builder = WebApplication.CreateBuilder(args);

// لایسنس Stimulsoft از تنظیمات خوانده می‌شود و در نبود آن مقدار پیش‌فرض به کار می‌رود
StiLicense.Key = builder.Configuration["Stimulsoft:LicenseKey"] is { Length: > 0 } configuredKey
    ? configuredKey
    : DefaultStiLicenseKey;

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddOpenApi();
builder.Services.AddDataProtection();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<IMeaurmentConversionService, MeaurmentConversionService>();
builder.Services.AddScoped<IFifoInventoryService, FifoInventoryService>();
builder.Services.AddScoped<IProductPurchasePriceHintService, ProductPurchasePriceHintService>();
builder.Services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();
builder.Services.AddScoped<ICurrencyExchangeRateService, CurrencyExchangeRateService>();
        builder.Services.AddScoped<IInvoicePostingService, InvoicePostingService>();
        builder.Services.AddScoped<IInvoiceReturnService, InvoiceReturnService>();
builder.Services.AddScoped<ICustomerReadService, CustomerReadService>();
builder.Services.AddScoped<ISupplierReadService, SupplierReadService>();
builder.Services.AddScoped<IEmployeeReadService, EmployeeReadService>();
builder.Services.AddScoped<IDepartmentReadService, DepartmentReadService>();
builder.Services.AddScoped<IShareholderReadService, ShareholderReadService>();
builder.Services.AddScoped<IInvoiceReportService, InvoiceReportService>();
builder.Services.AddScoped<IJournalReportService, JournalReportService>();
builder.Services.AddScoped<IProductReportService, ProductReportService>();
builder.Services.AddScoped<IProductionReportService, ProductionReportService>();
builder.Services.AddScoped<IWarehouseTurnoverService, WarehouseTurnoverService>();
builder.Services.AddScoped<IFinanceCategoryService, FinanceCategoryService>();
builder.Services.AddScoped<IProductionPostingService, ProductionPostingService>();
builder.Services.AddScoped<IProductionBatchReadService, ProductionBatchReadService>();
builder.Services.AddScoped<IProductionPlanReadService, ProductionPlanReadService>();
builder.Services.AddScoped<IProductionFormulaReadService, ProductionFormulaReadService>();
builder.Services.AddScoped<IProductionCostCategoryReadService, ProductionCostCategoryReadService>();
builder.Services.AddScoped<IJournalPostingService, JournalPostingService>();
builder.Services.AddScoped<IAccountingIntegrityService, AccountingIntegrityService>();
builder.Services.AddScoped<IAccountLookupService, AccountLookupService>();
builder.Services.AddScoped<IOperationalGlService, OperationalGlService>();
builder.Services.AddScoped<IFixedAssetPostingService, FixedAssetPostingService>();
builder.Services.AddScoped<ICashBoxService, CashBoxService>();
builder.Services.AddScoped<ICashBalanceService, CashBalanceService>();
        builder.Services.AddScoped<IFiscalYearCloseService, FiscalYearCloseService>();
        builder.Services.AddScoped<IShareholderEquityPostingService, ShareholderEquityPostingService>();
        builder.Services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IFinanceReadService, FinanceReadService>();
builder.Services.AddScoped<IFinanceStatementService, FinanceStatementService>();
builder.Services.AddScoped<IPartyOpeningBalanceService, PartyOpeningBalanceService>();
builder.Services.AddScoped<IBankAccountService, BankAccountService>();
builder.Services.AddScoped<IPartySettlementService, PartySettlementService>();
builder.Services.AddScoped<IInvoiceInstallmentService, InvoiceInstallmentService>();
builder.Services.AddScoped<ICurrencyExchangeService, CurrencyExchangeService>();
builder.Services.AddScoped<IInventoryOpeningService, InventoryOpeningService>();
builder.Services.AddScoped<IPurchaseInvoiceReadService, PurchaseInvoiceReadService>();
builder.Services.AddScoped<IDashboardReadService, DashboardReadService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "HamgamCement.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// 📌 کانکشن استرینگ قبل از Build
var connectionString = builder.Configuration.GetConnectionString("Local");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddHamgamCurrencyReference(builder.Configuration, SystemCodes.Cement);

var app = builder.Build();

StimulsoftSetup.RegisterReportFonts(app.Environment);

// بعد از ریستارت ویندوز SQL Server ممکن است دیرتر از IIS بالا بیاید؛ چند بار تلاش می‌کنیم
await RunStartupWithRetryAsync(
    async () =>
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    },
    app.Logger,
    "Database migrate");

await RunStartupWithRetryAsync(
    () => DataSeeder.SeedAsync(app.Services),
    app.Logger,
    "Database seed");

await RunStartupWithRetryAsync(
    async () =>
    {
        using var scope = app.Services.CreateScope();
        var currencySync = scope.ServiceProvider.GetRequiredService<ICurrencyReferenceSyncService>();
        await currencySync.EnsureReferenceDatabaseAsync();
        await currencySync.SeedReferenceFromLocalIfEmptyAsync();
        await currencySync.SyncFromReferenceToLocalAsync();
    },
    app.Logger,
    "Currency reference sync");

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "Reports", "Fonts")),
    RequestPath = "/report-fonts",
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
// Session باید پیش از Authentication/Authorization فعال شود تا در صورت نیاز در دسترس باشد
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "reportViewer",
    pattern: "report-viewer/{action=Index}/{id?}",
    defaults: new { controller = "ReportViewer" });

app.MapControllers();
app.MapFallbackToFile("/index.html");

app.Run();

static async Task RunStartupWithRetryAsync(
    Func<Task> action,
    ILogger logger,
    string operationName,
    int maxAttempts = 12,
    int delaySeconds = 5)
{
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await action();
            if (attempt > 1)
            {
                logger.LogInformation("{Operation} succeeded on attempt {Attempt}.", operationName, attempt);
            }

            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(
                ex,
                "{Operation} failed on attempt {Attempt}/{MaxAttempts}; retrying in {DelaySeconds}s.",
                operationName,
                attempt,
                maxAttempts,
                delaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
    }
}
