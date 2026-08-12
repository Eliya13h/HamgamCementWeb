using System.Globalization;
using Dapper;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Invoice;

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
    private const decimal WarehouseFullPercent = 95m;
    private const decimal WarehouseLowPercent = 20m;

    private readonly ISqlConnectionFactory _sql;

    public DashboardReadService(ISqlConnectionFactory sql)
    {
        _sql = sql;
    }

    // کارت‌های بالای داشبورد: تولید امروز، فروش/خرید امروز و ماه جاری
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

        var purchases = (await connection.QueryAsync<PerformanceAmountRow>(
            """
            SELECT InvoiceDate AS EventDate,
                   CAST(DocumentType AS int) AS DocumentType,
                   TotalAmountInBaseCurrency AS AmountInBase
            FROM PurchaseInvoices
            WHERE ISNULL(IsDeleted, 0) = 0
              AND IsPosted = 1
              AND EntrySource <> @ProductionEntrySource
              AND InvoiceDate >= @RangeStart
              AND InvoiceDate <= @RangeEnd
              AND (
                    DocumentType = @PurchaseReturn
                    OR (DocumentType = @InvoiceDoc AND Status = @InvoiceStatus)
                  )
            """,
            new
            {
                RangeStart = monthStart,
                RangeEnd = monthEnd,
                ProductionEntrySource = (int)PurchaseEntrySource.Production,
                PurchaseReturn = (int)InvoiceDocumentType.PurchaseReturn,
                InvoiceDoc = (int)InvoiceDocumentType.Invoice,
                InvoiceStatus = (int)InvoiceStatus.Invoice,
            })).AsList();

        var sales = (await connection.QueryAsync<PerformanceAmountRow>(
            """
            SELECT InvoiceDate AS EventDate,
                   CAST(DocumentType AS int) AS DocumentType,
                   TotalAmountInBaseCurrency AS AmountInBase
            FROM SaleInvoices
            WHERE ISNULL(IsDeleted, 0) = 0
              AND IsPosted = 1
              AND InvoiceDate >= @RangeStart
              AND InvoiceDate <= @RangeEnd
              AND (
                    DocumentType = @SaleReturn
                    OR (DocumentType = @InvoiceDoc AND Status = @InvoiceStatus)
                  )
            """,
            new
            {
                RangeStart = monthStart,
                RangeEnd = monthEnd,
                SaleReturn = (int)InvoiceDocumentType.SaleReturn,
                InvoiceDoc = (int)InvoiceDocumentType.Invoice,
                InvoiceStatus = (int)InvoiceStatus.Invoice,
            })).AsList();

        var todayProduction = await connection.ExecuteScalarAsync<decimal>(
            """
            SELECT ISNULL(SUM(ol.QuantityInBase), 0)
            FROM ProductionOutputLines ol
            INNER JOIN ProductionBatches pb ON pb.ProductionBatchID = ol.ProductionBatchId
            WHERE ISNULL(ol.IsDeleted, 0) = 0
              AND ISNULL(pb.IsDeleted, 0) = 0
              AND pb.IsPosted = 1
              AND pb.Status = @PostedStatus
              AND pb.ProductionDate >= @TodayStart
              AND pb.ProductionDate <= @TodayEnd
            """,
            new
            {
                TodayStart = todayStart,
                TodayEnd = todayEnd,
                PostedStatus = (int)ProductionBatchStatus.Posted,
            });

        static decimal NetAmount(IEnumerable<PerformanceAmountRow> rows, DateTime start, DateTime end, int returnType) =>
            rows
                .Where(i => i.EventDate >= start && i.EventDate <= end)
                .Sum(i => i.DocumentType == returnType ? -i.AmountInBase : i.AmountInBase);

        var todaySale = NetAmount(sales, todayStart, todayEnd, (int)InvoiceDocumentType.SaleReturn);
        var todayPurchase = NetAmount(purchases, todayStart, todayEnd, (int)InvoiceDocumentType.PurchaseReturn);
        var monthSale = NetAmount(sales, monthStart, monthEnd, (int)InvoiceDocumentType.SaleReturn);
        var monthPurchase = NetAmount(purchases, monthStart, monthEnd, (int)InvoiceDocumentType.PurchaseReturn);

        return new
        {
            todayProduction,
            todaySale,
            todayPurchase,
            monthSale,
            monthPurchase,
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

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);
        var p = new DynamicParameters();
        p.Add("RangeStart", rangeStart);
        p.Add("RangeEnd", rangeEnd);

        var purchases = (await connection.QueryAsync<PerformanceAmountRow>(
            """
            SELECT InvoiceDate AS EventDate,
                   CAST(DocumentType AS int) AS DocumentType,
                   TotalAmountInBaseCurrency AS AmountInBase
            FROM PurchaseInvoices
            WHERE ISNULL(IsDeleted, 0) = 0
              AND IsPosted = 1
              AND EntrySource <> @ProductionEntrySource
              AND InvoiceDate >= @RangeStart
              AND InvoiceDate <= @RangeEnd
              AND (
                    DocumentType = @PurchaseReturn
                    OR (DocumentType = @InvoiceDoc AND Status = @InvoiceStatus)
                  )
            """,
            new
            {
                RangeStart = rangeStart,
                RangeEnd = rangeEnd,
                ProductionEntrySource = (int)PurchaseEntrySource.Production,
                PurchaseReturn = (int)InvoiceDocumentType.PurchaseReturn,
                InvoiceDoc = (int)InvoiceDocumentType.Invoice,
                InvoiceStatus = (int)InvoiceStatus.Invoice,
            })).AsList();

        var sales = (await connection.QueryAsync<PerformanceAmountRow>(
            """
            SELECT InvoiceDate AS EventDate,
                   CAST(DocumentType AS int) AS DocumentType,
                   TotalAmountInBaseCurrency AS AmountInBase
            FROM SaleInvoices
            WHERE ISNULL(IsDeleted, 0) = 0
              AND IsPosted = 1
              AND InvoiceDate >= @RangeStart
              AND InvoiceDate <= @RangeEnd
              AND (
                    DocumentType = @SaleReturn
                    OR (DocumentType = @InvoiceDoc AND Status = @InvoiceStatus)
                  )
            """,
            new
            {
                RangeStart = rangeStart,
                RangeEnd = rangeEnd,
                SaleReturn = (int)InvoiceDocumentType.SaleReturn,
                InvoiceDoc = (int)InvoiceDocumentType.Invoice,
                InvoiceStatus = (int)InvoiceStatus.Invoice,
            })).AsList();

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
            var purchase = purchases
                .Where(i => i.EventDate >= period.Start && i.EventDate <= period.End)
                .Sum(i => i.DocumentType == (int)InvoiceDocumentType.PurchaseReturn
                    ? -i.AmountInBase
                    : i.AmountInBase);

            var sale = sales
                .Where(i => i.EventDate >= period.Start && i.EventDate <= period.End)
                .Sum(i => i.DocumentType == (int)InvoiceDocumentType.SaleReturn
                    ? -i.AmountInBase
                    : i.AmountInBase);

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
                purchase,
                sale,
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
                purchase = points.Sum(x => x.purchase),
                sale = points.Sum(x => x.sale),
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
                   WarehouseName,
                   DocumentType,
                   Status,
                   IsPosted
            FROM (
                SELECT N'purchase' AS OperationType,
                       pi.PurchaseInvoiceID AS EntityId,
                       pi.InvoiceNumber AS ReferenceNumber,
                       pi.InvoiceDate AS OperationDate,
                       pi.TotalAmountInBaseCurrency AS AmountInBase,
                       s.Name AS PartyName,
                       w.Name AS WarehouseName,
                       CAST(pi.DocumentType AS int) AS DocumentType,
                       CAST(pi.Status AS int) AS Status,
                       CAST(pi.IsPosted AS bit) AS IsPosted
                FROM PurchaseInvoices pi
                INNER JOIN Suppliers s ON s.SupplierID = pi.SupplierId
                INNER JOIN Warehouses w ON w.WarehouseID = pi.WarehouseId
                WHERE ISNULL(pi.IsDeleted, 0) = 0
                  AND pi.EntrySource <> @ProductionEntrySource

                UNION ALL

                SELECT N'sale',
                       si.SaleInvoiceID,
                       si.InvoiceNumber,
                       si.InvoiceDate,
                       si.TotalAmountInBaseCurrency,
                       c.Name,
                       w.Name,
                       CAST(si.DocumentType AS int),
                       CAST(si.Status AS int),
                       CAST(si.IsPosted AS bit)
                FROM SaleInvoices si
                INNER JOIN Customers c ON c.CustomerID = si.CustomerId
                INNER JOIN Warehouses w ON w.WarehouseID = si.WarehouseId
                WHERE ISNULL(si.IsDeleted, 0) = 0

                UNION ALL

                SELECT N'production',
                       pb.ProductionBatchID,
                       pb.BatchNumber,
                       pb.ProductionDate,
                       pb.TotalCostInBase,
                       ISNULL(f.Name, N'تولید'),
                       w.Name,
                       NULL,
                       CAST(pb.Status AS int),
                       CAST(pb.IsPosted AS bit)
                FROM ProductionBatches pb
                INNER JOIN Warehouses w ON w.WarehouseID = pb.OutputWarehouseId
                LEFT JOIN ProductionFormulas f ON f.ProductionFormulaID = pb.ProductionFormulaId
                WHERE ISNULL(pb.IsDeleted, 0) = 0
            ) ops
            ORDER BY OperationDate DESC, EntityId DESC
            """,
            new
            {
                Take = take,
                ProductionEntrySource = (int)PurchaseEntrySource.Production,
            })).AsList();

        return rows.Select(MapOperation).ToList();
    }

    public async Task<object> GetNotificationsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        var shortages = (await connection.QueryAsync<ProductShortageRow>(
            """
            SELECT p.ProductID AS ProductId,
                   p.Code,
                   p.Name,
                   p.MinStockQuantity,
                   ISNULL(SUM(s.QuantityInBase), 0) AS TotalStockQuantity,
                   m.Name AS UnitName
            FROM Products p
            INNER JOIN Meaurments m ON m.MeaurmentID = p.BaseMeaurmentId
            LEFT JOIN InventoryStocks s
                   ON s.ProductId = p.ProductID
                  AND ISNULL(s.IsDeleted, 0) = 0
            WHERE ISNULL(p.IsDeleted, 0) = 0
              AND ISNULL(p.IsActive, 1) = 1
              AND p.MinStockQuantity > 0
            GROUP BY p.ProductID, p.Code, p.Name, p.MinStockQuantity, m.Name
            HAVING ISNULL(SUM(s.QuantityInBase), 0) < p.MinStockQuantity
            ORDER BY (p.MinStockQuantity - ISNULL(SUM(s.QuantityInBase), 0)) DESC, p.Name
            """)).AsList();

        var warehouses = (await connection.QueryAsync<WarehouseFillRow>(
            """
            SELECT w.WarehouseID AS WarehouseId,
                   w.Name,
                   w.Capacity,
                   cm.Name AS CapacityUnit,
                   CAST(w.Capacity * cm.FactorToBase AS decimal(18,6)) AS CapacityInBase,
                   ISNULL((
                       SELECT SUM(s.QuantityInBase)
                       FROM InventoryStocks s
                       INNER JOIN Products p ON p.ProductID = s.ProductId
                       WHERE s.WarehouseId = w.WarehouseID
                         AND ISNULL(s.IsDeleted, 0) = 0
                         AND s.QuantityInBase > 0
                         AND p.BaseMeaurmentId = CASE
                             WHEN cm.IsBaseUnit = 1 THEN cm.MeaurmentID
                             ELSE cm.BaseMeaurmentId
                         END
                   ), 0) AS UsedInBase
            FROM Warehouses w
            INNER JOIN Meaurments cm ON cm.MeaurmentID = w.CapacityMeaurmentId
            WHERE ISNULL(w.IsDeleted, 0) = 0
              AND ISNULL(w.IsActive, 1) = 1
              AND w.Capacity IS NOT NULL
              AND w.Capacity > 0
              AND w.CapacityMeaurmentId IS NOT NULL
              AND cm.FactorToBase > 0
            ORDER BY w.Name
            """)).AsList();

        var items = new List<object>();

        foreach (var row in shortages)
        {
            items.Add(new
            {
                type = "product_shortage",
                severity = "warning",
                title = $"کمبود محصول «{row.Name}»",
                message =
                    $"موجودی {FormatQty(row.TotalStockQuantity)} {row.UnitName} — حداقل مجاز {FormatQty(row.MinStockQuantity)} {row.UnitName}",
                href = "/products/list",
                productId = row.ProductId,
                code = row.Code,
            });
        }

        foreach (var warehouse in warehouses)
        {
            if (warehouse.CapacityInBase <= 0) continue;

            var fillPercent = Math.Round(
                Math.Min(100m, warehouse.UsedInBase / warehouse.CapacityInBase * 100m),
                1);

            if (fillPercent >= WarehouseFullPercent)
            {
                items.Add(new
                {
                    type = "warehouse_full",
                    severity = "danger",
                    title = $"انبار «{warehouse.Name}» پر شده است",
                    message = $"پر بودن {fillPercent:0.#}٪ از ظرفیت ({FormatQty(warehouse.Capacity)} {warehouse.CapacityUnit})",
                    href = "/inventory/warehouses",
                    warehouseId = warehouse.WarehouseId,
                    fillPercent,
                });
            }
            else if (fillPercent < WarehouseLowPercent)
            {
                items.Add(new
                {
                    type = "warehouse_low",
                    severity = "info",
                    title = $"انبار «{warehouse.Name}» کمتر از ۲۰٪ پر است",
                    message = $"پر بودن {fillPercent:0.#}٪ — ظرفیت {FormatQty(warehouse.Capacity)} {warehouse.CapacityUnit}",
                    href = "/inventory/warehouses",
                    warehouseId = warehouse.WarehouseId,
                    fillPercent,
                });
            }
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
            "purchase" => ("خرید", "/transactions/purchase"),
            "sale" => ("فروش", "/transactions/sale"),
            "production" => ("تولید", "/production/daily"),
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
            warehouseName = row.WarehouseName,
            statusLabel = BuildStatusLabel(row),
            isPosted = row.IsPosted,
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
        if (row.OperationType == "production")
        {
            return row.IsPosted || row.Status == (int)ProductionBatchStatus.Posted
                ? "ثبت‌شده"
                : "پیش‌نویس";
        }

        if (row.DocumentType == (int)InvoiceDocumentType.PurchaseReturn)
            return "برگشت از خرید";
        if (row.DocumentType == (int)InvoiceDocumentType.SaleReturn)
            return "برگشت از فروش";

        return row.Status switch
        {
            (int)InvoiceStatus.Quotation => "استعلام",
            (int)InvoiceStatus.Proforma => "پیش‌فاکتور",
            (int)InvoiceStatus.Order => "آردر",
            (int)InvoiceStatus.Invoice => row.IsPosted ? "ثبت‌شده" : "فاکتور",
            _ => row.IsPosted ? "ثبت‌شده" : "در انتظار",
        };
    }

    private static string FormatQty(decimal value)
    {
        return value.ToString("0.####", CultureInfo.InvariantCulture);
    }

    private sealed class PerformanceAmountRow
    {
        public DateTime EventDate { get; set; }
        public int DocumentType { get; set; }
        public decimal AmountInBase { get; set; }
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
        public string? WarehouseName { get; set; }
        public int? DocumentType { get; set; }
        public int Status { get; set; }
        public bool IsPosted { get; set; }
    }

    private sealed class ProductShortageRow
    {
        public int ProductId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal MinStockQuantity { get; set; }
        public decimal TotalStockQuantity { get; set; }
        public string UnitName { get; set; } = string.Empty;
    }

    private sealed class WarehouseFillRow
    {
        public int WarehouseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Capacity { get; set; }
        public string CapacityUnit { get; set; } = string.Empty;
        public decimal CapacityInBase { get; set; }
        public decimal UsedInBase { get; set; }
    }
}
