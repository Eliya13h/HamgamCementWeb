using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Controllers.Common;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Invoice;
using HamgamCementWeb.Server.Data.Models.People;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Customers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomerController : ControllerBase
{
    private const string ViewDeletedPermission = "people.customers.viewDeleted";

    private readonly AppDbContext _db;
    private readonly ICustomerReadService _reads;
    private readonly IPartyOpeningBalanceService _opening;

    public CustomerController(
        AppDbContext db,
        ICustomerReadService reads,
        IPartyOpeningBalanceService opening)
    {
        _db = db;
        _reads = reads;
        _opening = opening;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await _reads.ListActiveAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [HasPermission("people.customers.view")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var canViewDeleted = await CanViewDeletedAsync(cancellationToken);
        var customer = await _reads.GetDetailAsync(id, canViewDeleted, cancellationToken);

        if (customer is null)
        {
            return NotFound(new { message = "مشتری یافت نشد." });
        }

        return Ok(new
        {
            customerId = customer.CustomerId,
            customer.Name,
            accountCode = customer.AccountCode,
            customer.PhoneNumber,
            customer.Address,
            customer.City,
            customer.Country,
            customer.InitialBalance,
            customerType = (int)customer.CustomerType,
            customerTypeName = customer.CustomerType == PersonType.LegalEntity ? "حقوقی" : "حقیقی",
            isActive = customer.IsActive,
            isDeleted = customer.IsDeleted,
            createdAt = customer.CreatedAt,
            totalPurchase = customer.TotalPurchase,
            totalPayment = customer.TotalPayment,
            balance = customer.Balance,
            accountStatus = customer.AccountStatus,
            accountStatusCode = customer.AccountStatusCode,
        });
    }

    [HttpPost("datatable")]
    [HasPermission("people.customers.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var canViewDeleted = await CanViewDeletedAsync(cancellationToken);
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var result = await _reads.QueryDataTableAsync(
            new CustomerDataTableQuery
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
            row.CustomerId,
            row.Name,
            accountCode = row.AccountCode,
            row.PhoneNumber,
            row.Address,
            row.City,
            row.Country,
            row.InitialBalance,
            customerType = (int)row.CustomerType,
            customerTypeName = row.CustomerType == PersonType.LegalEntity ? "حقوقی" : "حقیقی",
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

    [HttpPost("{id:int}/sale-invoices/datatable")]
    [HasPermission("people.customers.view")]
    public async Task<IActionResult> SaleInvoicesDataTable(
        int id,
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var canViewDeleted = await CanViewDeletedAsync(cancellationToken);

        if (!await _reads.CustomerExistsAsync(id, canViewDeleted, cancellationToken))
        {
            return NotFound(new { message = "مشتری یافت نشد." });
        }

        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var result = await _reads.QuerySaleInvoicesDataTableAsync(
            id,
            new CustomerInvoiceDataTableQuery
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
                saleInvoiceId = row.SaleInvoiceId,
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
    [HasPermission("people.customers.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveCustomerRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var customer = new Customer
            {
                Name = request.Name.Trim(),
                PhoneNumber = request.PhoneNumber.Trim(),
                Address = request.Address.Trim(),
                City = request.City.Trim(),
                Country = request.Country.Trim(),
                InitialBalance = request.InitialBalance,
                CustomerType = request.CustomerType,
                CreatedBy = ResolveCurrentUserId(),
                CreatedAt = DateTime.Now,
                IsActive = request.IsActive,
                IsDeleted = false,
            };

            _db.Customers.Add(customer);
            await _db.SaveChangesAsync(cancellationToken);

            if (customer.InitialBalance != 0)
            {
                await _opening.PostCustomerOpeningAsync(
                    customer.CustomerID,
                    customer.Name,
                    customer.InitialBalance,
                    DateTime.Today,
                    ResolveCurrentUserId(),
                    cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);

            return CreatedAtAction(
                nameof(Update),
                new { id = customer.CustomerID },
                new { message = "مشتری با موفقیت ایجاد شد.", customerId = customer.CustomerID });
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return BadRequest(new { message = ex.Message });
        }
    }

    // ثبت صریح مانده اولیه مشتری در دفترروزنامه
    [HttpPost("{id:int}/opening-balance")]
    [HasPermission("people.customers.edit")]
    public async Task<IActionResult> PostOpeningBalance(int id, CancellationToken cancellationToken)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.CustomerID == id && c.IsDeleted != true, cancellationToken);
        if (customer is null)
        {
            return NotFound(new { message = "مشتری یافت نشد." });
        }

        try
        {
            var journal = await _opening.PostCustomerOpeningAsync(
                customer.CustomerID,
                customer.Name,
                customer.InitialBalance,
                DateTime.Today,
                ResolveCurrentUserId(),
                cancellationToken);
            return Ok(new
            {
                message = "مانده اولیه مشتری در دفتر ثبت شد.",
                journalEntryId = journal.JournalEntryID,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [HasPermission("people.customers.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveCustomerRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.CustomerID == id && c.IsDeleted != true, cancellationToken);

        if (customer is null)
        {
            return NotFound(new { message = "مشتری یافت نشد." });
        }

        var balanceChanged = customer.InitialBalance != request.InitialBalance;
        var userId = ResolveCurrentUserId();

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            customer.Name = request.Name.Trim();
            customer.PhoneNumber = request.PhoneNumber.Trim();
            customer.Address = request.Address.Trim();
            customer.City = request.City.Trim();
            customer.Country = request.Country.Trim();
            customer.InitialBalance = request.InitialBalance;
            customer.CustomerType = request.CustomerType;
            customer.IsActive = request.IsActive;
            customer.UpdatedAt = DateTime.Now;
            customer.IsUpdated = true;
            customer.UpdatedBy = userId;
            await _db.SaveChangesAsync(cancellationToken);

            if (balanceChanged)
            {
                await _opening.SyncCustomerOpeningAsync(
                    customer.CustomerID,
                    customer.Name,
                    customer.InitialBalance,
                    userId,
                    cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return Ok(new { message = "مشتری با موفقیت ویرایش شد." });
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [HasPermission("people.customers.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.CustomerID == id && c.IsDeleted != true, cancellationToken);

        if (customer is null)
        {
            return NotFound(new { message = "مشتری یافت نشد." });
        }

        if (await _opening.HasCustomerGlActivityAsync(id, cancellationToken))
        {
            return Conflict(new { message = "مشتری گردش حسابداری دارد و قابل حذف نیست." });
        }

        var userId = ResolveCurrentUserId();
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _opening.ReverseCustomerOpeningAsync(id, userId, cancellationToken);

            customer.IsDeleted = true;
            customer.IsActive = false;
            customer.DeletedAt = DateTime.Now;
            customer.DeletedBy = userId;
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return Ok(new { message = "مشتری با موفقیت حذف شد." });
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return BadRequest(new { message = ex.Message });
        }
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

    public class SaveCustomerRequest
    {
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

        public PersonType CustomerType { get; set; } = PersonType.NaturalPerson;

        public bool IsActive { get; set; } = true;
    }
}
