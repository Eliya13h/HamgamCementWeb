using HamgamTransport.Server.Data;

using HamgamTransport.Server.Data.Models.Invoice;

using Microsoft.EntityFrameworkCore;



namespace HamgamTransport.Server.Services;



public class CreateInvoiceReturnLineRequest

{

    public int ReferenceItemId { get; set; }

    public decimal Quantity { get; set; }

    public int MeaurmentId { get; set; }

    public decimal UnitPrice { get; set; }

}



public class CreateInvoiceReturnRequest

{

    public DateTime InvoiceDate { get; set; }

    public string? Description { get; set; }

    public decimal PaidAmount { get; set; }

    public int? CurrencyId { get; set; }

    public decimal? BaseUnitsPerUnit { get; set; }

    public List<CreateInvoiceReturnLineRequest> Items { get; set; } = [];

}



public record ReturnableInvoiceLine(

    int ReferenceItemId,

    int ProductId,

    string ProductName,

    string ProductCode,

    int MeaurmentId,

    string MeaurmentName,

    string? MeaurmentSymbol,

    decimal OriginalQuantity,

    decimal ReturnedQuantity,

    decimal ReturnableQuantity,

    decimal UnitPrice);



public record InvoiceReturnSummary(

    int InvoiceId,

    string InvoiceNumber,

    DateTime InvoiceDate,

    DateTime? PostedAt,

    decimal TotalAmount,

    bool IsPosted);



public interface IInvoiceReturnService

{

    Task<IReadOnlyList<ReturnableInvoiceLine>> GetPurchaseReturnableLinesAsync(

        int purchaseInvoiceId,

        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReturnableInvoiceLine>> GetSaleReturnableLinesAsync(

        int saleInvoiceId,

        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvoiceReturnSummary>> GetPurchaseReturnsAsync(

        int purchaseInvoiceId,

        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvoiceReturnSummary>> GetSaleReturnsAsync(

        int saleInvoiceId,

        CancellationToken cancellationToken = default);

    Task<int> CreatePurchaseReturnAsync(

        int purchaseInvoiceId,

        CreateInvoiceReturnRequest request,

        int? userId,

        CancellationToken cancellationToken = default);

    Task<int> CreateSaleReturnAsync(

        int saleInvoiceId,

        CreateInvoiceReturnRequest request,

        int? userId,

        CancellationToken cancellationToken = default);

}



public class InvoiceReturnService : IInvoiceReturnService

{

    private readonly AppDbContext _db;

    private readonly IMeaurmentConversionService _conversion;

    private readonly IInvoicePostingService _posting;



    public InvoiceReturnService(

        AppDbContext db,

        IMeaurmentConversionService conversion,

        IInvoicePostingService posting)

    {

        _db = db;

        _conversion = conversion;

        _posting = posting;

    }



    public async Task<IReadOnlyList<ReturnableInvoiceLine>> GetPurchaseReturnableLinesAsync(

        int purchaseInvoiceId,

        CancellationToken cancellationToken = default)

    {

        var invoice = await GetPostedPurchaseAsync(purchaseInvoiceId, cancellationToken);



        return invoice.Items

            .OrderBy(i => i.PurchaseItemID)

            .Select(MapPurchaseReturnableLine)

            .Where(line => line.ReturnableQuantity > 0)

            .ToList();

    }



    public async Task<IReadOnlyList<ReturnableInvoiceLine>> GetSaleReturnableLinesAsync(

        int saleInvoiceId,

        CancellationToken cancellationToken = default)

    {

        var invoice = await GetPostedSaleAsync(saleInvoiceId, cancellationToken);



        return invoice.Items

            .OrderBy(i => i.SalesItemID)

            .Select(MapSaleReturnableLine)

            .Where(line => line.ReturnableQuantity > 0)

            .ToList();

    }



    public async Task<IReadOnlyList<InvoiceReturnSummary>> GetPurchaseReturnsAsync(

        int purchaseInvoiceId,

        CancellationToken cancellationToken = default)

    {

        return await _db.PurchaseInvoices

            .AsNoTracking()

            .Where(i =>

                i.IsDeleted != true &&

                i.ReferencePurchaseInvoiceId == purchaseInvoiceId &&

                i.DocumentType == InvoiceDocumentType.PurchaseReturn)

            .OrderByDescending(i => i.InvoiceDate)

            .ThenByDescending(i => i.PurchaseInvoiceID)

            .Select(i => new InvoiceReturnSummary(

                i.PurchaseInvoiceID,

                i.InvoiceNumber,

                i.InvoiceDate,

                i.PostedAt,

                i.TotalAmount,

                i.IsPosted))

            .ToListAsync(cancellationToken);

    }



    public async Task<IReadOnlyList<InvoiceReturnSummary>> GetSaleReturnsAsync(

        int saleInvoiceId,

        CancellationToken cancellationToken = default)

    {

        return await _db.SaleInvoices

            .AsNoTracking()

            .Where(i =>

                i.IsDeleted != true &&

                i.ReferenceSaleInvoiceId == saleInvoiceId &&

                i.DocumentType == InvoiceDocumentType.SaleReturn)

            .OrderByDescending(i => i.InvoiceDate)

            .ThenByDescending(i => i.SaleInvoiceID)

            .Select(i => new InvoiceReturnSummary(

                i.SaleInvoiceID,

                i.InvoiceNumber,

                i.InvoiceDate,

                i.PostedAt,

                i.TotalAmount,

                i.IsPosted))

            .ToListAsync(cancellationToken);

    }



    public async Task<int> CreatePurchaseReturnAsync(

        int purchaseInvoiceId,

        CreateInvoiceReturnRequest request,

        int? userId,

        CancellationToken cancellationToken = default)

    {

        var original = await GetPostedPurchaseAsync(purchaseInvoiceId, cancellationToken);

        var lines = await ResolvePurchaseReturnLinesAsync(original, request, cancellationToken);

        if (lines.Count == 0)

        {

            throw new InvalidOperationException("حداقل یک ردیف برای برگشت انتخاب کنید.");

        }



        var now = DateTime.Now;

        var currencyId = request.CurrencyId is > 0 ? request.CurrencyId.Value : original.CurrencyId;

        var returnInvoice = new PurchaseInvoice

        {

            InvoiceNumber = $"TMP{DateTime.UtcNow.Ticks}",

            SupplierId = original.SupplierId,

            WarehouseId = original.WarehouseId,

            InvoiceDate = request.InvoiceDate,

            Status = InvoiceStatus.Invoice,

            DocumentType = InvoiceDocumentType.PurchaseReturn,

            ReferencePurchaseInvoiceId = original.PurchaseInvoiceID,

            CurrencyId = currencyId,

            Description = request.Description?.Trim(),

            IsDeleted = false,

            IsActive = true,

            CreatedAt = now,

            CreatedBy = userId,

        };



        foreach (var line in lines)

        {

            returnInvoice.Items.Add(new PurchaseItem

            {

                ProductId = line.ProductId,

                MeaurmentId = line.MeaurmentId,

                Quantity = line.Quantity,

                UnitPrice = line.UnitPrice,

                ReferencePurchaseItemId = line.ReferenceItemId,

                IsDeleted = false,

                CreatedAt = now,

                CreatedBy = userId,

            });

        }



        await _posting.ApplyPurchaseCurrencyAsync(returnInvoice, cancellationToken, request.BaseUnitsPerUnit);

        ApplyPaidAmount(returnInvoice, request.PaidAmount);



        _db.PurchaseInvoices.Add(returnInvoice);

        await _db.SaveChangesAsync(cancellationToken);



        returnInvoice.InvoiceNumber = InvoiceCodeHelper.ForPurchaseReturn(returnInvoice.PurchaseInvoiceID);

        await _db.SaveChangesAsync(cancellationToken);



        return returnInvoice.PurchaseInvoiceID;

    }



    public async Task<int> CreateSaleReturnAsync(

        int saleInvoiceId,

        CreateInvoiceReturnRequest request,

        int? userId,

        CancellationToken cancellationToken = default)

    {

        var original = await GetPostedSaleAsync(saleInvoiceId, cancellationToken);

        var lines = await ResolveSaleReturnLinesAsync(original, request, cancellationToken);

        if (lines.Count == 0)

        {

            throw new InvalidOperationException("حداقل یک ردیف برای برگشت انتخاب کنید.");

        }



        var now = DateTime.Now;

        var currencyId = request.CurrencyId is > 0 ? request.CurrencyId.Value : original.CurrencyId;

        var returnInvoice = new SaleInvoice

        {

            InvoiceNumber = $"TMP{DateTime.UtcNow.Ticks}",

            CustomerId = original.CustomerId,

            WarehouseId = original.WarehouseId,

            InvoiceDate = request.InvoiceDate,

            Status = InvoiceStatus.Invoice,

            DocumentType = InvoiceDocumentType.SaleReturn,

            ReferenceSaleInvoiceId = original.SaleInvoiceID,

            CurrencyId = currencyId,

            Description = request.Description?.Trim(),

            IsDeleted = false,

            IsActive = true,

            CreatedAt = now,

            CreatedBy = userId,

        };



        foreach (var line in lines)

        {

            returnInvoice.Items.Add(new SalesItem

            {

                ProductId = line.ProductId,

                MeaurmentId = line.MeaurmentId,

                Quantity = line.Quantity,

                UnitPrice = line.UnitPrice,

                ReferenceSalesItemId = line.ReferenceItemId,

                IsDeleted = false,

                CreatedAt = now,

                CreatedBy = userId,

            });

        }



        await _posting.ApplySaleCurrencyAsync(returnInvoice, cancellationToken, request.BaseUnitsPerUnit);

        ApplyPaidAmount(returnInvoice, request.PaidAmount);



        _db.SaleInvoices.Add(returnInvoice);

        await _db.SaveChangesAsync(cancellationToken);



        returnInvoice.InvoiceNumber = InvoiceCodeHelper.ForSaleReturn(returnInvoice.SaleInvoiceID);

        await _db.SaveChangesAsync(cancellationToken);



        return returnInvoice.SaleInvoiceID;

    }



    private static void ApplyPaidAmount(PurchaseInvoice invoice, decimal paidAmount)

    {

        if (paidAmount < 0)

        {

            throw new InvalidOperationException("مبلغ پرداخت‌شده نمی‌تواند منفی باشد.");

        }



        if (paidAmount > invoice.TotalAmount)

        {

            throw new InvalidOperationException("مبلغ پرداخت‌شده نمی‌تواند بیشتر از جمع برگشت باشد.");

        }



        invoice.PaidAmount = paidAmount;

        invoice.IsCash = invoice.TotalAmount > 0 && paidAmount >= invoice.TotalAmount;

    }



    private static void ApplyPaidAmount(SaleInvoice invoice, decimal paidAmount)

    {

        if (paidAmount < 0)

        {

            throw new InvalidOperationException("مبلغ دریافت‌شده نمی‌تواند منفی باشد.");

        }



        if (paidAmount > invoice.TotalAmount)

        {

            throw new InvalidOperationException("مبلغ دریافت‌شده نمی‌تواند بیشتر از جمع برگشت باشد.");

        }



        invoice.PaidAmount = paidAmount;

        invoice.IsCash = invoice.TotalAmount > 0 && paidAmount >= invoice.TotalAmount;

    }



    private async Task<PurchaseInvoice> GetPostedPurchaseAsync(int purchaseInvoiceId, CancellationToken cancellationToken)

    {

        var invoice = await _db.PurchaseInvoices

            .Include(i => i.Items.Where(x => x.IsDeleted != true))

                .ThenInclude(x => x.Product)

            .Include(i => i.Items.Where(x => x.IsDeleted != true))

                .ThenInclude(x => x.Meaurment)

            .FirstOrDefaultAsync(

                i => i.PurchaseInvoiceID == purchaseInvoiceId &&

                     i.IsDeleted != true &&

                     i.DocumentType == InvoiceDocumentType.Invoice,

                cancellationToken)

            ?? throw new InvalidOperationException("فاکتور خرید یافت نشد.");



        if (!invoice.IsPosted)

        {

            throw new InvalidOperationException("فقط فاکتورهای ثبت‌شده قابل برگشت هستند.");

        }



        return invoice;

    }



    private async Task<SaleInvoice> GetPostedSaleAsync(int saleInvoiceId, CancellationToken cancellationToken)

    {

        var invoice = await _db.SaleInvoices

            .Include(i => i.Items.Where(x => x.IsDeleted != true))

                .ThenInclude(x => x.Product)

            .Include(i => i.Items.Where(x => x.IsDeleted != true))

                .ThenInclude(x => x.Meaurment)

            .FirstOrDefaultAsync(

                i => i.SaleInvoiceID == saleInvoiceId &&

                     i.IsDeleted != true &&

                     i.DocumentType == InvoiceDocumentType.Invoice,

                cancellationToken)

            ?? throw new InvalidOperationException("فاکتور فروش یافت نشد.");



        if (!invoice.IsPosted)

        {

            throw new InvalidOperationException("فقط فاکتورهای ثبت‌شده قابل برگشت هستند.");

        }



        return invoice;

    }



    private async Task<List<ResolvedReturnLine>> ResolvePurchaseReturnLinesAsync(

        PurchaseInvoice original,

        CreateInvoiceReturnRequest request,

        CancellationToken cancellationToken)

    {

        var sourceLines = new List<CreateInvoiceReturnLineRequest>();

        if (request.Items.Count > 0)

        {

            sourceLines.AddRange(request.Items);

        }

        else

        {

            foreach (var item in original.Items)

            {

                var returnedQty = await ConvertBaseToUnit(item.ReturnedQuantityInBase, item.MeaurmentId, cancellationToken);

                var qty = item.Quantity - returnedQty;

                if (qty > 0)

                {

                    sourceLines.Add(new CreateInvoiceReturnLineRequest

                    {

                        ReferenceItemId = item.PurchaseItemID,

                        MeaurmentId = item.MeaurmentId,

                        Quantity = qty,

                        UnitPrice = item.UnitPrice,

                    });

                }

            }

        }



        var resolved = new List<ResolvedReturnLine>();

        foreach (var line in sourceLines)

        {

            if (line.Quantity <= 0)

            {

                continue;

            }



            var originalItem = original.Items.FirstOrDefault(i => i.PurchaseItemID == line.ReferenceItemId)

                ?? throw new InvalidOperationException("ردیف مبدأ یافت نشد.");



            if (line.MeaurmentId != originalItem.MeaurmentId)

            {

                throw new InvalidOperationException($"واحد برگشت باید همان واحد فاکتور مبدأ باشد.");

            }



            var qtyInBase = await _conversion.ToBaseAsync(line.Quantity, line.MeaurmentId, cancellationToken);

            var returnableInBase = originalItem.QuantityInBase - originalItem.ReturnedQuantityInBase;

            if (qtyInBase > returnableInBase + 0.000001m)

            {

                throw new InvalidOperationException($"مقدار برگشت برای «{originalItem.Product?.Name}» بیش از حد مجاز است.");

            }



            var unitPrice = line.UnitPrice > 0 ? line.UnitPrice : originalItem.UnitPrice;

            resolved.Add(new ResolvedReturnLine(

                originalItem.PurchaseItemID,

                originalItem.ProductId,

                line.MeaurmentId,

                line.Quantity,

                unitPrice));

        }



        return resolved;

    }



    private async Task<List<ResolvedReturnLine>> ResolveSaleReturnLinesAsync(

        SaleInvoice original,

        CreateInvoiceReturnRequest request,

        CancellationToken cancellationToken)

    {

        var sourceLines = new List<CreateInvoiceReturnLineRequest>();

        if (request.Items.Count > 0)

        {

            sourceLines.AddRange(request.Items);

        }

        else

        {

            foreach (var item in original.Items)

            {

                var returnedQty = await ConvertBaseToUnit(item.ReturnedQuantityInBase, item.MeaurmentId, cancellationToken);

                var qty = item.Quantity - returnedQty;

                if (qty > 0)

                {

                    sourceLines.Add(new CreateInvoiceReturnLineRequest

                    {

                        ReferenceItemId = item.SalesItemID,

                        MeaurmentId = item.MeaurmentId,

                        Quantity = qty,

                        UnitPrice = item.UnitPrice,

                    });

                }

            }

        }



        var resolved = new List<ResolvedReturnLine>();

        foreach (var line in sourceLines)

        {

            if (line.Quantity <= 0)

            {

                continue;

            }



            var originalItem = original.Items.FirstOrDefault(i => i.SalesItemID == line.ReferenceItemId)

                ?? throw new InvalidOperationException("ردیف مبدأ یافت نشد.");



            if (line.MeaurmentId != originalItem.MeaurmentId)

            {

                throw new InvalidOperationException($"واحد برگشت باید همان واحد فاکتور مبدأ باشد.");

            }



            var qtyInBase = await _conversion.ToBaseAsync(line.Quantity, line.MeaurmentId, cancellationToken);

            var returnableInBase = originalItem.QuantityInBase - originalItem.ReturnedQuantityInBase;

            if (qtyInBase > returnableInBase + 0.000001m)

            {

                throw new InvalidOperationException($"مقدار برگشت برای «{originalItem.Product?.Name}» بیش از حد مجاز است.");

            }



            var unitPrice = line.UnitPrice > 0 ? line.UnitPrice : originalItem.UnitPrice;

            resolved.Add(new ResolvedReturnLine(

                originalItem.SalesItemID,

                originalItem.ProductId,

                line.MeaurmentId,

                line.Quantity,

                unitPrice));

        }



        return resolved;

    }



    private async Task<decimal> ConvertBaseToUnit(decimal quantityInBase, int meaurmentId, CancellationToken cancellationToken)

    {

        if (quantityInBase <= 0)

        {

            return 0;

        }



        var factor = await _db.Meaurments

            .AsNoTracking()

            .Where(m => m.MeaurmentID == meaurmentId)

            .Select(m => m.FactorToBase)

            .FirstOrDefaultAsync(cancellationToken);



        if (factor <= 0)

        {

            return quantityInBase;

        }



        return quantityInBase / factor;

    }



    private static ReturnableInvoiceLine MapPurchaseReturnableLine(PurchaseItem item)

    {

        var returnedInUnit = item.QuantityInBase > 0

            ? item.Quantity * item.ReturnedQuantityInBase / item.QuantityInBase

            : 0;



        return new ReturnableInvoiceLine(

            item.PurchaseItemID,

            item.ProductId,

            item.Product?.Name ?? string.Empty,

            item.Product?.Code ?? string.Empty,

            item.MeaurmentId,

            item.Meaurment?.Name ?? string.Empty,

            item.Meaurment?.Symbol,

            item.Quantity,

            returnedInUnit,

            Math.Max(0, item.Quantity - returnedInUnit),

            item.UnitPrice);

    }



    private static ReturnableInvoiceLine MapSaleReturnableLine(SalesItem item)

    {

        var returnedInUnit = item.QuantityInBase > 0

            ? item.Quantity * item.ReturnedQuantityInBase / item.QuantityInBase

            : 0;



        return new ReturnableInvoiceLine(

            item.SalesItemID,

            item.ProductId,

            item.Product?.Name ?? string.Empty,

            item.Product?.Code ?? string.Empty,

            item.MeaurmentId,

            item.Meaurment?.Name ?? string.Empty,

            item.Meaurment?.Symbol,

            item.Quantity,

            returnedInUnit,

            Math.Max(0, item.Quantity - returnedInUnit),

            item.UnitPrice);

    }



    private sealed record ResolvedReturnLine(

        int ReferenceItemId,

        int ProductId,

        int MeaurmentId,

        decimal Quantity,

        decimal UnitPrice);

}


