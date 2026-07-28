using System.ComponentModel.DataAnnotations;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Controllers.Transport;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Invoice;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Invoice;

[ApiController]
[Route("api/transactions/purchase-invoices")]
[Authorize]
public class PurchaseInvoiceController : InvoiceControllerBase
{
    private readonly IInvoicePostingService _posting;
    private readonly IInvoiceReturnService _returns;
    private readonly IFreightTripService _freight;
    private readonly IPurchaseInvoiceReadService _reads;

    public PurchaseInvoiceController(
        AppDbContext db,
        IInvoicePostingService posting,
        IInvoiceReturnService returns,
        IFreightTripService freight,
        IPurchaseInvoiceReadService reads) : base(db)
    {
        _posting = posting;
        _returns = returns;
        _freight = freight;
        _reads = reads;
    }

    [HttpPost("datatable")]
    [HasPermission("transactions.purchase.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reads.QueryDataTableAsync(request, cancellationToken);

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Rows.Select((r, i) => new
            {
                rowNumber = result.Start + i + 1,
                purchaseInvoiceId = r.PurchaseInvoiceId,
                invoiceNumber = r.InvoiceNumber,
                supplierId = r.SupplierId,
                supplierName = r.SupplierName,
                warehouseId = r.WarehouseId,
                warehouseName = r.WarehouseName,
                invoiceDate = r.InvoiceDate,
                status = r.Status,
                currencyId = r.CurrencyId,
                currencyName = r.CurrencyName,
                currencySymbol = r.CurrencySymbol,
                baseCurrencySymbol = r.BaseCurrencySymbol,
                totalAmount = r.TotalAmount,
                totalAmountInBaseCurrency = r.TotalAmountInBaseCurrency,
                documentType = r.DocumentType,
                entrySource = r.EntrySource,
                productionBatchId = r.ProductionBatchId,
                productionBatchNumber = r.ProductionBatchNumber,
                referencePurchaseInvoiceId = r.ReferencePurchaseInvoiceId,
                referenceInvoiceNumber = r.ReferenceInvoiceNumber,
                isPosted = r.IsPosted,
                itemsCount = r.ItemsCount,
                description = r.Description,
            }),
        });
    }

    [HttpGet("next-code-preview")]
    [HasPermission("transactions.purchase.view")]
    public async Task<IActionResult> NextCodePreview(CancellationToken cancellationToken)
    {
        var code = await _reads.GetNextCodePreviewAsync(cancellationToken);
        return Ok(new { code });
    }

    [HttpGet("{id:int}")]
    [HasPermission("transactions.purchase.view")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var invoice = await _reads.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            return NotFound(new { message = "فاکتور خرید یافت نشد." });
        }

        return Ok(new
        {
            purchaseInvoiceId = invoice.PurchaseInvoiceId,
            invoiceNumber = invoice.InvoiceNumber,
            supplierId = invoice.SupplierId,
            warehouseId = invoice.WarehouseId,
            invoiceDate = invoice.InvoiceDate,
            status = invoice.Status,
            currencyId = invoice.CurrencyId,
            baseCurrencyId = invoice.BaseCurrencyId,
            exchangeHistoryId = invoice.ExchangeHistoryId,
            baseUnitsPerUnitAtTransaction = invoice.BaseUnitsPerUnitAtTransaction,
            totalAmount = invoice.TotalAmount,
            totalAmountInBaseCurrency = invoice.TotalAmountInBaseCurrency,
            subTotalAmount = invoice.SubTotalAmount,
            taxPercent = invoice.TaxPercent,
            taxAmount = invoice.TaxAmount,
            paymentTermDays = invoice.PaymentTermDays,
            dueDate = invoice.DueDate,
            paidAmount = invoice.PaidAmount,
            isCash = invoice.IsCash,
            documentType = invoice.DocumentType,
            entrySource = invoice.EntrySource,
            productionBatchId = invoice.ProductionBatchId,
            productionBatchNumber = invoice.ProductionBatchNumber,
            referencePurchaseInvoiceId = invoice.ReferencePurchaseInvoiceId,
            referenceInvoiceNumber = invoice.ReferenceInvoiceNumber,
            isPosted = invoice.IsPosted,
            postedAt = invoice.PostedAt,
            description = invoice.Description,
            freightMode = invoice.FreightMode,
            freightRatePerTon = invoice.FreightRatePerTon,
            freightWeightTon = invoice.FreightWeightTon,
            freightAmount = invoice.FreightAmount,
            freightAmountInBaseCurrency = invoice.FreightAmountInBaseCurrency,
            freightVehicleId = invoice.FreightVehicleId,
            freightCarrierName = invoice.FreightCarrierName,
            transportTripId = invoice.TransportTripId,
            items = invoice.Items.Select(x => new
            {
                purchaseItemId = x.PurchaseItemId,
                productId = x.ProductId,
                productName = x.ProductName,
                productCode = x.ProductCode,
                meaurmentId = x.MeaurmentId,
                meaurmentName = x.MeaurmentName,
                quantity = x.Quantity,
                quantityInBase = x.QuantityInBase,
                unitPrice = x.UnitPrice,
                lineTotal = x.LineTotal,
                lineTotalInBaseCurrency = x.LineTotalInBaseCurrency,
                returnedQuantity = x.ReturnedQuantity,
                inventoryLotId = x.InventoryLotId,
            }),
        });
    }

    [HttpGet("{id:int}/production-trace")]
    [HasPermission("transactions.purchase.view")]
    public async Task<IActionResult> GetProductionTrace(int id, CancellationToken cancellationToken)
    {
        var invoice = await Db.PurchaseInvoices
            .AsNoTracking()
            .Where(i => i.PurchaseInvoiceID == id && i.IsDeleted != true)
            .Select(i => new { i.EntrySource, i.ProductionBatchId })
            .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null)
        {
            return NotFound(new { message = "فاکتور خرید یافت نشد." });
        }

        if (invoice.EntrySource != PurchaseEntrySource.Production || invoice.ProductionBatchId is null)
        {
            return BadRequest(new { message = "این فاکتور مربوط به ورود از تولید نیست." });
        }

        var posting = HttpContext.RequestServices.GetRequiredService<IProductionPostingService>();
        try
        {
            var trace = await posting.GetTraceAsync(invoice.ProductionBatchId.Value, cancellationToken);
            return Ok(trace);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/returnable-lines")]
    [HasPermission("transactions.purchase.view")]
    public async Task<IActionResult> GetReturnableLines(int id, CancellationToken cancellationToken)
    {
        try
        {
            var lines = await _returns.GetPurchaseReturnableLinesAsync(id, cancellationToken);
            return Ok(lines);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/returns")]
    [HasPermission("transactions.purchase.view")]
    public async Task<IActionResult> GetReturns(int id, CancellationToken cancellationToken)
    {
        var returns = await _returns.GetPurchaseReturnsAsync(id, cancellationToken);
        return Ok(returns);
    }

    // چرا edit: عملیات برگشت از خرید، تغییر در سند خرید موجود است و به .edit نگاشت می‌شود.
    [HttpPost("{id:int}/returns")]
    [HasPermission("transactions.purchase.edit")]
    public async Task<IActionResult> CreateReturn(
        int id,
        [FromBody] CreateInvoiceReturnRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = ResolveCurrentUserId();
            // چرا تراکنش: ساخت سند برگشت و ثبت نهایی آن باید اتمیک باشند تا در صورت خطای ثبت، سند برگشت ناقص نماند.
            await using var tx = await Db.Database.BeginTransactionAsync(cancellationToken);
            var returnId = await _returns.CreatePurchaseReturnAsync(id, request, userId, cancellationToken);
            await _posting.PostPurchaseAsync(returnId, userId, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return Ok(new
            {
                message = "برگشت از خرید ثبت شد.",
                purchaseInvoiceId = returnId,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [HasPermission("transactions.purchase.create")]
    public async Task<IActionResult> Create(
        [FromBody] SavePurchaseInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.Items.Count == 0)
        {
            return BadRequest(new { message = "فاکتور باید حداقل یک ردیف داشته باشد." });
        }

        var entryError = await ValidateEntrySourceAsync(request, cancellationToken);
        if (entryError is not null)
        {
            return BadRequest(new { message = entryError });
        }

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;

        var invoice = new PurchaseInvoice
        {
            InvoiceNumber = $"TMP{DateTime.UtcNow.Ticks}",
            SupplierId = request.SupplierId,
            WarehouseId = request.WarehouseId,
            InvoiceDate = request.InvoiceDate,
            Status = request.Status,
            CurrencyId = request.CurrencyId,
            EntrySource = request.EntrySource,
            ProductionBatchId = request.ProductionBatchId,
            TaxPercent = request.TaxPercent,
            PaymentTermDays = request.PaymentTermDays,
            DueDate = request.DueDate,
            Description = request.Description?.Trim(),
            FreightMode = request.FreightMode,
            FreightRatePerTon = request.FreightRatePerTon,
            FreightWeightTon = request.FreightWeightTon,
            FreightVehicleId = request.FreightVehicleId,
            FreightCarrierName = request.FreightCarrierName?.Trim(),
            IsDeleted = false,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId,
        };

        foreach (var line in request.Items)
        {
            invoice.Items.Add(new PurchaseItem
            {
                ProductId = line.ProductId,
                MeaurmentId = line.MeaurmentId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = userId,
            });
        }

        await _posting.ApplyPurchaseCurrencyAsync(invoice, cancellationToken, request.BaseUnitsPerUnit);
        try
        {
            _freight.NormalizeAndValidatePurchaseFreight(invoice);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        var paidError = TrySetPaidAmount(invoice, request.PaidAmount);
        if (paidError is not null)
        {
            return BadRequest(new { message = paidError });
        }

        Db.PurchaseInvoices.Add(invoice);
        await Db.SaveChangesAsync(cancellationToken);

        invoice.InvoiceNumber = InvoiceCodeHelper.ForPurchase(invoice.PurchaseInvoiceID);
        await Db.SaveChangesAsync(cancellationToken);

        if (request.Status == Data.InvoiceStatus.Invoice)
        {
            try
            {
                await _posting.PostPurchaseAsync(invoice.PurchaseInvoiceID, userId, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message, purchaseInvoiceId = invoice.PurchaseInvoiceID });
            }

            return Ok(new { message = "فاکتور خرید ثبت شد. موجودی و مصارف به‌روز شد.", purchaseInvoiceId = invoice.PurchaseInvoiceID });
        }

        return Ok(new { message = "فاکتور خرید با موفقیت ایجاد شد.", purchaseInvoiceId = invoice.PurchaseInvoiceID });
    }

    [HttpPut("{id:int}")]
    [HasPermission("transactions.purchase.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SavePurchaseInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.Items.Count == 0)
        {
            return BadRequest(new { message = "فاکتور باید حداقل یک ردیف داشته باشد." });
        }

        var invoice = await Db.PurchaseInvoices
            .Include(i => i.Items.Where(x => x.IsDeleted != true))
            .FirstOrDefaultAsync(i => i.PurchaseInvoiceID == id && i.IsDeleted != true, cancellationToken);

        if (invoice is null)
        {
            return NotFound(new { message = "فاکتور خرید یافت نشد." });
        }

        if (invoice.IsPosted)
        {
            return BadRequest(new { message = "فاکتور ثبت‌شده قابل ویرایش نیست." });
        }

        if (invoice.DocumentType != InvoiceDocumentType.Invoice)
        {
            return BadRequest(new { message = "سند برگشت قابل ویرایش نیست." });
        }

        var entryError = await ValidateEntrySourceAsync(request, cancellationToken);
        if (entryError is not null)
        {
            return BadRequest(new { message = entryError });
        }

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;

        invoice.SupplierId = request.SupplierId;
        invoice.WarehouseId = request.WarehouseId;
        invoice.InvoiceDate = request.InvoiceDate;
        invoice.Status = request.Status;
        invoice.CurrencyId = request.CurrencyId;
        invoice.EntrySource = request.EntrySource;
        invoice.ProductionBatchId = request.ProductionBatchId;
        invoice.TaxPercent = request.TaxPercent;
        invoice.PaymentTermDays = request.PaymentTermDays;
        invoice.DueDate = request.DueDate;
        invoice.Description = request.Description?.Trim();
        invoice.FreightMode = request.FreightMode;
        invoice.FreightRatePerTon = request.FreightRatePerTon;
        invoice.FreightWeightTon = request.FreightWeightTon;
        invoice.FreightVehicleId = request.FreightVehicleId;
        invoice.FreightCarrierName = request.FreightCarrierName?.Trim();
        invoice.IsUpdated = true;
        invoice.UpdatedAt = now;
        invoice.UpdatedBy = userId;

        var incomingIds = request.Items
            .Where(x => x.PurchaseItemId is > 0)
            .Select(x => x.PurchaseItemId!.Value)
            .ToHashSet();

        foreach (var existing in invoice.Items.Where(x => !incomingIds.Contains(x.PurchaseItemID)))
        {
            existing.IsDeleted = true;
            existing.DeletedAt = now;
            existing.DeletedBy = userId;
        }

        foreach (var line in request.Items)
        {
            var existing = line.PurchaseItemId is > 0
                ? invoice.Items.FirstOrDefault(x => x.PurchaseItemID == line.PurchaseItemId)
                : null;

            if (existing is null)
            {
                invoice.Items.Add(new PurchaseItem
                {
                    ProductId = line.ProductId,
                    MeaurmentId = line.MeaurmentId,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    IsDeleted = false,
                    CreatedAt = now,
                    CreatedBy = userId,
                });
            }
            else
            {
                existing.ProductId = line.ProductId;
                existing.MeaurmentId = line.MeaurmentId;
                existing.Quantity = line.Quantity;
                existing.UnitPrice = line.UnitPrice;
                existing.IsUpdated = true;
                existing.UpdatedAt = now;
                existing.UpdatedBy = userId;
            }
        }

        await _posting.ApplyPurchaseCurrencyAsync(invoice, cancellationToken, request.BaseUnitsPerUnit);
        try
        {
            _freight.NormalizeAndValidatePurchaseFreight(invoice);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        var paidError = TrySetPaidAmount(invoice, request.PaidAmount);
        if (paidError is not null)
        {
            return BadRequest(new { message = paidError });
        }

        await Db.SaveChangesAsync(cancellationToken);

        if (request.Status == Data.InvoiceStatus.Invoice)
        {
            try
            {
                await _posting.PostPurchaseAsync(invoice.PurchaseInvoiceID, userId, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            return Ok(new { message = "فاکتور خرید ثبت شد. موجودی و مصارف به‌روز شد." });
        }

        return Ok(new { message = "فاکتور خرید با موفقیت ویرایش شد." });
    }

    // چرا edit: ثبت نهایی (Post) تغییر وضعیت سند است و به .edit نگاشت می‌شود.
    [HttpPost("{id:int}/post")]
    [HasPermission("transactions.purchase.edit")]
    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        var invoice = await Db.PurchaseInvoices
            .AsNoTracking()
            .Where(i => i.PurchaseInvoiceID == id && i.IsDeleted != true)
            .Select(i => new { i.Status, i.IsPosted, i.DocumentType })
            .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null)
        {
            return NotFound(new { message = "فاکتور خرید یافت نشد." });
        }

        if (invoice.IsPosted)
        {
            return BadRequest(new { message = "این فاکتور قبلاً ثبت نهایی شده است." });
        }

        // چرا فقط Invoice: ثبت نهایی دستی فقط برای فاکتور نهایی مجاز است؛ در غیر این صورت (استعلام/پیش‌فاکتور/آردر)
        // ممکن بود مصرف مالی بدون ورود موجودی ثبت شود و داده ناسازگار گردد. اسناد برگشت مسیر ثبت جداگانه دارند.
        if (invoice.DocumentType != InvoiceDocumentType.Invoice)
        {
            return BadRequest(new { message = "اسناد برگشت از این مسیر ثبت نمی‌شوند." });
        }

        if (invoice.Status != InvoiceStatus.Invoice)
        {
            return BadRequest(new { message = "فقط فاکتور نهایی قابل ثبت نهایی است. ابتدا وضعیت فاکتور را به «فاکتور» تغییر دهید." });
        }

        try
        {
            await _posting.PostPurchaseAsync(id, ResolveCurrentUserId(), cancellationToken);
            return Ok(new { message = "فاکتور خرید ثبت نهایی شد. موجودی و مصارف به‌روز شد." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [HasPermission("transactions.purchase.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var invoice = await Db.PurchaseInvoices
            .Include(i => i.Items.Where(x => x.IsDeleted != true))
            .FirstOrDefaultAsync(i => i.PurchaseInvoiceID == id && i.IsDeleted != true, cancellationToken);

        if (invoice is null)
        {
            return NotFound(new { message = "فاکتور خرید یافت نشد." });
        }

        if (invoice.IsPosted)
        {
            return BadRequest(new { message = "فاکتور ثبت‌شده قابل حذف نیست." });
        }

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;

        invoice.IsDeleted = true;
        invoice.IsActive = false;
        invoice.DeletedAt = now;
        invoice.DeletedBy = userId;

        foreach (var item in invoice.Items)
        {
            item.IsDeleted = true;
            item.DeletedAt = now;
            item.DeletedBy = userId;
        }

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "فاکتور خرید با موفقیت حذف شد." });
    }

    private static string? TrySetPaidAmount(PurchaseInvoice invoice, decimal paidAmount)
    {
        if (paidAmount < 0)
        {
            return "مبلغ پرداخت‌شده نمی‌تواند منفی باشد.";
        }

        if (paidAmount > invoice.TotalAmount)
        {
            return "مبلغ پرداخت‌شده نمی‌تواند بیشتر از جمع فاکتور باشد.";
        }

        invoice.PaidAmount = paidAmount;
        invoice.IsCash = invoice.TotalAmount > 0 && paidAmount >= invoice.TotalAmount;
        return null;
    }

    private Task<string?> ValidateEntrySourceAsync(
        SavePurchaseInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EntrySource == PurchaseEntrySource.Production)
        {
            return Task.FromResult<string?>(
                "ورود از تولید دیگر پشتیبانی نمی‌شود. محصول را در انبار پردازش‌شده تولید کنید و مستقیم بفروشید.");
        }

        if (request.ProductionBatchId is > 0)
        {
            return Task.FromResult<string?>("سند تولید به فاکتور خرید متصل نمی‌شود.");
        }

        return Task.FromResult<string?>(null);
    }

    public class SavePurchaseInvoiceRequest
    {
        [Range(1, int.MaxValue)]
        public int SupplierId { get; set; }

        [Range(1, int.MaxValue)]
        public int WarehouseId { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; }

        public Data.InvoiceStatus Status { get; set; } = Data.InvoiceStatus.Quotation;

        [Range(1, int.MaxValue)]
        public int CurrencyId { get; set; }

        public PurchaseEntrySource EntrySource { get; set; } = PurchaseEntrySource.Market;

        public int? ProductionBatchId { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PaidAmount { get; set; }

        [Range(0, 100)]
        public decimal TaxPercent { get; set; }

        [Range(0, int.MaxValue)]
        public int PaymentTermDays { get; set; }

        public DateTime? DueDate { get; set; }

        public decimal? BaseUnitsPerUnit { get; set; }

        public FreightMode FreightMode { get; set; } = FreightMode.None;

        [Range(0, double.MaxValue)]
        public decimal FreightRatePerTon { get; set; }

        [Range(0, double.MaxValue)]
        public decimal FreightWeightTon { get; set; }

        public int? FreightVehicleId { get; set; }

        [MaxLength(200)]
        public string? FreightCarrierName { get; set; }

        public List<SavePurchaseItemRequest> Items { get; set; } = [];
    }

    public class SavePurchaseItemRequest
    {
        public int? PurchaseItemId { get; set; }

        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int MeaurmentId { get; set; }

        [Range(0.000001, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }
    }
}
