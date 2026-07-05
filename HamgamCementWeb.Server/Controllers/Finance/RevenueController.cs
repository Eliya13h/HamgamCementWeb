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
[Route("api/finance/revenues")]
[Authorize]
public class RevenueController : FinanceControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(Revenue.Title),
        [2] = nameof(Revenue.RevenueDate),
        [5] = nameof(Revenue.Amount),
        [6] = nameof(Revenue.AmountInBaseCurrency),
    };

    private readonly ICurrencyConversionService _currency;

    public RevenueController(AppDbContext db, ICurrencyConversionService currency) : base(db)
    {
        _currency = currency;
    }

    [HttpPost("datatable")]
    [HasPermission("accounting.revenues.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.Revenues
            .AsNoTracking()
            .Where(r => r.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(r =>
                r.Title.Contains(searchValue) ||
                (r.Description != null && r.Description.Contains(searchValue)) ||
                (r.Customer != null && r.Customer.Name.Contains(searchValue)) ||
                r.Category.Name.Contains(searchValue));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var ordered = query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(Revenue.RevenueDate), defaultDescending: true);

        var rows = await ordered
            .Skip(start)
            .Take(length)
            .Select(r => new
            {
                revenueId = r.RevenueID,
                title = r.Title,
                revenueDate = r.RevenueDate,
                categoryName = r.Category.Name,
                revenueCategoryId = r.RevenueCategoryId,
                source = r.Source,
                sourceLabel = r.Source == FinancialEntrySource.ProductSale
                    ? "فروش محصولات"
                    : r.Source == FinancialEntrySource.SaleReturn
                        ? "برگشت از فروش"
                        : r.Source == FinancialEntrySource.Miscellaneous
                            ? "متفرقه"
                            : r.Source.ToString(),
                customerId = r.CustomerId,
                customerName = r.Customer != null ? r.Customer.Name : null,
                currencyId = r.CurrencyId,
                currencyCode = r.Currency.CurrencyCode,
                currencySymbol = r.Currency.Symbol,
                amount = r.Amount,
                amountInBaseCurrency = r.AmountInBaseCurrency,
                profitInBaseCurrency = r.ProfitInBaseCurrency,
                description = r.Description,
                isFromInvoice = Db.SaleInvoices.Any(i =>
                    i.RevenueId == r.RevenueID && i.IsDeleted != true),
                invoiceNumber = Db.SaleInvoices
                    .Where(i => i.RevenueId == r.RevenueID && i.IsDeleted != true)
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
                r.revenueId,
                r.title,
                revenueDate = r.revenueDate.ToString("yyyy-MM-dd"),
                r.categoryName,
                r.revenueCategoryId,
                r.source,
                r.sourceLabel,
                r.customerId,
                r.customerName,
                r.currencyId,
                r.currencyCode,
                r.currencySymbol,
                r.amount,
                r.amountInBaseCurrency,
                r.profitInBaseCurrency,
                r.description,
                r.isFromInvoice,
                r.invoiceNumber,
            }),
        });
    }

    [HttpPost]
    [HasPermission("accounting.revenues.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveRevenueRequest request,
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

        var category = await Db.RevenueCategories
            .FirstOrDefaultAsync(
                c => c.RevenueCategoryID == request.RevenueCategoryId && c.IsDeleted != true && c.IsActive == true,
                cancellationToken);
        if (category is null)
        {
            return BadRequest(new { message = "دسته‌بندی عاید معتبر نیست." });
        }

        if (category.IsSystem && category.Code == FinanceCategoryCode.ProductSale)
        {
            return BadRequest(new { message = "دسته‌بندی فروش محصولات فقط از طریق فاکتور فروش ثبت می‌شود." });
        }

        if (request.CustomerId is int customerId)
        {
            var customerExists = await Db.Customers
                .AnyAsync(c => c.CustomerID == customerId && c.IsDeleted != true, cancellationToken);
            if (!customerExists)
            {
                return BadRequest(new { message = "مشتری یافت نشد." });
            }
        }

        var revenueDate = request.RevenueDate?.Date ?? DateTime.Now.Date;
        var snapshot = await _currency.GetSnapshotAsync(request.CurrencyId, revenueDate, cancellationToken);
        var amountInBase = _currency.ConvertToBase(request.Amount, snapshot);

        var revenue = new Revenue
        {
            Title = request.Title.Trim(),
            RevenueDate = revenueDate,
            RevenueCategoryId = request.RevenueCategoryId,
            Source = FinancialEntrySource.Miscellaneous,
            CustomerId = request.CustomerId,
            CurrencyId = snapshot.CurrencyId,
            BaseCurrencyId = snapshot.BaseCurrencyId,
            ExchangeHistoryId = snapshot.ExchangeHistoryId,
            BaseUnitsPerUnitAtTransaction = snapshot.BaseUnitsPerUnit,
            Amount = request.Amount,
            AmountInBaseCurrency = amountInBase,
            ProfitInBaseCurrency = 0,
            Description = request.Description?.Trim(),
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };

        Db.Revenues.Add(revenue);
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "عاید با موفقیت ثبت شد.", revenueId = revenue.RevenueID });
    }

    [HttpPut("{id:int}")]
    [HasPermission("accounting.revenues.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveRevenueRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var revenue = await Db.Revenues
            .FirstOrDefaultAsync(r => r.RevenueID == id && r.IsDeleted != true, cancellationToken);
        if (revenue is null)
        {
            return NotFound(new { message = "عاید یافت نشد." });
        }

        if (await IsLinkedToInvoiceAsync(id, cancellationToken))
        {
            return Conflict(new { message = "عاید ناشی از فاکتور فروش قابل ویرایش نیست." });
        }

        if (revenue.Source != FinancialEntrySource.Miscellaneous)
        {
            return Conflict(new { message = "فقط عواید متفرقه قابل ویرایش هستند." });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "مبلغ باید بزرگ‌تر از صفر باشد." });
        }

        var category = await Db.RevenueCategories
            .FirstOrDefaultAsync(
                c => c.RevenueCategoryID == request.RevenueCategoryId && c.IsDeleted != true && c.IsActive == true,
                cancellationToken);
        if (category is null)
        {
            return BadRequest(new { message = "دسته‌بندی عاید معتبر نیست." });
        }

        if (category.IsSystem && category.Code == FinanceCategoryCode.ProductSale)
        {
            return BadRequest(new { message = "دسته‌بندی فروش محصولات فقط از طریق فاکتور فروش ثبت می‌شود." });
        }

        var revenueDate = request.RevenueDate?.Date ?? revenue.RevenueDate.Date;
        var snapshot = await _currency.GetSnapshotAsync(request.CurrencyId, revenueDate, cancellationToken);
        var amountInBase = _currency.ConvertToBase(request.Amount, snapshot);

        revenue.Title = request.Title.Trim();
        revenue.RevenueDate = revenueDate;
        revenue.RevenueCategoryId = request.RevenueCategoryId;
        revenue.CustomerId = request.CustomerId;
        revenue.CurrencyId = snapshot.CurrencyId;
        revenue.BaseCurrencyId = snapshot.BaseCurrencyId;
        revenue.ExchangeHistoryId = snapshot.ExchangeHistoryId;
        revenue.BaseUnitsPerUnitAtTransaction = snapshot.BaseUnitsPerUnit;
        revenue.Amount = request.Amount;
        revenue.AmountInBaseCurrency = amountInBase;
        revenue.Description = request.Description?.Trim();
        revenue.IsUpdated = true;
        revenue.UpdatedAt = DateTime.Now;
        revenue.UpdatedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "عاید با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("accounting.revenues.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var revenue = await Db.Revenues
            .FirstOrDefaultAsync(r => r.RevenueID == id && r.IsDeleted != true, cancellationToken);
        if (revenue is null)
        {
            return NotFound(new { message = "عاید یافت نشد." });
        }

        if (await IsLinkedToInvoiceAsync(id, cancellationToken))
        {
            return Conflict(new { message = "عاید ناشی از فاکتور فروش قابل حذف نیست." });
        }

        if (revenue.Source != FinancialEntrySource.Miscellaneous)
        {
            return Conflict(new { message = "فقط عواید متفرقه قابل حذف هستند." });
        }

        revenue.IsDeleted = true;
        revenue.IsActive = false;
        revenue.DeletedAt = DateTime.Now;
        revenue.DeletedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "عاید با موفقیت حذف شد." });
    }

    private Task<bool> IsLinkedToInvoiceAsync(int revenueId, CancellationToken cancellationToken) =>
        Db.SaleInvoices.AnyAsync(i => i.RevenueId == revenueId && i.IsDeleted != true, cancellationToken);

    public class SaveRevenueRequest
    {
        [Required(ErrorMessage = "عنوان الزامی است.")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public DateTime? RevenueDate { get; set; }

        [Required(ErrorMessage = "دسته‌بندی الزامی است.")]
        public int RevenueCategoryId { get; set; }

        public int? CustomerId { get; set; }

        [Required(ErrorMessage = "ارز الزامی است.")]
        public int CurrencyId { get; set; }

        [Range(0.0001, double.MaxValue, ErrorMessage = "مبلغ باید بزرگ‌تر از صفر باشد.")]
        public decimal Amount { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }
    }
}
