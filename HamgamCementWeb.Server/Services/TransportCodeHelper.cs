namespace HamgamCementWeb.Server.Services;

/// <summary>
/// تولید کدهای خودکار حمل و نقل: پیشوند + شناسه با حداقل ۴ رقم (مثلاً HMV0001)
/// </summary>
public static class TransportCodeHelper
{
    public const string VehiclePrefix = "HMV";
    public const string RoutePrefix = "HMR";
    public const string TripPrefix = "HMT";
    public const string InvoicePrefix = "HMTE";

    public static string ForVehicle(int vehicleId) => Format(VehiclePrefix, vehicleId);

    public static string ForRoute(int routeId) => Format(RoutePrefix, routeId);

    public static string ForTrip(int tripId) => Format(TripPrefix, tripId);

    public static string ForInvoice(int invoiceId) => Format(InvoicePrefix, invoiceId);

    private static string Format(string prefix, int id) => $"{prefix}{id:D4}";
}
