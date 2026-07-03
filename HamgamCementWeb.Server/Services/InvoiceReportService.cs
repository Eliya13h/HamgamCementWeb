using HamgamCementWeb.Server.Data;

using HamgamCementWeb.Server.Data.Models.Invoice;

using HamgamCementWeb.Server.Data.Models.Product;

using Microsoft.EntityFrameworkCore;

using Stimulsoft.Report;

using Stimulsoft.Report.Components;

using System.Globalization;



namespace HamgamCementWeb.Server.Services;



public interface IInvoiceReportService

{

    Task<StiReport> BuildPurchaseInvoiceReportAsync(int purchaseInvoiceId, CancellationToken cancellationToken = default);

    Task<StiReport> BuildSalesInvoiceReportAsync(int saleInvoiceId, CancellationToken cancellationToken = default);

}



public class InvoiceReportService : IInvoiceReportService

{

    private readonly AppDbContext _db;

    private readonly IWebHostEnvironment _env;



    public InvoiceReportService(AppDbContext db, IWebHostEnvironment env)

    {

        _db = db;

        _env = env;

    }



    public async Task<StiReport> BuildPurchaseInvoiceReportAsync(

        int purchaseInvoiceId,

        CancellationToken cancellationToken = default)

    {

        var invoice = await _db.PurchaseInvoices

            .AsNoTracking()

            .Include(i => i.Supplier)

            .Include(i => i.Currency)

            .Include(i => i.BaseCurrency)

            .Include(i => i.Items.Where(x => x.IsDeleted != true))

                .ThenInclude(x => x.Product)

            .Include(i => i.Items.Where(x => x.IsDeleted != true))

                .ThenInclude(x => x.Meaurment)

            .FirstOrDefaultAsync(i => i.PurchaseInvoiceID == purchaseInvoiceId && i.IsDeleted != true, cancellationToken)

            ?? throw new InvalidOperationException("فاکتور خرید یافت نشد.");



        var invoiceSymbol = invoice.Currency?.Symbol ?? string.Empty;

        var baseSymbol = invoice.BaseCurrency?.Symbol ?? string.Empty;

        var isMultiCurrency = invoice.CurrencyId != invoice.BaseCurrencyId;

        var remaining = Math.Max(0, invoice.TotalAmount - invoice.PaidAmount);

        var paidInBase = invoice.TotalAmount > 0

            ? Math.Round(invoice.PaidAmount * invoice.TotalAmountInBaseCurrency / invoice.TotalAmount, 4)

            : 0m;

        var remainingInBase = Math.Max(0, invoice.TotalAmountInBaseCurrency - paidInBase);



        var info = new InvoiceReportInfo

        {

            InvoiceNumber = invoice.InvoiceNumber,

            InvoiceType = invoice.DocumentType switch

            {

                InvoiceDocumentType.PurchaseReturn => "برگشت از خرید",

                _ => $"{GetStatusName(invoice.Status)} خرید",

            },

            InvoiceRecipient = invoice.Supplier?.Name ?? string.Empty,

            InvoiceDate = JalaliDateHelper.FormatDate(invoice.InvoiceDate),

            PrintDate = JalaliDateHelper.FormatDate(DateTime.Now),

            CurrencySymbol = baseSymbol,

            IsChanged = isMultiCurrency,

            ShowPayment = true,

            TotalInvoice = FormatMoney(invoice.TotalAmount, invoiceSymbol),

            TotalPaid = FormatMoney(invoice.PaidAmount, invoiceSymbol),

            Remaining = FormatMoney(remaining, invoiceSymbol),

            TotalInvoiceBaseCurrency = FormatMoney(invoice.TotalAmountInBaseCurrency, baseSymbol),

            TotalPaidBaseCurrency = FormatMoney(paidInBase, baseSymbol),

            RemainingBaseCurrency = FormatMoney(remainingInBase, baseSymbol),

            Status = invoice.Status,

        };



        var items = invoice.Items

            .OrderBy(x => x.PurchaseItemID)

            .Select((item, index) => new InvoiceReportItem

            {

                Row = index + 1,

                Desc = FormatProductDesc(item.Product?.Name, item.Product?.Code),

                Qty = FormatQuantityWithUnit(item.Quantity, item.Meaurment),

                Price = FormatMoney(item.UnitPrice, invoiceSymbol),

                SubTotal = FormatMoney(item.LineTotal, invoiceSymbol),

                Comment = string.Empty,

            })

            .ToList();



        return BuildReport(info, items);

    }



    public async Task<StiReport> BuildSalesInvoiceReportAsync(

        int saleInvoiceId,

        CancellationToken cancellationToken = default)

    {

        var invoice = await _db.SaleInvoices

            .AsNoTracking()

            .Include(i => i.Customer)

            .Include(i => i.Currency)

            .Include(i => i.BaseCurrency)

            .Include(i => i.Items.Where(x => x.IsDeleted != true))

                .ThenInclude(x => x.Product)

            .Include(i => i.Items.Where(x => x.IsDeleted != true))

                .ThenInclude(x => x.Meaurment)

            .FirstOrDefaultAsync(i => i.SaleInvoiceID == saleInvoiceId && i.IsDeleted != true, cancellationToken)

            ?? throw new InvalidOperationException("فاکتور فروش یافت نشد.");



        var invoiceSymbol = invoice.Currency?.Symbol ?? string.Empty;

        var baseSymbol = invoice.BaseCurrency?.Symbol ?? string.Empty;

        var isMultiCurrency = invoice.CurrencyId != invoice.BaseCurrencyId;

        var showPayment = InvoiceStatusRules.ShowsPayment(invoice.Status);

        var remaining = Math.Max(0, invoice.TotalAmount - invoice.PaidAmount);

        var paidInBase = invoice.TotalAmount > 0

            ? Math.Round(invoice.PaidAmount * invoice.TotalAmountInBaseCurrency / invoice.TotalAmount, 4)

            : 0m;

        var remainingInBase = Math.Max(0, invoice.TotalAmountInBaseCurrency - paidInBase);



        var info = new InvoiceReportInfo

        {

            InvoiceNumber = invoice.InvoiceNumber,

            InvoiceType = invoice.DocumentType switch

            {

                InvoiceDocumentType.SaleReturn => "برگشت از فروش",

                _ => $"{GetStatusName(invoice.Status)} فروش",

            },

            InvoiceRecipient = invoice.Customer?.Name ?? string.Empty,

            InvoiceDate = JalaliDateHelper.FormatDate(invoice.InvoiceDate),

            PrintDate = JalaliDateHelper.FormatDate(DateTime.Now),

            CurrencySymbol = baseSymbol,

            IsChanged = isMultiCurrency,

            ShowPayment = showPayment,

            TotalInvoice = FormatMoney(invoice.TotalAmount, invoiceSymbol),

            TotalPaid = showPayment ? FormatMoney(invoice.PaidAmount, invoiceSymbol) : string.Empty,

            Remaining = showPayment ? FormatMoney(remaining, invoiceSymbol) : string.Empty,

            TotalInvoiceBaseCurrency = FormatMoney(invoice.TotalAmountInBaseCurrency, baseSymbol),

            TotalPaidBaseCurrency = showPayment ? FormatMoney(paidInBase, baseSymbol) : string.Empty,

            RemainingBaseCurrency = showPayment ? FormatMoney(remainingInBase, baseSymbol) : string.Empty,

            Status = invoice.Status,

        };



        var items = invoice.Items

            .OrderBy(x => x.SalesItemID)

            .Select((item, index) => new InvoiceReportItem

            {

                Row = index + 1,

                Desc = FormatProductDesc(item.Product?.Name, item.Product?.Code),

                Qty = FormatQuantityWithUnit(item.Quantity, item.Meaurment),

                Price = FormatMoney(item.UnitPrice, invoiceSymbol),

                SubTotal = FormatMoney(item.LineTotal, invoiceSymbol),

                Comment = string.Empty,

            })

            .ToList();



        return BuildReport(info, items);

    }



    private static readonly string[] MultiCurrencyComponentNames =

    [

        "Text24",

        "Text25",

        "Text26",

        "Text27",

        "Text28",

        "Text29",

    ];



    private static readonly string[] PaymentComponentNames =

    [

        "Text19",

        "Text20",

        "Text21",

        "Text22",

        "Text23",

        "Text25",

        "Text26",

        "Text27",

        "Text28",

        "Text29",

    ];



    private StiReport BuildReport(InvoiceReportInfo info, IReadOnlyList<InvoiceReportItem> items)

    {

        var reportPath = Path.Combine(_env.ContentRootPath, "Reports", "Inoive.mrt");

        if (!File.Exists(reportPath))

        {

            throw new FileNotFoundException("فایل گزارش فاکتور یافت نشد.", reportPath);

        }



        var report = new StiReport();

        report.Load(reportPath);

        report.RegBusinessObject("Info", info);

        report.RegBusinessObject("Items", items);

        report.Dictionary.Synchronize();

        ApplyMultiCurrencyVisibility(report, info.IsChanged);

        ApplyPaymentVisibility(report, info.ShowPayment, info.IsChanged);

        report.Compile();

        report.Render();

        return report;

    }



    private static void ApplyMultiCurrencyVisibility(StiReport report, bool showBaseCurrencySection)

    {

        foreach (var componentName in MultiCurrencyComponentNames)

        {

            if (report.GetComponentByName(componentName) is StiComponent component)

            {

                component.Enabled = showBaseCurrencySection;

            }

        }

    }



    private static void ApplyPaymentVisibility(StiReport report, bool showPayment, bool showBaseCurrencySection)

    {

        foreach (var componentName in PaymentComponentNames)

        {

            if (report.GetComponentByName(componentName) is not StiComponent component)

            {

                continue;

            }



            var isBaseCurrencyPayment = componentName is "Text25" or "Text26" or "Text27" or "Text28" or "Text29";

            component.Enabled = showPayment && (!isBaseCurrencyPayment || showBaseCurrencySection);

        }



        if (report.GetComponentByName("FooterBand1") is StiFooterBand footerBand)

        {

            if (!showPayment)

            {

                footerBand.Height = showBaseCurrencySection ? 18d : 18d;

            }

            else

            {

                footerBand.Height = showBaseCurrencySection ? 36d : 18d;

            }

        }

    }



    private static string FormatQuantityWithUnit(decimal quantity, Meaurment? meaurment)

    {

        if (quantity <= 0)

        {

            return string.Empty;

        }



        var formatted = quantity.ToString("#,##0.##", CultureInfo.InvariantCulture);

        var unit = meaurment?.Symbol ?? meaurment?.Name ?? string.Empty;

        return string.IsNullOrWhiteSpace(unit) ? formatted : $"{formatted} {unit}";

    }



    private static string FormatProductDesc(string? name, string? code)

    {

        var productName = name?.Trim() ?? string.Empty;

        var productCode = code?.Trim() ?? string.Empty;



        if (productName.Length > 0 && productCode.Length > 0)

        {

            return $"{productName} ({productCode})";

        }



        return productName.Length > 0 ? productName : productCode;

    }



    private static string FormatMoney(decimal amount, string symbol)

    {

        var formatted = amount.ToString("#,##0.##", CultureInfo.InvariantCulture);

        return string.IsNullOrWhiteSpace(symbol) ? formatted : $"{formatted} {symbol}";

    }



    private static string GetStatusName(InvoiceStatus status) => status switch

    {

        InvoiceStatus.Quotation => "استعلام قیمت",

        InvoiceStatus.Proforma => "پیش فاکتور",

        InvoiceStatus.Order => "آردر",

        InvoiceStatus.Inoivce => "فاکتور",

        _ => status.ToString(),

    };

}



public class InvoiceReportInfo

{

    public string InvoiceType { get; set; } = string.Empty;

    public string InvoiceRecipient { get; set; } = string.Empty;

    public string InvoiceDate { get; set; } = string.Empty;

    public string PrintDate { get; set; } = string.Empty;

    public string TotalInvoice { get; set; } = string.Empty;

    public string TotalPaid { get; set; } = string.Empty;

    public string Remaining { get; set; } = string.Empty;

    public string InvoiceNumber { get; set; } = string.Empty;

    public string WaterMark { get; set; } = string.Empty;

    public bool IsChanged { get; set; }

    public bool ShowPayment { get; set; } = true;

    public string CurrencySymbol { get; set; } = string.Empty;

    public string TotalInvoiceBaseCurrency { get; set; } = string.Empty;

    public string TotalPaidBaseCurrency { get; set; } = string.Empty;

    public string RemainingBaseCurrency { get; set; } = string.Empty;

    public InvoiceStatus Status { get; set; }

}



public class InvoiceReportItem

{

    public int Row { get; set; }

    public string Desc { get; set; } = string.Empty;

    public string Qty { get; set; } = string.Empty;

    public string Price { get; set; } = string.Empty;

    public string SubTotal { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;

}


