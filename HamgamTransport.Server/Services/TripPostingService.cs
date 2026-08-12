using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using HamgamTransport.Server.Data.Models.Transport;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Services;

public interface ITripPostingService
{
    Task<JournalEntry> PostTripRevenueAsync(int tripId, int? userId, CancellationToken cancellationToken = default);
    Task<JournalEntry> PostTripExpenseAsync(int tripExpenseId, int? userId, CancellationToken cancellationToken = default);
    Task<JournalEntry?> SettleTripAsync(int tripId, int? userId, CancellationToken cancellationToken = default);
}

public class TripPostingService : ITripPostingService
{
    private readonly AppDbContext _db;
    private readonly IJournalPostingService _journal;
    private readonly IAccountLookupService _accounts;
    private readonly IOperationalGlService _gl;
    private readonly ICashBalanceService _cashBalances;

    public TripPostingService(
        AppDbContext db,
        IJournalPostingService journal,
        IAccountLookupService accounts,
        IOperationalGlService gl,
        ICashBalanceService cashBalances)
    {
        _db = db;
        _journal = journal;
        _accounts = accounts;
        _gl = gl;
        _cashBalances = cashBalances;
    }

    public async Task<JournalEntry> PostTripRevenueAsync(
        int tripId,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var trip = await _db.TransportTrips
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.TransportTripId == tripId && t.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("سفر یافت نشد.");

        if (trip.IsRevenuePosted)
        {
            throw new InvalidOperationException("درآمد این سفر قبلاً ثبت شده است.");
        }

        if (trip.Status == TripStatus.Cancelled)
        {
            throw new InvalidOperationException("سفر لغوشده قابل ثبت درآمد نیست.");
        }

        var customerName = trip.Customer?.Name ?? "مشتری";
        var ar = await _accounts.EnsureCustomerAccountAsync(trip.CustomerId, customerName, cancellationToken);
        var revenueAccount = await _accounts.GetBySystemCodeAsync(AccountSystemCode.TransportRevenue, cancellationToken);

        var baseCurrencyId = await _db.Currencies
            .Where(c => c.IsBaseCurrency && c.IsDeleted != true)
            .Select(c => c.CurrencyID)
            .FirstAsync(cancellationToken);

        var primaryCc = await ResolveVehicleCostCenterAsync(trip.PrimaryVehicleId, cancellationToken);
        var secondaryCc = await ResolveVehicleCostCenterAsync(trip.SecondaryVehicleId, cancellationToken);

        var (primaryShare, secondaryShare) = await ResolveOwnerSharesAsync(trip, cancellationToken);
        var lines = new List<JournalLineDraft>
        {
            new(ar.AccountID, trip.Amount, 0, trip.AmountInBaseCurrency, 0, trip.CurrencyId,
                $"درآمد سفر {trip.TripNumber}", PartyId: trip.CustomerId),
        };

        if (secondaryCc is not null && secondaryShare > 0 && primaryShare + secondaryShare > 0)
        {
            var primaryAmount = Math.Round(trip.Amount * primaryShare / (primaryShare + secondaryShare), 4);
            var secondaryAmount = trip.Amount - primaryAmount;
            var primaryBase = Math.Round(trip.AmountInBaseCurrency * primaryShare / (primaryShare + secondaryShare), 4);
            var secondaryBase = trip.AmountInBaseCurrency - primaryBase;

            lines.Add(new(revenueAccount.AccountID, 0, primaryAmount, 0, primaryBase, trip.CurrencyId,
                $"درآمد سفر {trip.TripNumber}", CostCenterId: primaryCc));
            lines.Add(new(revenueAccount.AccountID, 0, secondaryAmount, 0, secondaryBase, trip.CurrencyId,
                $"درآمد سفر {trip.TripNumber}", CostCenterId: secondaryCc));
        }
        else
        {
            lines.Add(new(revenueAccount.AccountID, 0, trip.Amount, 0, trip.AmountInBaseCurrency, trip.CurrencyId,
                $"درآمد سفر {trip.TripNumber}", CostCenterId: primaryCc));
        }

        var entry = await _journal.PostAsync(
            trip.TripDate,
            $"درآمد سفر {trip.TripNumber}",
            JournalSource.TransportTrip,
            trip.TransportTripId,
            baseCurrencyId,
            lines,
            userId,
            cancellationToken);

        trip.IsRevenuePosted = true;
        trip.RevenueJournalEntryId = entry.JournalEntryID;
        trip.Status = trip.Status == TripStatus.Planned ? TripStatus.Delivered : trip.Status;
        await _db.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<JournalEntry> PostTripExpenseAsync(
        int tripExpenseId,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var expense = await _db.TripExpenses
            .Include(e => e.TransportTrip)
            .Include(e => e.Vehicle)
            .FirstOrDefaultAsync(e => e.TripExpenseId == tripExpenseId && e.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("هزینه سفر یافت نشد.");

        if (expense.IsPosted)
        {
            throw new InvalidOperationException("این هزینه قبلاً ثبت شده است.");
        }

        var expenseAccount = await _accounts.GetBySystemCodeAsync(AccountSystemCode.MiscExpense, cancellationToken);
        var creditAccountId = await ResolvePaymentAccountAsync(expense.CashBoxId, expense.BankAccountId, cancellationToken);

        if (expense.CashBoxId is int cashBoxId)
        {
            await _cashBalances.EnsureSufficientBalanceAsync(cashBoxId, expense.CurrencyId, expense.Amount, cancellationToken);
        }

        var costCenterId = expense.Vehicle?.CostCenterId
            ?? await ResolveVehicleCostCenterAsync(expense.VehicleId, cancellationToken);

        var baseCurrencyId = await _db.Currencies
            .Where(c => c.IsBaseCurrency && c.IsDeleted != true)
            .Select(c => c.CurrencyID)
            .FirstAsync(cancellationToken);

        var lines = new List<JournalLineDraft>
        {
            new(expenseAccount.AccountID, expense.Amount, 0, expense.AmountInBaseCurrency, 0, expense.CurrencyId,
                expense.Title, CostCenterId: costCenterId),
            new(creditAccountId, 0, expense.Amount, 0, expense.AmountInBaseCurrency, expense.CurrencyId,
                expense.Title, CashBoxId: expense.CashBoxId),
        };

        var entry = await _journal.PostAsync(
            expense.ExpenseDate,
            expense.Title,
            JournalSource.TripExpense,
            expense.TripExpenseId,
            baseCurrencyId,
            lines,
            userId,
            cancellationToken);

        expense.IsPosted = true;
        expense.JournalEntryId = entry.JournalEntryID;
        await _db.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<JournalEntry?> SettleTripAsync(
        int tripId,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var trip = await LoadTripForSettlementAsync(tripId, cancellationToken);

        if (!trip.IsRevenuePosted)
        {
            throw new InvalidOperationException("ابتدا درآمد سفر را ثبت کنید.");
        }

        if (trip.Status == TripStatus.Settled)
        {
            throw new InvalidOperationException("این سفر قبلاً تسویه شده است.");
        }

        var unpostedExpenses = trip.Expenses.Any(e => e.IsDeleted != true && !e.IsPosted);
        if (unpostedExpenses)
        {
            throw new InvalidOperationException("ابتدا تمام هزینه‌های سفر را ثبت کنید.");
        }

        var totalExpenses = trip.Expenses.Where(e => e.IsDeleted != true).Sum(e => e.AmountInBaseCurrency);
        var netProfit = trip.AmountInBaseCurrency - totalExpenses;

        var driverAmount = CalculateDriverAmount(trip, netProfit);
        var distributable = Math.Max(0, netProfit - driverAmount);

        var (primaryShare, secondaryShare) = await ResolveOwnerSharesAsync(trip, cancellationToken);
        var totalShare = primaryShare + secondaryShare;
        var primaryAmount = totalShare > 0 ? Math.Round(distributable * primaryShare / totalShare, 4) : 0m;
        var secondaryAmount = distributable - primaryAmount;

        var revenueAccount = await _accounts.GetBySystemCodeAsync(AccountSystemCode.TransportRevenue, cancellationToken);
        var baseCurrencyId = await _db.Currencies
            .Where(c => c.IsBaseCurrency && c.IsDeleted != true)
            .Select(c => c.CurrencyID)
            .FirstAsync(cancellationToken);

        var lines = new List<JournalLineDraft>();
        var distributionTotal = primaryAmount + secondaryAmount + driverAmount;

        if (distributionTotal <= 0)
        {
            trip.Status = TripStatus.Settled;
            await _db.SaveChangesAsync(cancellationToken);
            return null;
        }

        lines.Add(new(revenueAccount.AccountID, distributionTotal, 0, distributionTotal, 0, baseCurrencyId,
            $"تسویه سفر {trip.TripNumber}"));

        if (trip.PrimaryVehicleId is int primaryVehicleId && primaryAmount > 0)
        {
            var owner = await _db.Vehicles.AsNoTracking()
                .Where(v => v.VehicleId == primaryVehicleId)
                .Select(v => new { v.VehicleOwnerId, v.VehicleOwner!.Name })
                .FirstAsync(cancellationToken);
            var ownerAccount = await _accounts.EnsureVehicleOwnerAccountAsync(owner.VehicleOwnerId, owner.Name, cancellationToken);
            lines.Add(new(ownerAccount.AccountID, 0, primaryAmount, 0, primaryAmount, baseCurrencyId,
                $"سهم مالک کشنده — سفر {trip.TripNumber}"));
        }

        if (trip.SecondaryVehicleId is int secondaryVehicleId && secondaryAmount > 0)
        {
            var owner = await _db.Vehicles.AsNoTracking()
                .Where(v => v.VehicleId == secondaryVehicleId)
                .Select(v => new { v.VehicleOwnerId, v.VehicleOwner!.Name })
                .FirstAsync(cancellationToken);
            var ownerAccount = await _accounts.EnsureVehicleOwnerAccountAsync(owner.VehicleOwnerId, owner.Name, cancellationToken);
            lines.Add(new(ownerAccount.AccountID, 0, secondaryAmount, 0, secondaryAmount, baseCurrencyId,
                $"سهم مالک بونکر — سفر {trip.TripNumber}"));
        }

        if (trip.DriverId is int driverId && driverAmount > 0)
        {
            var driverName = await _db.Drivers.Where(d => d.DriverId == driverId).Select(d => d.Name).FirstAsync(cancellationToken);
            var driverAccount = await _accounts.EnsureDriverAccountAsync(driverId, driverName, cancellationToken);
            lines.Add(new(driverAccount.AccountID, 0, driverAmount, 0, driverAmount, baseCurrencyId,
                $"سهم راننده — سفر {trip.TripNumber}"));
        }

        var entry = await _journal.PostAsync(
            trip.TripDate,
            $"تسویه سفر {trip.TripNumber}",
            JournalSource.TransportTrip,
            trip.TransportTripId,
            baseCurrencyId,
            lines,
            userId,
            cancellationToken);

        trip.Status = TripStatus.Settled;
        await _db.SaveChangesAsync(cancellationToken);
        return entry;
    }

    private static decimal CalculateDriverAmount(TransportTrip trip, decimal netProfit)
    {
        if (trip.DriverCompensationType == DriverCompensationType.FixedAmount)
        {
            return trip.DriverFixedAmount ?? 0m;
        }

        var pct = trip.DriverProfitSharePercent ?? trip.Driver?.DefaultProfitSharePercent ?? 0m;
        return pct > 0 ? Math.Round(netProfit * pct / 100m, 4) : 0m;
    }

    private async Task<TransportTrip> LoadTripForSettlementAsync(int tripId, CancellationToken cancellationToken)
    {
        return await _db.TransportTrips
            .Include(t => t.Expenses)
            .Include(t => t.Driver)
            .FirstOrDefaultAsync(t => t.TransportTripId == tripId && t.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("سفر یافت نشد.");
    }

    private async Task<(decimal Primary, decimal Secondary)> ResolveOwnerSharesAsync(
        TransportTrip trip,
        CancellationToken cancellationToken)
    {
        if (trip.PrimaryOwnerSharePercent is decimal p && trip.SecondaryOwnerSharePercent is decimal s)
        {
            return (p, s);
        }

        if (trip.VehiclePairId is int pairId)
        {
            var agreement = await _db.OwnerShareAgreements
                .Where(a => a.VehiclePairId == pairId && a.IsDeleted != true
                    && a.EffectiveFrom <= trip.TripDate
                    && (a.EffectiveTo == null || a.EffectiveTo >= trip.TripDate))
                .OrderByDescending(a => a.EffectiveFrom)
                .FirstOrDefaultAsync(cancellationToken);

            if (agreement is not null)
            {
                return (agreement.PrimarySharePercent, agreement.SecondarySharePercent);
            }

            var pair = await _db.VehiclePairs.AsNoTracking()
                .FirstOrDefaultAsync(p => p.VehiclePairId == pairId, cancellationToken);
            if (pair is not null)
            {
                return (pair.PrimarySharePercent, pair.SecondarySharePercent);
            }
        }

        return (100m, 0m);
    }

    private async Task<int?> ResolveVehicleCostCenterAsync(int? vehicleId, CancellationToken cancellationToken)
    {
        if (vehicleId is not int id)
        {
            return null;
        }

        return await _db.Vehicles.AsNoTracking()
            .Where(v => v.VehicleId == id)
            .Select(v => v.CostCenterId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<int> ResolvePaymentAccountAsync(
        int? cashBoxId,
        int? bankAccountId,
        CancellationToken cancellationToken)
    {
        if (cashBoxId is int cbId)
        {
            return await _gl.ResolveSettlementAccountIdAsync(cbId, cancellationToken);
        }

        if (bankAccountId is int bankId)
        {
            var bank = await _db.BankAccounts.AsNoTracking()
                .FirstOrDefaultAsync(b => b.BankAccountID == bankId && b.IsDeleted != true, cancellationToken)
                ?? throw new InvalidOperationException("حساب بانکی یافت نشد.");
            return bank.AccountId;
        }

        throw new InvalidOperationException("صندوق یا حساب بانکی برای پرداخت الزامی است.");
    }
}
