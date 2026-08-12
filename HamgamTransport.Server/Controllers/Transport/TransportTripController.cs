using System.ComponentModel.DataAnnotations;
using Dapper;
using HamgamTransport.Server.Controllers.Common;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Transport;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Controllers.Transport;

[ApiController]
[Route("api/transport/trips")]
[Authorize]
public class TransportTripController : TransportControllerBase
{
    private readonly ISqlConnectionFactory _sql;
    private readonly ICurrencyConversionService _currency;
    private readonly ITripPostingService _posting;

    public TransportTripController(
        AppDbContext db,
        ISqlConnectionFactory sql,
        ICurrencyConversionService currency,
        ITripPostingService posting) : base(db)
    {
        _sql = sql;
        _currency = currency;
        _posting = posting;
    }

    [HttpPost("datatable")]
    public async Task<IActionResult> DataTable([FromBody] DataTableRequest request, CancellationToken ct)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);
        var search = request.Search?.Value?.Trim();

        await using var conn = (System.Data.Common.DbConnection)await _sql.OpenAsync(ct);
        const string baseWhere = "WHERE t.IsDeleted = 0";
        var where = baseWhere;
        var p = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += " AND (t.TripNumber LIKE @Search OR c.Name LIKE @Search OR t.Origin LIKE @Search OR t.Destination LIKE @Search)";
            p.Add("Search", $"%{search}%");
        }

        var recordsTotal = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM TransportTrips t WHERE t.IsDeleted = 0");
        var recordsFiltered = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM TransportTrips t INNER JOIN Customers c ON c.CustomerID = t.CustomerId {where}", p);
        p.Add("Offset", start);
        p.Add("Fetch", length);

        var rows = (await conn.QueryAsync(
            $"""
             SELECT t.TransportTripId AS transportTripId, t.TripNumber AS tripNumber,
                    t.TripDate AS tripDate, t.Status AS status,
                    c.Name AS customerName, t.Origin AS origin, t.Destination AS destination,
                    t.WeightTon AS weightTon, t.RatePerTon AS ratePerTon,
                    t.Amount AS amount, t.AmountInBaseCurrency AS amountInBaseCurrency,
                    t.IsRevenuePosted AS isRevenuePosted
             FROM TransportTrips t
             INNER JOIN Customers c ON c.CustomerID = t.CustomerId
             {where}
             ORDER BY t.TripDate DESC, t.TransportTripId DESC
             OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
             """, p)).ToList();

        return Ok(new { request.Draw, recordsTotal, recordsFiltered, data = rows.Select((r, i) => { var d = (IDictionary<string, object>)r; return new { rowNumber = start + i + 1, transportTripId = d["transportTripId"], tripNumber = d["tripNumber"], tripDate = d["tripDate"], status = d["status"], customerName = d["customerName"], origin = d["origin"], destination = d["destination"], weightTon = d["weightTon"], ratePerTon = d["ratePerTon"], amount = d["amount"], amountInBaseCurrency = d["amountInBaseCurrency"], isRevenuePosted = d["isRevenuePosted"] }; }) });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var trip = await Db.TransportTrips.AsNoTracking()
            .Include(t => t.Expenses.Where(e => e.IsDeleted != true))
            .ThenInclude(e => e.Category)
            .FirstOrDefaultAsync(t => t.TransportTripId == id && t.IsDeleted != true, ct);
        if (trip is null) return NotFound(new { message = "سفر یافت نشد." });

        return Ok(new
        {
            transportTripId = trip.TransportTripId,
            tripNumber = trip.TripNumber,
            tripDate = trip.TripDate,
            status = (int)trip.Status,
            customerId = trip.CustomerId,
            origin = trip.Origin,
            destination = trip.Destination,
            weightTon = trip.WeightTon,
            ratePerTon = trip.RatePerTon,
            amount = trip.Amount,
            currencyId = trip.CurrencyId,
            exchangeRate = trip.ExchangeRate,
            amountInBaseCurrency = trip.AmountInBaseCurrency,
            vehiclePairId = trip.VehiclePairId,
            primaryVehicleId = trip.PrimaryVehicleId,
            secondaryVehicleId = trip.SecondaryVehicleId,
            driverId = trip.DriverId,
            primaryOwnerSharePercent = trip.PrimaryOwnerSharePercent,
            secondaryOwnerSharePercent = trip.SecondaryOwnerSharePercent,
            driverCompensationType = (int)trip.DriverCompensationType,
            driverFixedAmount = trip.DriverFixedAmount,
            driverProfitSharePercent = trip.DriverProfitSharePercent,
            notes = trip.Notes,
            isRevenuePosted = trip.IsRevenuePosted,
            revenueJournalEntryId = trip.RevenueJournalEntryId,
            expenses = trip.Expenses.Select(e => new
            {
                tripExpenseId = e.TripExpenseId,
                tripExpenseCategoryId = e.TripExpenseCategoryId,
                categoryName = e.Category != null ? e.Category.Name : null,
                title = e.Title,
                expenseDate = e.ExpenseDate,
                amount = e.Amount,
                currencyId = e.CurrencyId,
                amountInBaseCurrency = e.AmountInBaseCurrency,
                vehicleId = e.VehicleId,
                cashBoxId = e.CashBoxId,
                bankAccountId = e.BankAccountId,
                isPosted = e.IsPosted,
                journalEntryId = e.JournalEntryId,
            }),
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TransportTripRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await ApplyPairDefaultsAsync(request, ct);

        var amount = Math.Round(request.WeightTon * request.RatePerTon, 4);
        var snapshot = await _currency.GetSnapshotAsync(request.CurrencyId, request.TripDate, ct);
        var amountBase = _currency.ConvertToBase(amount, snapshot);

        var tripNumber = await GenerateTripNumberAsync(ct);
        var entity = new TransportTrip
        {
            TripNumber = tripNumber,
            TripDate = request.TripDate,
            Status = request.Status,
            CustomerId = request.CustomerId,
            Origin = request.Origin.Trim(),
            Destination = request.Destination.Trim(),
            WeightTon = request.WeightTon,
            RatePerTon = request.RatePerTon,
            Amount = amount,
            CurrencyId = request.CurrencyId,
            ExchangeRate = request.ExchangeRate,
            AmountInBaseCurrency = amountBase,
            VehiclePairId = request.VehiclePairId,
            PrimaryVehicleId = request.PrimaryVehicleId,
            SecondaryVehicleId = request.SecondaryVehicleId,
            DriverId = request.DriverId,
            PrimaryOwnerSharePercent = request.PrimaryOwnerSharePercent,
            SecondaryOwnerSharePercent = request.SecondaryOwnerSharePercent,
            DriverCompensationType = request.DriverCompensationType,
            DriverFixedAmount = request.DriverFixedAmount,
            DriverProfitSharePercent = request.DriverProfitSharePercent,
            Notes = request.Notes,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };
        Db.TransportTrips.Add(entity);
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "سفر ثبت شد.", transportTripId = entity.TransportTripId, tripNumber });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] TransportTripRequest request, CancellationToken ct)
    {
        var entity = await Db.TransportTrips.FirstOrDefaultAsync(t => t.TransportTripId == id && t.IsDeleted != true, ct);
        if (entity is null) return NotFound();
        if (entity.IsRevenuePosted) return BadRequest(new { message = "سفر ثبت‌شده قابل ویرایش محدود است." });

        await ApplyPairDefaultsAsync(request, ct);
        entity.TripDate = request.TripDate;
        entity.Status = request.Status;
        entity.CustomerId = request.CustomerId;
        entity.Origin = request.Origin.Trim();
        entity.Destination = request.Destination.Trim();
        entity.WeightTon = request.WeightTon;
        entity.RatePerTon = request.RatePerTon;
        entity.Amount = Math.Round(request.WeightTon * request.RatePerTon, 4);
        entity.CurrencyId = request.CurrencyId;
        entity.ExchangeRate = request.ExchangeRate;
        var snapshot = await _currency.GetSnapshotAsync(request.CurrencyId, request.TripDate, ct);
        entity.AmountInBaseCurrency = _currency.ConvertToBase(entity.Amount, snapshot);
        entity.VehiclePairId = request.VehiclePairId;
        entity.PrimaryVehicleId = request.PrimaryVehicleId;
        entity.SecondaryVehicleId = request.SecondaryVehicleId;
        entity.DriverId = request.DriverId;
        entity.PrimaryOwnerSharePercent = request.PrimaryOwnerSharePercent;
        entity.SecondaryOwnerSharePercent = request.SecondaryOwnerSharePercent;
        entity.DriverCompensationType = request.DriverCompensationType;
        entity.DriverFixedAmount = request.DriverFixedAmount;
        entity.DriverProfitSharePercent = request.DriverProfitSharePercent;
        entity.Notes = request.Notes;
        entity.UpdatedAt = DateTime.Now;
        entity.UpdatedBy = ResolveCurrentUserId();
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "به‌روزرسانی شد." });
    }

    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] TripStatusRequest request, CancellationToken ct)
    {
        var entity = await Db.TransportTrips.FirstOrDefaultAsync(t => t.TransportTripId == id && t.IsDeleted != true, ct);
        if (entity is null) return NotFound();
        entity.Status = request.Status;
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "وضعیت به‌روز شد." });
    }

    [HttpPost("{id:int}/post-revenue")]
    public async Task<IActionResult> PostRevenue(int id, CancellationToken ct)
    {
        try
        {
            var entry = await _posting.PostTripRevenueAsync(id, ResolveCurrentUserId(), ct);
            return Ok(new { message = "درآمد سفر ثبت شد.", journalEntryId = entry.JournalEntryID });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/settle")]
    public async Task<IActionResult> Settle(int id, CancellationToken ct)
    {
        try
        {
            var entry = await _posting.SettleTripAsync(id, ResolveCurrentUserId(), ct);
            return Ok(new { message = entry is null ? "سفر تسویه شد (بدون سهم)." : "تسویه سفر ثبت شد.", journalEntryId = entry?.JournalEntryID });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/expenses")]
    public async Task<IActionResult> AddExpense(int id, [FromBody] TripExpenseRequest request, CancellationToken ct)
    {
        if (!await Db.TransportTrips.AnyAsync(t => t.TransportTripId == id && t.IsDeleted != true, ct))
            return NotFound(new { message = "سفر یافت نشد." });

        var snapshot = await _currency.GetSnapshotAsync(request.CurrencyId, request.ExpenseDate, ct);
        var amountBase = _currency.ConvertToBase(request.Amount, snapshot);
        var expense = new TripExpense
        {
            TransportTripId = id,
            TripExpenseCategoryId = request.TripExpenseCategoryId,
            Title = request.Title.Trim(),
            ExpenseDate = request.ExpenseDate,
            Amount = request.Amount,
            CurrencyId = request.CurrencyId,
            ExchangeRate = request.ExchangeRate,
            AmountInBaseCurrency = amountBase,
            VehicleId = request.VehicleId,
            CashBoxId = request.CashBoxId,
            BankAccountId = request.BankAccountId,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = ResolveCurrentUserId(),
        };
        Db.TripExpenses.Add(expense);
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "هزینه ثبت شد.", tripExpenseId = expense.TripExpenseId });
    }

    [HttpPost("expenses/{expenseId:int}/post")]
    public async Task<IActionResult> PostExpense(int expenseId, CancellationToken ct)
    {
        try
        {
            var entry = await _posting.PostTripExpenseAsync(expenseId, ResolveCurrentUserId(), ct);
            return Ok(new { message = "هزینه ثبت شد.", journalEntryId = entry.JournalEntryID });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await Db.TransportTrips.FirstOrDefaultAsync(t => t.TransportTripId == id && t.IsDeleted != true, ct);
        if (entity is null) return NotFound();
        if (entity.IsRevenuePosted) return BadRequest(new { message = "سفر ثبت‌شده قابل حذف نیست." });
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.DeletedBy = ResolveCurrentUserId();
        await Db.SaveChangesAsync(ct);
        return Ok(new { message = "حذف شد." });
    }

    private async Task ApplyPairDefaultsAsync(TransportTripRequest request, CancellationToken ct)
    {
        if (request.VehiclePairId is not int pairId) return;
        var pair = await Db.VehiclePairs.AsNoTracking()
            .FirstOrDefaultAsync(p => p.VehiclePairId == pairId, ct);
        if (pair is null) return;
        request.PrimaryVehicleId ??= pair.PrimaryVehicleId;
        request.SecondaryVehicleId ??= pair.SecondaryVehicleId;
        request.PrimaryOwnerSharePercent ??= pair.PrimarySharePercent;
        request.SecondaryOwnerSharePercent ??= pair.SecondarySharePercent;
    }

    private async Task<string> GenerateTripNumberAsync(CancellationToken ct)
    {
        var year = DateTime.Now.Year;
        var count = await Db.TransportTrips.CountAsync(t => t.TripDate.Year == year, ct);
        return $"TR-{year}-{(count + 1):D5}";
    }
}

public class TransportTripRequest
{
    public DateTime TripDate { get; set; } = DateTime.Now;
    public TripStatus Status { get; set; } = TripStatus.Planned;
    public int CustomerId { get; set; }
    [Required] public string Origin { get; set; } = string.Empty;
    [Required] public string Destination { get; set; } = string.Empty;
    public decimal WeightTon { get; set; }
    public decimal RatePerTon { get; set; }
    public int CurrencyId { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public int? VehiclePairId { get; set; }
    public int? PrimaryVehicleId { get; set; }
    public int? SecondaryVehicleId { get; set; }
    public int? DriverId { get; set; }
    public decimal? PrimaryOwnerSharePercent { get; set; }
    public decimal? SecondaryOwnerSharePercent { get; set; }
    public DriverCompensationType DriverCompensationType { get; set; } = DriverCompensationType.FixedAmount;
    public decimal? DriverFixedAmount { get; set; }
    public decimal? DriverProfitSharePercent { get; set; }
    public string? Notes { get; set; }
}

public class TripStatusRequest
{
    public TripStatus Status { get; set; }
}

public class TripExpenseRequest
{
    public int TripExpenseCategoryId { get; set; }
    [Required] public string Title { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; } = DateTime.Now;
    public decimal Amount { get; set; }
    public int CurrencyId { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public int? VehicleId { get; set; }
    public int? CashBoxId { get; set; }
    public int? BankAccountId { get; set; }
}
