using System.ComponentModel.DataAnnotations;
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

    public ExpenseController(AppDbContext db, ICurrencyConversionService currency) : base(db)
    {
        _currency = currency;
    }

    [HttpPost("datatable")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.Expenses
            .AsNoTracking()
            .Where(e => e.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(e =>
                e.Title.Contains(searchValue) ||
                (e.Description != null && e.Description.Contains(searchValue)) ||
                (e.Supplier != null && e.Supplier.Name.Contains(searchValue)) ||
                e.Category.Name.Contains(searchValue));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var ordered = query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(Expense.ExpenseDate), defaultDescending: true);

        var rows = await ordered
            .Skip(start)
            .Take(length)
            .Select(e => new
            {
                expenseId = e.ExpenseID,
                title = e.Title,
                expenseDate = e.ExpenseDate,
                categoryName = e.Category.Name,
                expenseCategoryId = e.ExpenseCategoryId,
                source = e.Source,
                sourceLabel = e.Source == FinancialEntrySource.ProductPurchase
                    ? "خرید محصولات"
                    : e.Source == FinancialEntrySource.PurchaseReturn
                        ? "برگشت از خرید"
                        : e.Source == FinancialEntrySource.Miscellaneous
                            ? "متفرقه"
                            : e.Source.ToString(),
                supplierId = e.SupplierId,
                supplierName = e.Supplier != null ? e.Supplier.Name : null,
                currencyId = e.CurrencyId,
                currencyCode = e.Currency.CurrencyCode,
                currencySymbol = e.Currency.Symbol,
                amount = e.Amount,
                amountInBaseCurrency = e.AmountInBaseCurrency,
                description = e.Description,
                isFromInvoice = Db.PurchaseInvoices.Any(i =>
                    i.ExpenseId == e.ExpenseID && i.IsDeleted != true),
                invoiceNumber = Db.PurchaseInvoices
                    .Where(i => i.ExpenseId == e.ExpenseID && i.IsDeleted != true)
                    .Select(i => i.InvoiceNumber)
                    .FirstOrDefault(),
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
                r.expenseId,
                r.title,
                expenseDate = r.expenseDate.ToString("yyyy-MM-dd"),
                r.categoryName,
                r.expenseCategoryId,
                r.source,
                r.sourceLabel,
                r.supplierId,
                r.supplierName,
                r.currencyId,
                r.currencyCode,
                r.currencySymbol,
                r.amount,
                r.amountInBaseCurrency,
                r.description,
                r.isFromInvoice,
                r.invoiceNumber,
            }),
        });
    }

    [HttpPost]
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
            CreatedBy = ResolveCurrentUserId(),
        };

        Db.Expenses.Add(expense);
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "مصرف با موفقیت ثبت شد.", expenseId = expense.ExpenseID });
    }

    [HttpPut("{id:int}")]
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

        var expenseDate = request.ExpenseDate?.Date ?? expense.ExpenseDate.Date;
        var snapshot = await _currency.GetSnapshotAsync(request.CurrencyId, expenseDate, cancellationToken);
        var amountInBase = _currency.ConvertToBase(request.Amount, snapshot);

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
        expense.UpdatedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "مصرف با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
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

        expense.IsDeleted = true;
        expense.IsActive = false;
        expense.DeletedAt = DateTime.Now;
        expense.DeletedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "مصرف با موفقیت حذف شد." });
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
