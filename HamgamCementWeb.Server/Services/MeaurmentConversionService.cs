using HamgamCementWeb.Server.Data;

using HamgamCementWeb.Server.Data.Models.Product;

using Microsoft.EntityFrameworkCore;



namespace HamgamCementWeb.Server.Services;



/// <summary>

/// تبدیل واحد فقط داخل یک خانواده — واحدهای با همان واحد پایه

/// </summary>

public interface IMeaurmentConversionService

{

    int GetRootBaseMeaurmentId(Meaurment meaurment);

    Task<Meaurment> GetBaseUnitAsync(int baseMeaurmentId, CancellationToken cancellationToken = default);

    Task<Meaurment> GetMeaurmentAsync(int meaurmentId, CancellationToken cancellationToken = default);

    void EnsureSameBaseFamily(Meaurment from, Meaurment to);

    decimal ToBaseQuantity(decimal quantity, Meaurment meaurment);

    decimal FromBaseQuantity(decimal quantityInBase, Meaurment meaurment);

    decimal Convert(decimal quantity, Meaurment from, Meaurment to);

    Task<decimal> ConvertAsync(

        decimal quantity,

        int fromMeaurmentId,

        int toMeaurmentId,

        CancellationToken cancellationToken = default);

    Task<decimal> ToBaseAsync(

        decimal quantity,

        int meaurmentId,

        CancellationToken cancellationToken = default);

    Task<decimal> FromBaseAsync(

        decimal quantityInBase,

        int meaurmentId,

        CancellationToken cancellationToken = default);

}



public class MeaurmentConversionService : IMeaurmentConversionService

{

    private readonly AppDbContext _db;



    public MeaurmentConversionService(AppDbContext db)

    {

        _db = db;

    }



    public int GetRootBaseMeaurmentId(Meaurment meaurment)

    {

        if (meaurment.IsBaseUnit)

        {

            return meaurment.MeaurmentID;

        }



        if (!meaurment.BaseMeaurmentId.HasValue)

        {

            throw new InvalidOperationException($"واحد «{meaurment.Name}» به واحد پایه متصل نیست.");

        }



        return meaurment.BaseMeaurmentId.Value;

    }



    public async Task<Meaurment> GetBaseUnitAsync(int baseMeaurmentId, CancellationToken cancellationToken = default)

    {

        var unit = await _db.Meaurments

            .AsNoTracking()

            .FirstOrDefaultAsync(

                m => m.MeaurmentID == baseMeaurmentId && m.IsDeleted != true && m.IsBaseUnit,

                cancellationToken);



        if (unit is null)

        {

            throw new InvalidOperationException("واحد پایه یافت نشد.");

        }



        return unit;

    }



    public async Task<Meaurment> GetMeaurmentAsync(int meaurmentId, CancellationToken cancellationToken = default)

    {

        var unit = await _db.Meaurments

            .AsNoTracking()

            .FirstOrDefaultAsync(m => m.MeaurmentID == meaurmentId && m.IsDeleted != true, cancellationToken);



        if (unit is null)

        {

            throw new InvalidOperationException("واحد اندازه‌گیری یافت نشد.");

        }



        return unit;

    }



    public void EnsureSameBaseFamily(Meaurment from, Meaurment to)

    {

        if (GetRootBaseMeaurmentId(from) != GetRootBaseMeaurmentId(to))

        {

            throw new InvalidOperationException("تبدیل بین خانواده‌های مختلف واحد مجاز نیست (مثلاً وزن با طول).");

        }

    }



    public decimal ToBaseQuantity(decimal quantity, Meaurment meaurment)

    {

        if (meaurment.FactorToBase <= 0)

        {

            throw new InvalidOperationException($"ضریب تبدیل واحد «{meaurment.Name}» نامعتبر است.");

        }



        return quantity * meaurment.FactorToBase;

    }



    public decimal FromBaseQuantity(decimal quantityInBase, Meaurment meaurment)

    {

        if (meaurment.FactorToBase <= 0)

        {

            throw new InvalidOperationException($"ضریب تبدیل واحد «{meaurment.Name}» نامعتبر است.");

        }



        return quantityInBase / meaurment.FactorToBase;

    }



    public decimal Convert(decimal quantity, Meaurment from, Meaurment to)

    {

        EnsureSameBaseFamily(from, to);



        if (from.MeaurmentID == to.MeaurmentID)

        {

            return quantity;

        }



        var inBase = ToBaseQuantity(quantity, from);

        return FromBaseQuantity(inBase, to);

    }



    public async Task<decimal> ConvertAsync(

        decimal quantity,

        int fromMeaurmentId,

        int toMeaurmentId,

        CancellationToken cancellationToken = default)

    {

        if (fromMeaurmentId == toMeaurmentId)

        {

            return quantity;

        }



        var from = await GetMeaurmentAsync(fromMeaurmentId, cancellationToken);

        var to = await GetMeaurmentAsync(toMeaurmentId, cancellationToken);

        return Convert(quantity, from, to);

    }



    public async Task<decimal> ToBaseAsync(

        decimal quantity,

        int meaurmentId,

        CancellationToken cancellationToken = default)

    {

        var unit = await GetMeaurmentAsync(meaurmentId, cancellationToken);

        return ToBaseQuantity(quantity, unit);

    }



    public async Task<decimal> FromBaseAsync(

        decimal quantityInBase,

        int meaurmentId,

        CancellationToken cancellationToken = default)

    {

        var unit = await GetMeaurmentAsync(meaurmentId, cancellationToken);

        return FromBaseQuantity(quantityInBase, unit);

    }

}


