using System.Globalization;
using Dapper;
using HamgamTransport.Server.Data;

namespace HamgamTransport.Server.Services;

public interface IDashboardReadService
{
    Task<object> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<object> GetPerformanceAsync(int months, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<object>> GetRecentOperationsAsync(int take = 15, CancellationToken cancellationToken = default);
    Task<object> GetNotificationsAsync(CancellationToken cancellationToken = default);
}

public class DashboardReadService : IDashboardReadService
{
    private readonly ISqlConnectionFactory _sql;

    public DashboardReadService(ISqlConnectionFactory sql)
    {
        _sql = sql;
    }

    public async Task<object> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var calendar = new PersianCalendar();
        var todayStart = DateTime.Today;
        var todayEnd = todayStart.AddDays(1).AddTicks(-1);

        var year = calendar.GetYear(todayStart);
        var month = calendar.GetMonth(todayStart);
        var monthStart = calendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
        var daysInMonth = calendar.GetDaysInMonth(year, month);
        var monthEnd = calendar.ToDateTime(year, month, daysInMonth, 23, 59, 59, 999);

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        var cancelledStatus = (int)TripStatus.Cancelled;

        var todayTrips = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM TransportTrips
            WHERE ISNULL(IsDeleted, 0) = 0
              AND Status <> @CancelledStatus
              AND TripDate >= @TodayStart
              AND TripDate <= @TodayEnd
            """,
            new { TodayStart = todayStart, TodayEnd = todayEnd, CancelledStatus = cancelledStatus });

        var monthTrips = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM TransportTrips
            WHERE ISNULL(IsDeleted, 0) = 0
              AND Status <> @CancelledStatus
              AND TripDate >= @MonthStart
              AND TripDate <= @MonthEnd
            """,
            new { MonthStart = monthStart, MonthEnd = monthEnd, CancelledStatus = cancelledStatus });

        var todayTripRevenue = await connection.ExecuteScalarAsync<decimal>(
            """
            SELECT ISNULL(SUM(AmountInBaseCurrency), 0)
            FROM TransportTrips
            WHERE ISNULL(IsDeleted, 0) = 0
              AND Status <> @CancelledStatus
              AND IsRevenuePosted = 1
              AND TripDate >= @TodayStart
              AND TripDate <= @TodayEnd
            """,
            new { TodayStart = todayStart, TodayEnd = todayEnd, CancelledStatus = cancelledStatus });

        var monthTripRevenue = await connection.ExecuteScalarAsync<decimal>(
            """
            SELECT ISNULL(SUM(AmountInBaseCurrency), 0)
            FROM TransportTrips
            WHERE ISNULL(IsDeleted, 0) = 0
              AND Status <> @CancelledStatus
              AND IsRevenuePosted = 1
              AND TripDate >= @MonthStart
              AND TripDate <= @MonthEnd
            """,
            new { MonthStart = monthStart, MonthEnd = monthEnd, CancelledStatus = cancelledStatus });

        var activeVehicles = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM Vehicles
            WHERE ISNULL(IsDeleted, 0) = 0
              AND ISNULL(IsActive, 1) = 1
            """);

        return new
        {
            todayTrips,
            monthTrips,
            todayTripRevenue,
            monthTripRevenue,
            activeVehicles,
            monthLabel = JalaliDateHelper.AfghanMonthNames[month - 1],
            year,
            month,
        };
    }

    public async Task<object> GetPerformanceAsync(int months, CancellationToken cancellationToken = default)
    {
        months = Math.Clamp(months, 1, 24);

        var calendar = new PersianCalendar();
        var today = DateTime.Today;
        var currentYear = calendar.GetYear(today);
        var currentMonth = calendar.GetMonth(today);

        var periods = new List<(int Year, int Month, DateTime Start, DateTime End, string Label)>(months);
        for (var i = months - 1; i >= 0; i--)
        {
            var monthIndex = currentMonth - i;
            var year = currentYear;
            while (monthIndex <= 0)
            {
                monthIndex += 12;
                year--;
            }

            var start = calendar.ToDateTime(year, monthIndex, 1, 0, 0, 0, 0);
            var daysInMonth = calendar.GetDaysInMonth(year, monthIndex);
            var end = calendar.ToDateTime(year, monthIndex, daysInMonth, 23, 59, 59, 999);
            var monthName = JalaliDateHelper.AfghanMonthNames[monthIndex - 1];
            var label = year != currentYear
                ? $"{monthName} {year}"
                : monthName;

            periods.Add((year, monthIndex, start, end, label));
        }

        var rangeStart = periods[0].Start;
        var rangeEnd = periods[^1].End;
        var cancelledStatus = (int)TripStatus.Cancelled;

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);
        var p = new DynamicParameters();
        p.Add("RangeStart", rangeStart);
        p.Add("RangeEnd", rangeEnd);
        p.Add("CancelledStatus", cancelledStatus);

        var tripRevenues = (await connection.QueryAsync<PerformanceSimpleRow>(
            """
            SELECT TripDate AS EventDate, AmountInBaseCurrency AS AmountInBase
            FROM TransportTrips
            WHERE ISNULL(IsDeleted, 0) = 0
              AND Status <> @CancelledStatus
              AND IsRevenuePosted = 1
              AND TripDate >= @RangeStart
              AND TripDate <= @RangeEnd
            """, p)).AsList();

        var tripExpenses = (await connection.QueryAsync<PerformanceSimpleRow>(
            """
            SELECT te.ExpenseDate AS EventDate, te.AmountInBaseCurrency AS AmountInBase
            FROM TripExpenses te
            INNER JOIN TransportTrips tt ON tt.TransportTripID = te.TransportTripId
            WHERE ISNULL(te.IsDeleted, 0) = 0
              AND ISNULL(tt.IsDeleted, 0) = 0
              AND tt.Status <> @CancelledStatus
              AND te.ExpenseDate >= @RangeStart
              AND te.ExpenseDate <= @RangeEnd
            """, p)).AsList();

        var revenues = (await connection.QueryAsync<PerformanceSimpleRow>(
            """
            SELECT RevenueDate AS EventDate, AmountInBaseCurrency AS AmountInBase
            FROM Revenues
            WHERE ISNULL(IsDeleted, 0) = 0
              AND RevenueDate >= @RangeStart
              AND RevenueDate <= @RangeEnd
            """, p)).AsList();

        var expenses = (await connection.QueryAsync<PerformanceSimpleRow>(
            """
            SELECT ExpenseDate AS EventDate, AmountInBaseCurrency AS AmountInBase
            FROM Expenses
            WHERE ISNULL(IsDeleted, 0) = 0
              AND ExpenseDate >= @RangeStart
              AND ExpenseDate <= @RangeEnd
            """, p)).AsList();

        var points = periods.Select(period =>
        {
            var tripRevenue = tripRevenues
                .Where(r => r.EventDate >= period.Start && r.EventDate <= period.End)
                .Sum(r => r.AmountInBase);

            var tripExpense = tripExpenses
                .Where(e => e.EventDate >= period.Start && e.EventDate <= period.End)
                .Sum(e => e.AmountInBase);

            var revenue = revenues
                .Where(r => r.EventDate >= period.Start && r.EventDate <= period.End)
                .Sum(r => r.AmountInBase);

            var expense = expenses
                .Where(e => e.EventDate >= period.Start && e.EventDate <= period.End)
                .Sum(e => e.AmountInBase);

            return new
            {
                year = period.Year,
                month = period.Month,
                label = period.Label,
                tripRevenue,
                tripExpense,
                revenue,
                expense,
            };
        }).ToList();

        return new
        {
            months,
            from = JalaliDateHelper.FormatDateWithMonthName(rangeStart),
            to = JalaliDateHelper.FormatDateWithMonthName(rangeEnd),
            totals = new
            {
                tripRevenue = points.Sum(x => x.tripRevenue),
                tripExpense = points.Sum(x => x.tripExpense),
                revenue = points.Sum(x => x.revenue),
                expense = points.Sum(x => x.expense),
            },
            points,
        };
    }

    public async Task<IReadOnlyList<object>> GetRecentOperationsAsync(
        int take = 15,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 50);
        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        var rows = (await connection.QueryAsync<RecentOperationRow>(
            """
            SELECT TOP (@Take)
                   OperationType,
                   EntityId,
                   ReferenceNumber,
                   OperationDate,
                   AmountInBase,
                   PartyName,
                   Status
            FROM (
                SELECT N'trip' AS OperationType,
                       tt.TransportTripID AS EntityId,
                       tt.TripNumber AS ReferenceNumber,
                       tt.TripDate AS OperationDate,
                       tt.AmountInBaseCurrency AS AmountInBase,
                       c.Name AS PartyName,
                       CAST(tt.Status AS int) AS Status
                FROM TransportTrips tt
                INNER JOIN Customers c ON c.CustomerID = tt.CustomerId
                WHERE ISNULL(tt.IsDeleted, 0) = 0

                UNION ALL

                SELECT N'revenue',
                       r.RevenueID,
                       ISNULL(NULLIF(LTRIM(RTRIM(r.Title)), N''), CAST(r.RevenueID AS nvarchar(20))),
                       r.RevenueDate,
                       r.AmountInBaseCurrency,
                       ISNULL(rc.Name, N'عواید'),
                       0
                FROM Revenues r
                LEFT JOIN RevenueCategories rc ON rc.RevenueCategoryID = r.RevenueCategoryId
                WHERE ISNULL(r.IsDeleted, 0) = 0

                UNION ALL

                SELECT N'expense',
                       e.ExpenseID,
                       ISNULL(NULLIF(LTRIM(RTRIM(e.Title)), N''), CAST(e.ExpenseID AS nvarchar(20))),
                       e.ExpenseDate,
                       e.AmountInBaseCurrency,
                       ISNULL(ec.Name, N'مصارف'),
                       0
                FROM Expenses e
                LEFT JOIN ExpenseCategories ec ON ec.ExpenseCategoryID = e.ExpenseCategoryId
                WHERE ISNULL(e.IsDeleted, 0) = 0
            ) ops
            ORDER BY OperationDate DESC, EntityId DESC
            """,
            new { Take = take })).AsList();

        return rows.Select(MapOperation).ToList();
    }

    public async Task<object> GetNotificationsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        var plannedStatus = (int)TripStatus.Planned;
        var inTransitStatus = (int)TripStatus.InTransit;
        var deliveredStatus = (int)TripStatus.Delivered;
        var cancelledStatus = (int)TripStatus.Cancelled;

        var pendingTrips = (await connection.QueryAsync<TripAlertRow>(
            """
            SELECT tt.TransportTripID AS TripId,
                   tt.TripNumber,
                   tt.TripDate,
                   CAST(tt.Status AS int) AS Status,
                   tt.IsRevenuePosted,
                   c.Name AS CustomerName
            FROM TransportTrips tt
            INNER JOIN Customers c ON c.CustomerID = tt.CustomerId
            WHERE ISNULL(tt.IsDeleted, 0) = 0
              AND tt.Status IN (@PlannedStatus, @InTransitStatus)
            ORDER BY tt.TripDate, tt.TripNumber
            """,
            new { PlannedStatus = plannedStatus, InTransitStatus = inTransitStatus })).AsList();

        var unpostedTrips = (await connection.QueryAsync<TripAlertRow>(
            """
            SELECT tt.TransportTripID AS TripId,
                   tt.TripNumber,
                   tt.TripDate,
                   CAST(tt.Status AS int) AS Status,
                   tt.IsRevenuePosted,
                   c.Name AS CustomerName
            FROM TransportTrips tt
            INNER JOIN Customers c ON c.CustomerID = tt.CustomerId
            WHERE ISNULL(tt.IsDeleted, 0) = 0
              AND tt.Status <> @CancelledStatus
              AND tt.IsRevenuePosted = 0
              AND tt.TripDate <= @TodayEnd
            ORDER BY tt.TripDate, tt.TripNumber
            """,
            new { CancelledStatus = cancelledStatus, TodayEnd = DateTime.Today.AddDays(1).AddTicks(-1) })).AsList();

        var awaitingSettlement = (await connection.QueryAsync<TripAlertRow>(
            """
            SELECT tt.TransportTripID AS TripId,
                   tt.TripNumber,
                   tt.TripDate,
                   CAST(tt.Status AS int) AS Status,
                   tt.IsRevenuePosted,
                   c.Name AS CustomerName
            FROM TransportTrips tt
            INNER JOIN Customers c ON c.CustomerID = tt.CustomerId
            WHERE ISNULL(tt.IsDeleted, 0) = 0
              AND tt.Status = @DeliveredStatus
            ORDER BY tt.TripDate, tt.TripNumber
            """,
            new { DeliveredStatus = deliveredStatus })).AsList();

        var items = new List<object>();

        foreach (var row in pendingTrips.Take(10))
        {
            var statusLabel = row.Status == inTransitStatus ? "در مسیر" : "برنامه‌ریزی";
            items.Add(new
            {
                type = "trip_pending",
                severity = row.Status == inTransitStatus ? "warning" : "info",
                title = $"سفر {row.TripNumber} — {statusLabel}",
                message = $"مشتری: {row.CustomerName} — {JalaliDateHelper.FormatDate(row.TripDate)}",
                href = "/transport/trips",
                tripId = row.TripId,
            });
        }

        foreach (var row in unpostedTrips.Take(10))
        {
            items.Add(new
            {
                type = "trip_unposted",
                severity = "warning",
                title = $"درآمد حمل ثبت نشده — {row.TripNumber}",
                message = $"مشتری: {row.CustomerName} — {JalaliDateHelper.FormatDate(row.TripDate)}",
                href = "/transport/trips",
                tripId = row.TripId,
            });
        }

        foreach (var row in awaitingSettlement.Take(10))
        {
            items.Add(new
            {
                type = "trip_awaiting_settlement",
                severity = "info",
                title = $"سفر تحویل‌شده — {row.TripNumber}",
                message = $"در انتظار تسویه — مشتری: {row.CustomerName}",
                href = "/transport/trips",
                tripId = row.TripId,
            });
        }

        return new
        {
            count = items.Count,
            items,
        };
    }

    private static object MapOperation(RecentOperationRow row)
    {
        var (typeLabel, href) = row.OperationType switch
        {
            "trip" => ("سفر", "/transport/trips"),
            "revenue" => ("عواید", "/accounting/revenues"),
            "expense" => ("مصارف", "/accounting/expenses"),
            _ => ("عملیات", "/"),
        };

        return new
        {
            type = row.OperationType,
            typeLabel,
            entityId = row.EntityId,
            referenceNumber = row.ReferenceNumber,
            title = BuildOperationTitle(row, typeLabel),
            operationDate = row.OperationDate,
            dateLabel = JalaliDateHelper.FormatDate(row.OperationDate),
            amountInBase = row.AmountInBase,
            partyName = row.PartyName,
            statusLabel = BuildStatusLabel(row),
            href,
        };
    }

    private static string BuildOperationTitle(RecentOperationRow row, string typeLabel)
    {
        var party = string.IsNullOrWhiteSpace(row.PartyName) ? null : row.PartyName.Trim();
        return party is null
            ? $"{typeLabel} {row.ReferenceNumber}"
            : $"{typeLabel} {row.ReferenceNumber} — {party}";
    }

    private static string BuildStatusLabel(RecentOperationRow row)
    {
        if (row.OperationType != "trip")
            return "ثبت‌شده";

        return row.Status switch
        {
            (int)TripStatus.Planned => "برنامه‌ریزی",
            (int)TripStatus.InTransit => "در مسیر",
            (int)TripStatus.Delivered => "تحویل‌شده",
            (int)TripStatus.Settled => "تسویه‌شده",
            (int)TripStatus.Cancelled => "لغو",
            _ => "—",
        };
    }

    private sealed class PerformanceSimpleRow
    {
        public DateTime EventDate { get; set; }
        public decimal AmountInBase { get; set; }
    }

    private sealed class RecentOperationRow
    {
        public string OperationType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public DateTime OperationDate { get; set; }
        public decimal AmountInBase { get; set; }
        public string? PartyName { get; set; }
        public int Status { get; set; }
    }

    private sealed class TripAlertRow
    {
        public int TripId { get; set; }
        public string TripNumber { get; set; } = string.Empty;
        public DateTime TripDate { get; set; }
        public int Status { get; set; }
        public bool IsRevenuePosted { get; set; }
        public string CustomerName { get; set; } = string.Empty;
    }
}
