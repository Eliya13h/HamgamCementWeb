using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Data.Models.Invoice;
using HamgamCementWeb.Server.Data.Models.Transport;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Services;

public interface IFreightTripService
{
    // اعتبارسنجی و نرمال‌سازی فیلدهای کرایه قبل از ذخیره/ثبت
    void NormalizeAndValidatePurchaseFreight(PurchaseInvoice invoice);
    void NormalizeAndValidateSaleFreight(SaleInvoice invoice);

    // ساخت سفر + ثبت هزینه حمل خرید (بدون تأثیر بر FIFO)
    Task ApplyPurchaseFreightAsync(
        PurchaseInvoice invoice,
        int? userId,
        int? cashBoxId,
        CancellationToken cancellationToken = default);

    // ساخت سفر + ثبت درآمد کرایه فروش
    Task ApplySaleFreightAsync(
        SaleInvoice invoice,
        int? userId,
        int? cashBoxId,
        CancellationToken cancellationToken = default);
}

public class FreightTripService : IFreightTripService
{
    public const decimal KgPerTon = 1000m;

    private readonly AppDbContext _db;
    private readonly IFinanceCategoryService _financeCategories;
    private readonly IOperationalGlService _gl;

    public FreightTripService(
        AppDbContext db,
        IFinanceCategoryService financeCategories,
        IOperationalGlService gl)
    {
        _db = db;
        _financeCategories = financeCategories;
        _gl = gl;
    }

    public static decimal WeightTonFromBaseKg(decimal quantityInBaseKg) =>
        Math.Round(quantityInBaseKg / KgPerTon, 4, MidpointRounding.AwayFromZero);

    public void NormalizeAndValidatePurchaseFreight(PurchaseInvoice invoice)
    {
        NormalizeCommon(
            invoice.FreightMode,
            invoice.FreightVehicleId,
            invoice.FreightCarrierName,
            invoice.FreightRatePerTon,
            invoice.FreightWeightTon,
            out var amount,
            out var carrier);

        invoice.FreightCarrierName = carrier;
        invoice.FreightAmount = amount;
        invoice.FreightAmountInBaseCurrency = RoundMoney(amount * invoice.BaseUnitsPerUnitAtTransaction);

        if (invoice.FreightMode == FreightMode.None)
        {
            invoice.FreightRatePerTon = 0;
            invoice.FreightWeightTon = 0;
            invoice.FreightAmount = 0;
            invoice.FreightAmountInBaseCurrency = 0;
            invoice.FreightVehicleId = null;
            invoice.FreightCarrierName = null;
        }
    }

    public void NormalizeAndValidateSaleFreight(SaleInvoice invoice)
    {
        NormalizeCommon(
            invoice.FreightMode,
            invoice.FreightVehicleId,
            invoice.FreightCarrierName,
            invoice.FreightRatePerTon,
            invoice.FreightWeightTon,
            out var amount,
            out var carrier);

        invoice.FreightCarrierName = carrier;
        invoice.FreightAmount = amount;
        invoice.FreightAmountInBaseCurrency = RoundMoney(amount * invoice.BaseUnitsPerUnitAtTransaction);

        if (invoice.FreightMode == FreightMode.None)
        {
            invoice.FreightRatePerTon = 0;
            invoice.FreightWeightTon = 0;
            invoice.FreightAmount = 0;
            invoice.FreightAmountInBaseCurrency = 0;
            invoice.FreightVehicleId = null;
            invoice.FreightCarrierName = null;
        }
    }

    public async Task ApplyPurchaseFreightAsync(
        PurchaseInvoice invoice,
        int? userId,
        int? cashBoxId,
        CancellationToken cancellationToken = default)
    {
        NormalizeAndValidatePurchaseFreight(invoice);
        if (invoice.FreightMode == FreightMode.None || invoice.FreightAmount <= 0)
        {
            return;
        }

        if (invoice.FreightMode == FreightMode.OwnFleet && invoice.FreightVehicleId is int vehicleId)
        {
            var exists = await _db.Vehicles.AnyAsync(
                v => v.VehicleID == vehicleId && v.IsDeleted != true,
                cancellationToken);
            if (!exists)
            {
                throw new InvalidOperationException("وسیله نقلیه انتخاب‌شده برای حمل یافت نشد.");
            }
        }

        var now = DateTime.Now;
        var trip = await CreateOrUpdateTripAsync(
            existingTripId: invoice.TransportTripId,
            purpose: TripPurpose.PurchaseInbound,
            mode: invoice.FreightMode,
            vehicleId: invoice.FreightVehicleId,
            carrierName: invoice.FreightCarrierName,
            ratePerTon: invoice.FreightRatePerTon,
            weightTon: invoice.FreightWeightTon,
            tripRevenue: 0,
            departureDate: invoice.InvoiceDate,
            cargoDescription: $"حمل خرید {invoice.InvoiceNumber}",
            purchaseInvoiceId: invoice.PurchaseInvoiceID,
            saleInvoiceId: null,
            userId: userId,
            now: now,
            cancellationToken: cancellationToken);

        invoice.TransportTripId = trip.TransportTripID;

        var categoryId = await _financeCategories.GetExpenseCategoryIdAsync(
            FinanceCategoryCode.TransportExpense,
            cancellationToken);

        var expense = new Expense
        {
            Title = $"کرایه حمل خرید — {invoice.InvoiceNumber}",
            ExpenseDate = invoice.InvoiceDate,
            ExpenseCategoryId = categoryId,
            Source = FinancialEntrySource.TransportExpense,
            CurrencyId = invoice.CurrencyId,
            BaseCurrencyId = invoice.BaseCurrencyId,
            ExchangeHistoryId = invoice.ExchangeHistoryId,
            BaseUnitsPerUnitAtTransaction = invoice.BaseUnitsPerUnitAtTransaction,
            Amount = invoice.FreightAmount,
            AmountInBaseCurrency = invoice.FreightAmountInBaseCurrency,
            Description = BuildFreightDescription(invoice.FreightMode, invoice.FreightCarrierName, invoice.FreightVehicleId, invoice.FreightWeightTon, invoice.FreightRatePerTon),
            IsActive = true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedBy = userId,
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync(cancellationToken);

        var journal = await _gl.PostMiscExpenseAsync(expense, userId, cashBoxId, cancellationToken);
        expense.JournalEntryId = journal.JournalEntryID;
        invoice.FreightExpenseId = expense.ExpenseID;
        invoice.FreightJournalEntryId = journal.JournalEntryID;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplySaleFreightAsync(
        SaleInvoice invoice,
        int? userId,
        int? cashBoxId,
        CancellationToken cancellationToken = default)
    {
        NormalizeAndValidateSaleFreight(invoice);
        if (invoice.FreightMode == FreightMode.None || invoice.FreightAmount <= 0)
        {
            return;
        }

        if (invoice.FreightMode == FreightMode.OwnFleet && invoice.FreightVehicleId is int vehicleId)
        {
            var exists = await _db.Vehicles.AnyAsync(
                v => v.VehicleID == vehicleId && v.IsDeleted != true,
                cancellationToken);
            if (!exists)
            {
                throw new InvalidOperationException("وسیله نقلیه انتخاب‌شده برای حمل یافت نشد.");
            }
        }

        var now = DateTime.Now;
        var trip = await CreateOrUpdateTripAsync(
            existingTripId: invoice.TransportTripId,
            purpose: TripPurpose.SaleDelivery,
            mode: invoice.FreightMode,
            vehicleId: invoice.FreightVehicleId,
            carrierName: invoice.FreightCarrierName,
            ratePerTon: invoice.FreightRatePerTon,
            weightTon: invoice.FreightWeightTon,
            tripRevenue: invoice.FreightAmount,
            departureDate: invoice.InvoiceDate,
            cargoDescription: $"حمل فروش {invoice.InvoiceNumber}",
            purchaseInvoiceId: null,
            saleInvoiceId: invoice.SaleInvoiceID,
            userId: userId,
            now: now,
            cancellationToken: cancellationToken);

        invoice.TransportTripId = trip.TransportTripID;

        var categoryId = await _financeCategories.GetRevenueCategoryIdAsync(
            FinanceCategoryCode.TransportRevenue,
            cancellationToken);

        var revenue = new Revenue
        {
            Title = $"کرایه حمل فروش — {invoice.InvoiceNumber}",
            RevenueDate = invoice.InvoiceDate,
            RevenueCategoryId = categoryId,
            Source = FinancialEntrySource.TransportRevenue,
            CustomerId = invoice.CustomerId,
            CurrencyId = invoice.CurrencyId,
            BaseCurrencyId = invoice.BaseCurrencyId,
            ExchangeHistoryId = invoice.ExchangeHistoryId,
            BaseUnitsPerUnitAtTransaction = invoice.BaseUnitsPerUnitAtTransaction,
            Amount = invoice.FreightAmount,
            AmountInBaseCurrency = invoice.FreightAmountInBaseCurrency,
            ProfitInBaseCurrency = invoice.FreightAmountInBaseCurrency,
            Description = BuildFreightDescription(invoice.FreightMode, invoice.FreightCarrierName, invoice.FreightVehicleId, invoice.FreightWeightTon, invoice.FreightRatePerTon),
            IsActive = true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedBy = userId,
        };

        _db.Revenues.Add(revenue);
        await _db.SaveChangesAsync(cancellationToken);

        var journal = await _gl.PostMiscRevenueAsync(revenue, userId, cashBoxId, cancellationToken);
        revenue.JournalEntryId = journal.JournalEntryID;
        invoice.FreightRevenueId = revenue.RevenueID;
        invoice.FreightJournalEntryId = journal.JournalEntryID;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<TransportTrip> CreateOrUpdateTripAsync(
        int? existingTripId,
        TripPurpose purpose,
        FreightMode mode,
        int? vehicleId,
        string? carrierName,
        decimal ratePerTon,
        decimal weightTon,
        decimal tripRevenue,
        DateTime departureDate,
        string cargoDescription,
        int? purchaseInvoiceId,
        int? saleInvoiceId,
        int? userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        TransportTrip? trip = null;
        if (existingTripId is int tripId)
        {
            trip = await _db.TransportTrips
                .FirstOrDefaultAsync(t => t.TransportTripID == tripId && t.IsDeleted != true, cancellationToken);
        }

        if (trip is null)
        {
            trip = new TransportTrip
            {
                TripNumber = $"TMP{DateTime.UtcNow.Ticks}",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = userId,
            };
            _db.TransportTrips.Add(trip);
        }
        else
        {
            trip.IsUpdated = true;
            trip.UpdatedAt = now;
            trip.UpdatedBy = userId;
        }

        trip.VehicleId = mode == FreightMode.OwnFleet ? vehicleId : null;
        trip.TransportRouteId = null;
        trip.TripPurpose = purpose;
        trip.FreightMode = mode;
        trip.FreightRatePerTon = ratePerTon;
        trip.FreightCarrierName = carrierName;
        trip.CargoWeightTon = weightTon;
        trip.CargoDescription = cargoDescription;
        trip.DepartureDate = departureDate;
        trip.ArrivalDate = departureDate;
        trip.TripRevenue = tripRevenue;
        trip.Status = TripStatus.Completed;
        trip.PurchaseInvoiceId = purchaseInvoiceId;
        trip.SaleInvoiceId = saleInvoiceId;
        trip.Description = carrierName;

        await _db.SaveChangesAsync(cancellationToken);

        if (trip.TripNumber.StartsWith("TMP", StringComparison.Ordinal))
        {
            trip.TripNumber = TransportCodeHelper.ForTrip(trip.TransportTripID);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return trip;
    }

    private static void NormalizeCommon(
        FreightMode mode,
        int? vehicleId,
        string? carrierName,
        decimal ratePerTon,
        decimal weightTon,
        out decimal amount,
        out string? carrier)
    {
        carrier = string.IsNullOrWhiteSpace(carrierName) ? null : carrierName.Trim();

        if (mode == FreightMode.None)
        {
            amount = 0;
            return;
        }

        if (mode is not FreightMode.OwnFleet and not FreightMode.Hired)
        {
            throw new InvalidOperationException("نوع حمل نامعتبر است.");
        }

        if (mode == FreightMode.OwnFleet && vehicleId is null or <= 0)
        {
            throw new InvalidOperationException("برای حمل با ناوگان خودی، انتخاب وسیله الزامی است.");
        }

        if (mode == FreightMode.Hired && string.IsNullOrWhiteSpace(carrier))
        {
            throw new InvalidOperationException("برای حمل کرایه‌ای، نام باربری/مالک الزامی است.");
        }

        if (ratePerTon <= 0)
        {
            throw new InvalidOperationException("نرخ کرایه هر تن باید بزرگ‌تر از صفر باشد.");
        }

        if (weightTon <= 0)
        {
            throw new InvalidOperationException("وزن حمل (تن) باید بزرگ‌تر از صفر باشد.");
        }

        amount = RoundMoney(ratePerTon * weightTon);
    }

    private static string BuildFreightDescription(
        FreightMode mode,
        string? carrier,
        int? vehicleId,
        decimal weightTon,
        decimal ratePerTon)
    {
        var modeLabel = mode == FreightMode.OwnFleet ? "خودی" : "کرایه‌ای";
        var party = mode == FreightMode.OwnFleet
            ? (vehicleId is int id ? $"وسیله #{id}" : "")
            : (carrier ?? "");
        return $"حمل {modeLabel} — {weightTon:0.####} تن × {ratePerTon:0.####} — {party}".Trim(' ', '—');
    }

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
