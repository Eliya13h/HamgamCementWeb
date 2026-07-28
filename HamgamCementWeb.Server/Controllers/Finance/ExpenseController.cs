using System.ComponentModel.DataAnnotations;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Controllers.Transport;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/expenses")]
[Authorize]
public class ExpenseController : FinanceControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(Expense.Title),
        [2] = nameof(Expense.ExpenseDate),
        [5] = nameof(Expense.Amount),
        [6] = nameof(Expense.AmountInBaseCurrency),
    };

    private readonly ICurrencyConversionService _currency;
    private readonly IOperationalGlService _gl;
    private readonly IJournalPostingService _journal;
    private readonly ICashBoxService _cashBoxes;
    private readonly IFinanceReadService _reads;

    public ExpenseController(
        AppDbContext db,
        ICurrencyConversionService currency,
        IOperationalGlService gl,
        IJournalPostingService journal,
        ICashBoxService cashBoxes,
        IFinanceReadService reads) : base(db)
    {
        _currency = currency;
        _gl = gl;
        _journal = journal;
        _cashBoxes = cashBoxes;
        _reads = reads;
    }

    [HttpPost("datatable")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);
        var (recordsTotal, recordsFiltered, rows) = await _reads.GetExpensesAsync(
            start, length, request.Search?.Value?.Trim(), cancellationToken);

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select((r, i) => new
            {
                rowNumber = start + i + 1,
                expenseId = r.ExpenseId,
                title = r.Title,
                expenseDate = r.ExpenseDate.ToString("yyyy-MM-dd"),
                categoryName = r.CategoryName,
                expenseCategoryId = r.ExpenseCategoryId,
                source = r.Source,
                sourceLabel = r.Source == (int)FinancialEntrySource.ProductPurchase
                    ? "خرید محصولات"
                    : r.Source == (int)FinancialEntrySource.PurchaseReturn
                        ? "برگشت از خرید"
                        : r.Source == (int)FinancialEntrySource.Miscellaneous
                            ? "متفرقه"
                            : r.Source == (int)FinancialEntrySource.TransportExpense
                                ? "هزینه حمل‌ونقل"
                                : r.Source.ToString(),
                supplierId = r.SupplierId,
                supplierName = r.SupplierName,
                currencyId = r.CurrencyId,
                currencyCode = r.CurrencyCode,
                currencySymbol = r.CurrencySymbol,
                amount = r.Amount,
                amountInBaseCurrency = r.AmountInBaseCurrency,
                description = r.Description,
                journalEntryId = r.JournalEntryId,
                isFromInvoice = !string.IsNullOrEmpty(r.InvoiceNumber),
                invoiceNumber = r.InvoiceNumber,
            }),
        });
    }

    [HttpPost]
    [HasPermission("accounting.expenses.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveExpenseRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "مبلغ باید بزرگ‌تر از صفر باشد." });
        }

        var category = await Db.ExpenseCategories
            .FirstOrDefaultAsync(
                c => c.ExpenseCategoryID == request.ExpenseCategoryId && c.IsDeleted != true && c.IsActive == true,
                cancellationToken);
        if (category is null)
        {
            return BadRequest(new { message = "دسته‌بندی مصرف معتبر نیست." });
        }

        if (category.IsSystem && category.Code == FinanceCategoryCode.ProductPurchase)
        {
            return BadRequest(new { message = "دسته‌بندی خرید محصولات فقط از طریق فاکتور خرید ثبت می‌شود." });
        }

        if (request.SupplierId is int supplierId)
        {
            var supplierExists = await Db.Suppliers
                .AnyAsync(s => s.SupplierID == supplierId && s.IsDeleted != true, cancellationToken);
            if (!supplierExists)
            {
                return BadRequest(new { message = "تأمین‌کننده یافت نشد." });
            }
        }

        var expenseDate = request.ExpenseDate?.Date ?? DateTime.Now.Date;
        var snapshot = await _currency.GetSnapshotAsync(request.CurrencyId, expenseDate, cancellationToken);
        var amountInBase = _currency.ConvertToBase(request.Amount, snapshot);

        var userId = ResolveCurrentUserId();
        var cashBoxId = await _cashBoxes.ResolveUserCashBoxIdAsync(userId, cancellationToken);
        if (request.SupplierId is null && cashBoxId is null)
        {
            return BadRequest(new { message = "برای ثبت مصرف نقدی، صندوق کاربر الزامی است." });
        }

        await using var tx = await Db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var expense = new Expense
            {
                Title = request.Title.Trim(),
                ExpenseDate = expenseDate,
                ExpenseCategoryId = request.ExpenseCategoryId,
                Source = FinancialEntrySource.Miscellaneous,
                SupplierId = request.SupplierId,
                CurrencyId = snapshot.CurrencyId,
                BaseCurrencyId = snapshot.BaseCurrencyId,
                ExchangeHistoryId = snapshot.ExchangeHistoryId,
                BaseUnitsPerUnitAtTransaction = snapshot.BaseUnitsPerUnit,
                Amount = request.Amount,
                AmountInBaseCurrency = amountInBase,
                Description = request.Description?.Trim(),
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreatedBy = userId,
            };

            Db.Expenses.Add(expense);
            await Db.SaveChangesAsync(cancellationToken);

            var journal = await _gl.PostMiscExpenseAsync(expense, userId, cashBoxId, cancellationToken);
            expense.JournalEntryId = journal.JournalEntryID;
            await Db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return Ok(new { message = "مصرف با موفقیت ثبت شد.", expenseId = expense.ExpenseID, journalEntryId = journal.JournalEntryID });
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [HasPermission("accounting.expenses.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveExpenseRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var expense = await Db.Expenses
            .FirstOrDefaultAsync(e => e.ExpenseID == id && e.IsDeleted != true, cancellationToken);
        if (expense is null)
        {
            return NotFound(new { message = "مصرف یافت نشد." });
        }

        if (await IsLinkedToInvoiceAsync(id, cancellationToken))
        {
            return Conflict(new { message = "مصرف ناشی از فاکتور خرید قابل ویرایش نیست." });
        }

        if (expense.Source != FinancialEntrySource.Miscellaneous)
        {
            return Conflict(new { message = "فقط مصارف متفرقه قابل ویرایش هستند." });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "مبلغ باید بزرگ‌تر از صفر باشد." });
        }

        var category = await Db.ExpenseCategories
            .FirstOrDefaultAsync(
                c => c.ExpenseCategoryID == request.ExpenseCategoryId && c.IsDeleted != true && c.IsActive == true,
                cancellationToken);
        if (category is null)
        {
            return BadRequest(new { message = "دسته‌بندی مصرف معتبر نیست." });
        }

        if (category.IsSystem && category.Code == FinanceCategoryCode.ProductPurchase)
        {
            return BadRequest(new { message = "دسته‌بندی خرید محصولات فقط از طریق فاکتور خرید ثبت می‌شود." });
        }

        if (request.SupplierId is int supplierId)
        {
            var supplierExists = await Db.Suppliers
                .AnyAsync(s => s.SupplierID == supplierId && s.IsDeleted != true, cancellationToken);
            if (!supplierExists)
            {
                return BadRequest(new { message = "تأمین‌کننده یافت نشد." });
            }
        }

        var userId = ResolveCurrentUserId();
        var cashBoxId = await _cashBoxes.ResolveUserCashBoxIdAsync(userId, cancellationToken);
        if (request.SupplierId is null && cashBoxId is null)
        {
            return BadRequest(new { message = "برای ثبت مصرف نقدی، صندوق کاربر الزامی است." });
        }

        var expenseDate = request.ExpenseDate?.Date ?? expense.ExpenseDate.Date;
        var snapshot = await _currency.GetSnapshotAsync(request.CurrencyId, expenseDate, cancellationToken);
        var amountInBase = _currency.ConvertToBase(request.Amount, snapshot);

        await using var tx = await Db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await ReverseExpenseJournalAsync(expense, userId, cancellationToken);

            expense.Title = request.Title.Trim();
            expense.ExpenseDate = expenseDate;
            expense.ExpenseCategoryId = request.ExpenseCategoryId;
            expense.SupplierId = request.SupplierId;
            expense.CurrencyId = snapshot.CurrencyId;
            expense.BaseCurrencyId = snapshot.BaseCurrencyId;
            expense.ExchangeHistoryId = snapshot.ExchangeHistoryId;
            expense.BaseUnitsPerUnitAtTransaction = snapshot.BaseUnitsPerUnit;
            expense.Amount = request.Amount;
            expense.AmountInBaseCurrency = amountInBase;
            expense.Description = request.Description?.Trim();
            expense.IsUpdated = true;
            expense.UpdatedAt = DateTime.Now;
            expense.UpdatedBy = userId;
            await Db.SaveChangesAsync(cancellationToken);

            var journal = await _gl.PostMiscExpenseAsync(expense, userId, cashBoxId, cancellationToken);
            expense.JournalEntryId = journal.JournalEntryID;
            await Db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return Ok(new { message = "مصرف با موفقیت ویرایش شد.", journalEntryId = journal.JournalEntryID });
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [HasPermission("accounting.expenses.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var expense = await Db.Expenses
            .FirstOrDefaultAsync(e => e.ExpenseID == id && e.IsDeleted != true, cancellationToken);
        if (expense is null)
        {
            return NotFound(new { message = "مصرف یافت نشد." });
        }

        if (await IsLinkedToInvoiceAsync(id, cancellationToken))
        {
            return Conflict(new { message = "مصرف ناشی از فاکتور خرید قابل حذف نیست." });
        }

        if (expense.Source != FinancialEntrySource.Miscellaneous)
        {
            return Conflict(new { message = "فقط مصارف متفرقه قابل حذف هستند." });
        }

        var userId = ResolveCurrentUserId();
        await using var tx = await Db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await ReverseExpenseJournalAsync(expense, userId, cancellationToken);

            expense.IsDeleted = true;
            expense.IsActive = false;
            expense.DeletedAt = DateTime.Now;
            expense.DeletedBy = userId;
            expense.JournalEntryId = null;
            await Db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return Ok(new { message = "مصرف با موفقیت حذف شد." });
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task ReverseExpenseJournalAsync(Expense expense, int? userId, CancellationToken cancellationToken)
    {
        if (expense.JournalEntryId is int jeId)
        {
            var exists = await Db.JournalEntries.AnyAsync(
                e => e.JournalEntryID == jeId && e.IsDeleted != true, cancellationToken);
            if (!exists)
            {
                throw new InvalidOperationException("سند حسابداری مرتبط با این مصرف یافت نشد.");
            }

            await _journal.ReverseEntryAsync(jeId, userId, null, cancellationToken);
            expense.JournalEntryId = null;
            return;
        }

        await _journal.ReverseBySourceAsync(JournalSource.Expense, expense.ExpenseID, userId, cancellationToken: cancellationToken);
        expense.JournalEntryId = null;
    }

    private Task<bool> IsLinkedToInvoiceAsync(int expenseId, CancellationToken cancellationToken) =>
        Db.PurchaseInvoices.AnyAsync(i => i.ExpenseId == expenseId && i.IsDeleted != true, cancellationToken);

    public class SaveExpenseRequest
    {
        [Required(ErrorMessage = "عنوان الزامی است.")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public DateTime? ExpenseDate { get; set; }

        [Required(ErrorMessage = "دسته‌بندی الزامی است.")]
        public int ExpenseCategoryId { get; set; }

        public int? SupplierId { get; set; }

        [Required(ErrorMessage = "ارز الزامی است.")]
        public int CurrencyId { get; set; }

        [Range(0.0001, double.MaxValue, ErrorMessage = "مبلغ باید بزرگ‌تر از صفر باشد.")]
        public decimal Amount { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }
    }
}
