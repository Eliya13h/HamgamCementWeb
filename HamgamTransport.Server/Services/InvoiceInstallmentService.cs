using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Services;

public interface IInvoiceInstallmentService
{
    Task<IReadOnlyList<InvoiceInstallment>> ListAsync(
        InvoiceInstallmentKind kind,
        int invoiceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvoiceInstallment>> GenerateEqualAsync(
        InvoiceInstallmentKind kind,
        int invoiceId,
        int count,
        DateTime? firstDueDate,
        int? userId,
        CancellationToken cancellationToken = default);
}

public class InvoiceInstallmentService : IInvoiceInstallmentService
{
    private readonly AppDbContext _db;

    public InvoiceInstallmentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<InvoiceInstallment>> ListAsync(
        InvoiceInstallmentKind kind,
        int invoiceId,
        CancellationToken cancellationToken = default)
    {
        return await _db.InvoiceInstallments
            .AsNoTracking()
            .Where(i => i.IsDeleted != true && i.InvoiceKind == kind && i.InvoiceId == invoiceId)
            .OrderBy(i => i.InstallmentNo)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InvoiceInstallment>> GenerateEqualAsync(
        InvoiceInstallmentKind kind,
        int invoiceId,
        int count,
        DateTime? firstDueDate,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        if (count < 1 || count > 60)
        {
            throw new InvalidOperationException("تعداد اقساط باید بین ۱ تا ۶۰ باشد.");
        }

        decimal total;
        decimal paid;
        DateTime dueBase;

        if (kind == InvoiceInstallmentKind.Sale)
        {
            var inv = await _db.SaleInvoices
                .FirstOrDefaultAsync(i => i.SaleInvoiceID == invoiceId && i.IsDeleted != true, cancellationToken)
                ?? throw new InvalidOperationException("فاکتور فروش یافت نشد.");
            total = inv.TotalAmount;
            paid = inv.PaidAmount;
            dueBase = firstDueDate ?? inv.DueDate ?? inv.InvoiceDate.AddDays(inv.PaymentTermDays);
            if (inv.DueDate is null && inv.PaymentTermDays > 0)
            {
                inv.DueDate = inv.InvoiceDate.AddDays(inv.PaymentTermDays);
            }
        }
        else
        {
            var inv = await _db.PurchaseInvoices
                .FirstOrDefaultAsync(i => i.PurchaseInvoiceID == invoiceId && i.IsDeleted != true, cancellationToken)
                ?? throw new InvalidOperationException("فاکتور خرید یافت نشد.");
            total = inv.TotalAmount;
            paid = inv.PaidAmount;
            dueBase = firstDueDate ?? inv.DueDate ?? inv.InvoiceDate.AddDays(inv.PaymentTermDays);
            if (inv.DueDate is null && inv.PaymentTermDays > 0)
            {
                inv.DueDate = inv.InvoiceDate.AddDays(inv.PaymentTermDays);
            }
        }

        var remaining = total - paid;
        if (remaining <= 0.01m)
        {
            throw new InvalidOperationException("مانده قابل قسط‌بندی وجود ندارد.");
        }

        var existing = await _db.InvoiceInstallments
            .Where(i => i.IsDeleted != true && i.InvoiceKind == kind && i.InvoiceId == invoiceId)
            .ToListAsync(cancellationToken);

        if (existing.Any(e => e.PaidAmount > 0.01m))
        {
            throw new InvalidOperationException("برای فاکتور با قسط پرداخت‌شده نمی‌توان اقساط را از نو ساخت.");
        }

        var now = DateTime.Now;
        foreach (var old in existing)
        {
            old.IsDeleted = true;
            old.IsActive = false;
            old.DeletedAt = now;
            old.DeletedBy = userId;
        }

        var baseAmount = Math.Round(remaining / count, 4, MidpointRounding.AwayFromZero);
        var allocated = 0m;
        var created = new List<InvoiceInstallment>();

        for (var n = 1; n <= count; n++)
        {
            var amount = n == count ? remaining - allocated : baseAmount;
            allocated += amount;
            var row = new InvoiceInstallment
            {
                InvoiceKind = kind,
                InvoiceId = invoiceId,
                InstallmentNo = n,
                DueDate = dueBase.Date.AddMonths(n - 1),
                Amount = amount,
                PaidAmount = 0,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = userId,
            };
            _db.InvoiceInstallments.Add(row);
            created.Add(row);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return created;
    }
}
