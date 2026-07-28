using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Data.Models.Invoice;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Services;

public record PartySettlementRequest(
    PartySettlementPartyType PartyType,
    int PartyId,
    DateTime SettlementDate,
    int CurrencyId,
    decimal Amount,
    decimal? AmountInBaseCurrency,
    int? CashBoxId,
    int? BankAccountId,
    int? SaleInvoiceId,
    int? PurchaseInvoiceId,
    int? InstallmentId,
    string? Description);

public interface IPartySettlementService
{
    Task<PartySettlement> PostAsync(PartySettlementRequest request, int? userId, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(int partySettlementId, int? userId, CancellationToken cancellationToken = default);
}

public class PartySettlementService : IPartySettlementService
{
    private readonly AppDbContext _db;
    private readonly IJournalPostingService _journal;
    private readonly IAccountLookupService _accounts;
    private readonly ICashBalanceService _cashBalances;
    private readonly ICurrencyConversionService _currencies;

    public PartySettlementService(
        AppDbContext db,
        IJournalPostingService journal,
        IAccountLookupService accounts,
        ICashBalanceService cashBalances,
        ICurrencyConversionService currencies)
    {
        _db = db;
        _journal = journal;
        _accounts = accounts;
        _cashBalances = cashBalances;
        _currencies = currencies;
    }

    public async Task<PartySettlement> PostAsync(
        PartySettlementRequest request,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new InvalidOperationException("مبلغ باید بزرگ‌تر از صفر باشد.");
        }

        var hasCash = request.CashBoxId is > 0;
        var hasBank = request.BankAccountId is > 0;
        if (hasCash == hasBank)
        {
            throw new InvalidOperationException("یکی از صندوق یا حساب بانکی باید مشخص شود.");
        }

        var currencyExists = await _db.Currencies
            .AnyAsync(c => c.CurrencyID == request.CurrencyId && c.IsDeleted != true, cancellationToken);
        if (!currencyExists)
        {
            throw new InvalidOperationException("ارز یافت نشد.");
        }

        var settlementDate = request.SettlementDate == default ? DateTime.Now : request.SettlementDate;
        var amountBase = request.AmountInBaseCurrency is > 0
            ? request.AmountInBaseCurrency.Value
            : _currencies.ConvertToBase(
                request.Amount,
                await _currencies.GetSnapshotAsync(request.CurrencyId, settlementDate, cancellationToken));

        if (amountBase <= 0)
        {
            throw new InvalidOperationException("معادل ارز پایه باید بزرگ‌تر از صفر باشد.");
        }

        int settlementAccountId;
        int? cashBoxId = null;
        int? bankAccountId = null;

        if (hasCash)
        {
            var box = await _db.CashBoxes
                .FirstOrDefaultAsync(c => c.CashBoxID == request.CashBoxId && c.IsDeleted != true && c.IsActive == true, cancellationToken)
                ?? throw new InvalidOperationException("صندوق یافت نشد یا غیرفعال است.");
            settlementAccountId = box.AccountId;
            cashBoxId = box.CashBoxID;
        }
        else
        {
            var bank = await _db.BankAccounts
                .FirstOrDefaultAsync(b => b.BankAccountID == request.BankAccountId && b.IsDeleted != true && b.IsActive == true, cancellationToken)
                ?? throw new InvalidOperationException("حساب بانکی یافت نشد یا غیرفعال است.");
            settlementAccountId = bank.AccountId;
            bankAccountId = bank.BankAccountID;
        }

        Account partyAccount;
        string partyName;
        string description;

        if (request.PartyType == PartySettlementPartyType.Customer)
        {
            var customer = await _db.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerID == request.PartyId && c.IsDeleted != true, cancellationToken)
                ?? throw new InvalidOperationException("مشتری یافت نشد.");
            partyName = customer.Name;
            partyAccount = await _accounts.EnsureCustomerAccountAsync(customer.CustomerID, customer.Name, cancellationToken);
            description = string.IsNullOrWhiteSpace(request.Description)
                ? $"دریافت از مشتری — {partyName}"
                : request.Description.Trim();

            if (request.PurchaseInvoiceId is not null)
            {
                throw new InvalidOperationException("برای مشتری فقط فاکتور فروش قابل تخصیص است.");
            }
        }
        else if (request.PartyType == PartySettlementPartyType.Supplier)
        {
            var supplier = await _db.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SupplierID == request.PartyId && s.IsDeleted != true, cancellationToken)
                ?? throw new InvalidOperationException("تأمین‌کننده یافت نشد.");
            partyName = supplier.Name;
            partyAccount = await _accounts.EnsureSupplierAccountAsync(supplier.SupplierID, supplier.Name, cancellationToken);
            description = string.IsNullOrWhiteSpace(request.Description)
                ? $"پرداخت به تأمین‌کننده — {partyName}"
                : request.Description.Trim();

            if (request.SaleInvoiceId is not null)
            {
                throw new InvalidOperationException("برای تأمین‌کننده فقط فاکتور خرید قابل تخصیص است.");
            }
        }
        else
        {
            throw new InvalidOperationException("نوع طرف تسویه نامعتبر است.");
        }

        SaleInvoice? saleInvoice = null;
        PurchaseInvoice? purchaseInvoice = null;
        InvoiceInstallment? installment = null;

        if (request.SaleInvoiceId is int saleId)
        {
            if (request.PartyType != PartySettlementPartyType.Customer)
            {
                throw new InvalidOperationException("فاکتور فروش فقط برای مشتری مجاز است.");
            }

            saleInvoice = await _db.SaleInvoices
                .FirstOrDefaultAsync(i => i.SaleInvoiceID == saleId && i.IsDeleted != true, cancellationToken)
                ?? throw new InvalidOperationException("فاکتور فروش یافت نشد.");

            if (saleInvoice.CustomerId != request.PartyId)
            {
                throw new InvalidOperationException("فاکتور فروش متعلق به این مشتری نیست.");
            }

            if (saleInvoice.CurrencyId != request.CurrencyId)
            {
                throw new InvalidOperationException("ارز تسویه با ارز فاکتور یکسان نیست.");
            }

            if (!saleInvoice.IsPosted)
            {
                throw new InvalidOperationException("فقط فاکتور فروش ثبت‌شده در دفتر قابل تسویه است.");
            }

            var saleRemaining = saleInvoice.TotalAmount - saleInvoice.PaidAmount;
            if (request.Amount > saleRemaining + 0.0001m)
            {
                throw new InvalidOperationException(
                    $"مبلغ تسویه از مانده فاکتور بیشتر است. مانده: {Math.Max(0, saleRemaining):N2}");
            }
        }

        if (request.PurchaseInvoiceId is int purchaseId)
        {
            if (request.PartyType != PartySettlementPartyType.Supplier)
            {
                throw new InvalidOperationException("فاکتور خرید فقط برای تأمین‌کننده مجاز است.");
            }

            purchaseInvoice = await _db.PurchaseInvoices
                .FirstOrDefaultAsync(i => i.PurchaseInvoiceID == purchaseId && i.IsDeleted != true, cancellationToken)
                ?? throw new InvalidOperationException("فاکتور خرید یافت نشد.");

            if (purchaseInvoice.SupplierId != request.PartyId)
            {
                throw new InvalidOperationException("فاکتور خرید متعلق به این تأمین‌کننده نیست.");
            }

            if (purchaseInvoice.CurrencyId != request.CurrencyId)
            {
                throw new InvalidOperationException("ارز تسویه با ارز فاکتور یکسان نیست.");
            }

            if (!purchaseInvoice.IsPosted)
            {
                throw new InvalidOperationException("فقط فاکتور خرید ثبت‌شده در دفتر قابل تسویه است.");
            }

            var purchaseRemaining = purchaseInvoice.TotalAmount - purchaseInvoice.PaidAmount;
            if (request.Amount > purchaseRemaining + 0.0001m)
            {
                throw new InvalidOperationException(
                    $"مبلغ تسویه از مانده فاکتور بیشتر است. مانده: {Math.Max(0, purchaseRemaining):N2}");
            }
        }

        if (request.InstallmentId is int installmentId)
        {
            installment = await _db.InvoiceInstallments.FirstOrDefaultAsync(
                i => i.InvoiceInstallmentID == installmentId && i.IsDeleted != true,
                cancellationToken) ?? throw new InvalidOperationException("قسط یافت نشد.");

            var expectedKind = request.PartyType == PartySettlementPartyType.Customer
                ? InvoiceInstallmentKind.Sale
                : InvoiceInstallmentKind.Purchase;
            var expectedInvoiceId = saleInvoice?.SaleInvoiceID ?? purchaseInvoice?.PurchaseInvoiceID;
            if (installment.InvoiceKind != expectedKind || expectedInvoiceId is null || installment.InvoiceId != expectedInvoiceId)
            {
                throw new InvalidOperationException("قسط متعلق به فاکتور انتخاب‌شده نیست.");
            }

            var installmentRemaining = installment.Amount - installment.PaidAmount;
            if (request.Amount > installmentRemaining + 0.0001m)
            {
                throw new InvalidOperationException(
                    $"مبلغ تسویه از مانده قسط بیشتر است. مانده: {Math.Max(0, installmentRemaining):N2}");
            }
        }

        // پرداخت به تأمین‌کننده از صندوق نیاز به کنترل مانده دارد
        if (request.PartyType == PartySettlementPartyType.Supplier && cashBoxId is int outBoxId)
        {
            await _cashBalances.EnsureSufficientBalanceAsync(outBoxId, request.CurrencyId, request.Amount, cancellationToken);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTime.Now;
        var settlement = new PartySettlement
        {
            PartyType = request.PartyType,
            PartyId = request.PartyId,
            SettlementDate = settlementDate,
            CurrencyId = request.CurrencyId,
            Amount = request.Amount,
            AmountInBaseCurrency = amountBase,
            CashBoxId = cashBoxId,
            BankAccountId = bankAccountId,
            SaleInvoiceId = saleInvoice?.SaleInvoiceID,
            PurchaseInvoiceId = purchaseInvoice?.PurchaseInvoiceID,
            InstallmentId = installment?.InvoiceInstallmentID,
            Description = description,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedBy = userId,
        };

        _db.PartySettlements.Add(settlement);
        await _db.SaveChangesAsync(cancellationToken);

        List<JournalLineDraft> lines;
        if (request.PartyType == PartySettlementPartyType.Customer)
        {
            // دریافت از مشتری: بدهکار صندوق/بانک — بستانکار دریافتنی مشتری
            lines =
            [
                new(settlementAccountId, request.Amount, 0, amountBase, 0, request.CurrencyId,
                    $"دریافت از {partyName}", CashBoxId: cashBoxId),
                new(partyAccount.AccountID, 0, request.Amount, 0, amountBase, request.CurrencyId,
                    $"تسویه دریافتنی — {partyName}", PartyId: request.PartyId),
            ];
        }
        else
        {
            // پرداخت به تأمین‌کننده: بدهکار پرداختنی — بستانکار صندوق/بانک
            lines =
            [
                new(partyAccount.AccountID, request.Amount, 0, amountBase, 0, request.CurrencyId,
                    $"تسویه پرداختنی — {partyName}", PartyId: request.PartyId),
                new(settlementAccountId, 0, request.Amount, 0, amountBase, request.CurrencyId,
                    $"پرداخت به {partyName}", CashBoxId: cashBoxId),
            ];
        }

        var baseCurrency = await _currencies.GetBaseCurrencyAsync(cancellationToken);
        var journal = await _journal.PostAsync(
            settlementDate,
            description,
            JournalSource.PartySettlement,
            settlement.PartySettlementID,
            baseCurrency.CurrencyID,
            lines,
            userId,
            cancellationToken);

        settlement.JournalEntryId = journal.JournalEntryID;

        if (saleInvoice is not null)
        {
            var remaining = Math.Max(0, saleInvoice.TotalAmount - saleInvoice.PaidAmount);
            var allocate = Math.Min(request.Amount, remaining);
            saleInvoice.PaidAmount += allocate;
            saleInvoice.IsUpdated = true;
            saleInvoice.UpdatedAt = now;
            saleInvoice.UpdatedBy = userId;
        }

        if (purchaseInvoice is not null)
        {
            var remaining = Math.Max(0, purchaseInvoice.TotalAmount - purchaseInvoice.PaidAmount);
            var allocate = Math.Min(request.Amount, remaining);
            purchaseInvoice.PaidAmount += allocate;
            purchaseInvoice.IsUpdated = true;
            purchaseInvoice.UpdatedAt = now;
            purchaseInvoice.UpdatedBy = userId;
        }

        if (installment is not null)
        {
            var remaining = Math.Max(0, installment.Amount - installment.PaidAmount);
            installment.PaidAmount += Math.Min(request.Amount, remaining);
            installment.IsUpdated = true;
            installment.UpdatedAt = now;
            installment.UpdatedBy = userId;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return settlement;
    }

    public async Task SoftDeleteAsync(int partySettlementId, int? userId, CancellationToken cancellationToken = default)
    {
        var settlement = await _db.PartySettlements
            .FirstOrDefaultAsync(s => s.PartySettlementID == partySettlementId && s.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("تسویه یافت نشد.");

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        if (settlement.SaleInvoiceId is int saleId)
        {
            var invoice = await _db.SaleInvoices
                .FirstOrDefaultAsync(i => i.SaleInvoiceID == saleId && i.IsDeleted != true, cancellationToken);
            if (invoice is not null)
            {
                // بازگرداندن تخصیص — سقف مانده پرداخت‌شده
                var reverse = Math.Min(settlement.Amount, invoice.PaidAmount);
                invoice.PaidAmount = Math.Max(0, invoice.PaidAmount - reverse);
                invoice.IsUpdated = true;
                invoice.UpdatedAt = DateTime.Now;
                invoice.UpdatedBy = userId;
            }
        }

        if (settlement.PurchaseInvoiceId is int purchaseId)
        {
            var invoice = await _db.PurchaseInvoices
                .FirstOrDefaultAsync(i => i.PurchaseInvoiceID == purchaseId && i.IsDeleted != true, cancellationToken);
            if (invoice is not null)
            {
                var reverse = Math.Min(settlement.Amount, invoice.PaidAmount);
                invoice.PaidAmount = Math.Max(0, invoice.PaidAmount - reverse);
                invoice.IsUpdated = true;
                invoice.UpdatedAt = DateTime.Now;
                invoice.UpdatedBy = userId;
            }
        }

        if (settlement.InstallmentId is int installmentId)
        {
            var installment = await _db.InvoiceInstallments.FirstOrDefaultAsync(
                i => i.InvoiceInstallmentID == installmentId && i.IsDeleted != true, cancellationToken);
            if (installment is not null)
            {
                installment.PaidAmount = Math.Max(0, installment.PaidAmount - Math.Min(settlement.Amount, installment.PaidAmount));
                installment.IsUpdated = true;
                installment.UpdatedAt = DateTime.Now;
                installment.UpdatedBy = userId;
            }
        }

        await _journal.ReverseBySourceAsync(
            JournalSource.PartySettlement,
            settlement.PartySettlementID,
            userId,
            cancellationToken: cancellationToken);

        var now = DateTime.Now;
        settlement.IsDeleted = true;
        settlement.IsActive = false;
        settlement.DeletedAt = now;
        settlement.DeletedBy = userId;
        settlement.JournalEntryId = null;

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }
}
