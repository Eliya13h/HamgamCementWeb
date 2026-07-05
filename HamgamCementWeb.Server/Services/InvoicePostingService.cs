using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Data.Models.Invoice;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Services;

public record SaleLineProfitPreview(
    int ProductId,
    decimal QuantityInBase,
    decimal LineTotalInBaseCurrency,
    decimal LineCostInBaseCurrency,
    decimal LineProfitInBaseCurrency,
    IReadOnlyList<FifoAllocation> Allocations);

public record SaleProfitPreview(
    decimal TotalAmountInBaseCurrency,
    decimal TotalCostInBaseCurrency,
    decimal TotalProfitInBaseCurrency,
    IReadOnlyList<SaleLineProfitPreview> Lines);

public interface IInvoicePostingService
{
    Task ApplyPurchaseCurrencyAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken = default,
        decimal? baseUnitsPerUnitOverride = null);
    Task ApplySaleCurrencyAsync(
        SaleInvoice invoice,
        CancellationToken cancellationToken = default,
        decimal? baseUnitsPerUnitOverride = null);
    Task PostPurchaseAsync(int purchaseInvoiceId, int? userId, CancellationToken cancellationToken = default);
    Task PostSaleAsync(int saleInvoiceId, int? userId, CancellationToken cancellationToken = default);
    Task ValidateSaleStockAsync(SaleInvoice invoice, CancellationToken cancellationToken = default);
    Task<SaleProfitPreview> PreviewSaleProfitAsync(SaleInvoice invoice, CancellationToken cancellationToken = default);
}

public class InvoicePostingService : IInvoicePostingService
{
    private readonly AppDbContext _db;
    private readonly ICurrencyConversionService _currency;
    private readonly IMeaurmentConversionService _conversion;
    private readonly IFifoInventoryService _fifo;
    private readonly IFinanceCategoryService _financeCategories;

    public InvoicePostingService(
        AppDbContext db,
        ICurrencyConversionService currency,
        IMeaurmentConversionService conversion,
        IFifoInventoryService fifo,
        IFinanceCategoryService financeCategories)
    {
        _db = db;
        _currency = currency;
        _conversion = conversion;
        _fifo = fifo;
        _financeCategories = financeCategories;
    }

    // چرا این helper: نرخ اسنپ‌شات ذخیره‌شده روی فاکتور را فقط در صورت ارز غیرپایه و معتبر بودن به‌عنوان override برمی‌گرداند
    // تا هنگام ثبت نهایی همان نرخ خودِ فاکتور استفاده شود و نرخ سراسری سیستم دست‌کاری نشود.
    private static decimal? ResolveInvoiceRateOverride(int currencyId, int baseCurrencyId, decimal storedRate) =>
        storedRate > 0 && currencyId != baseCurrencyId ? storedRate : null;

    public async Task ApplyPurchaseCurrencyAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken = default,
        decimal? baseUnitsPerUnitOverride = null)
    {
        var snapshot = await _currency.GetSnapshotAsync(invoice.CurrencyId, invoice.InvoiceDate, cancellationToken);
        if (baseUnitsPerUnitOverride is > 0 && !snapshot.IsBaseCurrency)
        {
            snapshot = snapshot with { BaseUnitsPerUnit = baseUnitsPerUnitOverride.Value };
        }

        invoice.BaseCurrencyId = snapshot.BaseCurrencyId;
        invoice.ExchangeHistoryId = snapshot.ExchangeHistoryId;
        invoice.BaseUnitsPerUnitAtTransaction = snapshot.BaseUnitsPerUnit;

        decimal total = 0;
        decimal totalBase = 0;

        foreach (var item in invoice.Items.Where(i => i.IsDeleted != true))
        {
            item.QuantityInBase = await _conversion.ToBaseAsync(item.Quantity, item.MeaurmentId, cancellationToken);
            item.LineTotal = item.QuantityInBase * item.UnitPrice;
            item.LineTotalInBaseCurrency = _currency.ConvertToBase(item.LineTotal, snapshot);
            total += item.LineTotal;
            totalBase += item.LineTotalInBaseCurrency;
        }

        invoice.FixedCostInBaseCurrency = _currency.ConvertToBase(invoice.FixedCost, snapshot);
        invoice.VariableCostInBaseCurrency = _currency.ConvertToBase(invoice.VariableCost, snapshot);
        total += invoice.FixedCost + invoice.VariableCost;
        totalBase += invoice.FixedCostInBaseCurrency + invoice.VariableCostInBaseCurrency;

        invoice.TotalAmount = total;
        invoice.TotalAmountInBaseCurrency = totalBase;
    }

    public async Task ApplySaleCurrencyAsync(
        SaleInvoice invoice,
        CancellationToken cancellationToken = default,
        decimal? baseUnitsPerUnitOverride = null)
    {
        var snapshot = await _currency.GetSnapshotAsync(invoice.CurrencyId, invoice.InvoiceDate, cancellationToken);
        if (baseUnitsPerUnitOverride is > 0 && !snapshot.IsBaseCurrency)
        {
            snapshot = snapshot with { BaseUnitsPerUnit = baseUnitsPerUnitOverride.Value };
        }

        invoice.BaseCurrencyId = snapshot.BaseCurrencyId;
        invoice.ExchangeHistoryId = snapshot.ExchangeHistoryId;
        invoice.BaseUnitsPerUnitAtTransaction = snapshot.BaseUnitsPerUnit;

        decimal total = 0;
        decimal totalBase = 0;

        foreach (var item in invoice.Items.Where(i => i.IsDeleted != true))
        {
            item.QuantityInBase = await _conversion.ToBaseAsync(item.Quantity, item.MeaurmentId, cancellationToken);
            item.LineTotal = item.QuantityInBase * item.UnitPrice;
            item.LineTotalInBaseCurrency = _currency.ConvertToBase(item.LineTotal, snapshot);
            total += item.LineTotal;
            totalBase += item.LineTotalInBaseCurrency;
        }

        invoice.TotalAmount = total;
        invoice.TotalAmountInBaseCurrency = totalBase;
    }

    // چرا تراکنش: عملیات ثبت شامل چند SaveChanges (ساخت Lot، مصرف FIFO، ثبت مالی) است؛ اگر یکی از مراحل
    // خطا دهد، همه‌ی تغییرات باید برگردند تا داده نیمه‌کاره باقی نماند. گارد ownsTransaction از تراکنش تودرتو
    // (وقتی فراخوان بیرونی خودش تراکنش باز کرده) جلوگیری می‌کند.
    private async Task RunInTransactionAsync(Func<Task> work, CancellationToken cancellationToken)
    {
        var ownsTransaction = _db.Database.CurrentTransaction is null;
        await using var tx = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await work();

        if (tx is not null)
        {
            await tx.CommitAsync(cancellationToken);
        }
    }

    public Task PostPurchaseAsync(int purchaseInvoiceId, int? userId, CancellationToken cancellationToken = default) =>
        RunInTransactionAsync(() => PostPurchaseCoreAsync(purchaseInvoiceId, userId, cancellationToken), cancellationToken);

    private async Task PostPurchaseCoreAsync(int purchaseInvoiceId, int? userId, CancellationToken cancellationToken = default)
    {
        var invoice = await _db.PurchaseInvoices
            .Include(i => i.Items.Where(x => x.IsDeleted != true))
            .Include(i => i.Supplier)
            .FirstOrDefaultAsync(i => i.PurchaseInvoiceID == purchaseInvoiceId && i.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("فاکتور خرید یافت نشد.");

        if (invoice.DocumentType == InvoiceDocumentType.PurchaseReturn)
        {
            await PostPurchaseReturnAsync(invoice, userId, cancellationToken);
            return;
        }

        if (invoice.IsPosted)
        {
            throw new InvalidOperationException("این فاکتور قبلاً ثبت نهایی شده است.");
        }

        if (invoice.Items.Count == 0)
        {
            throw new InvalidOperationException("فاکتور باید حداقل یک ردیف داشته باشد.");
        }

        // چرا override با نرخ خودِ فاکتور: نرخ ارز به‌صورت اسنپ‌شات روی همین فاکتور قفل می‌شود؛ اگر کاربر نرخ دستی
        // وارد کرده باشد در ثبت نهایی نیز حفظ می‌شود و نرخ سراسری سیستم دست‌کاری نمی‌گردد.
        await ApplyPurchaseCurrencyAsync(invoice, cancellationToken, ResolveInvoiceRateOverride(invoice.CurrencyId, invoice.BaseCurrencyId, invoice.BaseUnitsPerUnitAtTransaction));

        var now = DateTime.Now;

        if (invoice.TotalAmount > 0)
        {
            var purchaseCategoryId = await _financeCategories.GetExpenseCategoryIdAsync(
                FinanceCategoryCode.ProductPurchase,
                cancellationToken);

            // چرا کل مبلغ فاکتور: مصرف بر مبنای تعهدی (accrual) و بر اساس کل مبلغ فاکتور ثبت می‌شود، نه مبلغ پرداختی؛
            // مانده بدهی/طلب به‌صورت جدا در تراز طرف‌حساب (SupplierReadService) مدیریت می‌شود.
            var expense = new Expense
            {
                Title = $"خرید — {invoice.InvoiceNumber}",
                ExpenseDate = invoice.InvoiceDate,
                ExpenseCategoryId = purchaseCategoryId,
                Source = FinancialEntrySource.ProductPurchase,
                SupplierId = invoice.SupplierId,
                CurrencyId = invoice.CurrencyId,
                BaseCurrencyId = invoice.BaseCurrencyId,
                ExchangeHistoryId = invoice.ExchangeHistoryId,
                BaseUnitsPerUnitAtTransaction = invoice.BaseUnitsPerUnitAtTransaction,
                Amount = invoice.TotalAmount,
                AmountInBaseCurrency = invoice.TotalAmountInBaseCurrency,
                Description = invoice.Description,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = userId,
            };

            _db.Expenses.Add(expense);
            await _db.SaveChangesAsync(cancellationToken);

            invoice.ExpenseId = expense.ExpenseID;
        }

        if (invoice.Status == InvoiceStatus.Invoice)
        {
            var isFromProduction = invoice.EntrySource == PurchaseEntrySource.Production && invoice.ProductionBatchId is > 0;

            // برای ورود از تولید، سند تولید را قبل از حلقه بارگذاری و اعتبارسنجی می‌کنیم تا انبار خروجی آن در دسترس باشد.
            Data.Models.Production.ProductionBatch? batch = null;
            if (isFromProduction)
            {
                batch = await _db.ProductionBatches
                    .FirstOrDefaultAsync(b => b.ProductionBatchID == invoice.ProductionBatchId && b.IsDeleted != true, cancellationToken)
                    ?? throw new InvalidOperationException("سند تولید مرتبط یافت نشد.");

                if (!batch.IsPosted)
                {
                    throw new InvalidOperationException("سند تولید باید قبل از ورود به چرخه فروش ثبت نهایی شده باشد.");
                }

                if (batch.IsTransferredToSales)
                {
                    throw new InvalidOperationException("خروجی این سند تولید قبلاً به چرخه فروش منتقل شده است.");
                }
            }

            var itemsBaseTotal = invoice.Items.Sum(i => i.LineTotalInBaseCurrency);
            var extraBaseCost = invoice.FixedCostInBaseCurrency + invoice.VariableCostInBaseCurrency;

            foreach (var item in invoice.Items)
            {
                if (item.QuantityInBase <= 0)
                {
                    throw new InvalidOperationException("مقدار ردیف باید بزرگ‌تر از صفر باشد.");
                }

                // چرا مصرف Lot تولید: خروجی تولید هنگام ثبت سند تولید یک‌بار به‌صورت Lot وارد انبار خروجی شده است؛
                // برای جلوگیری از دوباره‌شماری، همان مقدار از Lotهای همین batch مصرف (خارج) می‌شود و سپس به‌عنوان
                // Lot خرید در انبار فاکتور ثبت می‌گردد (اگر انبار یکی باشد، خالص موجودی تغییری نمی‌کند).
                if (batch is not null)
                {
                    await _fifo.AllocateAndApplyAsync(new AllocateStockRequest
                    {
                        ProductId = item.ProductId,
                        WarehouseId = batch.OutputWarehouseId,
                        QuantityInBase = item.QuantityInBase,
                        ProductionBatchId = batch.ProductionBatchID,
                    }, allowInsufficientStock: false, cancellationToken);
                }

                var lineShare = itemsBaseTotal > 0
                    ? item.LineTotalInBaseCurrency / itemsBaseTotal
                    : 1m / invoice.Items.Count;
                var lineExtraCost = extraBaseCost * lineShare;
                var unitCostInBase = (item.LineTotalInBaseCurrency + lineExtraCost) / item.QuantityInBase;

                var lot = await _fifo.ReceiveAsync(new ReceiveStockRequest
                {
                    ProductId = item.ProductId,
                    WarehouseId = invoice.WarehouseId,
                    QuantityInBase = item.QuantityInBase,
                    UnitCost = unitCostInBase,
                    ReceivedAt = invoice.InvoiceDate,
                    CreatedBy = userId,
                    PurchaseInvoiceId = invoice.PurchaseInvoiceID,
                    PurchaseItemId = item.PurchaseItemID,
                    ProductionBatchId = isFromProduction ? invoice.ProductionBatchId : null,
                }, cancellationToken);

                item.InventoryLotId = lot.InventoryLotID;
            }

            if (batch is not null)
            {
                batch.IsTransferredToSales = true;
                batch.IsUpdated = true;
                batch.UpdatedAt = now;
                batch.UpdatedBy = userId;
            }
        }

        invoice.IsPosted = true;
        invoice.PostedAt = now;
        invoice.IsUpdated = true;
        invoice.UpdatedAt = now;
        invoice.UpdatedBy = userId;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task PostSaleAsync(int saleInvoiceId, int? userId, CancellationToken cancellationToken = default) =>
        RunInTransactionAsync(() => PostSaleCoreAsync(saleInvoiceId, userId, cancellationToken), cancellationToken);

    private async Task PostSaleCoreAsync(int saleInvoiceId, int? userId, CancellationToken cancellationToken = default)
    {
        var invoice = await _db.SaleInvoices
            .Include(i => i.Items.Where(x => x.IsDeleted != true))
                .ThenInclude(x => x.LotAllocations.Where(a => a.IsDeleted != true))
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.SaleInvoiceID == saleInvoiceId && i.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("فاکتور فروش یافت نشد.");

        if (invoice.DocumentType == InvoiceDocumentType.SaleReturn)
        {
            await PostSaleReturnAsync(invoice, userId, cancellationToken);
            return;
        }

        if (invoice.IsPosted)
        {
            throw new InvalidOperationException("این فاکتور قبلاً ثبت نهایی شده است.");
        }

        if (invoice.Items.Count == 0)
        {
            throw new InvalidOperationException("فاکتور باید حداقل یک ردیف داشته باشد.");
        }

        // چرا override با نرخ خودِ فاکتور: نرخ ارز به‌صورت اسنپ‌شات روی همین فاکتور قفل می‌شود و نرخ سراسری سیستم تغییر نمی‌کند.
        await ApplySaleCurrencyAsync(invoice, cancellationToken, ResolveInvoiceRateOverride(invoice.CurrencyId, invoice.BaseCurrencyId, invoice.BaseUnitsPerUnitAtTransaction));

        if (invoice.Status == InvoiceStatus.Quotation && invoice.PaidAmount > 0)
        {
            throw new InvalidOperationException("فاکتور استعلام قیمت نمی‌تواند مبلغ دریافتی داشته باشد.");
        }

        if (invoice.PaidAmount > invoice.TotalAmount)
        {
            throw new InvalidOperationException("مبلغ دریافت‌شده نمی‌تواند بیشتر از جمع فاکتور باشد.");
        }

        if (InvoiceStatusRules.RequiresStockValidation(invoice.Status))
        {
            await ValidateSaleStockAsync(invoice, cancellationToken);
        }

        decimal totalCost = 0;
        var now = DateTime.Now;
        var allowInsufficientStock = invoice.Status == InvoiceStatus.Order;

        if (InvoiceStatusRules.DeductsInventory(invoice.Status))
        {
            foreach (var item in invoice.Items)
            {
                foreach (var old in item.LotAllocations.ToList())
                {
                    old.IsDeleted = true;
                    old.DeletedAt = now;
                    old.DeletedBy = userId;
                }

                var allocations = await _fifo.AllocateAndApplyAsync(new AllocateStockRequest
                {
                    ProductId = item.ProductId,
                    WarehouseId = invoice.WarehouseId,
                    QuantityInBase = item.QuantityInBase,
                }, allowInsufficientStock, cancellationToken);

                decimal lineCost = 0;
                foreach (var allocation in allocations)
                {
                    var lot = await _db.InventoryLots
                        .AsNoTracking()
                        .FirstAsync(l => l.InventoryLotID == allocation.InventoryLotId, cancellationToken);

                    item.LotAllocations.Add(new SaleItemLotAllocation
                    {
                        InventoryLotId = allocation.InventoryLotId,
                        PurchaseInvoiceId = lot.PurchaseInvoiceId,
                        QuantityInBase = allocation.QuantityInBase,
                        UnitCostInBase = allocation.UnitCost,
                        LineCostInBase = allocation.LineCost,
                        IsDeleted = false,
                        CreatedAt = now,
                        CreatedBy = userId,
                    });

                    lineCost += allocation.LineCost;
                }

                item.LineCostInBaseCurrency = lineCost;
                item.LineProfitInBaseCurrency = item.LineTotalInBaseCurrency - lineCost;
                totalCost += lineCost;
            }
        }
        else
        {
            foreach (var item in invoice.Items)
            {
                foreach (var old in item.LotAllocations.ToList())
                {
                    old.IsDeleted = true;
                    old.DeletedAt = now;
                    old.DeletedBy = userId;
                }

                item.LineCostInBaseCurrency = 0;
                item.LineProfitInBaseCurrency = item.LineTotalInBaseCurrency;
            }
        }

        invoice.TotalCostInBaseCurrency = totalCost;
        invoice.TotalProfitInBaseCurrency = invoice.TotalAmountInBaseCurrency - totalCost;

        if (InvoiceStatusRules.AddsRevenue(invoice.Status) && invoice.TotalAmount > 0)
        {
            var saleCategoryId = await _financeCategories.GetRevenueCategoryIdAsync(
                FinanceCategoryCode.ProductSale,
                cancellationToken);

            // چرا کل مبلغ فاکتور: عاید بر مبنای تعهدی (accrual) و بر اساس کل مبلغ فاکتور ثبت می‌شود، نه مبلغ دریافتی؛
            // مانده طلب مشتری به‌صورت جدا در تراز طرف‌حساب (CustomerReadService) مدیریت می‌شود.
            var revenue = new Revenue
            {
                Title = $"فروش — {invoice.InvoiceNumber}",
                RevenueDate = invoice.InvoiceDate,
                RevenueCategoryId = saleCategoryId,
                Source = FinancialEntrySource.ProductSale,
                CustomerId = invoice.CustomerId,
                CurrencyId = invoice.CurrencyId,
                BaseCurrencyId = invoice.BaseCurrencyId,
                ExchangeHistoryId = invoice.ExchangeHistoryId,
                BaseUnitsPerUnitAtTransaction = invoice.BaseUnitsPerUnitAtTransaction,
                Amount = invoice.TotalAmount,
                AmountInBaseCurrency = invoice.TotalAmountInBaseCurrency,
                ProfitInBaseCurrency = invoice.TotalProfitInBaseCurrency,
                Description = invoice.Description,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = userId,
            };

            _db.Revenues.Add(revenue);
            await _db.SaveChangesAsync(cancellationToken);
            invoice.RevenueId = revenue.RevenueID;
        }

        invoice.IsPosted = true;
        invoice.PostedAt = now;
        invoice.IsUpdated = true;
        invoice.UpdatedAt = now;
        invoice.UpdatedBy = userId;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ValidateSaleStockAsync(
        SaleInvoice invoice,
        CancellationToken cancellationToken = default)
    {
        if (!InvoiceStatusRules.RequiresStockValidation(invoice.Status))
        {
            return;
        }

        var lines = invoice.Items
            .Where(i => i.IsDeleted != true && i.QuantityInBase > 0)
            .Select(i => new AllocateStockRequest
            {
                ProductId = i.ProductId,
                WarehouseId = invoice.WarehouseId,
                QuantityInBase = i.QuantityInBase,
            })
            .ToList();

        await _fifo.ValidateAvailableStockAsync(invoice.WarehouseId, lines, cancellationToken);
    }

    public async Task<SaleProfitPreview> PreviewSaleProfitAsync(
        SaleInvoice invoice,
        CancellationToken cancellationToken = default)
    {
        await ApplySaleCurrencyAsync(invoice, cancellationToken);

        var lines = new List<SaleLineProfitPreview>();
        decimal totalCost = 0;
        var allowInsufficientStock = invoice.Status == InvoiceStatus.Order;
        var previewInventory = InvoiceStatusRules.DeductsInventory(invoice.Status);

        foreach (var item in invoice.Items.Where(i => i.IsDeleted != true))
        {
            decimal lineCost = 0;
            IReadOnlyList<FifoAllocation> allocations = [];

            if (previewInventory)
            {
                allocations = await _fifo.PreviewAllocationAsync(new AllocateStockRequest
                {
                    ProductId = item.ProductId,
                    WarehouseId = invoice.WarehouseId,
                    QuantityInBase = item.QuantityInBase,
                }, allowInsufficientStock, cancellationToken);

                lineCost = allocations.Sum(a => a.LineCost);
            }

            var lineProfit = item.LineTotalInBaseCurrency - lineCost;
            totalCost += lineCost;

            lines.Add(new SaleLineProfitPreview(
                item.ProductId,
                item.QuantityInBase,
                item.LineTotalInBaseCurrency,
                lineCost,
                lineProfit,
                allocations));
        }

        return new SaleProfitPreview(
            invoice.TotalAmountInBaseCurrency,
            totalCost,
            invoice.TotalAmountInBaseCurrency - totalCost,
            lines);
    }

    private async Task PostPurchaseReturnAsync(
        PurchaseInvoice invoice,
        int? userId,
        CancellationToken cancellationToken)
    {
        if (invoice.IsPosted)
        {
            throw new InvalidOperationException("این برگشت قبلاً ثبت شده است.");
        }

        if (invoice.ReferencePurchaseInvoiceId is not int referenceId)
        {
            throw new InvalidOperationException("فاکتور مبدأ برای برگشت مشخص نیست.");
        }

        if (invoice.Items.Count == 0)
        {
            throw new InvalidOperationException("برگشت باید حداقل یک ردیف داشته باشد.");
        }

        await ApplyPurchaseCurrencyAsync(invoice, cancellationToken,
            ResolveInvoiceRateOverride(invoice.CurrencyId, invoice.BaseCurrencyId, invoice.BaseUnitsPerUnitAtTransaction));

        if (invoice.PaidAmount > invoice.TotalAmount)
        {
            throw new InvalidOperationException("مبلغ بازپرداخت نمی‌تواند بیشتر از جمع برگشت باشد.");
        }

        var now = DateTime.Now;

        if (invoice.TotalAmount > 0)
        {
            var purchaseCategoryId = await _financeCategories.GetExpenseCategoryIdAsync(
                FinanceCategoryCode.ProductPurchase,
                cancellationToken);

            // چرا مبلغ منفی: برگشت از خرید یک قلم «کاهنده مصرف» (contra-expense) است. مبلغ منفی ذخیره می‌شود تا هر
            // گزارشی که AmountInBaseCurrency را جمع می‌زند، خالص مصارف را درست محاسبه کند و همزمان Source=PurchaseReturn
            // برای تفکیک در گزارش‌های تفصیلی حفظ شود. (تراز تأمین‌کننده جداگانه از خود فاکتور با علامت DocumentType محاسبه می‌شود.)
            var expense = new Expense
            {
                Title = $"برگشت خرید — {invoice.InvoiceNumber}",
                ExpenseDate = invoice.InvoiceDate,
                ExpenseCategoryId = purchaseCategoryId,
                Source = FinancialEntrySource.PurchaseReturn,
                SupplierId = invoice.SupplierId,
                CurrencyId = invoice.CurrencyId,
                BaseCurrencyId = invoice.BaseCurrencyId,
                ExchangeHistoryId = invoice.ExchangeHistoryId,
                BaseUnitsPerUnitAtTransaction = invoice.BaseUnitsPerUnitAtTransaction,
                Amount = -invoice.TotalAmount,
                AmountInBaseCurrency = -invoice.TotalAmountInBaseCurrency,
                Description = invoice.Description,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = userId,
            };

            _db.Expenses.Add(expense);
            await _db.SaveChangesAsync(cancellationToken);
            invoice.ExpenseId = expense.ExpenseID;
        }

        foreach (var returnItem in invoice.Items)
        {
            if (returnItem.ReferencePurchaseItemId is not int originalItemId)
            {
                throw new InvalidOperationException("ردیف برگشت به فاکتور مبدأ متصل نیست.");
            }

            var originalItem = await _db.PurchaseItems
                .FirstOrDefaultAsync(
                    i => i.PurchaseItemID == originalItemId && i.IsDeleted != true,
                    cancellationToken)
                ?? throw new InvalidOperationException("ردیف مبدأ یافت نشد.");

            if (originalItem.InventoryLotId is not int lotId)
            {
                throw new InvalidOperationException("ردیف خرید مبدأ به موجودی متصل نیست.");
            }

            var returnable = originalItem.QuantityInBase - originalItem.ReturnedQuantityInBase;
            if (returnItem.QuantityInBase > returnable + 0.000001m)
            {
                throw new InvalidOperationException("مقدار برگشت بیش از حد مجاز است.");
            }

            await _fifo.ReturnFromLotAsync(lotId, returnItem.QuantityInBase, cancellationToken);
            originalItem.ReturnedQuantityInBase += returnItem.QuantityInBase;
            originalItem.IsUpdated = true;
            originalItem.UpdatedAt = now;
            originalItem.UpdatedBy = userId;
        }

        invoice.IsPosted = true;
        invoice.PostedAt = now;
        invoice.IsUpdated = true;
        invoice.UpdatedAt = now;
        invoice.UpdatedBy = userId;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task PostSaleReturnAsync(
        SaleInvoice invoice,
        int? userId,
        CancellationToken cancellationToken)
    {
        if (invoice.IsPosted)
        {
            throw new InvalidOperationException("این برگشت قبلاً ثبت شده است.");
        }

        if (invoice.ReferenceSaleInvoiceId is not int referenceId)
        {
            throw new InvalidOperationException("فاکتور مبدأ برای برگشت مشخص نیست.");
        }

        if (invoice.Items.Count == 0)
        {
            throw new InvalidOperationException("برگشت باید حداقل یک ردیف داشته باشد.");
        }

        await ApplySaleCurrencyAsync(invoice, cancellationToken,
            ResolveInvoiceRateOverride(invoice.CurrencyId, invoice.BaseCurrencyId, invoice.BaseUnitsPerUnitAtTransaction));

        decimal totalCost = 0;
        var now = DateTime.Now;

        foreach (var returnItem in invoice.Items)
        {
            if (returnItem.ReferenceSalesItemId is not int originalItemId)
            {
                throw new InvalidOperationException("ردیف برگشت به فاکتور مبدأ متصل نیست.");
            }

            var originalItem = await _db.SalesItems
                .Include(i => i.LotAllocations.Where(a => a.IsDeleted != true))
                .FirstOrDefaultAsync(
                    i => i.SalesItemID == originalItemId && i.IsDeleted != true,
                    cancellationToken)
                ?? throw new InvalidOperationException("ردیف مبدأ یافت نشد.");

            var returnable = originalItem.QuantityInBase - originalItem.ReturnedQuantityInBase;
            if (returnItem.QuantityInBase > returnable + 0.000001m)
            {
                throw new InvalidOperationException("مقدار برگشت بیش از حد مجاز است.");
            }

            if (originalItem.QuantityInBase <= 0)
            {
                throw new InvalidOperationException("ردیف مبدأ مقدار معتبر ندارد.");
            }

            decimal lineCost = 0;
            foreach (var allocation in originalItem.LotAllocations)
            {
                var restoreQty = returnItem.QuantityInBase * allocation.QuantityInBase / originalItem.QuantityInBase;
                if (restoreQty <= 0)
                {
                    continue;
                }

                await _fifo.RestoreToLotAsync(allocation.InventoryLotId, restoreQty, cancellationToken);

                var lineRestoreCost = restoreQty * allocation.UnitCostInBase;
                lineCost += lineRestoreCost;

                returnItem.LotAllocations.Add(new SaleItemLotAllocation
                {
                    InventoryLotId = allocation.InventoryLotId,
                    PurchaseInvoiceId = allocation.PurchaseInvoiceId,
                    QuantityInBase = restoreQty,
                    UnitCostInBase = allocation.UnitCostInBase,
                    LineCostInBase = lineRestoreCost,
                    IsDeleted = false,
                    CreatedAt = now,
                    CreatedBy = userId,
                });
            }

            returnItem.LineCostInBaseCurrency = lineCost;
            returnItem.LineProfitInBaseCurrency = returnItem.LineTotalInBaseCurrency - lineCost;
            totalCost += lineCost;

            originalItem.ReturnedQuantityInBase += returnItem.QuantityInBase;
            originalItem.IsUpdated = true;
            originalItem.UpdatedAt = now;
            originalItem.UpdatedBy = userId;
        }

        invoice.TotalCostInBaseCurrency = totalCost;
        invoice.TotalProfitInBaseCurrency = invoice.TotalAmountInBaseCurrency - totalCost;

        var saleCategoryId = await _financeCategories.GetRevenueCategoryIdAsync(
            FinanceCategoryCode.ProductSale,
            cancellationToken);

        // چرا مبلغ منفی: برگشت از فروش یک قلم «کاهنده عاید» (contra-revenue) است. مبلغ و سود منفی ذخیره می‌شوند تا هر
        // گزارشی که AmountInBaseCurrency/ProfitInBaseCurrency را جمع می‌زند، خالص عواید و سود را درست محاسبه کند و همزمان
        // Source=SaleReturn برای تفکیک در گزارش تفصیلی حفظ شود. (تراز مشتری جداگانه از خود فاکتور با علامت DocumentType محاسبه می‌شود.)
        var revenue = new Revenue
        {
            Title = $"برگشت فروش — {invoice.InvoiceNumber}",
            RevenueDate = invoice.InvoiceDate,
            RevenueCategoryId = saleCategoryId,
            Source = FinancialEntrySource.SaleReturn,
            CustomerId = invoice.CustomerId,
            CurrencyId = invoice.CurrencyId,
            BaseCurrencyId = invoice.BaseCurrencyId,
            ExchangeHistoryId = invoice.ExchangeHistoryId,
            BaseUnitsPerUnitAtTransaction = invoice.BaseUnitsPerUnitAtTransaction,
            Amount = -invoice.TotalAmount,
            AmountInBaseCurrency = -invoice.TotalAmountInBaseCurrency,
            ProfitInBaseCurrency = -invoice.TotalProfitInBaseCurrency,
            Description = invoice.Description,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedBy = userId,
        };

        _db.Revenues.Add(revenue);
        await _db.SaveChangesAsync(cancellationToken);

        invoice.RevenueId = revenue.RevenueID;
        invoice.IsPosted = true;
        invoice.PostedAt = now;
        invoice.IsUpdated = true;
        invoice.UpdatedAt = now;
        invoice.UpdatedBy = userId;
        await _db.SaveChangesAsync(cancellationToken);
    }
}

internal static class InvoiceStatusRules
{
    internal static bool DeductsInventory(InvoiceStatus status) =>
        status is InvoiceStatus.Order or InvoiceStatus.Invoice;

    internal static bool AddsRevenue(InvoiceStatus status) =>
        status is not InvoiceStatus.Quotation;

    internal static bool RequiresStockValidation(InvoiceStatus status) =>
        status is not InvoiceStatus.Order;

    internal static bool ShowsPayment(InvoiceStatus status) =>
        status is not InvoiceStatus.Quotation;
}
