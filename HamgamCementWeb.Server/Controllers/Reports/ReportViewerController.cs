using HamgamCementWeb.Server.Services;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Stimulsoft.Report;

using Stimulsoft.Report.Mvc;



namespace HamgamCementWeb.Server.Controllers.Reports;



[Authorize]

public class ReportViewerController : Controller

{

    private const string PurchaseInvoiceSessionKey = "ReportPurchaseInvoiceId";

    private const string SaleInvoiceSessionKey = "ReportSaleInvoiceId";



    private readonly IInvoiceReportService _invoiceReports;



    public ReportViewerController(IInvoiceReportService invoiceReports)

    {

        _invoiceReports = invoiceReports;

    }



    public IActionResult Index()

    {

        return View();

    }



    public IActionResult GetReport()

    {

        var report = new StiReport();

        report.Load(StiNetCoreHelper.MapPath(this, "Reports/Report.mrt"));

        return StiNetCoreViewer.GetReportResult(this, report);

    }



    [HttpGet]

    public IActionResult Invoice(int purchaseInvoiceId)

    {

        if (purchaseInvoiceId <= 0)

        {

            return BadRequest("شناسه فاکتور نامعتبر است.");

        }



        HttpContext.Session.SetInt32(PurchaseInvoiceSessionKey, purchaseInvoiceId);

        HttpContext.Session.Remove(SaleInvoiceSessionKey);

        return View();

    }



    [HttpGet]

    public IActionResult SaleInvoice(int saleInvoiceId)

    {

        if (saleInvoiceId <= 0)

        {

            return BadRequest("شناسه فاکتور نامعتبر است.");

        }



        HttpContext.Session.SetInt32(SaleInvoiceSessionKey, saleInvoiceId);

        HttpContext.Session.Remove(PurchaseInvoiceSessionKey);

        return View("Invoice");

    }



    public async Task<IActionResult> GetInvoiceReport(CancellationToken cancellationToken)

    {

        var saleInvoiceId = HttpContext.Session.GetInt32(SaleInvoiceSessionKey);

        if (saleInvoiceId is > 0)

        {

            try

            {

                var saleReport = await _invoiceReports.BuildSalesInvoiceReportAsync(saleInvoiceId.Value, cancellationToken);

                return StiNetCoreViewer.GetReportResult(this, saleReport);

            }

            catch (InvalidOperationException ex)

            {

                return BadRequest(ex.Message);

            }

            catch (FileNotFoundException ex)

            {

                return NotFound(ex.Message);

            }

        }



        var purchaseInvoiceId = HttpContext.Session.GetInt32(PurchaseInvoiceSessionKey);

        if (purchaseInvoiceId is not > 0)

        {

            return BadRequest("فاکتور برای چاپ مشخص نشده است.");

        }



        try

        {

            var report = await _invoiceReports.BuildPurchaseInvoiceReportAsync(purchaseInvoiceId.Value, cancellationToken);

            return StiNetCoreViewer.GetReportResult(this, report);

        }

        catch (InvalidOperationException ex)

        {

            return BadRequest(ex.Message);

        }

        catch (FileNotFoundException ex)

        {

            return NotFound(ex.Message);

        }

    }



    public IActionResult InvoiceViewerEvent()

    {

        return StiNetCoreViewer.ViewerEventResult(this);

    }



    public IActionResult ViewerEvent()

    {

        return StiNetCoreViewer.ViewerEventResult(this);

    }

}


