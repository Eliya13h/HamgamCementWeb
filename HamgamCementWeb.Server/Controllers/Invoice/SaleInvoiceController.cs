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
[Route("api/transactions/sale-invoices")]
[Authorize]
public class SaleInvoiceController : InvoiceControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(SaleInvoice.InvoiceNumber),
        [2] = nameof(SaleInvoice.CustomerId),
        [3] = nameof(SaleInvoice.WarehouseId),
        [4] = nameof(SaleInvoice.InvoiceDate),
        [5] = nameof(SaleInvoice.TotalAmount),
        [6] = nameof(SaleInvoice.TotalProfitInBaseCurrency),
        [7] = nameof(SaleInvoice.IsPosted),
    };

    private readonly IInvoicePostingService _posting;
    private readonly IInvoiceReturnService _returns;
    private readonly IFreightTripService _freight;

    public SaleInvoiceController(
        AppDbContext db,
        IInvoicePostingService posting,
        IInvoiceReturnService returns,
        IFreightTripService freight) : base(db)
    {
        _posting = posting;
        _returns = returns;
        _freight = freight;
    }

    [HttpPost("datatable")]
    [HasPermission("transactions.sale.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.SaleInvoices
            .AsNoTracking()
            .Where(i => i.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(i =>
                i.InvoiceNumber.Contains(searchValue) ||
                (i.Description != null && i.Description.Contains(searchValue)) ||
                (i.Customer != null && i.Customer.Name.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(SaleInvoice.CreatedAt))
            .Skip(start)
            .Take(length)
            .Select(i => new
            {
                saleInvoiceId = i.SaleInvoiceID,
                invoiceNumber = i.InvoiceNumber,
                customerId = i.CustomerId,
                customerName = i.Customer != null ? i.Customer.Name : string.Empty,
                warehouseId = i.WarehouseId,
                warehouseName = i.Warehouse != null ? i.Warehouse.Name : string.Empty,
                invoiceDate = i.InvoiceDate,
                status = (int)i.Status,
                currencyId = i.CurrencyId,
                currencyName = i.Currency != null ? i.Currency.Name : string.Empty,
                totalAmount = i.TotalAmount,
                totalAmountInBaseCurrency = i.TotalAmountInBaseCurrency,
                subTotalAmount = i.SubTotalAmount,
                taxPercent = i.TaxPercent,
                taxAmount = i.TaxAmount,
                paymentTermDays = i.PaymentTermDays,
                dueDate = i.DueDate,
                paidAmount = i.PaidAmount,
                totalCostInBaseCurrency = i.TotalCostInBaseCurrency,
                totalProfitInBaseCurrency = i.TotalProfitInBaseCurrency,
                documentType = (int)i.DocumentType,
                referenceSaleInvoiceId = i.ReferenceSaleInvoiceId,
                referenceInvoiceNumber = i.ReferenceSaleInvoice != null ? i.ReferenceSaleInvoice.InvoiceNumber : null,
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
                r.saleInvoiceId,
                invoiceNumber = r.invoiceNumber,
                customerId = r.customerId,
                customerName = r.customerName,
                warehouseId = r.warehouseId,
                warehouseName = r.warehouseName,
                invoiceDate = r.invoiceDate,
                status = r.status,
                currencyId = r.currencyId,
                currencyName = r.currencyName,
                totalAmount = r.totalAmount,
                totalAmountInBaseCurrency = r.totalAmountInBaseCurrency,
                totalCostInBaseCurrency = r.totalCostInBaseCurrency,
                totalProfitInBaseCurrency = r.totalProfitInBaseCurrency,
                documentType = r.documentType,
                referenceSaleInvoiceId = r.referenceSaleInvoiceId,
                referenceInvoiceNumber = r.referenceInvoiceNumber,
                isPosted = r.isPosted,
                itemsCount = r.itemsCount,
                description = r.description,
            }),
        });
    }

    [HttpGet("{id:int}")]
    [HasPermission("transactions.sale.view")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var invoice = await Db.SaleInvoices
            .AsNoTracking()
            .Where(i => i.SaleInvoiceID == id && i.IsDeleted != true)
            .Select(i => new
            {
                saleInvoiceId = i.SaleInvoiceID,
                invoiceNumber = i.InvoiceNumber,
                customerId = i.CustomerId,
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
                totalCostInBaseCurrency = i.TotalCostInBaseCurrency,
                totalProfitInBaseCurrency = i.TotalProfitInBaseCurrency,
                documentType = (int)i.DocumentType,
                referenceSaleInvoiceId = i.ReferenceSaleInvoiceId,
                referenceInvoiceNumber = i.ReferenceSaleInvoice != null
                    ? i.ReferenceSaleInvoice.InvoiceNumber
                    : null,
                isPosted = i.IsPosted,
                postedAt = i.PostedAt,
                description = i.Description,
                freightMode = (int)i.FreightMode,
                freightRatePerTon = i.FreightRatePerTon,
                freightWeightTon = i.FreightWeightTon,
                freightAmount = i.FreightAmount,
                freightAmountInBaseCurrency = i.FreightAmountInBaseCurrency,
                freightVehicleId = i.FreightVehicleId,
                freightCarrierName = i.FreightCarrierName,
                transportTripId = i.TransportTripId,
                items = i.Items
                    .Where(x => x.IsDeleted != true)
                    .OrderBy(x => x.SalesItemID)
                    .Select(x => new
                    {
                        salesItemId = x.SalesItemID,
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
                        lineCostInBaseCurrency = x.LineCostInBaseCurrency,
                        lineProfitInBaseCurrency = x.LineProfitInBaseCurrency,
                        lotAllocations = x.LotAllocations
                            .Where(a => a.IsDeleted != true)
                            .Select(a => new
                            {
                                inventoryLotId = a.InventoryLotId,
                                lotCode = a.InventoryLot != null ? a.InventoryLot.LotCode : string.Empty,
                                purchaseInvoiceId = a.PurchaseInvoiceId,
                                quantityInBase = a.QuantityInBase,
                                unitCostInBase = a.UnitCostInBase,
                                lineCostInBase = a.LineCostInBase,
                            })
                            .ToList(),
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null)
        {
            return NotFound(new { message = "فاکتور فروش یافت نشد." });
        }

        return Ok(invoice);
    }

    [HttpGet("{id:int}/returnable-lines")]
    [HasPermission("transactions.sale.view")]
    public async Task<IActionResult> GetReturnableLines(int id, CancellationToken cancellationToken)
    {
        try
        {
            var lines = await _returns.GetSaleReturnableLinesAsync(id, cancellationToken);
            return Ok(lines);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/returns")]
    [HasPermission("transactions.sale.view")]
    public async Task<IActionResult> GetReturns(int id, CancellationToken cancellationToken)
    {
        var returns = await _returns.GetSaleReturnsAsync(id, cancellationToken);
        return Ok(returns);
    }

    // چرا edit: برگشت از فروش، تغییر در سند فروش موجود است و به .edit نگاشت می‌شود.
    [HttpPost("{id:int}/returns")]
    [HasPermission("transactions.sale.edit")]
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
            var returnId = await _returns.CreateSaleReturnAsync(id, request, userId, cancellationToken);
            await _posting.PostSaleAsync(returnId, userId, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return Ok(new
            {
                message = "برگشت از فروش ثبت شد.",
                saleInvoiceId = returnId,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // چرا view: پیش‌نمایش سود فقط محاسبه‌ی خواندنی است و در فرم فاکتور استفاده می‌شود.
    [HttpPost("preview-profit")]
    [HasPermission("transactions.sale.view")]
    public async Task<IActionResult> PreviewProfit(
        [FromBody] SaveSaleInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            return BadRequest(new { message = "حداقل یک ردیف لازم است." });
        }

        var invoice = new SaleInvoice
        {
            WarehouseId = request.WarehouseId,
            InvoiceDate = request.InvoiceDate,
            Status = request.Status,
            CurrencyId = request.CurrencyId,
            Items = request.Items.Select(line => new SalesItem
            {
                ProductId = line.ProductId,
                MeaurmentId = line.MeaurmentId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                IsDeleted = false,
            }).ToList(),
        };

        try
        {
            var preview = await _posting.PreviewSaleProfitAsync(invoice, cancellationToken);
            return Ok(new
            {
                totalAmountInBaseCurrency = preview.TotalAmountInBaseCurrency,
                totalCostInBaseCurrency = preview.TotalCostInBaseCurrency,
                totalProfitInBaseCurrency = preview.TotalProfitInBaseCurrency,
                lines = preview.Lines.Select(l => new
                {
                    productId = l.ProductId,
                    quantityInBase = l.QuantityInBase,
                    lineTotalInBaseCurrency = l.LineTotalInBaseCurrency,
                    lineCostInBaseCurrency = l.LineCostInBaseCurrency,
                    lineProfitInBaseCurrency = l.LineProfitInBaseCurrency,
                    allocations = l.Allocations.Select(a => new
                    {
                        inventoryLotId = a.InventoryLotId,
                        lotCode = a.LotCode,
                        purchaseInvoiceId = a.PurchaseInvoiceId,
                        quantityInBase = a.QuantityInBase,
                        unitCost = a.UnitCost,
                        lineCost = a.LineCost,
                    }),
                }),
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [HasPermission("transactions.sale.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveSaleInvoiceRequest request,
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

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;

        var invoice = new SaleInvoice
        {
            InvoiceNumber = $"TMP{DateTime.UtcNow.Ticks}",
            CustomerId = request.CustomerId,
            WarehouseId = request.WarehouseId,
            InvoiceDate = request.InvoiceDate,
            Status = request.Status,
            CurrencyId = request.CurrencyId,
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
            invoice.Items.Add(new SalesItem
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

        await _posting.ApplySaleCurrencyAsync(invoice, cancellationToken, request.BaseUnitsPerUnit);
        try
        {
            _freight.NormalizeAndValidateSaleFreight(invoice);
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

        try
        {
            await _posting.ValidateSaleStockAsync(invoice, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        Db.SaleInvoices.Add(invoice);
        await Db.SaveChangesAsync(cancellationToken);

        invoice.InvoiceNumber = InvoiceCodeHelper.ForSale(invoice.SaleInvoiceID);
        await Db.SaveChangesAsync(cancellationToken);

        if (request.Status == Data.InvoiceStatus.Invoice)
        {
            try
            {
                await _posting.PostSaleAsync(invoice.SaleInvoiceID, userId, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message, saleInvoiceId = invoice.SaleInvoiceID });
            }

            return Ok(new { message = "فاکتور فروش ثبت شد. موجودی و درآمد به‌روز شد.", saleInvoiceId = invoice.SaleInvoiceID });
        }

        return Ok(new { message = "فاکتور فروش با موفقیت ایجاد شد.", saleInvoiceId = invoice.SaleInvoiceID });
    }

    [HttpPut("{id:int}")]
    [HasPermission("transactions.sale.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveSaleInvoiceRequest request,
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

        var invoice = await Db.SaleInvoices
            .Include(i => i.Items.Where(x => x.IsDeleted != true))
            .FirstOrDefaultAsync(i => i.SaleInvoiceID == id && i.IsDeleted != true, cancellationToken);

        if (invoice is null)
        {
            return NotFound(new { message = "فاکتور فروش یافت نشد." });
        }

        if (invoice.IsPosted)
        {
            return BadRequest(new { message = "فاکتور ثبت‌شده قابل ویرایش نیست." });
        }

        if (invoice.DocumentType != InvoiceDocumentType.Invoice)
        {
            return BadRequest(new { message = "سند برگشت قابل ویرایش نیست." });
        }

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;

        invoice.CustomerId = request.CustomerId;
        invoice.WarehouseId = request.WarehouseId;
        invoice.InvoiceDate = request.InvoiceDate;
        invoice.Status = request.Status;
        invoice.CurrencyId = request.CurrencyId;
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
            .Where(x => x.SalesItemId is > 0)
            .Select(x => x.SalesItemId!.Value)
            .ToHashSet();

        foreach (var existing in invoice.Items.Where(x => !incomingIds.Contains(x.SalesItemID)))
        {
            existing.IsDeleted = true;
            existing.DeletedAt = now;
            existing.DeletedBy = userId;
        }

        foreach (var line in request.Items)
        {
            var existing = line.SalesItemId is > 0
                ? invoice.Items.FirstOrDefault(x => x.SalesItemID == line.SalesItemId)
                : null;

            if (existing is null)
            {
                invoice.Items.Add(new SalesItem
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

        await _posting.ApplySaleCurrencyAsync(invoice, cancellationToken, request.BaseUnitsPerUnit);
        try
        {
            _freight.NormalizeAndValidateSaleFreight(invoice);
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

        try
        {
            await _posting.ValidateSaleStockAsync(invoice, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        await Db.SaveChangesAsync(cancellationToken);

        if (request.Status == Data.InvoiceStatus.Invoice)
        {
            try
            {
                await _posting.PostSaleAsync(invoice.SaleInvoiceID, userId, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            return Ok(new { message = "فاکتور فروش ثبت شد. موجودی و درآمد به‌روز شد." });
        }

        return Ok(new { message = "فاکتور فروش با موفقیت ویرایش شد." });
    }

    // چرا edit: ثبت نهایی (Post) تغییر وضعیت سند است و به .edit نگاشت می‌شود.
    [HttpPost("{id:int}/post")]
    [HasPermission("transactions.sale.edit")]
    public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
    {
        var invoice = await Db.SaleInvoices
            .AsNoTracking()
            .Where(i => i.SaleInvoiceID == id && i.IsDeleted != true)
            .Select(i => new { i.Status, i.IsPosted, i.DocumentType })
            .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null)
        {
            return NotFound(new { message = "فاکتور فروش یافت نشد." });
        }

        if (invoice.IsPosted)
        {
            return BadRequest(new { message = "این فاکتور قبلاً ثبت نهایی شده است." });
        }

        // چرا این محدودیت‌ها: اسناد برگشت مسیر ثبت جداگانه دارند و ثبت «استعلام قیمت» هیچ اثر مالی/انباری ندارد
        // و فقط سند را بی‌دلیل ثبت‌شده علامت می‌زند؛ سایر وضعیت‌ها (پیش‌فاکتور/آردر/فاکتور) رفتار معنادار دارند.
        if (invoice.DocumentType != InvoiceDocumentType.Invoice)
        {
            return BadRequest(new { message = "اسناد برگشت از این مسیر ثبت نمی‌شوند." });
        }

        if (invoice.Status == InvoiceStatus.Quotation)
        {
            return BadRequest(new { message = "فاکتور استعلام قیمت قابل ثبت نهایی نیست." });
        }

        try
        {
            await _posting.PostSaleAsync(id, ResolveCurrentUserId(), cancellationToken);
            return Ok(new { message = "فاکتور فروش ثبت نهایی شد. موجودی، درآمد و سود FIFO محاسبه شد." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [HasPermission("transactions.sale.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var invoice = await Db.SaleInvoices
            .Include(i => i.Items.Where(x => x.IsDeleted != true))
            .FirstOrDefaultAsync(i => i.SaleInvoiceID == id && i.IsDeleted != true, cancellationToken);

        if (invoice is null)
        {
            return NotFound(new { message = "فاکتور فروش یافت نشد." });
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

        return Ok(new { message = "فاکتور فروش با موفقیت حذف شد." });
    }

    private static string? TrySetPaidAmount(SaleInvoice invoice, decimal paidAmount)
    {
        if (invoice.Status == InvoiceStatus.Quotation)
        {
            invoice.PaidAmount = 0;
            invoice.IsCash = true;
            return null;
        }

        if (paidAmount < 0)
        {
            return "مبلغ دریافت‌شده نمی‌تواند منفی باشد.";
        }

        if (paidAmount > invoice.TotalAmount)
        {
            return "مبلغ دریافت‌شده نمی‌تواند بیشتر از جمع فاکتور باشد.";
        }

        invoice.PaidAmount = paidAmount;
        invoice.IsCash = invoice.TotalAmount > 0 && paidAmount >= invoice.TotalAmount;
        return null;
    }

    public class SaveSaleInvoiceRequest
    {
        [Range(1, int.MaxValue)]
        public int CustomerId { get; set; }

        [Range(1, int.MaxValue)]
        public int WarehouseId { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; }

        public Data.InvoiceStatus Status { get; set; } = Data.InvoiceStatus.Quotation;

        [Range(1, int.MaxValue)]
        public int CurrencyId { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

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

        public List<SaveSaleItemRequest> Items { get; set; } = [];
    }

    public class SaveSaleItemRequest
    {
        public int? SalesItemId { get; set; }

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
