using Dapper;
using HamgamTransport.Server.Data;

namespace HamgamTransport.Server.Services;

public record VehiclePlRow(
    int VehicleId,
    string PlateNumber,
    string OwnerName,
    decimal Revenue,
    decimal Expenses,
    decimal NetProfit);

public record OwnerBalanceRow(
    int VehicleOwnerId,
    string OwnerName,
    decimal Accrued,
    decimal Paid,
    decimal Balance);

public record CustomerArRow(
    int CustomerId,
    string CustomerName,
    decimal TripRevenue,
    decimal Received,
    decimal Balance);

public interface IFleetReportService
{
    Task<IReadOnlyList<VehiclePlRow>> GetVehiclePlAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OwnerBalanceRow>> GetOwnerBalancesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerArRow>> GetCustomerArAsync(CancellationToken cancellationToken = default);
}

public class FleetReportService : IFleetReportService
{
    private readonly ISqlConnectionFactory _sql;

    public FleetReportService(ISqlConnectionFactory sql)
    {
        _sql = sql;
    }

    public async Task<IReadOnlyList<VehiclePlRow>> GetVehiclePlAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                v.VehicleId AS VehicleId,
                v.PlateNumber AS PlateNumber,
                vo.Name AS OwnerName,
                ISNULL(rev.Revenue, 0) AS Revenue,
                ISNULL(exp.Expenses, 0) AS Expenses,
                ISNULL(rev.Revenue, 0) - ISNULL(exp.Expenses, 0) AS NetProfit
            FROM Vehicles v
            INNER JOIN VehicleOwners vo ON vo.VehicleOwnerId = v.VehicleOwnerId
            LEFT JOIN (
                SELECT jl.CostCenterId, SUM(jl.CreditInBaseCurrency) AS Revenue
                FROM JournalLines jl
                INNER JOIN JournalEntries je ON je.JournalEntryID = jl.JournalEntryId
                WHERE je.Source = @TripSource AND je.IsDeleted = 0 AND jl.IsDeleted = 0
                  AND jl.CreditInBaseCurrency > 0
                  AND (@From IS NULL OR je.EntryDate >= @From)
                  AND (@To IS NULL OR je.EntryDate <= @To)
                GROUP BY jl.CostCenterId
            ) rev ON rev.CostCenterId = v.CostCenterId
            LEFT JOIN (
                SELECT te.VehicleId, SUM(te.AmountInBaseCurrency) AS Expenses
                FROM TripExpenses te
                INNER JOIN TransportTrips t ON t.TransportTripId = te.TransportTripId
                WHERE te.IsDeleted = 0 AND te.IsPosted = 1 AND t.IsDeleted = 0
                  AND (@From IS NULL OR te.ExpenseDate >= @From)
                  AND (@To IS NULL OR te.ExpenseDate <= @To)
                GROUP BY te.VehicleId
            ) exp ON exp.VehicleId = v.VehicleId
            WHERE v.IsDeleted = 0
            ORDER BY v.PlateNumber
            """;

        var rows = await connection.QueryAsync<VehiclePlRow>(sql, new
        {
            TripSource = (int)JournalSource.TransportTrip,
            From = from,
            To = to,
        });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<OwnerBalanceRow>> GetOwnerBalancesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                vo.VehicleOwnerId AS VehicleOwnerId,
                vo.Name AS OwnerName,
                ISNULL(acc.Accrued, 0) AS Accrued,
                ISNULL(paid.Paid, 0) AS Paid,
                ISNULL(acc.Accrued, 0) - ISNULL(paid.Paid, 0) AS Balance
            FROM VehicleOwners vo
            LEFT JOIN (
                SELECT a.AccountID, SUM(jl.CreditInBaseCurrency) AS Accrued
                FROM JournalLines jl
                INNER JOIN Accounts a ON a.AccountID = jl.AccountId
                WHERE jl.IsDeleted = 0 AND a.SystemCode LIKE 'OWNER_%'
                GROUP BY a.AccountID
            ) acc ON acc.AccountID = vo.AccountId
            LEFT JOIN (
                SELECT ps.PartyId, SUM(ps.AmountInBaseCurrency) AS Paid
                FROM PartySettlements ps
                WHERE ps.IsDeleted = 0 AND ps.PartyType = @OwnerPartyType
                GROUP BY ps.PartyId
            ) paid ON paid.PartyId = vo.VehicleOwnerId
            WHERE vo.IsDeleted = 0
            ORDER BY vo.Name
            """;

        var rows = await connection.QueryAsync<OwnerBalanceRow>(sql, new
        {
            OwnerPartyType = (int)PartySettlementPartyType.VehicleOwner,
        });
        return rows.ToList();
    }

    public async Task<IReadOnlyList<CustomerArRow>> GetCustomerArAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                c.CustomerID AS CustomerId,
                c.Name AS CustomerName,
                ISNULL(trip.Revenue, 0) AS TripRevenue,
                ISNULL(stl.Received, 0) AS Received,
                ISNULL(trip.Revenue, 0) - ISNULL(stl.Received, 0) AS Balance
            FROM Customers c
            LEFT JOIN (
                SELECT CustomerId, SUM(AmountInBaseCurrency) AS Revenue
                FROM TransportTrips
                WHERE IsDeleted = 0 AND IsRevenuePosted = 1
                GROUP BY CustomerId
            ) trip ON trip.CustomerId = c.CustomerID
            LEFT JOIN (
                SELECT PartyId, SUM(AmountInBaseCurrency) AS Received
                FROM PartySettlements
                WHERE IsDeleted = 0 AND PartyType = @CustomerPartyType
                GROUP BY PartyId
            ) stl ON stl.PartyId = c.CustomerID
            WHERE c.IsDeleted = 0
            ORDER BY c.Name
            """;

        var rows = await connection.QueryAsync<CustomerArRow>(sql, new
        {
            CustomerPartyType = (int)PartySettlementPartyType.Customer,
        });
        return rows.ToList();
    }
}
