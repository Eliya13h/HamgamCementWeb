using Dapper;

namespace HamgamCementWeb.Server.Services;

public interface ITransportReportService
{
    Task<TransportReportSummary> GetSummaryAsync(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default);
}

public class TransportReportService : ITransportReportService
{
    private readonly ISqlConnectionFactory _sql;

    public TransportReportService(ISqlConnectionFactory sql)
    {
        _sql = sql;
    }

    public async Task<TransportReportSummary> GetSummaryAsync(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);
        var p = new
        {
            FromDate = fromDate?.Date,
            ToDate = toDate?.Date.AddDays(1),
        };

        const string tripFilter = """
            WHERE t.IsDeleted = 0
              AND (@FromDate IS NULL OR t.DepartureDate >= @FromDate)
              AND (@ToDate IS NULL OR t.DepartureDate < @ToDate)
            """;

        var byVehicle = (await connection.QueryAsync<TransportVehicleRow>(
            $"""
             SELECT
               ISNULL(v.VehicleID, 0) AS VehicleId,
               ISNULL(v.Code + N' — ' + v.PlateNumber, ISNULL(t.FreightCarrierName, N'کرایه‌ای')) AS VehicleLabel,
               t.FreightMode AS FreightMode,
               COUNT(1) AS TripCount,
               ISNULL(SUM(t.CargoWeightTon), 0) AS TotalWeightTon,
               ISNULL(SUM(t.TripRevenue), 0) AS TotalRevenue,
               ISNULL(SUM(CASE WHEN t.TripPurpose = 1 THEN t.FreightRatePerTon * ISNULL(t.CargoWeightTon, 0) ELSE 0 END), 0) AS PurchaseFreightAmount,
               ISNULL(SUM(CASE WHEN t.TripPurpose = 2 THEN t.TripRevenue ELSE 0 END), 0) AS SaleFreightAmount
             FROM TransportTrips t
             LEFT JOIN Vehicles v ON v.VehicleID = t.VehicleId
             {tripFilter}
             GROUP BY ISNULL(v.VehicleID, 0),
                      ISNULL(v.Code + N' — ' + v.PlateNumber, ISNULL(t.FreightCarrierName, N'کرایه‌ای')),
                      t.FreightMode
             ORDER BY TotalWeightTon DESC
             """, p)).ToList();

        var byPurpose = (await connection.QueryAsync<TransportPurposeRow>(
            $"""
             SELECT
               t.TripPurpose AS TripPurpose,
               COUNT(1) AS TripCount,
               ISNULL(SUM(t.CargoWeightTon), 0) AS TotalWeightTon,
               ISNULL(SUM(t.TripRevenue), 0) AS TotalRevenue
             FROM TransportTrips t
             {tripFilter}
             GROUP BY t.TripPurpose
             ORDER BY t.TripPurpose
             """, p)).ToList();

        var maintenance = (await connection.QueryAsync<TransportMaintenanceRow>(
            """
            SELECT
              v.VehicleID AS VehicleId,
              v.Code + N' — ' + v.PlateNumber AS VehicleLabel,
              ISNULL((SELECT SUM(m.Cost) FROM VehicleMaintenances m
                      WHERE m.VehicleId = v.VehicleID AND m.IsDeleted = 0
                        AND (@FromDate IS NULL OR m.MaintenanceDate >= @FromDate)
                        AND (@ToDate IS NULL OR m.MaintenanceDate < @ToDate)), 0) AS MaintenanceCost,
              ISNULL((SELECT SUM(p.TotalCost) FROM VehiclePartReplacements p
                      WHERE p.VehicleId = v.VehicleID AND p.IsDeleted = 0
                        AND (@FromDate IS NULL OR p.ReplacementDate >= @FromDate)
                        AND (@ToDate IS NULL OR p.ReplacementDate < @ToDate)), 0) AS PartsCost,
              ISNULL((SELECT SUM(d.AmountInBaseCurrency) FROM FixedAssetDepreciations d
                      INNER JOIN FixedAssets fa ON fa.FixedAssetID = d.FixedAssetId
                      WHERE v.FixedAssetId = fa.FixedAssetID AND d.IsDeleted = 0 AND fa.IsDeleted = 0
                        AND (@FromDate IS NULL OR d.DepreciationDate >= @FromDate)
                        AND (@ToDate IS NULL OR d.DepreciationDate < @ToDate)), 0) AS DepreciationCost
            FROM Vehicles v
            WHERE v.IsDeleted = 0
            ORDER BY v.Code
            """, p)).ToList();

        return new TransportReportSummary
        {
            ByVehicle = byVehicle,
            ByPurpose = byPurpose,
            Maintenance = maintenance.Where(m => m.MaintenanceCost > 0 || m.PartsCost > 0 || m.DepreciationCost > 0).ToList(),
            TotalTrips = byPurpose.Sum(x => x.TripCount),
            TotalWeightTon = byPurpose.Sum(x => x.TotalWeightTon),
            TotalTripRevenue = byPurpose.Sum(x => x.TotalRevenue),
            OwnFleetTrips = byVehicle.Where(x => x.FreightMode == 1).Sum(x => x.TripCount),
            HiredTrips = byVehicle.Where(x => x.FreightMode == 2).Sum(x => x.TripCount),
        };
    }
}

public class TransportReportSummary
{
    public IReadOnlyList<TransportVehicleRow> ByVehicle { get; set; } = [];
    public IReadOnlyList<TransportPurposeRow> ByPurpose { get; set; } = [];
    public IReadOnlyList<TransportMaintenanceRow> Maintenance { get; set; } = [];
    public int TotalTrips { get; set; }
    public decimal TotalWeightTon { get; set; }
    public decimal TotalTripRevenue { get; set; }
    public int OwnFleetTrips { get; set; }
    public int HiredTrips { get; set; }
}

public class TransportVehicleRow
{
    public int VehicleId { get; set; }
    public string VehicleLabel { get; set; } = string.Empty;
    public int FreightMode { get; set; }
    public int TripCount { get; set; }
    public decimal TotalWeightTon { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal PurchaseFreightAmount { get; set; }
    public decimal SaleFreightAmount { get; set; }
}

public class TransportPurposeRow
{
    public int TripPurpose { get; set; }
    public int TripCount { get; set; }
    public decimal TotalWeightTon { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class TransportMaintenanceRow
{
    public int VehicleId { get; set; }
    public string VehicleLabel { get; set; } = string.Empty;
    public decimal MaintenanceCost { get; set; }
    public decimal PartsCost { get; set; }
    public decimal DepreciationCost { get; set; }
}
