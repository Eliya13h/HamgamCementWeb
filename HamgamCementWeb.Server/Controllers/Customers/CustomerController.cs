using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.People;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Customers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomerController : ControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(Customer.Name),
        [2] = nameof(Customer.PhoneNumber),
        [3] = nameof(Customer.City),
        [4] = nameof(Customer.CustomerType),
        [5] = nameof(Customer.InitialBalance),
        [6] = nameof(Customer.IsActive),
    };

    private readonly AppDbContext _db;

    public CustomerController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("datatable")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var draw = request.Draw;
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = _db.Customers
            .AsNoTracking()
            .Where(c => c.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(c =>
                c.Name.Contains(searchValue) ||
                c.PhoneNumber.Contains(searchValue) ||
                c.City.Contains(searchValue) ||
                c.Address.Contains(searchValue));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var orderedQuery = ApplyOrdering(query, request.Order);
        var rows = await orderedQuery
            .Skip(start)
            .Take(length)
            .Select(c => new CustomerTableRow
            {
                CustomerId = c.CustomerID,
                Name = c.Name,
                PhoneNumber = c.PhoneNumber,
                Address = c.Address,
                City = c.City,
                Country = c.Country,
                InitialBalance = c.InitialBalance,
                CustomerType = c.CustomerType,
                IsActive = c.IsActive == true,
            })
            .ToListAsync(cancellationToken);

        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].RowNumber = start + i + 1;
        }

        return Ok(new
        {
            draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select(r => new
            {
                r.RowNumber,
                r.CustomerId,
                r.Name,
                r.PhoneNumber,
                r.Address,
                r.City,
                r.Country,
                r.InitialBalance,
                customerType = (int)r.CustomerType,
                customerTypeName = r.CustomerType == PersonType.LegalEntity ? "حقوقی" : "حقیقی",
                r.IsActive,
            }),
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveCustomerRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

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

        return CreatedAtAction(
            nameof(Update),
            new { id = customer.CustomerID },
            new { message = "مشتری با موفقیت ایجاد شد." });
    }

    [HttpPut("{id:int}")]
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
        customer.UpdatedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "مشتری با موفقیت ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.CustomerID == id && c.IsDeleted != true, cancellationToken);

        if (customer is null)
        {
            return NotFound(new { message = "مشتری یافت نشد." });
        }

        customer.IsDeleted = true;
        customer.IsActive = false;
        customer.DeletedAt = DateTime.Now;
        customer.DeletedBy = ResolveCurrentUserId();

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "مشتری با موفقیت حذف شد." });
    }

    private static IQueryable<Customer> ApplyOrdering(
        IQueryable<Customer> query,
        List<DataTableOrder>? orders)
    {
        if (orders is null || orders.Count == 0)
        {
            return query.OrderByDescending(c => c.CreatedAt);
        }

        IOrderedQueryable<Customer>? ordered = null;
        foreach (var order in orders)
        {
            if (!OrderColumns.TryGetValue(order.Column, out var column))
            {
                continue;
            }

            var descending = string.Equals(order.Dir, "desc", StringComparison.OrdinalIgnoreCase);

            ordered = column switch
            {
                nameof(Customer.Name) when ordered is null => descending
                    ? query.OrderByDescending(c => c.Name)
                    : query.OrderBy(c => c.Name),
                nameof(Customer.Name) => descending
                    ? ordered!.ThenByDescending(c => c.Name)
                    : ordered!.ThenBy(c => c.Name),
                nameof(Customer.PhoneNumber) when ordered is null => descending
                    ? query.OrderByDescending(c => c.PhoneNumber)
                    : query.OrderBy(c => c.PhoneNumber),
                nameof(Customer.PhoneNumber) => descending
                    ? ordered!.ThenByDescending(c => c.PhoneNumber)
                    : ordered!.ThenBy(c => c.PhoneNumber),
                nameof(Customer.City) when ordered is null => descending
                    ? query.OrderByDescending(c => c.City)
                    : query.OrderBy(c => c.City),
                nameof(Customer.City) => descending
                    ? ordered!.ThenByDescending(c => c.City)
                    : ordered!.ThenBy(c => c.City),
                nameof(Customer.CustomerType) when ordered is null => descending
                    ? query.OrderByDescending(c => c.CustomerType)
                    : query.OrderBy(c => c.CustomerType),
                nameof(Customer.CustomerType) => descending
                    ? ordered!.ThenByDescending(c => c.CustomerType)
                    : ordered!.ThenBy(c => c.CustomerType),
                nameof(Customer.InitialBalance) when ordered is null => descending
                    ? query.OrderByDescending(c => c.InitialBalance)
                    : query.OrderBy(c => c.InitialBalance),
                nameof(Customer.InitialBalance) => descending
                    ? ordered!.ThenByDescending(c => c.InitialBalance)
                    : ordered!.ThenBy(c => c.InitialBalance),
                nameof(Customer.IsActive) when ordered is null => descending
                    ? query.OrderByDescending(c => c.IsActive)
                    : query.OrderBy(c => c.IsActive),
                nameof(Customer.IsActive) => descending
                    ? ordered!.ThenByDescending(c => c.IsActive)
                    : ordered!.ThenBy(c => c.IsActive),
                _ => ordered,
            };
        }

        return ordered ?? query.OrderByDescending(c => c.CreatedAt);
    }

    private int? ResolveCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public DataTableSearch? Search { get; set; }
        public List<DataTableOrder>? Order { get; set; }
    }

    public class DataTableSearch
    {
        public string? Value { get; set; }
        public bool Regex { get; set; }
    }

    public class DataTableOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; } = "asc";
    }

    public class CustomerTableRow
    {
        public int RowNumber { get; set; }
        public int CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public decimal InitialBalance { get; set; }
        public PersonType CustomerType { get; set; }
        public bool IsActive { get; set; }
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
