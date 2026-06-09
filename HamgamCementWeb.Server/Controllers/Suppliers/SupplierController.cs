using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.People;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Suppliers;

[ApiController]
[Route("api/suppliers")]
[Authorize]
public class SupplierController : ControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(Supplier.Name),
        [2] = nameof(Supplier.PhoneNumber),
        [3] = nameof(Supplier.City),
        [4] = nameof(Supplier.SupplierType),
        [5] = nameof(Supplier.InitialBalance),
        [6] = nameof(Supplier.IsActive),
    };

    private readonly AppDbContext _db;

    public SupplierController(AppDbContext db)
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

        var query = _db.Suppliers
            .AsNoTracking()
            .Where(s => s.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(s =>
                s.Name.Contains(searchValue) ||
                s.PhoneNumber.Contains(searchValue) ||
                s.City.Contains(searchValue) ||
                s.Address.Contains(searchValue));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var orderedQuery = ApplyOrdering(query, request.Order);
        var rows = await orderedQuery
            .Skip(start)
            .Take(length)
            .Select(s => new SupplierTableRow
            {
                SupplierId = s.SupplierID,
                Title = s.Title,
                Name = s.Name,
                PhoneNumber = s.PhoneNumber,
                Address = s.Address,
                City = s.City,
                Country = s.Country,
                InitialBalance = s.InitialBalance,
                SupplierType = s.SupplierType,
                IsActive = s.IsActive == true,
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
                r.SupplierId,
                title = (int)r.Title,
                r.Name,
                r.PhoneNumber,
                r.Address,
                r.City,
                r.Country,
                r.InitialBalance,
                supplierType = (int)r.SupplierType,
                supplierTypeName = r.SupplierType == PersonType.LegalEntity ? "حقوقی" : "حقیقی",
                r.IsActive,
            }),
        });
    }

    [HttpPost]
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
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
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

    private static IQueryable<Supplier> ApplyOrdering(
        IQueryable<Supplier> query,
        List<DataTableOrder>? orders)
    {
        if (orders is null || orders.Count == 0)
        {
            return query.OrderByDescending(s => s.CreatedAt);
        }

        IOrderedQueryable<Supplier>? ordered = null;
        foreach (var order in orders)
        {
            if (!OrderColumns.TryGetValue(order.Column, out var column))
            {
                continue;
            }

            var descending = string.Equals(order.Dir, "desc", StringComparison.OrdinalIgnoreCase);

            ordered = column switch
            {
                nameof(Supplier.Name) when ordered is null => descending
                    ? query.OrderByDescending(s => s.Name)
                    : query.OrderBy(s => s.Name),
                nameof(Supplier.Name) => descending
                    ? ordered!.ThenByDescending(s => s.Name)
                    : ordered!.ThenBy(s => s.Name),
                nameof(Supplier.PhoneNumber) when ordered is null => descending
                    ? query.OrderByDescending(s => s.PhoneNumber)
                    : query.OrderBy(s => s.PhoneNumber),
                nameof(Supplier.PhoneNumber) => descending
                    ? ordered!.ThenByDescending(s => s.PhoneNumber)
                    : ordered!.ThenBy(s => s.PhoneNumber),
                nameof(Supplier.City) when ordered is null => descending
                    ? query.OrderByDescending(s => s.City)
                    : query.OrderBy(s => s.City),
                nameof(Supplier.City) => descending
                    ? ordered!.ThenByDescending(s => s.City)
                    : ordered!.ThenBy(s => s.City),
                nameof(Supplier.SupplierType) when ordered is null => descending
                    ? query.OrderByDescending(s => s.SupplierType)
                    : query.OrderBy(s => s.SupplierType),
                nameof(Supplier.SupplierType) => descending
                    ? ordered!.ThenByDescending(s => s.SupplierType)
                    : ordered!.ThenBy(s => s.SupplierType),
                nameof(Supplier.InitialBalance) when ordered is null => descending
                    ? query.OrderByDescending(s => s.InitialBalance)
                    : query.OrderBy(s => s.InitialBalance),
                nameof(Supplier.InitialBalance) => descending
                    ? ordered!.ThenByDescending(s => s.InitialBalance)
                    : ordered!.ThenBy(s => s.InitialBalance),
                nameof(Supplier.IsActive) when ordered is null => descending
                    ? query.OrderByDescending(s => s.IsActive)
                    : query.OrderBy(s => s.IsActive),
                nameof(Supplier.IsActive) => descending
                    ? ordered!.ThenByDescending(s => s.IsActive)
                    : ordered!.ThenBy(s => s.IsActive),
                _ => ordered,
            };
        }

        return ordered ?? query.OrderByDescending(s => s.CreatedAt);
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

    public class SupplierTableRow
    {
        public int RowNumber { get; set; }
        public int SupplierId { get; set; }
        public PersonTitle Title { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public decimal InitialBalance { get; set; }
        public PersonType SupplierType { get; set; }
        public bool IsActive { get; set; }
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
