using System.ComponentModel.DataAnnotations;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Data.Models.Transport;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Transport;

[ApiController]
[Route("api/transport/invoices")]
[Authorize]
public class TransportInvoiceController : TransportControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(TransportInvoice.InvoiceNumber),
        [4] = nameof(TransportInvoice.InvoiceDate),
        [5] = nameof(TransportInvoice.TotalAmount),
    };

    private readonly ICurrencyConversionService _currency;
    private readonly IFinanceCategoryService _financeCategories;
    private readonly IOperationalGlService _gl;
    private readonly ICashBoxService _cashBoxes;

    public TransportInvoiceController(
        AppDbContext db,
        ICurrencyConversionService currency,
        IFinanceCategoryService financeCategories,
        IOperationalGlService gl,
        ICashBoxService cashBoxes) : base(db)
    {
        _currency = currency;
        _financeCategories = financeCategories;
        _gl = gl;
        _cashBoxes = cashBoxes;
    }

    [HttpPost("datatable")]
    [HasPermission("transport.invoices.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.TransportInvoices
            .AsNoTracking()
            .Where(i => i.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(i =>
                i.InvoiceNumber.Contains(searchValue) ||
                (i.Description != null && i.Description.Contains(searchValue)) ||
                (i.Vehicle != null && (i.Vehicle.Code.Contains(searchValue) || i.Vehicle.PlateNumber.Contains(searchValue))) ||
                (i.Trip != null && i.Trip.TripNumber.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(TransportInvoice.CreatedAt))
            .Skip(start)
            .Take(length)
            .Select(i => new
            {
                transportInvoiceId = i.TransportInvoiceID,
                invoiceNumber = i.InvoiceNumber,
                vehicleId = i.VehicleId,
                vehicleLabel = i.Vehicle != null ? i.Vehicle.Code + " — " + i.Vehicle.PlateNumber : string.Empty,
                transportTripId = i.TransportTripId,
                tripNumber = i.Trip != null ? i.Trip.TripNumber : null,
                invoiceDate = i.InvoiceDate,
                totalAmount = i.TotalAmount,
                itemsCount = i.Expenses.Count(e => e.IsDeleted != true),
                description = i.Description,
                isActive = i.IsActive == true,
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
                r.transportInvoiceId,
                r.invoiceNumber,
                r.vehicleId,
                r.vehicleLabel,
                r.transportTripId,
                r.tripNumber,
                r.invoiceDate,
                r.totalAmount,
                r.itemsCount,
                r.description,
                r.isActive,
            }),
        });
    }

    // دریافت فاکتور به همراه ردیف‌های مصارف برای ویرایش
    [HttpGet("{id:int}")]
    [HasPermission("transport.invoices.view")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var invoice = await Db.TransportInvoices
            .AsNoTracking()
            .Where(i => i.TransportInvoiceID == id && i.IsDeleted != true)
            .Select(i => new
            {
                transportInvoiceId = i.TransportInvoiceID,
                invoiceNumber = i.InvoiceNumber,
                vehicleId = i.VehicleId,
                transportTripId = i.TransportTripId,
                invoiceDate = i.InvoiceDate,
                totalAmount = i.TotalAmount,
                description = i.Description,
                expenses = i.Expenses
                    .Where(e => e.IsDeleted != true)
                    .OrderBy(e => e.TransportExpenseID)
                    .Select(e => new
                    {
                        transportExpenseId = e.TransportExpenseID,
                        expensesCategoryId = e.ExpensesCategoryId,
                        categoryName = e.Category != null ? e.Category.Name : string.Empty,
                        title = e.Title,
                        amount = e.Amount,
                        currencyId = e.CurrencyId,
                        expenseDate = e.ExpenseDate,
                        description = e.Description,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null)
        {
            return NotFound(new { message = "فاکتور یافت نشد." });
        }

        return Ok(invoice);
    }

    [HttpPost]
    [HasPermission("transport.invoices.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.Expenses.Count == 0)
        {
            return BadRequest(new { message = "فاکتور باید حداقل یک ردیف مصرف داشته باشد." });
        }

        var validationError = await ValidateInvoiceReferencesAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;

        // چرا تراکنش: ایجاد فاکتور، ردیف‌ها و رکورد مصرف حسابداری مرتبط باید اتمیک باشد.
        await using var tx = await Db.Database.BeginTransactionAsync(cancellationToken);

        var invoice = new TransportInvoice
        {
            InvoiceNumber = $"TMP{DateTime.UtcNow.Ticks}",
            VehicleId = request.VehicleId,
            TransportTripId = request.TransportTripId,
            InvoiceDate = request.InvoiceDate,
            Description = request.Description?.Trim(),
            IsActive = true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedBy = userId,
        };

        foreach (var line in request.Expenses)
        {
            var expense = new TransportExpense
            {
                ExpensesCategoryId = line.ExpensesCategoryId,
                Title = line.Title.Trim(),
                Amount = line.Amount,
                CurrencyId = line.CurrencyId,
                ExpenseDate = line.ExpenseDate ?? request.InvoiceDate,
                Description = line.Description?.Trim(),
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = userId,
            };

            await ApplyExpenseCurrencyAsync(expense, cancellationToken);
            invoice.Expenses.Add(expense);
        }

        invoice.TotalAmount = invoice.Expenses.Sum(e => e.Amount);
        invoice.TotalAmountInBaseCurrency = invoice.Expenses.Sum(e => e.AmountInBaseCurrency);

        Db.TransportInvoices.Add(invoice);
        await Db.SaveChangesAsync(cancellationToken);

        invoice.InvoiceNumber = TransportCodeHelper.ForInvoice(invoice.TransportInvoiceID);

        await SyncAccountingExpenseAsync(invoice, userId, cancellationToken);
        await Db.SaveChangesAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);

        return Ok(new { message = "فاکتور مصارف با موفقیت ایجاد شد." });
    }

    [HttpPut("{id:int}")]
    [HasPermission("transport.invoices.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.Expenses.Count == 0)
        {
            return BadRequest(new { message = "فاکتور باید حداقل یک ردیف مصرف داشته باشد." });
        }

        var validationError = await ValidateInvoiceReferencesAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var invoice = await Db.TransportInvoices
            .Include(i => i.Expenses.Where(e => e.IsDeleted != true))
            .FirstOrDefaultAsync(i => i.TransportInvoiceID == id && i.IsDeleted != true, cancellationToken);
        if (invoice is null)
        {
            return NotFound(new { message = "فاکتور یافت نشد." });
        }

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;

        // چرا تراکنش: به‌روزرسانی فاکتور، ردیف‌ها و رکورد مصرف حسابداری مرتبط باید اتمیک باشد.
        await using var tx = await Db.Database.BeginTransactionAsync(cancellationToken);

        invoice.VehicleId = request.VehicleId;
        invoice.TransportTripId = request.TransportTripId;
        invoice.InvoiceDate = request.InvoiceDate;
        invoice.Description = request.Description?.Trim();
        invoice.IsUpdated = true;
        invoice.UpdatedAt = now;
        invoice.UpdatedBy = userId;

        // همگام‌سازی ردیف‌ها: ردیف حذف‌شده سافت دیلیت، ردیف موجود ویرایش و ردیف جدید اضافه می‌شود
        var incomingIds = request.Expenses
            .Where(e => e.TransportExpenseId is > 0)
            .Select(e => e.TransportExpenseId!.Value)
            .ToHashSet();

        foreach (var existing in invoice.Expenses.Where(e => !incomingIds.Contains(e.TransportExpenseID)))
        {
            existing.IsDeleted = true;
            existing.IsActive = false;
            existing.DeletedAt = now;
            existing.DeletedBy = userId;
        }

        foreach (var line in request.Expenses)
        {
            var existing = line.TransportExpenseId is > 0
                ? invoice.Expenses.FirstOrDefault(e => e.TransportExpenseID == line.TransportExpenseId)
                : null;

            if (existing is null)
            {
                var created = new TransportExpense
                {
                    ExpensesCategoryId = line.ExpensesCategoryId,
                    Title = line.Title.Trim(),
                    Amount = line.Amount,
                    CurrencyId = line.CurrencyId,
                    ExpenseDate = line.ExpenseDate ?? request.InvoiceDate,
                    Description = line.Description?.Trim(),
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    CreatedBy = userId,
                };

                await ApplyExpenseCurrencyAsync(created, cancellationToken);
                invoice.Expenses.Add(created);
            }
            else
            {
                existing.ExpensesCategoryId = line.ExpensesCategoryId;
                existing.Title = line.Title.Trim();
                existing.Amount = line.Amount;
                existing.CurrencyId = line.CurrencyId;
                existing.ExpenseDate = line.ExpenseDate ?? request.InvoiceDate;
                existing.Description = line.Description?.Trim();
                existing.IsUpdated = true;
                existing.UpdatedAt = now;
                existing.UpdatedBy = userId;

                await ApplyExpenseCurrencyAsync(existing, cancellationToken);
            }
        }

        var activeExpenses = invoice.Expenses.Where(e => e.IsDeleted != true).ToList();
        invoice.TotalAmount = activeExpenses.Sum(e => e.Amount);
        invoice.TotalAmountInBaseCurrency = activeExpenses.Sum(e => e.AmountInBaseCurrency);

        await SyncAccountingExpenseAsync(invoice, userId, cancellationToken);
        await Db.SaveChangesAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);

        return Ok(new { message = "فاکتور مصارف با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("transport.invoices.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var invoice = await Db.TransportInvoices
            .Include(i => i.Expenses.Where(e => e.IsDeleted != true))
            .FirstOrDefaultAsync(i => i.TransportInvoiceID == id && i.IsDeleted != true, cancellationToken);
        if (invoice is null)
        {
            return NotFound(new { message = "فاکتور یافت نشد." });
        }

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;

        invoice.IsDeleted = true;
        invoice.IsActive = false;
        invoice.DeletedAt = now;
        invoice.DeletedBy = userId;

        foreach (var expense in invoice.Expenses)
        {
            expense.IsDeleted = true;
            expense.IsActive = false;
            expense.DeletedAt = now;
            expense.DeletedBy = userId;
        }

        // حذف رکورد مصرف حسابداری مرتبط تا در گزارش‌های مالی باقی نماند.
        if (invoice.ExpenseId is int expenseId)
        {
            var accountingExpense = await Db.Expenses
                .FirstOrDefaultAsync(e => e.ExpenseID == expenseId && e.IsDeleted != true, cancellationToken);
            if (accountingExpense is not null)
            {
                accountingExpense.IsDeleted = true;
                accountingExpense.IsActive = false;
                accountingExpense.DeletedAt = now;
                accountingExpense.DeletedBy = userId;
            }
        }

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "فاکتور مصارف با موفقیت حذف شد." });
    }

    // چرا: مبلغ هر ردیف مصرف را با اسنپ‌شات نرخ ارز به ارز پایه تبدیل و روی ردیف ذخیره می‌کند
    // تا جمع فاکتورهای چندارزی درست باشد. اگر ارز مشخص نشده باشد، ارز پایه فرض می‌شود.
    private async Task ApplyExpenseCurrencyAsync(TransportExpense expense, CancellationToken cancellationToken)
    {
        var baseCurrency = await _currency.GetBaseCurrencyAsync(cancellationToken);
        var currencyId = expense.CurrencyId ?? baseCurrency.CurrencyID;

        var snapshot = await _currency.GetSnapshotAsync(currencyId, expense.ExpenseDate, cancellationToken);

        expense.BaseCurrencyId = snapshot.BaseCurrencyId;
        expense.ExchangeHistoryId = snapshot.ExchangeHistoryId;
        expense.BaseUnitsPerUnitAtTransaction = snapshot.BaseUnitsPerUnit;
        expense.AmountInBaseCurrency = _currency.ConvertToBase(expense.Amount, snapshot);
    }

    // چرا: فاکتور مصارف حمل‌ونقل باید در حسابداری به‌صورت یک رکورد مصرف (Expense) به ارز پایه منعکس شود؛
    // این متد رکورد را در ایجاد/به‌روزرسانی هماهنگ نگه می‌دارد (ساخت یا به‌روزرسانی مبلغ کل).
    private async Task SyncAccountingExpenseAsync(TransportInvoice invoice, int? userId, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var baseCurrency = await _currency.GetBaseCurrencyAsync(cancellationToken);
        var categoryId = await _financeCategories.GetExpenseCategoryIdAsync(
            FinanceCategoryCode.TransportExpense,
            cancellationToken);

        Expense? expense = invoice.ExpenseId is int existingId
            ? await Db.Expenses.FirstOrDefaultAsync(e => e.ExpenseID == existingId, cancellationToken)
            : null;

        if (invoice.TotalAmountInBaseCurrency <= 0)
        {
            // اگر مبلغی باقی نمانده، رکورد مصرف موجود حذف (soft delete) می‌شود.
            if (expense is not null)
            {
                expense.IsDeleted = true;
                expense.IsActive = false;
                expense.DeletedAt = now;
                expense.DeletedBy = userId;
                invoice.ExpenseId = null;
            }

            return;
        }

        if (expense is null)
        {
            expense = new Expense
            {
                ExpenseDate = invoice.InvoiceDate,
                ExpenseCategoryId = categoryId,
                Source = FinancialEntrySource.TransportExpense,
                CurrencyId = baseCurrency.CurrencyID,
                BaseCurrencyId = baseCurrency.CurrencyID,
                BaseUnitsPerUnitAtTransaction = 1m,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = userId,
            };
            Db.Expenses.Add(expense);
        }

        // مبلغ حسابداری همیشه به ارز پایه ثبت می‌شود؛ چون فاکتور می‌تواند ردیف‌های چندارزی داشته باشد.
        expense.Title = $"مصارف حمل‌ونقل — {invoice.InvoiceNumber}";
        expense.ExpenseDate = invoice.InvoiceDate;
        expense.ExpenseCategoryId = categoryId;
        expense.Amount = invoice.TotalAmountInBaseCurrency;
        expense.AmountInBaseCurrency = invoice.TotalAmountInBaseCurrency;
        expense.Description = invoice.Description;

        if (expense.ExpenseID != 0)
        {
            expense.IsUpdated = true;
            expense.UpdatedAt = now;
            expense.UpdatedBy = userId;
            expense.IsDeleted = false;
            expense.IsActive = true;
        }

        await Db.SaveChangesAsync(cancellationToken);
        invoice.ExpenseId = expense.ExpenseID;

        if (expense.JournalEntryId is null)
        {
            var cashBoxId = await _cashBoxes.ResolveUserCashBoxIdAsync(userId, cancellationToken);
            var journal = await _gl.PostMiscExpenseAsync(expense, userId, cashBoxId, cancellationToken);
            expense.JournalEntryId = journal.JournalEntryID;
            await Db.SaveChangesAsync(cancellationToken);
        }
    }

    // اعتبارسنجی وجود کلیدهای خارجی و سازگاری سفر با وسیله.
    private async Task<string?> ValidateInvoiceReferencesAsync(SaveInvoiceRequest request, CancellationToken cancellationToken)
    {
        var vehicleExists = await Db.Vehicles
            .AnyAsync(v => v.VehicleID == request.VehicleId && v.IsDeleted != true, cancellationToken);
        if (!vehicleExists)
        {
            return "وسیله نقلیه انتخاب‌شده یافت نشد.";
        }

        if (request.TransportTripId is int tripId)
        {
            var trip = await Db.TransportTrips
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TransportTripID == tripId && t.IsDeleted != true, cancellationToken);
            if (trip is null)
            {
                return "سفر انتخاب‌شده یافت نشد.";
            }

            if (trip.VehicleId is int tripVehicleId && tripVehicleId != request.VehicleId)
            {
                return "سفر انتخاب‌شده متعلق به وسیله نقلیه‌ی این فاکتور نیست.";
            }
        }

        var categoryIds = request.Expenses.Select(e => e.ExpensesCategoryId).Distinct().ToList();
        var validCategoryCount = await Db.ExpensesCategories
            .CountAsync(c => categoryIds.Contains(c.ExpensesCategoryID) && c.IsDeleted != true, cancellationToken);
        if (validCategoryCount != categoryIds.Count)
        {
            return "یک یا چند دسته‌بندی مصرف نامعتبر است.";
        }

        var currencyIds = request.Expenses
            .Where(e => e.CurrencyId is int)
            .Select(e => e.CurrencyId!.Value)
            .Distinct()
            .ToList();
        if (currencyIds.Count > 0)
        {
            var validCurrencyCount = await Db.Currencies
                .CountAsync(c => currencyIds.Contains(c.CurrencyID) && c.IsDeleted != true, cancellationToken);
            if (validCurrencyCount != currencyIds.Count)
            {
                return "یک یا چند ارز انتخاب‌شده نامعتبر است.";
            }
        }

        return null;
    }

    public class SaveInvoiceRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "انتخاب وسیله نقلیه الزامی است.")]
        public int VehicleId { get; set; }

        public int? TransportTripId { get; set; }

        [Required(ErrorMessage = "تاریخ فاکتور الزامی است.")]
        public DateTime InvoiceDate { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public List<SaveInvoiceLineRequest> Expenses { get; set; } = [];
    }

    public class SaveInvoiceLineRequest
    {
        // شناسه ردیف موجود — برای ردیف جدید null است
        public int? TransportExpenseId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "انتخاب دسته‌بندی الزامی است.")]
        public int ExpensesCategoryId { get; set; }

        [Required(ErrorMessage = "عنوان مصرف الزامی است.")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Range(0.0001, double.MaxValue, ErrorMessage = "مبلغ باید بزرگ‌تر از صفر باشد.")]
        public decimal Amount { get; set; }

        public int? CurrencyId { get; set; }

        public DateTime? ExpenseDate { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
    }
}
