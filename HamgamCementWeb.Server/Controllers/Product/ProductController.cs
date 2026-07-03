using System.ComponentModel.DataAnnotations;
using HamgamCementWeb.Server.Controllers.Transport;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Product;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Product;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductController : ProductControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(Data.Models.Product.Product.Code),
        [2] = nameof(Data.Models.Product.Product.Name),
        [3] = nameof(Data.Models.Product.Product.DefaultPurchasePrice),
        [4] = nameof(Data.Models.Product.Product.DefaultSalePrice),
    };

    private readonly IMeaurmentConversionService _conversion;

    public ProductController(AppDbContext db, IMeaurmentConversionService conversion) : base(db)
    {
        _conversion = conversion;
    }

    [HttpPost("datatable")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.Products
            .AsNoTracking()
            .Where(p => p.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(p =>
                p.Code.Contains(searchValue) ||
                p.Name.Contains(searchValue) ||
                (p.Description != null && p.Description.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(Data.Models.Product.Product.CreatedAt))
            .Skip(start)
            .Take(length)
            .Select(p => new
            {
                productId = p.ProductID,
                code = p.Code,
                name = p.Name,
                baseMeaurmentName = p.BaseMeaurment.Name,
                defaultPurchasePrice = p.DefaultPurchasePrice,
                defaultSalePrice = p.DefaultSalePrice,
                minStockQuantity = p.MinStockQuantity,
                totalStockQuantity = Db.InventoryStocks
                    .Where(s => s.ProductId == p.ProductID && s.IsDeleted != true)
                    .Sum(s => (decimal?)s.QuantityInBase) ?? 0m,
                categories = p.ProductCategories
                    .Where(pc => pc.IsDeleted != true)
                    .Select(pc => pc.Category.Name)
                    .ToList(),
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
                r.productId,
                r.code,
                r.name,
                r.baseMeaurmentName,
                r.defaultPurchasePrice,
                r.defaultSalePrice,
                r.minStockQuantity,
                r.totalStockQuantity,
                isBelowMinStock = r.minStockQuantity > 0 && r.totalStockQuantity < r.minStockQuantity,
                categoriesText = string.Join("، ", r.categories),
            }),
        });
    }

    [HttpGet("list")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await Db.Products
            .AsNoTracking()
            .Where(p => p.IsDeleted != true && p.IsActive == true)
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                value = p.ProductID,
                label = $"{p.Code} — {p.Name}",
                baseMeaurmentId = p.BaseMeaurmentId,
                defaultMeaurmentId = p.DefaultMeaurmentId,
                defaultPurchasePrice = p.DefaultPurchasePrice,
                defaultSalePrice = p.DefaultSalePrice,
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("next-code-preview")]
    public async Task<IActionResult> NextCodePreview(CancellationToken cancellationToken)
    {
        var nextId = (await Db.Products.MaxAsync(p => (int?)p.ProductID, cancellationToken) ?? 0) + 1;
        return Ok(new { code = ProductCodeHelper.ForProduct(nextId) });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await Db.Products
            .AsNoTracking()
            .Where(p => p.ProductID == id && p.IsDeleted != true)
            .Select(p => new
            {
                productId = p.ProductID,
                code = p.Code,
                name = p.Name,
                description = p.Description,
                baseMeaurmentId = p.BaseMeaurmentId,
                defaultMeaurmentId = p.DefaultMeaurmentId,
                defaultPurchasePrice = p.DefaultPurchasePrice,
                defaultSalePrice = p.DefaultSalePrice,
                minStockQuantity = p.MinStockQuantity,
                categoryIds = p.ProductCategories
                    .Where(pc => pc.IsDeleted != true)
                    .Select(pc => pc.CategoryId)
                    .ToList(),
                meaurmentIds = p.ProductMeaurments
                    .Where(pm => pm.IsDeleted != true)
                    .Select(pm => pm.MeaurmentId)
                    .ToList(),
                isActive = p.IsActive == true,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            return NotFound(new { message = "محصول یافت نشد." });
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveProductRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var validationError = await ValidateMeaurmentsAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var baseExists = await Db.Meaurments
            .AnyAsync(
                m => m.MeaurmentID == request.BaseMeaurmentId &&
                     m.IsDeleted != true &&
                     m.IsBaseUnit,
                cancellationToken);
        if (!baseExists)
        {
            return BadRequest(new { message = "واحد پایه یافت نشد." });
        }

        var product = new Data.Models.Product.Product
        {
            Code = "TEMP",
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            BaseMeaurmentId = request.BaseMeaurmentId,
            DefaultMeaurmentId = request.DefaultMeaurmentId,
            DefaultPurchasePrice = request.DefaultPurchasePrice,
            DefaultSalePrice = request.DefaultSalePrice,
            MinStockQuantity = request.MinStockQuantity,
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };

        Db.Products.Add(product);
        await Db.SaveChangesAsync(cancellationToken);

        product.Code = ProductCodeHelper.ForProduct(product.ProductID);
        await SyncProductRelationsAsync(product.ProductID, request, cancellationToken);

        return Ok(new
        {
            message = "محصول با موفقیت ایجاد شد.",
            productId = product.ProductID,
            code = product.Code,
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveProductRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = await Db.Products
            .Include(p => p.ProductMeaurments)
            .Include(p => p.ProductCategories)
            .FirstOrDefaultAsync(p => p.ProductID == id && p.IsDeleted != true, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "محصول یافت نشد." });
        }

        if (entity.BaseMeaurmentId != request.BaseMeaurmentId)
        {
            return BadRequest(new { message = "تغییر واحد پایه محصول پس از ثبت مجاز نیست." });
        }

        var validationError = await ValidateMeaurmentsAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.DefaultMeaurmentId = request.DefaultMeaurmentId;
        entity.DefaultPurchasePrice = request.DefaultPurchasePrice;
        entity.DefaultSalePrice = request.DefaultSalePrice;
        entity.MinStockQuantity = request.MinStockQuantity;
        entity.IsActive = request.IsActive;
        entity.IsUpdated = true;
        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = ResolveCurrentUserId();

        await SyncProductRelationsAsync(id, request, cancellationToken);
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "محصول با موفقیت ویرایش شد.", code = entity.Code });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await Db.Products
            .FirstOrDefaultAsync(p => p.ProductID == id && p.IsDeleted != true, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "محصول یافت نشد." });
        }

        var inStock = await Db.InventoryStocks
            .AnyAsync(s => s.ProductId == id && s.IsDeleted != true && s.QuantityInBase > 0, cancellationToken);
        if (inStock)
        {
            return Conflict(new { message = "این محصول دارای موجودی انبار است و قابل حذف نیست." });
        }

        var hasLots = await Db.InventoryLots
            .AnyAsync(l => l.ProductId == id && l.IsDeleted != true && l.RemainingQuantityInBase > 0, cancellationToken);
        if (hasLots)
        {
            return Conflict(new { message = "این محصول دارای Lot فعال است و قابل حذف نیست." });
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = DateTime.Now;
        entity.DeletedBy = ResolveCurrentUserId();

        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "محصول با موفقیت حذف شد." });
    }

    [HttpPost("convert")]
    public async Task<IActionResult> Convert(
        [FromBody] ConvertQuantityRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _conversion.ConvertAsync(
                request.Quantity,
                request.FromMeaurmentId,
                request.ToMeaurmentId,
                cancellationToken);

            return Ok(new
            {
                quantity = request.Quantity,
                fromMeaurmentId = request.FromMeaurmentId,
                toMeaurmentId = request.ToMeaurmentId,
                convertedQuantity = result,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<string?> ValidateMeaurmentsAsync(
        SaveProductRequest request,
        CancellationToken cancellationToken)
    {
        if (request.MeaurmentIds is null || request.MeaurmentIds.Count == 0)
        {
            return "حداقل یک واحد اندازه‌گیری برای محصول انتخاب کنید.";
        }

        var units = await Db.Meaurments
            .AsNoTracking()
            .Where(m => request.MeaurmentIds.Contains(m.MeaurmentID) && m.IsDeleted != true)
            .ToListAsync(cancellationToken);

        if (units.Count != request.MeaurmentIds.Count)
        {
            return "یکی از واحدهای انتخاب‌شده نامعتبر است.";
        }

        if (units.Any(u => _conversion.GetRootBaseMeaurmentId(u) != request.BaseMeaurmentId))
        {
            return "همه واحدهای محصول باید از یک واحد پایه باشند.";
        }

        if (request.DefaultMeaurmentId.HasValue &&
            !request.MeaurmentIds.Contains(request.DefaultMeaurmentId.Value))
        {
            return "واحد پیش‌فرض باید در لیست واحدهای محصول باشد.";
        }

        return null;
    }

    private async Task SyncProductRelationsAsync(
        int productId,
        SaveProductRequest request,
        CancellationToken cancellationToken)
    {
        var meaurmentIds = request.MeaurmentIds.Distinct().ToList();
        var defaultId = request.DefaultMeaurmentId ?? meaurmentIds.First();

        var existingMeaurments = await Db.ProductMeaurments
            .Where(pm => pm.ProductId == productId)
            .ToListAsync(cancellationToken);

        foreach (var pm in existingMeaurments)
        {
            if (!meaurmentIds.Contains(pm.MeaurmentId))
            {
                pm.IsDeleted = true;
                pm.DeletedAt = DateTime.Now;
                pm.DeletedBy = ResolveCurrentUserId();
            }
            else
            {
                pm.IsDeleted = false;
                pm.IsDefault = pm.MeaurmentId == defaultId;
            }
        }

        foreach (var meaurmentId in meaurmentIds)
        {
            if (!existingMeaurments.Any(pm => pm.MeaurmentId == meaurmentId))
            {
                Db.ProductMeaurments.Add(new ProductMeaurment
                {
                    ProductId = productId,
                    MeaurmentId = meaurmentId,
                    IsDefault = meaurmentId == defaultId,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now,
                    CreatedBy = ResolveCurrentUserId(),
                });
            }
        }

        var categoryIds = (request.CategoryIds ?? []).Distinct().ToList();
        var existingCategories = await Db.ProductCategories
            .Where(pc => pc.ProductId == productId)
            .ToListAsync(cancellationToken);

        foreach (var pc in existingCategories)
        {
            if (!categoryIds.Contains(pc.CategoryId))
            {
                pc.IsDeleted = true;
                pc.DeletedAt = DateTime.Now;
                pc.DeletedBy = ResolveCurrentUserId();
            }
            else
            {
                pc.IsDeleted = false;
            }
        }

        foreach (var categoryId in categoryIds)
        {
            if (!existingCategories.Any(pc => pc.CategoryId == categoryId))
            {
                Db.ProductCategories.Add(new ProductCategory
                {
                    ProductId = productId,
                    CategoryId = categoryId,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now,
                    CreatedBy = ResolveCurrentUserId(),
                });
            }
        }

        var product = await Db.Products.FindAsync([productId], cancellationToken);
        if (product is not null)
        {
            product.DefaultMeaurmentId = defaultId;
        }

        await Db.SaveChangesAsync(cancellationToken);
    }

    public class SaveProductRequest
    {
        [Required(ErrorMessage = "نام الزامی است.")]
        [MaxLength(300)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        public int BaseMeaurmentId { get; set; }

        public int? DefaultMeaurmentId { get; set; }

        public decimal DefaultPurchasePrice { get; set; }

        public decimal DefaultSalePrice { get; set; }

        public decimal MinStockQuantity { get; set; }

        public List<int> MeaurmentIds { get; set; } = [];

        public List<int>? CategoryIds { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class ConvertQuantityRequest
    {
        public decimal Quantity { get; set; }

        [Required]
        public int FromMeaurmentId { get; set; }

        [Required]
        public int ToMeaurmentId { get; set; }
    }
}
