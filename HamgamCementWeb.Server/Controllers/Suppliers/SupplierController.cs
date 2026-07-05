using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Controllers.Transport;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Invoice;
using HamgamCementWeb.Server.Data.Models.People;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Suppliers;

[ApiController]
[Route("api/suppliers")]
[Authorize]
public class SupplierController : ControllerBase
{
    private const string ViewDeletedPermission = "people.suppliers.viewDeleted";

    private readonly AppDbContext _db;
    private readonly ISupplierReadService _reads;

    public SupplierController(AppDbContext db, ISupplierReadService reads)
    {
        _db = db;
        _reads = reads;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await _reads.ListActiveAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [HasPermission("people.suppliers.view")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var canViewDeleted = await CanViewDeletedAsync(cancellationToken);
        var supplier = await _reads.GetDetailAsync(id, canViewDeleted, cancellationToken);

        if (supplier is null)
        {
            return NotFound(new { message = "تأمین‌کننده یافت نشد." });
        }

        return Ok(new
        {
            supplierId = supplier.SupplierId,
            title = (int)supplier.Title,
            titleName = supplier.Title == PersonTitle.Mrs ? "خانم" : "آقا",
            supplier.Name,
            supplier.PhoneNumber,
            supplier.Address,
            supplier.City,
            supplier.Country,
            supplier.InitialBalance,
            supplierType = (int)supplier.SupplierType,
            supplierTypeName = supplier.SupplierType == PersonType.LegalEntity ? "حقوقی" : "حقیقی",
            isActive = supplier.IsActive,
            isDeleted = supplier.IsDeleted,
            createdAt = supplier.CreatedAt,
            totalPurchase = supplier.TotalPurchase,
            totalPayment = supplier.TotalPayment,
            balance = supplier.Balance,
            accountStatus = supplier.AccountStatus,
            accountStatusCode = supplier.AccountStatusCode,
        });
    }

    [HttpPost("datatable")]
    [HasPermission("people.suppliers.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var canViewDeleted = await CanViewDeletedAsync(cancellationToken);
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var result = await _reads.QueryDataTableAsync(
            new SupplierDataTableQuery
            {
                IncludeDeleted = canViewDeleted,
                Start = start,
                Length = length,
                Search = request.Search?.Value,
                Order = request.Order,
            },
            cancellationToken);

        var currencySymbol = await _reads.GetBaseCurrencySymbolAsync(cancellationToken);

        var data = result.Rows.Select((row, index) => new
        {
            rowNumber = start + index + 1,
            row.SupplierId,
            title = (int)row.Title,
            row.Name,
            row.PhoneNumber,
            row.Address,
            row.City,
            row.Country,
            row.InitialBalance,
            supplierType = (int)row.SupplierType,
            supplierTypeName = row.SupplierType == PersonType.LegalEntity ? "حقوقی" : "حقیقی",
            isActive = row.IsActive,
            isDeleted = row.IsDeleted,
            row.TotalPurchase,
            row.TotalPayment,
            row.Balance,
            accountStatus = row.AccountStatus,
            accountStatusCode = row.AccountStatusCode,
        });

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            currencySymbol,
            data,
        });
    }

    [HttpPost("{id:int}/purchase-invoices/datatable")]
    [HasPermission("people.suppliers.view")]
    public async Task<IActionResult> PurchaseInvoicesDataTable(
        int id,
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var canViewDeleted = await CanViewDeletedAsync(cancellationToken);

        if (!await _reads.SupplierExistsAsync(id, canViewDeleted, cancellationToken))
        {
            return NotFound(new { message = "تأمین‌کننده یافت نشد." });
        }

        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var result = await _reads.QueryPurchaseInvoicesDataTableAsync(
            id,
            new SupplierInvoiceDataTableQuery
            {
                Start = start,
                Length = length,
                Search = request.Search?.Value,
                Order = request.Order,
            },
            cancellationToken);

        var currencySymbol = await _reads.GetBaseCurrencySymbolAsync(cancellationToken);

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            totalPurchase = result.Totals.TotalPurchase,
            totalPayment = result.Totals.TotalPayment,
            currencySymbol,
            data = result.Rows.Select((row, index) => new
            {
                rowNumber = start + index + 1,
                purchaseInvoiceId = row.PurchaseInvoiceId,
                invoiceNumber = row.InvoiceNumber,
                invoiceDate = row.InvoiceDate,
                itemsCount = row.ItemsCount,
                totalAmount = row.TotalAmount,
                paidAmount = row.PaidAmount,
                status = (int)row.Status,
                statusName = GetInvoiceStatusName(row.Status),
                isPosted = row.IsPosted,
            }),
        });
    }

    [HttpPost]
    [HasPermission("people.suppliers.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveSupplierRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var supplier = new Supplier
        {
            Title = request.Title,
            Name = request.Name.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            Country = request.Country.Trim(),
            InitialBalance = request.InitialBalance,
            SupplierType = request.SupplierType,
            CreatedBy = ResolveCurrentUserId(),
            CreatedAt = DateTime.Now,
            IsActive = request.IsActive,
            IsDeleted = false,
        };

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(Update),
            new { id = supplier.SupplierID },
            new { message = "تأمین‌کننده با موفقیت ایجاد شد." });
    }

    [HttpPut("{id:int}")]
    [HasPermission("people.suppliers.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveSupplierRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var supplier = await _db.Suppliers
            .FirstOrDefaultAsync(s => s.SupplierID == id && s.IsDeleted != true, cancellationToken);

        if (supplier is null)
        {
            return NotFound(new { message = "تأمین‌کننده یافت نشد." });
        }

        supplier.Title = request.Title;
        supplier.Name = request.Name.Trim();
        supplier.PhoneNumber = request.PhoneNumber.Trim();
        supplier.Address = request.Address.Trim();
        supplier.City = request.City.Trim();
        supplier.Country = request.Country.Trim();
        supplier.InitialBalance = request.InitialBalance;
        supplier.SupplierType = request.SupplierType;
        supplier.IsActive = request.IsActive;
        supplier.UpdatedAt = DateTime.Now;
        supplier.IsUpdated = true;
        supplier.UpdatedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "تأمین‌کننده با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("people.suppliers.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var canViewDeleted = await CanViewDeletedAsync(cancellationToken);
        var detail = await _reads.GetDetailAsync(id, canViewDeleted, cancellationToken);

        if (detail is null || detail.IsDeleted)
        {
            return NotFound(new { message = "تأمین‌کننده یافت نشد." });
        }

        if (!string.Equals(detail.AccountStatusCode, "settled", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "فقط تأمین‌کنندگان با وضعیت تسویه قابل حذف هستند." });
        }

        var supplier = await _db.Suppliers
            .FirstOrDefaultAsync(s => s.SupplierID == id && s.IsDeleted != true, cancellationToken);

        if (supplier is null)
        {
            return NotFound(new { message = "تأمین‌کننده یافت نشد." });
        }

        supplier.IsDeleted = true;
        supplier.IsActive = false;
        supplier.DeletedAt = DateTime.Now;
        supplier.DeletedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "تأمین‌کننده با موفقیت حذف شد." });
    }

    private async Task<bool> CanViewDeletedAsync(CancellationToken cancellationToken)
    {
        var access = await ResolveCurrentUserAccessAsync(cancellationToken);
        return PermissionService.HasPermission(
            access.HasFullAccess,
            access.PermissionKeys,
            ViewDeletedPermission);
    }

    private async Task<UserAccessContext> ResolveCurrentUserAccessAsync(CancellationToken cancellationToken)
    {
        var userId = ResolveCurrentUserId();
        if (userId is null)
        {
            return new UserAccessContext(false, []);
        }

        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Permissions)
            .FirstOrDefaultAsync(u => u.UserID == userId && u.IsDeleted != true, cancellationToken);

        if (user is null)
        {
            return new UserAccessContext(false, []);
        }

        var permissionKeys = user.HasFullAccess
            ? Array.Empty<string>()
            : user.Permissions.Select(p => p.PermissionKey).ToArray();

        return new UserAccessContext(user.HasFullAccess, permissionKeys);
    }

    private int? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string GetInvoiceStatusName(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Proforma => "پیش فاکتور",
        InvoiceStatus.Order => "آردر",
        InvoiceStatus.Invoice => "فاکتور",
        _ => "استعلام قیمت",
    };

    private sealed class UserAccessContext(bool hasFullAccess, IReadOnlyCollection<string> permissionKeys)
    {
        public bool HasFullAccess { get; } = hasFullAccess;
        public IReadOnlyCollection<string> PermissionKeys { get; } = permissionKeys;
    }

    public class SaveSupplierRequest
    {
        public PersonTitle Title { get; set; } = PersonTitle.Mr;

        [Required(ErrorMessage = "نام الزامی است.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "شماره تماس الزامی است.")]
        [MaxLength(50)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Country { get; set; } = string.Empty;

        public decimal InitialBalance { get; set; }

        public PersonType SupplierType { get; set; } = PersonType.NaturalPerson;

        public bool IsActive { get; set; } = true;
    }
}
