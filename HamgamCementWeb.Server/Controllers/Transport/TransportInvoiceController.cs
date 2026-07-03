using System.ComponentModel.DataAnnotations;
using HamgamCementWeb.Server.Data;
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

    public TransportInvoiceController(AppDbContext db) : base(db)
    {
    }

    [HttpPost("datatable")]
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

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;

        var invoice = new TransportInvoice
        {
            InvoiceNumber = $"TMP{DateTime.UtcNow.Ticks}",
            VehicleId = request.VehicleId,
            TransportTripId = request.TransportTripId,
            InvoiceDate = request.InvoiceDate,
            Description = request.Description?.Trim(),
            TotalAmount = request.Expenses.Sum(e => e.Amount),
            IsActive = true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedBy = userId,
        };

        foreach (var line in request.Expenses)
        {
            invoice.Expenses.Add(new TransportExpense
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
            });
        }

        Db.TransportInvoices.Add(invoice);
        await Db.SaveChangesAsync(cancellationToken);

        invoice.InvoiceNumber = TransportCodeHelper.ForInvoice(invoice.TransportInvoiceID);
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "فاکتور مصارف با موفقیت ایجاد شد." });
    }

    [HttpPut("{id:int}")]
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

        var invoice = await Db.TransportInvoices
            .Include(i => i.Expenses.Where(e => e.IsDeleted != true))
            .FirstOrDefaultAsync(i => i.TransportInvoiceID == id && i.IsDeleted != true, cancellationToken);
        if (invoice is null)
        {
            return NotFound(new { message = "فاکتور یافت نشد." });
        }

        var userId = ResolveCurrentUserId();
        var now = DateTime.Now;

        invoice.VehicleId = request.VehicleId;
        invoice.TransportTripId = request.TransportTripId;
        invoice.InvoiceDate = request.InvoiceDate;
        invoice.Description = request.Description?.Trim();
        invoice.TotalAmount = request.Expenses.Sum(e => e.Amount);
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
                invoice.Expenses.Add(new TransportExpense
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
                });
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
            }
        }

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "فاکتور مصارف با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
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

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "فاکتور مصارف با موفقیت حذف شد." });
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
