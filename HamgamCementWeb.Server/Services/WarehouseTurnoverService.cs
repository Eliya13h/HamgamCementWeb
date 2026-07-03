using HamgamCementWeb.Server.Controllers.Transport;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Inventory;
using HamgamCementWeb.Server.Data.Models.Invoice;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Services;

public class WarehouseTurnoverDataTableRequest : DataTableRequest
{
    public int? WarehouseId { get; set; }
    public int? ProductId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public record WarehouseTurnoverRow(
    int RowNumber,
    DateTime MovementDate,
    string MovementType,
    string MovementTypeCode,
    int DocumentType,
    string DocumentNumber,
    int? DocumentId,
    string CounterpartyName,
    int ProductId,
    string ProductCode,
    string ProductName,
    decimal Quantity,
    string MeaurmentName,
    string? MeaurmentSymbol,
    decimal QuantityInBase,
    decimal QuantityIn,
    decimal QuantityOut,
    decimal? RunningBalanceInBase,
    decimal UnitPrice,
    decimal LineTotal,
    int WarehouseId,
    string WarehouseName);

public interface IWarehouseTurnoverService
{
    Task<(int RecordsTotal, int RecordsFiltered, IReadOnlyList<WarehouseTurnoverRow> Rows)> GetDataTableAsync(
        WarehouseTurnoverDataTableRequest request,
        CancellationToken cancellationToken = default);
}

public class WarehouseTurnoverService : IWarehouseTurnoverService
{
    private readonly AppDbContext _db;

    public WarehouseTurnoverService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(int RecordsTotal, int RecordsFiltered, IReadOnlyList<WarehouseTurnoverRow> Rows)> GetDataTableAsync(
        WarehouseTurnoverDataTableRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.WarehouseId is not > 0)
        {
            return (0, 0, []);
        }

        var warehouseId = request.WarehouseId.Value;
        var movements = await LoadMovementsAsync(warehouseId, request.ProductId, request.DateFrom, request.DateTo, cancellationToken);
        var recordsTotal = movements.Count;

        var searchValue = request.Search?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            movements = movements
                .Where(m =>
                    m.ProductName.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                    m.ProductCode.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                    m.DocumentNumber.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                    m.CounterpartyName.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                    m.MovementTypeLabel.Contains(searchValue, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var recordsFiltered = movements.Count;
        movements = ApplyOrdering(movements, request.Order);

        if (request.ProductId is > 0)
        {
            ApplyRunningBalance(movements);
        }

        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 15 : Math.Min(request.Length, 100);
        var page = movements.Skip(start).Take(length).ToList();

        var rows = page.Select((m, index) => new WarehouseTurnoverRow(
            start + index + 1,
            m.MovementDate,
            m.MovementTypeLabel,
            m.MovementKind,
            m.DocumentType,
            m.DocumentNumber,
            m.DocumentId,
            m.CounterpartyName,
            m.ProductId,
            m.ProductCode,
            m.ProductName,
            m.Quantity,
            m.MeaurmentName,
            m.MeaurmentSymbol,
            m.QuantityInBase,
            m.QuantityIn,
            m.QuantityOut,
            m.RunningBalanceInBase,
            m.UnitPrice,
            m.LineTotal,
            m.WarehouseId,
            m.WarehouseName)).ToList();

        return (recordsTotal, recordsFiltered, rows);
    }

    private async Task<List<MovementEntry>> LoadMovementsAsync(
        int warehouseId,
        int? productId,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        var warehouseName = await _db.Warehouses
            .AsNoTracking()
            .Where(w => w.WarehouseID == warehouseId && w.IsDeleted != true)
            .Select(w => w.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var movements = new List<MovementEntry>();

        var purchaseQuery = _db.PurchaseItems
            .AsNoTracking()
            .Where(i =>
                i.IsDeleted != true &&
                i.Invoice.IsDeleted != true &&
                i.Invoice.IsPosted &&
                i.Invoice.WarehouseId == warehouseId &&
                (
                    i.Invoice.DocumentType == InvoiceDocumentType.PurchaseReturn ||
                    (i.Invoice.DocumentType == InvoiceDocumentType.Invoice &&
                     i.Invoice.Status == InvoiceStatus.Inoivce)));

        if (productId is > 0)
        {
            purchaseQuery = purchaseQuery.Where(i => i.ProductId == productId);
        }

        if (dateFrom.HasValue)
        {
            purchaseQuery = purchaseQuery.Where(i => i.Invoice.InvoiceDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var end = dateTo.Value.Date.AddDays(1).AddTicks(-1);
            purchaseQuery = purchaseQuery.Where(i => i.Invoice.InvoiceDate <= end);
        }

        var purchaseRows = await purchaseQuery
            .Select(i => new
            {
                i.PurchaseItemID,
                i.ProductId,
                ProductCode = i.Product.Code,
                ProductName = i.Product.Name,
                i.MeaurmentId,
                MeaurmentName = i.Meaurment.Name,
                MeaurmentSymbol = i.Meaurment.Symbol,
                i.Quantity,
                i.QuantityInBase,
                i.UnitPrice,
                i.LineTotal,
                Invoice = i.Invoice,
                SupplierName = i.Invoice.Supplier != null ? i.Invoice.Supplier.Name : string.Empty,
            })
            .ToListAsync(cancellationToken);

        foreach (var row in purchaseRows)
        {
            var isReturn = row.Invoice.DocumentType == InvoiceDocumentType.PurchaseReturn;
            movements.Add(new MovementEntry
            {
                MovementDate = row.Invoice.InvoiceDate,
                SortTimestamp = row.Invoice.PostedAt ?? row.Invoice.InvoiceDate,
                SortId = row.PurchaseItemID,
                MovementKind = isReturn ? "PurchaseReturnOut" : "PurchaseIn",
                MovementTypeLabel = isReturn ? "برگشت خرید" : "ورود خرید",
                DocumentType = (int)row.Invoice.DocumentType,
                DocumentNumber = row.Invoice.InvoiceNumber,
                DocumentId = row.Invoice.PurchaseInvoiceID,
                CounterpartyName = row.SupplierName,
                ProductId = row.ProductId,
                ProductCode = row.ProductCode,
                ProductName = row.ProductName,
                MeaurmentId = row.MeaurmentId,
                MeaurmentName = row.MeaurmentName,
                MeaurmentSymbol = row.MeaurmentSymbol,
                Quantity = row.Quantity,
                QuantityInBase = row.QuantityInBase,
                QuantityIn = isReturn ? 0 : row.QuantityInBase,
                QuantityOut = isReturn ? row.QuantityInBase : 0,
                UnitPrice = row.UnitPrice,
                LineTotal = row.LineTotal,
                WarehouseId = warehouseId,
                WarehouseName = warehouseName,
            });
        }

        var saleQuery = _db.SalesItems
            .AsNoTracking()
            .Where(i =>
                i.IsDeleted != true &&
                i.Invoice.IsDeleted != true &&
                i.Invoice.IsPosted &&
                i.Invoice.WarehouseId == warehouseId &&
                (
                    i.Invoice.DocumentType == InvoiceDocumentType.SaleReturn ||
                    (i.Invoice.DocumentType == InvoiceDocumentType.Invoice &&
                     (i.Invoice.Status == InvoiceStatus.Order || i.Invoice.Status == InvoiceStatus.Inoivce))));

        if (productId is > 0)
        {
            saleQuery = saleQuery.Where(i => i.ProductId == productId);
        }

        if (dateFrom.HasValue)
        {
            saleQuery = saleQuery.Where(i => i.Invoice.InvoiceDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var end = dateTo.Value.Date.AddDays(1).AddTicks(-1);
            saleQuery = saleQuery.Where(i => i.Invoice.InvoiceDate <= end);
        }

        var saleRows = await saleQuery
            .Select(i => new
            {
                i.SalesItemID,
                i.ProductId,
                ProductCode = i.Product.Code,
                ProductName = i.Product.Name,
                i.MeaurmentId,
                MeaurmentName = i.Meaurment.Name,
                MeaurmentSymbol = i.Meaurment.Symbol,
                i.Quantity,
                i.QuantityInBase,
                i.UnitPrice,
                i.LineTotal,
                Invoice = i.Invoice,
                CustomerName = i.Invoice.Customer != null ? i.Invoice.Customer.Name : string.Empty,
            })
            .ToListAsync(cancellationToken);

        foreach (var row in saleRows)
        {
            var isReturn = row.Invoice.DocumentType == InvoiceDocumentType.SaleReturn;
            movements.Add(new MovementEntry
            {
                MovementDate = row.Invoice.InvoiceDate,
                SortTimestamp = row.Invoice.PostedAt ?? row.Invoice.InvoiceDate,
                SortId = row.SalesItemID,
                MovementKind = isReturn ? "SaleReturnIn" : "SaleOut",
                MovementTypeLabel = isReturn ? "برگشت فروش" : "خروج فروش",
                DocumentType = (int)row.Invoice.DocumentType,
                DocumentNumber = row.Invoice.InvoiceNumber,
                DocumentId = row.Invoice.SaleInvoiceID,
                CounterpartyName = row.CustomerName,
                ProductId = row.ProductId,
                ProductCode = row.ProductCode,
                ProductName = row.ProductName,
                MeaurmentId = row.MeaurmentId,
                MeaurmentName = row.MeaurmentName,
                MeaurmentSymbol = row.MeaurmentSymbol,
                Quantity = row.Quantity,
                QuantityInBase = row.QuantityInBase,
                QuantityIn = isReturn ? row.QuantityInBase : 0,
                QuantityOut = isReturn ? 0 : row.QuantityInBase,
                UnitPrice = row.UnitPrice,
                LineTotal = row.LineTotal,
                WarehouseId = warehouseId,
                WarehouseName = warehouseName,
            });
        }

        var stocktakingQuery = _db.StocktakingLines
            .AsNoTracking()
            .Where(l =>
                l.IsDeleted != true &&
                l.Stocktaking.IsDeleted != true &&
                l.Stocktaking.Status == StocktakingStatus.Confirmed &&
                l.Stocktaking.WarehouseId == warehouseId &&
                l.DifferenceInBase != 0);

        if (productId is > 0)
        {
            stocktakingQuery = stocktakingQuery.Where(l => l.ProductId == productId);
        }

        if (dateFrom.HasValue)
        {
            stocktakingQuery = stocktakingQuery.Where(l => l.Stocktaking.StocktakingDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var end = dateTo.Value.Date.AddDays(1).AddTicks(-1);
            stocktakingQuery = stocktakingQuery.Where(l => l.Stocktaking.StocktakingDate <= end);
        }

        var stocktakingRows = await stocktakingQuery
            .Select(l => new
            {
                l.StocktakingLineID,
                l.ProductId,
                ProductCode = l.Product.Code,
                ProductName = l.Product.Name,
                l.CountedQuantity,
                l.CountedMeaurmentId,
                MeaurmentName = l.CountedMeaurment.Name,
                MeaurmentSymbol = l.CountedMeaurment.Symbol,
                l.CountedQuantityInBase,
                l.DifferenceInBase,
                Stocktaking = l.Stocktaking,
            })
            .ToListAsync(cancellationToken);

        foreach (var row in stocktakingRows)
        {
            var diff = row.DifferenceInBase;
            movements.Add(new MovementEntry
            {
                MovementDate = row.Stocktaking.StocktakingDate,
                SortTimestamp = row.Stocktaking.UpdatedAt ?? row.Stocktaking.StocktakingDate,
                SortId = row.StocktakingLineID,
                MovementKind = "StocktakingAdjust",
                MovementTypeLabel = "تعدیل انبارگردانی",
                DocumentType = 0,
                DocumentNumber = row.Stocktaking.Code,
                DocumentId = row.Stocktaking.StocktakingID,
                CounterpartyName = string.Empty,
                ProductId = row.ProductId,
                ProductCode = row.ProductCode,
                ProductName = row.ProductName,
                MeaurmentId = row.CountedMeaurmentId,
                MeaurmentName = row.MeaurmentName,
                MeaurmentSymbol = row.MeaurmentSymbol,
                Quantity = row.CountedQuantity,
                QuantityInBase = Math.Abs(diff),
                QuantityIn = diff > 0 ? diff : 0,
                QuantityOut = diff < 0 ? Math.Abs(diff) : 0,
                UnitPrice = 0,
                LineTotal = 0,
                WarehouseId = warehouseId,
                WarehouseName = warehouseName,
            });
        }

        return movements;
    }

    private static List<MovementEntry> ApplyOrdering(List<MovementEntry> movements, List<DataTableOrder>? orders)
    {
        if (orders is null || orders.Count == 0)
        {
            return movements
                .OrderBy(m => m.MovementDate)
                .ThenBy(m => m.SortTimestamp)
                .ThenBy(m => m.SortId)
                .ToList();
        }

        IOrderedEnumerable<MovementEntry>? ordered = null;
        foreach (var order in orders)
        {
            var descending = string.Equals(order.Dir, "desc", StringComparison.OrdinalIgnoreCase);
            ordered = order.Column switch
            {
                2 => Apply(ordered, movements, m => m.MovementDate, descending),
                3 => Apply(ordered, movements, m => m.MovementTypeLabel, descending),
                4 => Apply(ordered, movements, m => m.DocumentNumber, descending),
                5 => Apply(ordered, movements, m => m.CounterpartyName, descending),
                6 => Apply(ordered, movements, m => m.ProductCode, descending),
                7 => Apply(ordered, movements, m => m.ProductName, descending),
                8 => Apply(ordered, movements, m => m.QuantityIn, descending),
                9 => Apply(ordered, movements, m => m.QuantityOut, descending),
                10 => Apply(ordered, movements, m => m.RunningBalanceInBase ?? 0, descending),
                _ => ordered,
            };

            if (ordered is null && order.Column is 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9 or 10)
            {
                ordered = Apply(null, movements, m => m.MovementDate, descending);
            }
        }

        return (ordered ?? movements.OrderBy(m => m.MovementDate).ThenBy(m => m.SortId)).ToList();
    }

    private static IOrderedEnumerable<MovementEntry> Apply<T>(
        IOrderedEnumerable<MovementEntry>? ordered,
        List<MovementEntry> source,
        Func<MovementEntry, T> keySelector,
        bool descending)
    {
        if (ordered is null)
        {
            return descending ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector);
        }

        return descending ? ordered.ThenByDescending(keySelector) : ordered.ThenBy(keySelector);
    }

    private static void ApplyRunningBalance(List<MovementEntry> movements)
    {
        var chronological = movements
            .OrderBy(m => m.MovementDate)
            .ThenBy(m => m.SortTimestamp)
            .ThenBy(m => m.SortId)
            .ToList();

        decimal balance = 0;
        var balanceByKey = new Dictionary<string, decimal>();
        foreach (var entry in chronological)
        {
            balance += entry.QuantityIn - entry.QuantityOut;
            balanceByKey[EntryKey(entry)] = balance;
        }

        foreach (var entry in movements)
        {
            if (balanceByKey.TryGetValue(EntryKey(entry), out var value))
            {
                entry.RunningBalanceInBase = value;
            }
        }
    }

    private static string EntryKey(MovementEntry entry) =>
        $"{entry.MovementKind}:{entry.DocumentId}:{entry.SortId}";

    private sealed class MovementEntry
    {
        public DateTime MovementDate { get; init; }
        public DateTime SortTimestamp { get; init; }
        public int SortId { get; init; }
        public string MovementKind { get; init; } = string.Empty;
        public string MovementTypeLabel { get; init; } = string.Empty;
        public int DocumentType { get; init; }
        public string DocumentNumber { get; init; } = string.Empty;
        public int? DocumentId { get; init; }
        public string CounterpartyName { get; init; } = string.Empty;
        public int ProductId { get; init; }
        public string ProductCode { get; init; } = string.Empty;
        public string ProductName { get; init; } = string.Empty;
        public int MeaurmentId { get; init; }
        public string MeaurmentName { get; init; } = string.Empty;
        public string? MeaurmentSymbol { get; init; }
        public decimal Quantity { get; init; }
        public decimal QuantityInBase { get; init; }
        public decimal QuantityIn { get; init; }
        public decimal QuantityOut { get; init; }
        public decimal? RunningBalanceInBase { get; set; }
        public decimal UnitPrice { get; init; }
        public decimal LineTotal { get; init; }
        public int WarehouseId { get; init; }
        public string WarehouseName { get; init; } = string.Empty;
    }
}
