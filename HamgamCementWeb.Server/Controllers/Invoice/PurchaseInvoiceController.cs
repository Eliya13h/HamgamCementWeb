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
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(PurchaseInvoice.InvoiceNumber),
        [2] = nameof(PurchaseInvoice.SupplierId),
        [3] = nameof(PurchaseInvoice.WarehouseId),
        [4] = nameof(PurchaseInvoice.InvoiceDate),
        [5] = nameof(PurchaseInvoice.TotalAmount),
        [6] = nameof(PurchaseInvoice.TotalAmountInBaseCurrency),
        [7] = nameof(PurchaseInvoice.IsPosted),
    };

    private readonly IInvoicePostingService _posting;
    private readonly IInvoiceReturnService _returns;

    public PurchaseInvoiceController(
        AppDbContext db,
        IInvoicePostingService posting,
        IInvoiceReturnService returns) : base(db)
    {
        _posting = posting;
        _returns = returns;
    }

    [HttpPost("datatable")]
    [HasPermission("transactions.purchase.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.PurchaseInvoices
            .AsNoTracking()
            .Where(i => i.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(i =>
                i.InvoiceNumber.Contains(searchValue) ||
                (i.Description != null && i.Description.Contains(searchValue)) ||
                (i.Supplier != null && i.Supplier.Name.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(PurchaseInvoice.CreatedAt))
            .Skip(start)
            .Take(length)
            .Select(i => new
            {
                purchaseInvoiceId = i.PurchaseInvoiceID,
                invoiceNumber = i.InvoiceNumber,
                supplierId = i.SupplierId,
                supplierName = i.Supplier != null ? i.Supplier.Name : string.Empty,
                warehouseId = i.WarehouseId,
                warehouseName = i.Warehouse != null ? i.Warehouse.Name : string.Empty,
                invoiceDate = i.InvoiceDate,
                status = (int)i.Status,
                currencyId = i.CurrencyId,
                currencyName = i.Currency != null ? i.Currency.Name : string.Empty,
                currencySymbol = i.Currency != null ? i.Currency.Symbol : string.Empty,
                baseCurrencySymbol = i.BaseCurrency != null ? i.BaseCurrency.Symbol : string.Empty,
                totalAmount = i.TotalAmount,
                totalAmountInBaseCurrency = i.TotalAmountInBaseCurrency,
                documentType = (int)i.DocumentType,
                entrySource = i.EntrySource == 0 ? (int)PurchaseEntrySource.Market : (int)i.EntrySource,
                productionBatchId = i.ProductionBatchId,
                productionBatchNumber = i.ProductionBatch != null ? i.ProductionBatch.BatchNumber : null,
                fixedCost = i.FixedCost,
                variableCost = i.VariableCost,
                referencePurchaseInvoiceId = i.ReferencePurchaseInvoiceId,
                referenceInvoiceNumber = i.ReferencePurchaseInvoice != null ? i.ReferencePurchaseInvoice.InvoiceNumber : null,
                isPosted = i.IsPosted,
                itemsCount = i.Items.Count(x => x.IsDeleted != true),
                description = i.Description,
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select((r, i) => new
            {
                rowNumber = start + i + 1,
                r.purchaseInvoiceId,
                invoiceNumber = r.invoiceNumber,
                supplierId = r.supplierId,
                supplierName = r.supplierName,
                warehouseId = r.warehouseId,
                warehouseName = r.warehouseName,
                invoiceDate = r.invoiceDate,
                status = r.status,
                currencyId = r.currencyId,
                currencyName = r.currencyName,
                currencySymbol = r.currencySymbol,
                baseCurrencySymbol = r.baseCurrencySymbol,
                totalAmount = r.totalAmount,
                totalAmountInBaseCurrency = r.totalAmountInBaseCurrency,
                documentType = r.documentType,
                entrySource = r.entrySource,
                productionBatchId = r.productionBatchId,
                productionBatchNumber = r.productionBatchNumber,
                fixedCost = r.fixedCost,
                variableCost = r.variableCost,
                referencePurchaseInvoiceId = r.referencePurchaseInvoiceId,
                referenceInvoiceNumber = r.referenceInvoiceNumber,
                isPosted = r.isPosted,
                itemsCount = r.itemsCount,
                description = r.description,
            }),
        });
    }

    [HttpGet("next-code-preview")]
    [HasPermission("transactions.purchase.view")]
    public async Task<IActionResult> NextCodePreview(CancellationToken cancellationToken)
    {
        var nextId = (await Db.PurchaseInvoices.MaxAsync(i => (int?)i.PurchaseInvoiceID, cancellationToken) ?? 0) + 1;
        return Ok(new { code = InvoiceCodeHelper.ForPurchase(nextId) });
    }

    [HttpGet("{id:int}")]
    [HasPermission("transactions.purchase.view")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var invoice = await Db.PurchaseInvoices
            .AsNoTracking()
            .Where(i => i.PurchaseInvoiceID == id && i.IsDeleted != true)
            .Select(i => new
            {
                purchaseInvoiceId = i.PurchaseInvoiceID,
                invoiceNumber = i.InvoiceNumber,
                supplierId = i.SupplierId,
                warehouseId = i.WarehouseId,
                invoiceDate = i.InvoiceDate,
                status = (int)i.Status,
                currencyId = i.CurrencyId,
                baseCurrencyId = i.BaseCurrencyId,
                exchangeHistoryId = i.ExchangeHistoryId,
                baseUnitsPerUnitAtTransaction = i.BaseUnitsPerUnitAtTransaction,
                totalAmount = i.TotalAmount,
                totalAmountInBaseCurrency = i.TotalAmountInBaseCurrency,
                paidAmount = i.PaidAmount,
                isCash = i.IsCash,
                documentType = (int)i.DocumentType,
                entrySource = i.EntrySource == 0 ? (int)PurchaseEntrySource.Market : (int)i.EntrySource,
                productionBatchId = i.ProductionBatchId,
                productionBatchNumber = i.ProductionBatch != null
                    ? i.ProductionBatch.BatchNumber
                    : null,
                fixedCost = i.FixedCost,
                variableCost = i.VariableCost,
                referencePurchaseInvoiceId = i.ReferencePurchaseInvoiceId,
                referenceInvoiceNumber = i.ReferencePurchaseInvoice != null
                    ? i.ReferencePurchaseInvoice.InvoiceNumber
                    : null,
                isPosted = i.IsPosted,
                postedAt = i.PostedAt,
                description = i.Description,
                items = i.Items
                    .Where(x => x.IsDeleted != true)
                    .OrderBy(x => x.PurchaseItemID)
                    .Select(x => new
                    {
                        purchaseItemId = x.PurchaseItemID,
                        productId = x.ProductId,
                        productName = x.Product != null ? x.Product.Name : string.Empty,
                        productCode = x.Product != null ? x.Product.Code : string.Empty,
                        meaurmentId = x.MeaurmentId,
                        meaurmentName = x.Meaurment != null ? x.Meaurment.Name : string.Empty,
                        quantity = x.Quantity,
                        quantityInBase = x.QuantityInBase,
                        unitPrice = x.UnitPrice,
                        lineTotal = x.LineTotal,
                        lineTotalInBaseCurrency = x.LineTotalInBaseCurrency,
                        returnedQuantity = x.QuantityInBase > 0
                            ? x.Quantity * x.ReturnedQuantityInBase / x.QuantityInBase
                            : 0,
                        inventoryLotId = x.InventoryLotId,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null)
        {
            return NotFound(new { message = "فاکتور خرید یافت نشد." });
        }

        return Ok(invoice);
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
            FixedCost = request.FixedCost,
            VariableCost = request.VariableCost,
            Description = request.Description?.Trim(),
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
            await _posting.PostPurchaseAsync(invoice.PurchaseInvoiceID, userId, cancellationToken);
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
        invoice.FixedCost = request.FixedCost;
        invoice.VariableCost = request.VariableCost;
        invoice.Description = request.Description?.Trim();
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
        var paidError = TrySetPaidAmount(invoice, request.PaidAmount);
        if (paidError is not null)
        {
            return BadRequest(new { message = paidError });
        }

        await Db.SaveChangesAsync(cancellationToken);

        if (request.Status == Data.InvoiceStatus.Invoice)
        {
            await _posting.PostPurchaseAsync(invoice.PurchaseInvoiceID, userId, cancellationToken);
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

    private async Task<string?> ValidateEntrySourceAsync(
        SavePurchaseInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EntrySource == PurchaseEntrySource.Production)
        {
            if (request.ProductionBatchId is null or <= 0)
            {
                return "برای ورود از تولید، انتخاب سند تولید الزامی است.";
            }

            var batch = await Db.ProductionBatches
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    b => b.ProductionBatchID == request.ProductionBatchId &&
                         b.IsDeleted != true,
                    cancellationToken);

            if (batch is null)
            {
                return "سند تولید انتخاب‌شده یافت نشد.";
            }

            if (!batch.IsPosted)
            {
                return "سند تولید باید ثبت نهایی شده باشد.";
            }

            if (batch.IsTransferredToSales)
            {
                return "خروجی این سند تولید قبلاً به چرخه فروش منتقل شده است.";
            }
        }
        else if (request.ProductionBatchId is > 0)
        {
            return "سند تولید فقط برای ورود از بخش تولید قابل انتخاب است.";
        }

        return null;
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

        [Range(0, double.MaxValue)]
        public decimal FixedCost { get; set; }

        [Range(0, double.MaxValue)]
        public decimal VariableCost { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PaidAmount { get; set; }

        public decimal? BaseUnitsPerUnit { get; set; }

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
