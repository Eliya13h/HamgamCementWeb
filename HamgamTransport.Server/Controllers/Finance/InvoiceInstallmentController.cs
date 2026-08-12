using System.ComponentModel.DataAnnotations;
using HamgamTransport.Server.Authorization;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HamgamTransport.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/installments")]
[Authorize]
public class InvoiceInstallmentController : FinanceControllerBase
{
    private readonly IInvoiceInstallmentService _installments;

    public InvoiceInstallmentController(AppDbContext db, IInvoiceInstallmentService installments) : base(db)
    {
        _installments = installments;
    }

    [HttpGet]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> List(
        [FromQuery] int kind,
        [FromQuery] int invoiceId,
        CancellationToken cancellationToken)
    {
        if (kind is not ((int)InvoiceInstallmentKind.Sale or (int)InvoiceInstallmentKind.Purchase))
        {
            return BadRequest(new { message = "نوع فاکتور نامعتبر است." });
        }

        var rows = await _installments.ListAsync((InvoiceInstallmentKind)kind, invoiceId, cancellationToken);
        return Ok(rows.Select(Map));
    }

    [HttpPost("generate")]
    [HasPermission("accounting.expenses.create")]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateInstallmentsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var kind = (InvoiceInstallmentKind)request.Kind;
            DateTime? firstDue = null;
            if (!string.IsNullOrWhiteSpace(request.FirstDueDate)
                && DateTime.TryParse(request.FirstDueDate, out var parsed))
            {
                firstDue = parsed.Date;
            }

            var rows = await _installments.GenerateEqualAsync(
                kind,
                request.InvoiceId,
                request.Count,
                firstDue,
                ResolveCurrentUserId(),
                cancellationToken);

            return Ok(new
            {
                message = "اقساط مساوی ایجاد شد.",
                items = rows.Select(Map),
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static object Map(Data.Models.Finance.InvoiceInstallment i) => new
    {
        invoiceInstallmentId = i.InvoiceInstallmentID,
        invoiceKind = (int)i.InvoiceKind,
        invoiceId = i.InvoiceId,
        installmentNo = i.InstallmentNo,
        dueDate = i.DueDate.ToString("yyyy-MM-dd"),
        amount = i.Amount,
        paidAmount = i.PaidAmount,
        remaining = i.Amount - i.PaidAmount,
    };
}

public class GenerateInstallmentsRequest
{
    [Range(1, 2)]
    public int Kind { get; set; }

    [Range(1, int.MaxValue)]
    public int InvoiceId { get; set; }

    [Range(1, 60)]
    public int Count { get; set; } = 1;

    public string? FirstDueDate { get; set; }
}
