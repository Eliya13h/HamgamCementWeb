using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Data.Models.Inventory;
using HamgamCementWeb.Server.Data.Models.Invoice;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Services;

/// <summary>
/// ساخت سند دوطرفه از رویدادهای عملیاتی (فاکتور، مصرف، عاید، انتقال صندوق، انبارگردانی، انتقال انبار).
/// </summary>
public interface IOperationalGlService
{
    Task<JournalEntry> PostPurchaseAsync(PurchaseInvoice invoice, int? userId, int? cashBoxId, CancellationToken cancellationToken = default);
    Task<JournalEntry> PostPurchaseReturnAsync(PurchaseInvoice invoice, int? userId, int? cashBoxId, CancellationToken cancellationToken = default);
    Task<JournalEntry> PostSaleAsync(SaleInvoice invoice, int? userId, int? cashBoxId, CancellationToken cancellationToken = default);
    Task<JournalEntry> PostSaleReturnAsync(SaleInvoice invoice, int? userId, int? cashBoxId, CancellationToken cancellationToken = default);
    Task<JournalEntry> PostMiscExpenseAsync(Expense expense, int? userId, int? cashBoxId, CancellationToken cancellationToken = default);
    Task<JournalEntry> PostMiscRevenueAsync(Revenue revenue, int? userId, int? cashBoxId, CancellationToken cancellationToken = default);
    Task<JournalEntry> PostCashTransferAsync(CashTransfer transfer, Account fromAccount, Account toAccount, int? userId, CancellationToken cancellationToken = default);
    Task<JournalEntry?> PostStocktakingAsync(Stocktaking stocktaking, Warehouse warehouse, int? userId, CancellationToken cancellationToken = default);
    Task<JournalEntry?> PostWarehouseTransferAsync(WarehouseTransfer transfer, Warehouse fromWarehouse, Warehouse toWarehouse, int? userId, CancellationToken cancellationToken = default);
    Task<int> ResolveSettlementAccountIdAsync(int? cashBoxId, CancellationToken cancellationToken = default);
}

public class OperationalGlService : IOperationalGlService
{
    private readonly AppDbContext _db;
    private readonly IJournalPostingService _journal;
    private readonly IAccountLookupService _accounts;
    private readonly ICashBalanceService _cashBalances;

    public OperationalGlService(
        AppDbContext db,
        IJournalPostingService journal,
        IAccountLookupService accounts,
        ICashBalanceService cashBalances)
    {
        _db = db;
        _journal = journal;
        _accounts = accounts;
        _cashBalances = cashBalances;
    }

    public async Task<int> ResolveSettlementAccountIdAsync(int? cashBoxId, CancellationToken cancellationToken = default)
    {
        if (cashBoxId is int id)
        {
            var box = await _db.CashBoxes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CashBoxID == id && c.IsDeleted != true, cancellationToken);
            if (box is not null)
            {
                return box.AccountId;
            }
        }

        throw new InvalidOperationException("صندوق یا حساب بانکی برای تسویه نقدی الزامی است.");
    }

    public async Task<JournalEntry> PostPurchaseAsync(
        PurchaseInvoice invoice,
        int? userId,
        int? cashBoxId,
        CancellationToken cancellationToken = default)
    {
        var warehouse = await _db.Warehouses
            .AsNoTracking()
            .FirstAsync(w => w.WarehouseID == invoice.WarehouseId, cancellationToken);

        var inventoryAccountId = await _accounts.ResolveInventoryAccountIdAsync(warehouse.WarehouseType, cancellationToken);
        var supplierName = invoice.Supplier?.Name
            ?? await _db.Suppliers.Where(s => s.SupplierID == invoice.SupplierId).Select(s => s.Name).FirstAsync(cancellationToken);
        var ap = await _accounts.EnsureSupplierAccountAsync(invoice.SupplierId, supplierName, cancellationToken);

        var amount = invoice.TotalAmount;
        var amountBase = invoice.TotalAmountInBaseCurrency;
        var subTotal = invoice.SubTotalAmount > 0 ? invoice.SubTotalAmount : amount;
        var subTotalBase = invoice.SubTotalAmountInBaseCurrency > 0 ? invoice.SubTotalAmountInBaseCurrency : amountBase;
        var tax = invoice.TaxAmount;
        var taxBase = invoice.TaxAmountInBaseCurrency;
        var taxAccount = tax > 0
            ? await _accounts.GetBySystemCodeAsync(AccountSystemCode.TaxReceivable, cancellationToken)
            : null;
        var lines = new List<JournalLineDraft>
        {
            new(inventoryAccountId, subTotal, 0, subTotalBase, 0, invoice.CurrencyId, $"خرید {invoice.InvoiceNumber}"),
            new(ap.AccountID, 0, amount, 0, amountBase, invoice.CurrencyId, $"بدهی تأمین‌کننده — {invoice.InvoiceNumber}", PartyId: invoice.SupplierId),
        };
        if (taxAccount is not null)
        {
            lines.Add(new(taxAccount.AccountID, tax, 0, taxBase, 0, invoice.CurrencyId, $"مالیات خرید — {invoice.InvoiceNumber}"));
        }

        if (invoice.PaidAmount > 0)
        {
            var paid = invoice.PaidAmount;
            var paidBase = paid * (amount > 0 ? amountBase / amount : 0);
            await EnsureCashOutAsync(cashBoxId, invoice.CurrencyId, paid, cancellationToken);
            var cashAccountId = await ResolveSettlementAccountIdAsync(cashBoxId, cancellationToken);
            lines.Add(new(ap.AccountID, paid, 0, paidBase, 0, invoice.CurrencyId, "پرداخت فاکتور خرید", PartyId: invoice.SupplierId));
            lines.Add(new(cashAccountId, 0, paid, 0, paidBase, invoice.CurrencyId, "خروج نقد/بانک", CashBoxId: cashBoxId));
        }

        return await _journal.PostAsync(
            invoice.InvoiceDate,
            $"خرید {invoice.InvoiceNumber}",
            JournalSource.PurchaseInvoice,
            invoice.PurchaseInvoiceID,
            invoice.BaseCurrencyId,
            lines,
            userId,
            cancellationToken);
    }

    public async Task<JournalEntry> PostPurchaseReturnAsync(
        PurchaseInvoice invoice,
        int? userId,
        int? cashBoxId,
        CancellationToken cancellationToken = default)
    {
        var warehouse = await _db.Warehouses
            .AsNoTracking()
            .FirstAsync(w => w.WarehouseID == invoice.WarehouseId, cancellationToken);

        var inventoryAccountId = await _accounts.ResolveInventoryAccountIdAsync(warehouse.WarehouseType, cancellationToken);
        var supplierName = invoice.Supplier?.Name
            ?? await _db.Suppliers.Where(s => s.SupplierID == invoice.SupplierId).Select(s => s.Name).FirstAsync(cancellationToken);
        var ap = await _accounts.EnsureSupplierAccountAsync(invoice.SupplierId, supplierName, cancellationToken);

        var amount = invoice.TotalAmount;
        var amountBase = invoice.TotalAmountInBaseCurrency;
        var subTotal = invoice.SubTotalAmount > 0 ? invoice.SubTotalAmount : amount;
        var subTotalBase = invoice.SubTotalAmountInBaseCurrency > 0 ? invoice.SubTotalAmountInBaseCurrency : amountBase;
        var tax = invoice.TaxAmount;
        var taxBase = invoice.TaxAmountInBaseCurrency;
        var taxAccount = tax > 0
            ? await _accounts.GetBySystemCodeAsync(AccountSystemCode.TaxReceivable, cancellationToken)
            : null;
        var lines = new List<JournalLineDraft>
        {
            new(ap.AccountID, amount, 0, amountBase, 0, invoice.CurrencyId, $"برگشت خرید {invoice.InvoiceNumber}", PartyId: invoice.SupplierId),
            new(inventoryAccountId, 0, subTotal, 0, subTotalBase, invoice.CurrencyId, $"کاهش موجودی — {invoice.InvoiceNumber}"),
        };
        if (taxAccount is not null)
        {
            lines.Add(new(taxAccount.AccountID, 0, tax, 0, taxBase, invoice.CurrencyId, $"برگشت مالیات خرید — {invoice.InvoiceNumber}"));
        }

        if (invoice.PaidAmount > 0)
        {
            var paid = invoice.PaidAmount;
            var paidBase = paid * (amount > 0 ? amountBase / amount : 0);
            var cashAccountId = await ResolveSettlementAccountIdAsync(cashBoxId, cancellationToken);
            lines.Add(new(cashAccountId, paid, 0, paidBase, 0, invoice.CurrencyId, "بازپرداخت نقد", CashBoxId: cashBoxId));
            lines.Add(new(ap.AccountID, 0, paid, 0, paidBase, invoice.CurrencyId, "تسویه برگشت", PartyId: invoice.SupplierId));
        }

        return await _journal.PostAsync(
            invoice.InvoiceDate,
            $"برگشت خرید {invoice.InvoiceNumber}",
            JournalSource.PurchaseInvoice,
            invoice.PurchaseInvoiceID,
            invoice.BaseCurrencyId,
            lines,
            userId,
            cancellationToken);
    }

    public async Task<JournalEntry> PostSaleAsync(
        SaleInvoice invoice,
        int? userId,
        int? cashBoxId,
        CancellationToken cancellationToken = default)
    {
        var warehouse = await _db.Warehouses
            .AsNoTracking()
            .FirstAsync(w => w.WarehouseID == invoice.WarehouseId, cancellationToken);

        var inventoryAccountId = await _accounts.ResolveInventoryAccountIdAsync(warehouse.WarehouseType, cancellationToken);
        var customerName = invoice.Customer?.Name
            ?? await _db.Customers.Where(c => c.CustomerID == invoice.CustomerId).Select(c => c.Name).FirstAsync(cancellationToken);
        var ar = await _accounts.EnsureCustomerAccountAsync(invoice.CustomerId, customerName, cancellationToken);
        var sales = await _accounts.GetBySystemCodeAsync(AccountSystemCode.ProductSales, cancellationToken);
        var cogs = await _accounts.GetBySystemCodeAsync(AccountSystemCode.Cogs, cancellationToken);

        var amount = invoice.TotalAmount;
        var amountBase = invoice.TotalAmountInBaseCurrency;
        var subTotal = invoice.SubTotalAmount > 0 ? invoice.SubTotalAmount : amount;
        var subTotalBase = invoice.SubTotalAmountInBaseCurrency > 0 ? invoice.SubTotalAmountInBaseCurrency : amountBase;
        var tax = invoice.TaxAmount;
        var taxBase = invoice.TaxAmountInBaseCurrency;
        var taxPayable = tax > 0
            ? await _accounts.GetBySystemCodeAsync(AccountSystemCode.TaxPayable, cancellationToken)
            : null;
        var costBase = invoice.TotalCostInBaseCurrency;
        var costDoc = amount > 0 && amountBase > 0
            ? costBase * (amount / amountBase)
            : costBase;

        var lines = new List<JournalLineDraft>
        {
            new(ar.AccountID, amount, 0, amountBase, 0, invoice.CurrencyId, $"فروش {invoice.InvoiceNumber}", PartyId: invoice.CustomerId),
            new(sales.AccountID, 0, subTotal, 0, subTotalBase, invoice.CurrencyId, $"درآمد فروش — {invoice.InvoiceNumber}"),
        };
        if (taxPayable is not null)
        {
            lines.Add(new(taxPayable.AccountID, 0, tax, 0, taxBase, invoice.CurrencyId, $"مالیات فروش — {invoice.InvoiceNumber}"));
        }

        if (costBase > 0)
        {
            lines.Add(new(cogs.AccountID, costDoc, 0, costBase, 0, invoice.CurrencyId, "بهای تمام‌شده فروش"));
            lines.Add(new(inventoryAccountId, 0, costDoc, 0, costBase, invoice.CurrencyId, "خروج موجودی"));
        }

        if (invoice.PaidAmount > 0)
        {
            var paid = invoice.PaidAmount;
            var paidBase = paid * (amount > 0 ? amountBase / amount : 0);
            var cashAccountId = await ResolveSettlementAccountIdAsync(cashBoxId, cancellationToken);
            lines.Add(new(cashAccountId, paid, 0, paidBase, 0, invoice.CurrencyId, "دریافت نقد", CashBoxId: cashBoxId));
            lines.Add(new(ar.AccountID, 0, paid, 0, paidBase, invoice.CurrencyId, "تسویه دریافتنی", PartyId: invoice.CustomerId));
        }

        return await _journal.PostAsync(
            invoice.InvoiceDate,
            $"فروش {invoice.InvoiceNumber}",
            JournalSource.SaleInvoice,
            invoice.SaleInvoiceID,
            invoice.BaseCurrencyId,
            lines,
            userId,
            cancellationToken);
    }

    public async Task<JournalEntry> PostSaleReturnAsync(
        SaleInvoice invoice,
        int? userId,
        int? cashBoxId,
        CancellationToken cancellationToken = default)
    {
        var warehouse = await _db.Warehouses
            .AsNoTracking()
            .FirstAsync(w => w.WarehouseID == invoice.WarehouseId, cancellationToken);

        var inventoryAccountId = await _accounts.ResolveInventoryAccountIdAsync(warehouse.WarehouseType, cancellationToken);
        var customerName = invoice.Customer?.Name
            ?? await _db.Customers.Where(c => c.CustomerID == invoice.CustomerId).Select(c => c.Name).FirstAsync(cancellationToken);
        var ar = await _accounts.EnsureCustomerAccountAsync(invoice.CustomerId, customerName, cancellationToken);
        var sales = await _accounts.GetBySystemCodeAsync(AccountSystemCode.ProductSales, cancellationToken);
        var cogs = await _accounts.GetBySystemCodeAsync(AccountSystemCode.Cogs, cancellationToken);

        var amount = invoice.TotalAmount;
        var amountBase = invoice.TotalAmountInBaseCurrency;
        var subTotal = invoice.SubTotalAmount > 0 ? invoice.SubTotalAmount : amount;
        var subTotalBase = invoice.SubTotalAmountInBaseCurrency > 0 ? invoice.SubTotalAmountInBaseCurrency : amountBase;
        var tax = invoice.TaxAmount;
        var taxBase = invoice.TaxAmountInBaseCurrency;
        var taxPayable = tax > 0
            ? await _accounts.GetBySystemCodeAsync(AccountSystemCode.TaxPayable, cancellationToken)
            : null;
        var costBase = invoice.TotalCostInBaseCurrency;
        var costDoc = amount > 0 && amountBase > 0
            ? costBase * (amount / amountBase)
            : costBase;

        var lines = new List<JournalLineDraft>
        {
            new(sales.AccountID, subTotal, 0, subTotalBase, 0, invoice.CurrencyId, $"برگشت فروش {invoice.InvoiceNumber}"),
            new(ar.AccountID, 0, amount, 0, amountBase, invoice.CurrencyId, $"کاهش طلب — {invoice.InvoiceNumber}", PartyId: invoice.CustomerId),
        };
        if (taxPayable is not null)
        {
            lines.Add(new(taxPayable.AccountID, tax, 0, taxBase, 0, invoice.CurrencyId, $"برگشت مالیات فروش — {invoice.InvoiceNumber}"));
        }

        if (costBase > 0)
        {
            lines.Add(new(inventoryAccountId, costDoc, 0, costBase, 0, invoice.CurrencyId, "برگشت موجودی"));
            lines.Add(new(cogs.AccountID, 0, costDoc, 0, costBase, invoice.CurrencyId, "برگشت بهای تمام‌شده"));
        }

        if (invoice.PaidAmount > 0)
        {
            var paid = invoice.PaidAmount;
            var paidBase = paid * (amount > 0 ? amountBase / amount : 0);
            await EnsureCashOutAsync(cashBoxId, invoice.CurrencyId, paid, cancellationToken);
            var cashAccountId = await ResolveSettlementAccountIdAsync(cashBoxId, cancellationToken);
            lines.Add(new(ar.AccountID, paid, 0, paidBase, 0, invoice.CurrencyId, "بازپرداخت به مشتری", PartyId: invoice.CustomerId));
            lines.Add(new(cashAccountId, 0, paid, 0, paidBase, invoice.CurrencyId, "خروج نقد", CashBoxId: cashBoxId));
        }

        return await _journal.PostAsync(
            invoice.InvoiceDate,
            $"برگشت فروش {invoice.InvoiceNumber}",
            JournalSource.SaleInvoice,
            invoice.SaleInvoiceID,
            invoice.BaseCurrencyId,
            lines,
            userId,
            cancellationToken);
    }

    public async Task<JournalEntry> PostMiscExpenseAsync(
        Expense expense,
        int? userId,
        int? cashBoxId,
        CancellationToken cancellationToken = default)
    {
        var category = await _db.ExpenseCategories
            .FirstAsync(c => c.ExpenseCategoryID == expense.ExpenseCategoryId, cancellationToken);

        var expenseAccountId = category.AccountId
            ?? (await _accounts.GetBySystemCodeAsync(AccountSystemCode.MiscExpense, cancellationToken)).AccountID;

        int creditAccountId;
        int? lineCashBoxId = null;
        if (expense.SupplierId is int supplierId)
        {
            var name = await _db.Suppliers.Where(s => s.SupplierID == supplierId).Select(s => s.Name).FirstAsync(cancellationToken);
            creditAccountId = (await _accounts.EnsureSupplierAccountAsync(supplierId, name, cancellationToken)).AccountID;
        }
        else
        {
            creditAccountId = await ResolveSettlementAccountIdAsync(cashBoxId, cancellationToken);
            await EnsureCashOutAsync(cashBoxId, expense.CurrencyId, expense.Amount, cancellationToken);
            lineCashBoxId = cashBoxId;
        }

        var lines = new List<JournalLineDraft>
        {
            new(expenseAccountId, expense.Amount, 0, expense.AmountInBaseCurrency, 0, expense.CurrencyId, expense.Title),
            new(creditAccountId, 0, expense.Amount, 0, expense.AmountInBaseCurrency, expense.CurrencyId, expense.Title,
                CashBoxId: lineCashBoxId,
                PartyId: expense.SupplierId),
        };

        return await _journal.PostAsync(
            expense.ExpenseDate,
            expense.Title,
            JournalSource.Expense,
            expense.ExpenseID,
            expense.BaseCurrencyId,
            lines,
            userId,
            cancellationToken);
    }

    public async Task<JournalEntry> PostMiscRevenueAsync(
        Revenue revenue,
        int? userId,
        int? cashBoxId,
        CancellationToken cancellationToken = default)
    {
        var category = await _db.RevenueCategories
            .FirstAsync(c => c.RevenueCategoryID == revenue.RevenueCategoryId, cancellationToken);

        var revenueAccountId = category.AccountId
            ?? (await _accounts.GetBySystemCodeAsync(AccountSystemCode.OtherRevenue, cancellationToken)).AccountID;

        int debitAccountId;
        int? lineCashBoxId = null;
        if (revenue.CustomerId is int customerId)
        {
            var name = await _db.Customers.Where(c => c.CustomerID == customerId).Select(c => c.Name).FirstAsync(cancellationToken);
            debitAccountId = (await _accounts.EnsureCustomerAccountAsync(customerId, name, cancellationToken)).AccountID;
        }
        else
        {
            debitAccountId = await ResolveSettlementAccountIdAsync(cashBoxId, cancellationToken);
            lineCashBoxId = cashBoxId;
        }

        var lines = new List<JournalLineDraft>
        {
            new(debitAccountId, revenue.Amount, 0, revenue.AmountInBaseCurrency, 0, revenue.CurrencyId, revenue.Title,
                CashBoxId: lineCashBoxId,
                PartyId: revenue.CustomerId),
            new(revenueAccountId, 0, revenue.Amount, 0, revenue.AmountInBaseCurrency, revenue.CurrencyId, revenue.Title),
        };

        return await _journal.PostAsync(
            revenue.RevenueDate,
            revenue.Title,
            JournalSource.Revenue,
            revenue.RevenueID,
            revenue.BaseCurrencyId,
            lines,
            userId,
            cancellationToken);
    }

    public Task<JournalEntry> PostCashTransferAsync(
        CashTransfer transfer,
        Account fromAccount,
        Account toAccount,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        return PostCashTransferCoreAsync(transfer, fromAccount, toAccount, userId, cancellationToken);
    }

    public async Task<JournalEntry?> PostStocktakingAsync(
        Stocktaking stocktaking,
        Warehouse warehouse,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var shortageCost = stocktaking.Lines
            .Where(l => l.IsDeleted != true && l.DifferenceInBase < 0 && l.AdjustmentCostInBase > 0)
            .Sum(l => l.AdjustmentCostInBase);

        var surplusCost = stocktaking.Lines
            .Where(l => l.IsDeleted != true && l.DifferenceInBase > 0 && l.AdjustmentCostInBase > 0)
            .Sum(l => l.AdjustmentCostInBase);

        if (shortageCost <= 0 && surplusCost <= 0)
        {
            return null;
        }

        var inventoryAccountId = await _accounts.ResolveInventoryAccountIdAsync(warehouse.WarehouseType, cancellationToken);
        var adj = await _accounts.GetBySystemCodeAsync(AccountSystemCode.InventoryAdjustment, cancellationToken);
        var baseCurrencyId = await ResolveBaseCurrencyIdAsync(cancellationToken);

        var lines = new List<JournalLineDraft>();

        // کسری: بدهکار ضایعات/تعدیل — بستانکار موجودی
        if (shortageCost > 0)
        {
            lines.Add(new(adj.AccountID, shortageCost, 0, shortageCost, 0, baseCurrencyId, $"کسری انبارگردانی {stocktaking.Code}"));
            lines.Add(new(inventoryAccountId, 0, shortageCost, 0, shortageCost, baseCurrencyId, $"کاهش موجودی — {stocktaking.Code}"));
        }

        // اضافی: بدهکار موجودی — بستانکار ضایعات/تعدیل
        if (surplusCost > 0)
        {
            lines.Add(new(inventoryAccountId, surplusCost, 0, surplusCost, 0, baseCurrencyId, $"اضافی انبارگردانی {stocktaking.Code}"));
            lines.Add(new(adj.AccountID, 0, surplusCost, 0, surplusCost, baseCurrencyId, $"افزایش موجودی — {stocktaking.Code}"));
        }

        return await _journal.PostAsync(
            stocktaking.StocktakingDate,
            $"انبارگردانی {stocktaking.Code}",
            JournalSource.Stocktaking,
            stocktaking.StocktakingID,
            baseCurrencyId,
            lines,
            userId,
            cancellationToken);
    }

    public async Task<JournalEntry?> PostWarehouseTransferAsync(
        WarehouseTransfer transfer,
        Warehouse fromWarehouse,
        Warehouse toWarehouse,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var amount = transfer.TotalCostInBaseCurrency;
        if (amount <= 0)
        {
            return null;
        }

        var fromAccountId = await _accounts.ResolveInventoryAccountIdAsync(fromWarehouse.WarehouseType, cancellationToken);
        var toAccountId = await _accounts.ResolveInventoryAccountIdAsync(toWarehouse.WarehouseType, cancellationToken);

        // اگر هر دو انبار به یک حساب موجودی وصل باشند، سند دفتر تغییری در مانده نمی‌دهد؛ برای ردیابی همچنان ثبت می‌شود.
        var baseCurrencyId = await ResolveBaseCurrencyIdAsync(cancellationToken);
        var lines = new List<JournalLineDraft>
        {
            new(toAccountId, amount, 0, amount, 0, baseCurrencyId, $"ورود انتقال {transfer.Code} — {toWarehouse.Name}"),
            new(fromAccountId, 0, amount, 0, amount, baseCurrencyId, $"خروج انتقال {transfer.Code} — {fromWarehouse.Name}"),
        };

        return await _journal.PostAsync(
            transfer.TransferDate,
            $"انتقال انبار {transfer.Code}",
            JournalSource.WarehouseTransfer,
            transfer.WarehouseTransferID,
            baseCurrencyId,
            lines,
            userId,
            cancellationToken);
    }

    private async Task<JournalEntry> PostCashTransferCoreAsync(
        CashTransfer transfer,
        Account fromAccount,
        Account toAccount,
        int? userId,
        CancellationToken cancellationToken)
    {
        var baseCurrencyId = await ResolveBaseCurrencyIdAsync(cancellationToken);
        var transferLines = transfer.Lines?.Where(l => l.IsDeleted != true).ToList() ?? [];

        if (transferLines.Count == 0 && transfer.AmountInBaseCurrency > 0)
        {
            // سازگاری با انتقال‌های قدیمی تک‌مبلغ پایه
            transferLines =
            [
                new CashTransferLine
                {
                    CurrencyId = baseCurrencyId,
                    Amount = transfer.AmountInBaseCurrency,
                    AmountInBaseCurrency = transfer.AmountInBaseCurrency,
                },
            ];
        }

        if (transferLines.Count == 0)
        {
            throw new InvalidOperationException("خطوط انتقال صندوق خالی است.");
        }

        var lines = new List<JournalLineDraft>();
        foreach (var line in transferLines)
        {
            lines.Add(new(
                toAccount.AccountID,
                line.Amount, 0,
                line.AmountInBaseCurrency, 0,
                line.CurrencyId,
                "ورود انتقال صندوق",
                CashBoxId: transfer.ToCashBoxId));
            lines.Add(new(
                fromAccount.AccountID,
                0, line.Amount,
                0, line.AmountInBaseCurrency,
                line.CurrencyId,
                "خروج انتقال صندوق",
                CashBoxId: transfer.FromCashBoxId));
        }

        return await _journal.PostAsync(
            transfer.TransferDate,
            transfer.Description ?? "انتقال صندوق",
            JournalSource.CashTransfer,
            transfer.CashTransferID,
            baseCurrencyId,
            lines,
            userId,
            cancellationToken);
    }

    private async Task EnsureCashOutAsync(
        int? cashBoxId,
        int currencyId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        if (cashBoxId is not int id || amount <= 0)
        {
            return;
        }

        await _cashBalances.EnsureSufficientBalanceAsync(id, currencyId, amount, cancellationToken);
    }

    private async Task<int> ResolveBaseCurrencyIdAsync(CancellationToken cancellationToken)
    {
        var baseCurrencyId = await _db.Currencies
            .Where(c => c.IsBaseCurrency && c.IsDeleted != true)
            .Select(c => c.CurrencyID)
            .FirstOrDefaultAsync(cancellationToken);

        if (baseCurrencyId == 0)
        {
            baseCurrencyId = await _db.Currencies
                .Where(c => c.IsDeleted != true)
                .OrderBy(c => c.CurrencyID)
                .Select(c => c.CurrencyID)
                .FirstAsync(cancellationToken);
        }

        return baseCurrencyId;
    }
}
