using System.ComponentModel.DataAnnotations;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Data.Models.Transport;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Transport;

/// <summary>
/// کنترلر نگهداشت وسایل نقلیه: تعمیرات و تعویض قطعات
/// </summary>
[ApiController]
[Route("api/transport")]
[Authorize]
public class VehicleCareController : TransportControllerBase
{
    private readonly IOperationalGlService _gl;
    private readonly IFinanceCategoryService _financeCategories;
    private readonly IJournalPostingService _journal;
    private readonly ICashBoxService _cashBoxes;

    public VehicleCareController(
        AppDbContext db,
        IOperationalGlService gl,
        IFinanceCategoryService financeCategories,
        IJournalPostingService journal,
        ICashBoxService cashBoxes) : base(db)
    {
        _gl = gl;
        _financeCategories = financeCategories;
        _journal = journal;
        _cashBoxes = cashBoxes;
    }

    // ---------- تعمیرات و نگهداری ----------

    private static readonly Dictionary<int, string> MaintenanceOrderColumns = new()
    {
        [2] = nameof(VehicleMaintenance.Title),
        [3] = nameof(VehicleMaintenance.MaintenanceDate),
        [4] = nameof(VehicleMaintenance.OdometerKm),
        [5] = nameof(VehicleMaintenance.Cost),
        [6] = nameof(VehicleMaintenance.NextServiceDate),
    };

    [HttpPost("maintenances/datatable")]
    [HasPermission("transport.maintenance.view")]
    public async Task<IActionResult> MaintenancesDataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.VehicleMaintenances
            .AsNoTracking()
            .Where(m => m.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(m =>
                m.Title.Contains(searchValue) ||
                (m.WorkshopName != null && m.WorkshopName.Contains(searchValue)) ||
                (m.Vehicle != null && (m.Vehicle.Code.Contains(searchValue) || m.Vehicle.PlateNumber.Contains(searchValue))));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, MaintenanceOrderColumns, nameof(VehicleMaintenance.CreatedAt))
            .Skip(start)
            .Take(length)
            .Select(m => new
            {
                vehicleMaintenanceId = m.VehicleMaintenanceID,
                vehicleId = m.VehicleId,
                vehicleLabel = m.Vehicle != null ? m.Vehicle.Code + " — " + m.Vehicle.PlateNumber : string.Empty,
                title = m.Title,
                maintenanceDate = m.MaintenanceDate,
                odometerKm = m.OdometerKm,
                cost = m.Cost,
                workshopName = m.WorkshopName,
                nextServiceDate = m.NextServiceDate,
                description = m.Description,
                journalEntryId = m.JournalEntryId,
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
                r.vehicleMaintenanceId,
                r.vehicleId,
                r.vehicleLabel,
                r.title,
                r.maintenanceDate,
                r.odometerKm,
                r.cost,
                r.workshopName,
                r.nextServiceDate,
                r.description,
            }),
        });
    }

    [HttpPost("maintenances")]
    [HasPermission("transport.maintenance.create")]
    public async Task<IActionResult> CreateMaintenance(
        [FromBody] SaveMaintenanceRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = ResolveCurrentUserId();
        var maintenance = new VehicleMaintenance
        {
            VehicleId = request.VehicleId,
            Title = request.Title.Trim(),
            MaintenanceDate = request.MaintenanceDate,
            OdometerKm = request.OdometerKm,
            Cost = request.Cost,
            WorkshopName = request.WorkshopName?.Trim(),
            NextServiceDate = request.NextServiceDate,
            Description = request.Description?.Trim(),
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = userId,
        };

        Db.VehicleMaintenances.Add(maintenance);
        await Db.SaveChangesAsync(cancellationToken);

        if (maintenance.Cost > 0)
        {
            try
            {
                await PostMaintenanceExpenseAsync(maintenance, userId, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        return Ok(new
        {
            message = "تعمیر/سرویس با موفقیت ثبت شد.",
            vehicleMaintenanceId = maintenance.VehicleMaintenanceID,
            journalEntryId = maintenance.JournalEntryId,
        });
    }

    [HttpPut("maintenances/{id:int}")]
    [HasPermission("transport.maintenance.edit")]
    public async Task<IActionResult> UpdateMaintenance(
        int id,
        [FromBody] SaveMaintenanceRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = await Db.VehicleMaintenances
            .FirstOrDefaultAsync(m => m.VehicleMaintenanceID == id && m.IsDeleted != true, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "رکورد تعمیر یافت نشد." });
        }

        var userId = ResolveCurrentUserId();
        var previousCost = entity.Cost;

        entity.VehicleId = request.VehicleId;
        entity.Title = request.Title.Trim();
        entity.MaintenanceDate = request.MaintenanceDate;
        entity.OdometerKm = request.OdometerKm;
        entity.Cost = request.Cost;
        entity.WorkshopName = request.WorkshopName?.Trim();
        entity.NextServiceDate = request.NextServiceDate;
        entity.Description = request.Description?.Trim();
        entity.IsUpdated = true;
        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = userId;

        if (previousCost > 0 && entity.ExpenseId is not null)
        {
            await ReverseMaintenanceExpenseAsync(entity, userId, cancellationToken);
        }

        if (entity.Cost > 0)
        {
            try
            {
                await PostMaintenanceExpenseAsync(entity, userId, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "تعمیر/سرویس با موفقیت ویرایش شد.",
            journalEntryId = entity.JournalEntryId,
        });
    }

    [HttpDelete("maintenances/{id:int}")]
    [HasPermission("transport.maintenance.delete")]
    public async Task<IActionResult> DeleteMaintenance(int id, CancellationToken cancellationToken)
    {
        var entity = await Db.VehicleMaintenances
            .FirstOrDefaultAsync(m => m.VehicleMaintenanceID == id && m.IsDeleted != true, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "رکورد تعمیر یافت نشد." });
        }

        var userId = ResolveCurrentUserId();
        if (entity.ExpenseId is not null)
        {
            await ReverseMaintenanceExpenseAsync(entity, userId, cancellationToken);
        }

        SoftDelete(entity);
        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "رکورد تعمیر با موفقیت حذف شد." });
    }

    private async Task PostMaintenanceExpenseAsync(
        VehicleMaintenance maintenance,
        int? userId,
        CancellationToken cancellationToken)
    {
        var categoryId = await _financeCategories.GetExpenseCategoryIdAsync(
            FinanceCategoryCode.TransportExpense,
            cancellationToken);
        var baseCurrencyId = await ResolveBaseCurrencyIdAsync(cancellationToken);

        var vehicleLabel = await Db.Vehicles
            .AsNoTracking()
            .Where(v => v.VehicleID == maintenance.VehicleId)
            .Select(v => v.Code + " — " + v.PlateNumber)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var title = string.IsNullOrWhiteSpace(vehicleLabel)
            ? $"تعمیر وسیله — {maintenance.Title}"
            : $"تعمیر {vehicleLabel} — {maintenance.Title}";

        var expense = new Expense
        {
            Title = title,
            ExpenseDate = maintenance.MaintenanceDate,
            ExpenseCategoryId = categoryId,
            Source = FinancialEntrySource.TransportExpense,
            CurrencyId = baseCurrencyId,
            BaseCurrencyId = baseCurrencyId,
            BaseUnitsPerUnitAtTransaction = 1m,
            Amount = maintenance.Cost,
            AmountInBaseCurrency = maintenance.Cost,
            Description = maintenance.Description,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = userId,
        };

        Db.Expenses.Add(expense);
        await Db.SaveChangesAsync(cancellationToken);

        var cashBoxId = await _cashBoxes.ResolveUserCashBoxIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("برای ثبت هزینه تعمیر، صندوق کاربر الزامی است.");

        var journal = await _gl.PostMiscExpenseAsync(expense, userId, cashBoxId, cancellationToken);
        expense.JournalEntryId = journal.JournalEntryID;
        maintenance.ExpenseId = expense.ExpenseID;
        maintenance.JournalEntryId = journal.JournalEntryID;
        await Db.SaveChangesAsync(cancellationToken);
    }

    private async Task ReverseMaintenanceExpenseAsync(
        VehicleMaintenance maintenance,
        int? userId,
        CancellationToken cancellationToken)
    {
        if (maintenance.ExpenseId is not int expenseId)
        {
            return;
        }

        await _journal.ReverseBySourceAsync(JournalSource.Expense, expenseId, userId, cancellationToken: cancellationToken);

        var expense = await Db.Expenses
            .FirstOrDefaultAsync(e => e.ExpenseID == expenseId && e.IsDeleted != true, cancellationToken);
        if (expense is not null)
        {
            expense.IsDeleted = true;
            expense.IsActive = false;
            expense.DeletedAt = DateTime.Now;
            expense.DeletedBy = userId;
        }

        maintenance.ExpenseId = null;
        maintenance.JournalEntryId = null;
    }

    private async Task<int> ResolveBaseCurrencyIdAsync(CancellationToken cancellationToken)
    {
        var baseId = await Db.Currencies
            .Where(c => c.IsBaseCurrency && c.IsDeleted != true)
            .Select(c => c.CurrencyID)
            .FirstOrDefaultAsync(cancellationToken);

        if (baseId != 0)
        {
            return baseId;
        }

        return await Db.Currencies
            .Where(c => c.IsDeleted != true)
            .OrderBy(c => c.CurrencyID)
            .Select(c => c.CurrencyID)
            .FirstAsync(cancellationToken);
    }

    // ---------- تعویض قطعات و لوازم مصرفی ----------

    private static readonly Dictionary<int, string> PartOrderColumns = new()
    {
        [2] = nameof(VehiclePartReplacement.PartName),
        [3] = nameof(VehiclePartReplacement.Quantity),
        [4] = nameof(VehiclePartReplacement.UnitCost),
        [5] = nameof(VehiclePartReplacement.TotalCost),
        [6] = nameof(VehiclePartReplacement.ReplacementDate),
    };

    [HttpPost("parts/datatable")]
    [HasPermission("transport.maintenance.view")]
    public async Task<IActionResult> PartsDataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        var query = Db.VehiclePartReplacements
            .AsNoTracking()
            .Where(p => p.IsDeleted != true);

        var recordsTotal = await query.CountAsync(cancellationToken);

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(p =>
                p.PartName.Contains(searchValue) ||
                (p.Vehicle != null && (p.Vehicle.Code.Contains(searchValue) || p.Vehicle.PlateNumber.Contains(searchValue))));
        }

        var recordsFiltered = await query.CountAsync(cancellationToken);

        var rows = await query
            .ApplyDataTableOrder(request.Order, PartOrderColumns, nameof(VehiclePartReplacement.CreatedAt))
            .Skip(start)
            .Take(length)
            .Select(p => new
            {
                vehiclePartReplacementId = p.VehiclePartReplacementID,
                vehicleId = p.VehicleId,
                vehicleLabel = p.Vehicle != null ? p.Vehicle.Code + " — " + p.Vehicle.PlateNumber : string.Empty,
                partName = p.PartName,
                quantity = p.Quantity,
                unitCost = p.UnitCost,
                totalCost = p.TotalCost,
                replacementDate = p.ReplacementDate,
                odometerKm = p.OdometerKm,
                description = p.Description,
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
                r.vehiclePartReplacementId,
                r.vehicleId,
                r.vehicleLabel,
                r.partName,
                r.quantity,
                r.unitCost,
                r.totalCost,
                r.replacementDate,
                r.odometerKm,
                r.description,
            }),
        });
    }

    [HttpPost("parts")]
    [HasPermission("transport.maintenance.create")]
    public async Task<IActionResult> CreatePart(
        [FromBody] SavePartRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = ResolveCurrentUserId();
        var totalCost = request.Quantity * request.UnitCost;

        await using var tx = await Db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var entity = new VehiclePartReplacement
            {
                VehicleId = request.VehicleId,
                PartName = request.PartName.Trim(),
                Quantity = request.Quantity,
                UnitCost = request.UnitCost,
                TotalCost = totalCost,
                ReplacementDate = request.ReplacementDate,
                OdometerKm = request.OdometerKm,
                Description = request.Description?.Trim(),
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreatedBy = userId,
            };

            Db.VehiclePartReplacements.Add(entity);
            await Db.SaveChangesAsync(cancellationToken);

            if (totalCost > 0)
            {
                await PostPartExpenseAsync(entity, userId, cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return Ok(new { message = "تعویض قطعه با موفقیت ثبت شد." });
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("parts/{id:int}")]
    [HasPermission("transport.maintenance.edit")]
    public async Task<IActionResult> UpdatePart(
        int id,
        [FromBody] SavePartRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = await Db.VehiclePartReplacements
            .FirstOrDefaultAsync(p => p.VehiclePartReplacementID == id && p.IsDeleted != true, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "رکورد قطعه یافت نشد." });
        }

        var userId = ResolveCurrentUserId();
        var totalCost = request.Quantity * request.UnitCost;

        await using var tx = await Db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await ReversePartExpenseAsync(entity, userId, cancellationToken);

            entity.VehicleId = request.VehicleId;
            entity.PartName = request.PartName.Trim();
            entity.Quantity = request.Quantity;
            entity.UnitCost = request.UnitCost;
            entity.TotalCost = totalCost;
            entity.ReplacementDate = request.ReplacementDate;
            entity.OdometerKm = request.OdometerKm;
            entity.Description = request.Description?.Trim();
            entity.IsUpdated = true;
            entity.UpdatedAt = DateTime.Now;
            entity.UpdatedBy = userId;
            await Db.SaveChangesAsync(cancellationToken);

            if (totalCost > 0)
            {
                await PostPartExpenseAsync(entity, userId, cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return Ok(new { message = "تعویض قطعه با موفقیت ویرایش شد." });
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("parts/{id:int}")]
    [HasPermission("transport.maintenance.delete")]
    public async Task<IActionResult> DeletePart(int id, CancellationToken cancellationToken)
    {
        var entity = await Db.VehiclePartReplacements
            .FirstOrDefaultAsync(p => p.VehiclePartReplacementID == id && p.IsDeleted != true, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "رکورد قطعه یافت نشد." });
        }

        var userId = ResolveCurrentUserId();
        await using var tx = await Db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await ReversePartExpenseAsync(entity, userId, cancellationToken);
            SoftDelete(entity);
            await Db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return Ok(new { message = "رکورد قطعه با موفقیت حذف شد." });
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task PostPartExpenseAsync(
        VehiclePartReplacement part,
        int? userId,
        CancellationToken cancellationToken)
    {
        var categoryId = await _financeCategories.GetExpenseCategoryIdAsync(
            FinanceCategoryCode.TransportExpense,
            cancellationToken);
        var baseCurrencyId = await ResolveBaseCurrencyIdAsync(cancellationToken);
        var cashBoxId = await _cashBoxes.ResolveUserCashBoxIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("برای ثبت هزینه تعویض قطعه، صندوق کاربر الزامی است.");

        var vehicleLabel = await Db.Vehicles
            .AsNoTracking()
            .Where(v => v.VehicleID == part.VehicleId)
            .Select(v => v.Code + " — " + v.PlateNumber)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var title = string.IsNullOrWhiteSpace(vehicleLabel)
            ? $"تعویض قطعه — {part.PartName}"
            : $"تعویض قطعه {vehicleLabel} — {part.PartName}";

        var expense = new Expense
        {
            Title = title,
            ExpenseDate = part.ReplacementDate,
            ExpenseCategoryId = categoryId,
            Source = FinancialEntrySource.TransportExpense,
            CurrencyId = baseCurrencyId,
            BaseCurrencyId = baseCurrencyId,
            BaseUnitsPerUnitAtTransaction = 1m,
            Amount = part.TotalCost,
            AmountInBaseCurrency = part.TotalCost,
            Description = part.Description,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = userId,
        };

        Db.Expenses.Add(expense);
        await Db.SaveChangesAsync(cancellationToken);

        var journal = await _gl.PostMiscExpenseAsync(expense, userId, cashBoxId, cancellationToken);
        expense.JournalEntryId = journal.JournalEntryID;
        part.ExpenseId = expense.ExpenseID;
        part.JournalEntryId = journal.JournalEntryID;
        await Db.SaveChangesAsync(cancellationToken);
    }

    private async Task ReversePartExpenseAsync(
        VehiclePartReplacement part,
        int? userId,
        CancellationToken cancellationToken)
    {
        if (part.ExpenseId is not int expenseId)
        {
            return;
        }

        await _journal.ReverseBySourceAsync(JournalSource.Expense, expenseId, userId, cancellationToken: cancellationToken);

        var expense = await Db.Expenses
            .FirstOrDefaultAsync(e => e.ExpenseID == expenseId && e.IsDeleted != true, cancellationToken);
        if (expense is not null)
        {
            expense.IsDeleted = true;
            expense.IsActive = false;
            expense.DeletedAt = DateTime.Now;
            expense.DeletedBy = userId;
            expense.JournalEntryId = null;
        }

        part.ExpenseId = null;
        part.JournalEntryId = null;
    }

    private void SoftDelete(Data.Models.BaseEntity entity)
    {
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = DateTime.Now;
        entity.DeletedBy = ResolveCurrentUserId();
    }

    public class SaveMaintenanceRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "انتخاب وسیله نقلیه الزامی است.")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "عنوان تعمیر الزامی است.")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاریخ تعمیر الزامی است.")]
        public DateTime MaintenanceDate { get; set; }

        public decimal? OdometerKm { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "هزینه نامعتبر است.")]
        public decimal Cost { get; set; }

        [MaxLength(200)]
        public string? WorkshopName { get; set; }

        public DateTime? NextServiceDate { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
    }

    public class SavePartRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "انتخاب وسیله نقلیه الزامی است.")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "نام قطعه الزامی است.")]
        [MaxLength(300)]
        public string PartName { get; set; } = string.Empty;

        [Range(0.0001, double.MaxValue, ErrorMessage = "تعداد باید بزرگ‌تر از صفر باشد.")]
        public decimal Quantity { get; set; } = 1;

        [Range(0, double.MaxValue, ErrorMessage = "قیمت واحد نامعتبر است.")]
        public decimal UnitCost { get; set; }

        [Required(ErrorMessage = "تاریخ تعویض الزامی است.")]
        public DateTime ReplacementDate { get; set; }

        public decimal? OdometerKm { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
    }
}
