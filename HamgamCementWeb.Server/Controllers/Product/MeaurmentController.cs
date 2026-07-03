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

[Route("api/products/meaurments")]

[Authorize]

public class MeaurmentController : ProductControllerBase

{

    private static readonly Dictionary<int, string> OrderColumns = new()

    {

        [1] = nameof(Meaurment.IsBaseUnit),

        [2] = "BaseUnitName",

        [3] = nameof(Meaurment.Name),

        [4] = nameof(Meaurment.Symbol),

        [5] = nameof(Meaurment.FactorToBase),

        [6] = "ProductsCount",

        [7] = nameof(Meaurment.IsActive),

    };



    private readonly IMeaurmentConversionService _conversion;



    public MeaurmentController(AppDbContext db, IMeaurmentConversionService conversion) : base(db)

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



        var query = Db.Meaurments

            .AsNoTracking()

            .Where(m => m.IsDeleted != true);



        var recordsTotal = await query.CountAsync(cancellationToken);



        var searchValue = request.Search?.Value?.Trim();

        if (!string.IsNullOrWhiteSpace(searchValue))

        {

            query = query.Where(m =>

                m.Name.Contains(searchValue) ||

                (m.Symbol != null && m.Symbol.Contains(searchValue)) ||

                (m.BaseMeaurment != null && m.BaseMeaurment.Name.Contains(searchValue)));

        }



        var recordsFiltered = await query.CountAsync(cancellationToken);



        var rows = await query

            .OrderByDescending(m => m.IsBaseUnit)

            .ThenBy(m => m.BaseMeaurment != null ? m.BaseMeaurment.Name : m.Name)

            .ThenBy(m => m.FactorToBase)

            .Skip(start)

            .Take(length)

            .Select(m => new

            {

                meaurmentId = m.MeaurmentID,

                baseMeaurmentId = m.IsBaseUnit ? m.MeaurmentID : m.BaseMeaurmentId,

                baseUnitName = m.IsBaseUnit

                    ? m.Name

                    : (m.BaseMeaurment != null ? m.BaseMeaurment.Name : string.Empty),

                name = m.Name,

                symbol = m.Symbol,

                factorToBase = m.FactorToBase,

                isBaseUnit = m.IsBaseUnit,

                productsCount = m.ProductMeaurments.Count(pm => pm.IsDeleted != true),

                isActive = m.IsActive == true,

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

                r.meaurmentId,

                r.baseMeaurmentId,

                r.baseUnitName,

                r.name,

                r.symbol,

                r.factorToBase,

                r.isBaseUnit,

                r.productsCount,

                r.isActive,

            }),

        });

    }



    [HttpGet("list/base-units")]

    public async Task<IActionResult> ListBaseUnits(CancellationToken cancellationToken)

    {

        var items = await Db.Meaurments

            .AsNoTracking()

            .Where(m => m.IsDeleted != true && m.IsActive == true && m.IsBaseUnit)

            .OrderBy(m => m.Name)

            .Select(m => new { value = m.MeaurmentID, label = m.Name, symbol = m.Symbol })

            .ToListAsync(cancellationToken);



        return Ok(items);

    }



    [HttpGet("list")]

    public async Task<IActionResult> List(

        [FromQuery] int? baseMeaurmentId,

        CancellationToken cancellationToken)

    {

        var query = Db.Meaurments

            .AsNoTracking()

            .Where(m => m.IsDeleted != true && m.IsActive == true);



        if (baseMeaurmentId is > 0)

        {

            query = query.Where(m =>

                (m.IsBaseUnit && m.MeaurmentID == baseMeaurmentId) ||

                m.BaseMeaurmentId == baseMeaurmentId);

        }



        var items = await query

            .OrderByDescending(m => m.IsBaseUnit)

            .ThenBy(m => m.FactorToBase)

            .Select(m => new

            {

                value = m.MeaurmentID,

                label = m.Name,

                symbol = m.Symbol,

                factorToBase = m.FactorToBase,

                isBaseUnit = m.IsBaseUnit,

                baseMeaurmentId = m.IsBaseUnit ? m.MeaurmentID : m.BaseMeaurmentId,

            })

            .ToListAsync(cancellationToken);



        return Ok(items);

    }



    [HttpGet("ratios")]

    public async Task<IActionResult> Ratios(

        [FromQuery] int? baseMeaurmentId,

        CancellationToken cancellationToken)

    {

        var query = Db.Meaurments

            .AsNoTracking()

            .Where(m => m.IsDeleted != true && m.IsActive == true);



        if (baseMeaurmentId is > 0)

        {

            query = query.Where(m =>

                (m.IsBaseUnit && m.MeaurmentID == baseMeaurmentId) ||

                m.BaseMeaurmentId == baseMeaurmentId);

        }



        var units = await query

            .OrderBy(m => m.BaseMeaurmentId)

            .ThenBy(m => m.FactorToBase)

            .ToListAsync(cancellationToken);



        var ratios = new List<object>();

        foreach (var familyUnits in units.GroupBy(u => _conversion.GetRootBaseMeaurmentId(u)))

        {

            var familyList = familyUnits.ToList();

            var baseUnit = familyList.First(u => u.IsBaseUnit);

            var baseName = baseUnit.Name;



            foreach (var from in familyList)

            {

                foreach (var to in familyList)

                {

                    if (from.MeaurmentID == to.MeaurmentID)

                    {

                        continue;

                    }



                    var converted = _conversion.Convert(1, from, to);

                    ratios.Add(new

                    {

                        baseMeaurmentId = baseUnit.MeaurmentID,

                        baseUnitName = baseName,

                        fromMeaurmentId = from.MeaurmentID,

                        fromName = from.Name,

                        toMeaurmentId = to.MeaurmentID,

                        toName = to.Name,

                        ratio = converted,

                        description = $"[{baseName}] ۱ {from.Name} = {converted:N4} {to.Name}",

                    });

                }

            }

        }



        return Ok(ratios);

    }



    [HttpPost]

    public async Task<IActionResult> Create(

        [FromBody] SaveMeaurmentRequest request,

        CancellationToken cancellationToken)

    {

        if (!ModelState.IsValid)

        {

            return ValidationProblem(ModelState);

        }



        var validationError = await ValidateRequestAsync(request, null, cancellationToken);

        if (validationError is not null)

        {

            return BadRequest(new { message = validationError });

        }



        var name = request.Name.Trim();

        var isBaseUnit = request.IsBaseUnit;



        if (isBaseUnit)

        {

            var baseExists = await Db.Meaurments

                .AnyAsync(m => m.IsDeleted != true && m.IsBaseUnit && m.Name == name, cancellationToken);

            if (baseExists)

            {

                return Conflict(new { message = "واحد پایه با این نام قبلاً ثبت شده است." });

            }

        }

        else

        {

            var derivedExists = await Db.Meaurments

                .AnyAsync(

                    m => m.IsDeleted != true &&

                         !m.IsBaseUnit &&

                         m.BaseMeaurmentId == request.BaseMeaurmentId &&

                         m.Name == name,

                    cancellationToken);

            if (derivedExists)

            {

                return Conflict(new { message = "واحد با این نام در این خانواده قبلاً ثبت شده است." });

            }

        }



        Db.Meaurments.Add(new Meaurment

        {

            Name = name,

            Symbol = request.Symbol?.Trim(),

            IsBaseUnit = isBaseUnit,

            BaseMeaurmentId = isBaseUnit ? null : request.BaseMeaurmentId,

            FactorToBase = isBaseUnit ? 1 : request.FactorToBase,

            IsActive = request.IsActive,

            IsDeleted = false,

            CreatedAt = DateTime.Now,

            CreatedBy = ResolveCurrentUserId(),

        });



        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "واحد با موفقیت ایجاد شد." });

    }



    [HttpPut("{id:int}")]

    public async Task<IActionResult> Update(

        int id,

        [FromBody] SaveMeaurmentRequest request,

        CancellationToken cancellationToken)

    {

        if (!ModelState.IsValid)

        {

            return ValidationProblem(ModelState);

        }



        var entity = await Db.Meaurments

            .FirstOrDefaultAsync(m => m.MeaurmentID == id && m.IsDeleted != true, cancellationToken);

        if (entity is null)

        {

            return NotFound(new { message = "واحد یافت نشد." });

        }



        if (entity.IsBaseUnit != request.IsBaseUnit)

        {

            return BadRequest(new { message = "تغییر نوع واحد (پایه/مشتق) پس از ثبت مجاز نیست." });

        }



        if (!entity.IsBaseUnit && entity.BaseMeaurmentId != request.BaseMeaurmentId)

        {

            return BadRequest(new { message = "تغییر واحد پایه خانواده پس از ثبت مجاز نیست." });

        }



        var validationError = await ValidateRequestAsync(request, id, cancellationToken);

        if (validationError is not null)

        {

            return BadRequest(new { message = validationError });

        }



        var name = request.Name.Trim();



        if (entity.IsBaseUnit)

        {

            var baseExists = await Db.Meaurments

                .AnyAsync(

                    m => m.IsDeleted != true && m.IsBaseUnit && m.Name == name && m.MeaurmentID != id,

                    cancellationToken);

            if (baseExists)

            {

                return Conflict(new { message = "واحد پایه با این نام قبلاً ثبت شده است." });

            }

        }

        else

        {

            var derivedExists = await Db.Meaurments

                .AnyAsync(

                    m => m.IsDeleted != true &&

                         !m.IsBaseUnit &&

                         m.BaseMeaurmentId == entity.BaseMeaurmentId &&

                         m.Name == name &&

                         m.MeaurmentID != id,

                    cancellationToken);

            if (derivedExists)

            {

                return Conflict(new { message = "واحد با این نام در این خانواده قبلاً ثبت شده است." });

            }



            entity.FactorToBase = request.FactorToBase;

        }



        entity.Name = name;

        entity.Symbol = request.Symbol?.Trim();

        entity.IsActive = request.IsActive;

        entity.IsUpdated = true;

        entity.UpdatedAt = DateTime.Now;

        entity.UpdatedBy = ResolveCurrentUserId();



        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "واحد با موفقیت ویرایش شد." });

    }



    [HttpDelete("{id:int}")]

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)

    {

        var entity = await Db.Meaurments

            .FirstOrDefaultAsync(m => m.MeaurmentID == id && m.IsDeleted != true, cancellationToken);

        if (entity is null)

        {

            return NotFound(new { message = "واحد یافت نشد." });

        }



        if (entity.IsBaseUnit)

        {

            var hasDerived = await Db.Meaurments

                .AnyAsync(m => m.IsDeleted != true && m.BaseMeaurmentId == id, cancellationToken);

            if (hasDerived)

            {

                return Conflict(new { message = "این واحد پایه دارای واحد مشتق است و قابل حذف نیست." });

            }



            var inProducts = await Db.Products

                .AnyAsync(p => p.BaseMeaurmentId == id && p.IsDeleted != true, cancellationToken);

            if (inProducts)

            {

                return Conflict(new { message = "این واحد پایه به محصولات متصل است و قابل حذف نیست." });

            }

        }



        var inUse = await Db.ProductMeaurments

            .AnyAsync(pm => pm.MeaurmentId == id && pm.IsDeleted != true, cancellationToken);

        if (inUse)

        {

            return Conflict(new { message = "این واحد به محصولات متصل است و قابل حذف نیست." });

        }



        entity.IsDeleted = true;

        entity.IsActive = false;

        entity.DeletedAt = DateTime.Now;

        entity.DeletedBy = ResolveCurrentUserId();



        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "واحد با موفقیت حذف شد." });

    }



    private async Task<string?> ValidateRequestAsync(

        SaveMeaurmentRequest request,

        int? excludeId,

        CancellationToken cancellationToken)

    {

        if (string.IsNullOrWhiteSpace(request.Name))

        {

            return "نام واحد الزامی است.";

        }



        if (request.IsBaseUnit)

        {

            return null;

        }



        if (request.BaseMeaurmentId is not > 0)

        {

            return "واحد پایه خانواده را انتخاب کنید.";

        }



        var baseExists = await Db.Meaurments

            .AnyAsync(

                m => m.MeaurmentID == request.BaseMeaurmentId &&

                     m.IsDeleted != true &&

                     m.IsBaseUnit,

                cancellationToken);

        if (!baseExists)

        {

            return "واحد پایه انتخاب‌شده یافت نشد.";

        }



        if (request.FactorToBase <= 0)

        {

            return "ضریب تبدیل باید بزرگ‌تر از صفر باشد.";

        }



        return null;

    }



    public class SaveMeaurmentRequest

    {

        [Required(ErrorMessage = "نام الزامی است.")]

        [MaxLength(100)]

        public string Name { get; set; } = string.Empty;



        [MaxLength(20)]

        public string? Symbol { get; set; }



        public bool IsBaseUnit { get; set; }



        public int? BaseMeaurmentId { get; set; }



        public decimal FactorToBase { get; set; } = 1;



        public bool IsActive { get; set; } = true;

    }

}


