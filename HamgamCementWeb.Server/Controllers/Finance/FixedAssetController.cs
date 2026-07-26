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
[Route("api/finance/fixed-assets")]
[Authorize]
public class FixedAssetController : FinanceControllerBase
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = nameof(FixedAsset.Code),
        [2] = nameof(FixedAsset.Name),
        [4] = nameof(FixedAsset.AcquisitionDate),
        [5] = nameof(FixedAsset.CostAmountInBaseCurrency),
        [8] = nameof(FixedAsset.Status),
    };

    private readonly ICurrencyConversionService _currency;
    private readonly IFixedAssetPostingService _posting;
    private readonly ICashBoxService _cashBoxes;

    public FixedAssetController(
        AppDbContext db,
        ICurrencyConversionService currency,
        IFixedAssetPostingService posting,
        ICashBoxService cashBoxes) : base(db)
    {
        _currency = currency;
        _posting = posting;
        _cashBoxes = cashBoxes;
    }

    [HttpPost("datatable")]
    [HasPermission("accounting.fixed-assets.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.FixedAssets
            .AsNoTracking()
            .Where(a => a.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(a =>
                a.Code.Contains(searchValue) ||
                a.Name.Contains(searchValue) ||
                a.Category.Name.Contains(searchValue) ||
                (a.Description != null && a.Description.Contains(searchValue)));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, OrderColumns, nameof(FixedAsset.CreatedAt))
            .Skip(start)
            .Take(length)
            .Select(a => new
            {
                fixedAssetId = a.FixedAssetID,
                code = a.Code,
                name = a.Name,
                fixedAssetCategoryId = a.FixedAssetCategoryId,
                categoryName = a.Category.Name,
                acquisitionDate = a.AcquisitionDate,
                costAmount = a.CostAmount,
                costAmountInBaseCurrency = a.CostAmountInBaseCurrency,
                salvageValue = a.SalvageValue,
                salvageValueInBaseCurrency = a.SalvageValueInBaseCurrency,
                usefulLifeMonths = a.UsefulLifeMonths,
                accumulatedDepreciationInBaseCurrency = a.AccumulatedDepreciationInBaseCurrency,
                bookValueInBaseCurrency = a.CostAmountInBaseCurrency - a.AccumulatedDepreciationInBaseCurrency,
                status = (int)a.Status,
                currencyId = a.CurrencyId,
                supplierId = a.SupplierId,
                supplierName = a.Supplier != null ? a.Supplier.Name : null,
                description = a.Description,
                acquisitionJournalEntryId = a.AcquisitionJournalEntryId,
                disposalDate = a.DisposalDate,
                disposalAmount = a.DisposalAmount,
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
                r.fixedAssetId,
                r.code,
                r.name,
                r.fixedAssetCategoryId,
                r.categoryName,
                acquisitionDate = r.acquisitionDate.ToString("yyyy-MM-dd"),
                r.costAmount,
                r.costAmountInBaseCurrency,
                r.salvageValue,
                r.salvageValueInBaseCurrency,
                r.usefulLifeMonths,
                r.accumulatedDepreciationInBaseCurrency,
                r.bookValueInBaseCurrency,
                r.status,
                statusLabel = StatusLabel(r.status),
                r.currencyId,
                r.supplierId,
                r.supplierName,
                r.description,
                r.acquisitionJournalEntryId,
                disposalDate = r.disposalDate?.ToString("yyyy-MM-dd"),
                r.disposalAmount,
                canDepreciate = r.status == (int)FixedAssetStatus.Active
                    && r.bookValueInBaseCurrency > r.salvageValueInBaseCurrency + 0.01m,
                canDispose = r.status != (int)FixedAssetStatus.Disposed,
            }),
        });
    }

    [HttpPost]
    [HasPermission("accounting.fixed-assets.create")]
    public async Task<IActionResult> Create(
        [FromBody] SaveFixedAssetRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.CostAmount <= 0)
        {
            return BadRequest(new { message = "بهای تمام‌شده باید بزرگ‌تر از صفر باشد." });
        }

        if (request.UsefulLifeMonths <= 0)
        {
            return BadRequest(new { message = "عمر مفید باید بزرگ‌تر از صفر باشد." });
        }

        if (request.SalvageValue < 0 || request.SalvageValue >= request.CostAmount)
        {
            return BadRequest(new { message = "ارزش اسقاط باید بین صفر و بهای تمام‌شده باشد." });
        }

        var category = await Db.FixedAssetCategories
            .FirstOrDefaultAsync(
                c => c.FixedAssetCategoryID == request.FixedAssetCategoryId && c.IsDeleted != true && c.IsActive == true,
                cancellationToken);
        if (category is null)
        {
            return BadRequest(new { message = "دسته‌بندی دارایی معتبر نیست." });
        }

        if (category.AssetAccountId is null or 0)
        {
            return BadRequest(new { message = "برای این دسته حساب دارایی تعریف نشده است." });
        }

        if (request.SupplierId is int supplierId)
        {
            var exists = await Db.Suppliers
                .AnyAsync(s => s.SupplierID == supplierId && s.IsDeleted != true, cancellationToken);
            if (!exists)
            {
                return BadRequest(new { message = "تأمین‌کننده یافت نشد." });
            }
        }

        var acquisitionDate = request.AcquisitionDate?.Date ?? DateTime.Now.Date;
        var snapshot = await _currency.GetSnapshotAsync(request.CurrencyId, acquisitionDate, cancellationToken);
        var costInBase = _currency.ConvertToBase(request.CostAmount, snapshot);
        var salvageInBase = _currency.ConvertToBase(request.SalvageValue, snapshot);

        var code = string.IsNullOrWhiteSpace(request.Code)
            ? await NextAssetCodeAsync(cancellationToken)
            : request.Code.Trim();

        if (await Db.FixedAssets.AnyAsync(a => a.Code == code && a.IsDeleted != true, cancellationToken))
        {
            return Conflict(new { message = "کد دارایی تکراری است." });
        }

        var asset = new FixedAsset
        {
            Code = code,
            Name = request.Name.Trim(),
            FixedAssetCategoryId = request.FixedAssetCategoryId,
            AcquisitionDate = acquisitionDate,
            SupplierId = request.SupplierId,
            CurrencyId = snapshot.CurrencyId,
            BaseCurrencyId = snapshot.BaseCurrencyId,
            ExchangeHistoryId = snapshot.ExchangeHistoryId,
            BaseUnitsPerUnitAtTransaction = snapshot.BaseUnitsPerUnit,
            CostAmount = request.CostAmount,
            CostAmountInBaseCurrency = costInBase,
            SalvageValue = request.SalvageValue,
            SalvageValueInBaseCurrency = salvageInBase,
            UsefulLifeMonths = request.UsefulLifeMonths,
            DepreciationMethod = DepreciationMethod.StraightLine,
            AccumulatedDepreciationInBaseCurrency = 0,
            Status = FixedAssetStatus.Active,
            Description = request.Description?.Trim(),
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };

        Db.FixedAssets.Add(asset);
        await Db.SaveChangesAsync(cancellationToken);

        try
        {
            var userId = ResolveCurrentUserId();
            var cashBoxId = await _cashBoxes.ResolveUserCashBoxIdAsync(userId, cancellationToken);
            var journal = await _posting.PostAcquisitionAsync(asset, userId, cashBoxId, cancellationToken);
            asset.AcquisitionJournalEntryId = journal.JournalEntryID;
            await Db.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                message = "دارایی ثابت ثبت و سند حسابداری صادر شد.",
                fixedAssetId = asset.FixedAssetID,
                journalEntryId = journal.JournalEntryID,
            });
        }
        catch (Exception ex)
        {
            asset.IsDeleted = true;
            asset.IsActive = false;
            asset.DeletedAt = DateTime.Now;
            asset.DeletedBy = ResolveCurrentUserId();
            await Db.SaveChangesAsync(cancellationToken);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [HasPermission("accounting.fixed-assets.edit")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveFixedAssetRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var asset = await Db.FixedAssets
            .FirstOrDefaultAsync(a => a.FixedAssetID == id && a.IsDeleted != true, cancellationToken);
        if (asset is null)
        {
            return NotFound(new { message = "دارایی یافت نشد." });
        }

        if (asset.Status == FixedAssetStatus.Disposed)
        {
            return Conflict(new { message = "دارایی فروخته/اسقاط‌شده قابل ویرایش نیست." });
        }

        // بعد از ثبت خرید فقط مشخصات غیرمالی قابل ویرایش است
        asset.Name = request.Name.Trim();
        asset.Description = request.Description?.Trim();
        if (request.UsefulLifeMonths > 0)
        {
            asset.UsefulLifeMonths = request.UsefulLifeMonths;
        }

        asset.IsUpdated = true;
        asset.UpdatedAt = DateTime.Now;
        asset.UpdatedBy = ResolveCurrentUserId();
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "دارایی ویرایش شد." });
    }

    [HttpDelete("{id:int}")]
    [HasPermission("accounting.fixed-assets.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var asset = await Db.FixedAssets
            .FirstOrDefaultAsync(a => a.FixedAssetID == id && a.IsDeleted != true, cancellationToken);
        if (asset is null)
        {
            return NotFound(new { message = "دارایی یافت نشد." });
        }

        if (asset.Status == FixedAssetStatus.Disposed)
        {
            return Conflict(new { message = "دارایی فروخته/اسقاط‌شده قابل حذف نیست." });
        }

        var hasDep = await Db.FixedAssetDepreciations
            .AnyAsync(d => d.FixedAssetId == id && d.IsDeleted != true, cancellationToken);
        if (hasDep || asset.AccumulatedDepreciationInBaseCurrency > 0)
        {
            return Conflict(new { message = "دارایی دارای استهلاک است؛ به‌جای حذف از فروش/اسقاط استفاده کنید." });
        }

        var userId = ResolveCurrentUserId();
        await _posting.SoftDeleteAcquisitionAsync(id, userId, cancellationToken);

        asset.IsDeleted = true;
        asset.IsActive = false;
        asset.DeletedAt = DateTime.Now;
        asset.DeletedBy = userId;
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "دارایی و سند خرید آن حذف شد." });
    }

    // اجرای استهلاک ماهانه برای همه دارایی‌های فعال یک دوره شمسی
    [HttpPost("depreciate-period")]
    [HasPermission("accounting.fixed-assets.create")]
    public async Task<IActionResult> DepreciatePeriod(
        [FromBody] DepreciatePeriodRequest request,
        CancellationToken cancellationToken)
    {
        var asOf = request.AsOfDate?.Date ?? DateTime.Now.Date;
        var (year, month) = request.PeriodSolarYear is > 0 && request.PeriodMonth is >= 1 and <= 12
            ? (request.PeriodSolarYear.Value, request.PeriodMonth.Value)
            : FixedAssetPostingService.GetSolarPeriod(asOf);

        var assets = await Db.FixedAssets
            .Where(a => a.IsDeleted != true && a.Status == FixedAssetStatus.Active)
            .ToListAsync(cancellationToken);

        var userId = ResolveCurrentUserId();
        var posted = 0;
        var skipped = 0;

        foreach (var asset in assets)
        {
            if (asset.AcquisitionDate.Date > asOf)
            {
                skipped++;
                continue;
            }

            var already = await Db.FixedAssetDepreciations
                .AnyAsync(
                    d => d.FixedAssetId == asset.FixedAssetID
                         && d.PeriodSolarYear == year
                         && d.PeriodMonth == month
                         && d.IsDeleted != true,
                    cancellationToken);
            if (already)
            {
                skipped++;
                continue;
            }

            var remaining = asset.CostAmountInBaseCurrency
                            - asset.SalvageValueInBaseCurrency
                            - asset.AccumulatedDepreciationInBaseCurrency;
            if (remaining <= 0.01m)
            {
                if (asset.Status == FixedAssetStatus.Active)
                {
                    asset.Status = FixedAssetStatus.FullyDepreciated;
                }

                skipped++;
                continue;
            }

            var monthly = _posting.CalculateMonthlyDepreciationInBase(asset);
            var amount = Math.Min(monthly, remaining);
            if (amount <= 0.01m)
            {
                skipped++;
                continue;
            }

            var dep = new FixedAssetDepreciation
            {
                FixedAssetId = asset.FixedAssetID,
                PeriodSolarYear = year,
                PeriodMonth = month,
                DepreciationDate = asOf,
                Amount = amount,
                AmountInBaseCurrency = amount,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreatedBy = userId,
            };

            Db.FixedAssetDepreciations.Add(dep);
            await Db.SaveChangesAsync(cancellationToken);

            try
            {
                var journal = await _posting.PostDepreciationAsync(dep, asset, userId, cancellationToken);
                dep.JournalEntryId = journal.JournalEntryID;
                asset.AccumulatedDepreciationInBaseCurrency += amount;
                if (asset.CostAmountInBaseCurrency - asset.AccumulatedDepreciationInBaseCurrency
                    <= asset.SalvageValueInBaseCurrency + 0.01m)
                {
                    asset.Status = FixedAssetStatus.FullyDepreciated;
                }

                await Db.SaveChangesAsync(cancellationToken);
                posted++;
            }
            catch
            {
                dep.IsDeleted = true;
                dep.DeletedAt = DateTime.Now;
                dep.DeletedBy = userId;
                await Db.SaveChangesAsync(cancellationToken);
                throw;
            }
        }

        return Ok(new
        {
            message = $"استهلاک دوره {year}/{month:00} انجام شد.",
            periodSolarYear = year,
            periodMonth = month,
            posted,
            skipped,
        });
    }

    [HttpPost("{id:int}/dispose")]
    [HasPermission("accounting.fixed-assets.edit")]
    public async Task<IActionResult> Dispose(
        int id,
        [FromBody] DisposeFixedAssetRequest request,
        CancellationToken cancellationToken)
    {
        var asset = await Db.FixedAssets
            .FirstOrDefaultAsync(a => a.FixedAssetID == id && a.IsDeleted != true, cancellationToken);
        if (asset is null)
        {
            return NotFound(new { message = "دارایی یافت نشد." });
        }

        if (asset.Status == FixedAssetStatus.Disposed)
        {
            return Conflict(new { message = "این دارایی قبلاً فروخته/اسقاط شده است." });
        }

        var disposalDate = request.DisposalDate?.Date ?? DateTime.Now.Date;
        if (disposalDate < asset.AcquisitionDate.Date)
        {
            return BadRequest(new { message = "تاریخ فروش نمی‌تواند قبل از تاریخ خرید باشد." });
        }

        var proceeds = request.DisposalAmount ?? 0m;
        if (proceeds < 0)
        {
            return BadRequest(new { message = "مبلغ فروش نمی‌تواند منفی باشد." });
        }

        var snapshot = await _currency.GetSnapshotAsync(asset.CurrencyId, disposalDate, cancellationToken);
        var proceedsBase = _currency.ConvertToBase(proceeds, snapshot);

        asset.DisposalDate = disposalDate;
        asset.DisposalAmount = proceeds;
        asset.DisposalAmountInBaseCurrency = proceedsBase;

        var userId = ResolveCurrentUserId();
        var cashBoxId = await _cashBoxes.ResolveUserCashBoxIdAsync(userId, cancellationToken);

        try
        {
            var journal = await _posting.PostDisposalAsync(asset, userId, cashBoxId, cancellationToken);
            asset.DisposalJournalEntryId = journal.JournalEntryID;
            asset.Status = FixedAssetStatus.Disposed;
            asset.IsUpdated = true;
            asset.UpdatedAt = DateTime.Now;
            asset.UpdatedBy = userId;
            await Db.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                message = "فروش/اسقاط دارایی ثبت شد.",
                journalEntryId = journal.JournalEntryID,
            });
        }
        catch (Exception ex)
        {
            asset.DisposalDate = null;
            asset.DisposalAmount = null;
            asset.DisposalAmountInBaseCurrency = null;
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<string> NextAssetCodeAsync(CancellationToken cancellationToken)
    {
        var year = JalaliDateHelper.GetSolarYear(DateTime.Now);
        var prefix = $"FA-{year}-";
        var last = await Db.FixedAssets
            .Where(a => a.Code.StartsWith(prefix))
            .OrderByDescending(a => a.Code)
            .Select(a => a.Code)
            .FirstOrDefaultAsync(cancellationToken);

        var next = 1;
        if (last is not null && last.Length > prefix.Length
            && int.TryParse(last[prefix.Length..], out var n))
        {
            next = n + 1;
        }

        return $"{prefix}{next:0000}";
    }

    private static string StatusLabel(int status) => status switch
    {
        (int)FixedAssetStatus.Active => "فعال",
        (int)FixedAssetStatus.FullyDepreciated => "کاملاً مستهلک",
        (int)FixedAssetStatus.Disposed => "فروخته/اسقاط",
        _ => status.ToString(),
    };

    public class SaveFixedAssetRequest
    {
        [MaxLength(50)]
        public string? Code { get; set; }

        [Required, MaxLength(300)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int FixedAssetCategoryId { get; set; }

        public DateTime? AcquisitionDate { get; set; }

        public int? SupplierId { get; set; }

        [Required]
        public int CurrencyId { get; set; }

        [Range(0.0001, double.MaxValue)]
        public decimal CostAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal SalvageValue { get; set; }

        [Range(1, 1200)]
        public int UsefulLifeMonths { get; set; } = 60;

        [MaxLength(2000)]
        public string? Description { get; set; }
    }

    public class DepreciatePeriodRequest
    {
        public DateTime? AsOfDate { get; set; }
        public int? PeriodSolarYear { get; set; }
        public int? PeriodMonth { get; set; }
    }

    public class DisposeFixedAssetRequest
    {
        public DateTime? DisposalDate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? DisposalAmount { get; set; }
    }
}
