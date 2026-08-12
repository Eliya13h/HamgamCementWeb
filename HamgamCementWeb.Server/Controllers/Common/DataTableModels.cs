using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Common;

public class DataTableRequest
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public DataTableSearch? Search { get; set; }
    public List<DataTableOrder>? Order { get; set; }
}

public class DataTableSearch
{
    public string? Value { get; set; }
    public bool Regex { get; set; }
}

public class DataTableOrder
{
    public int Column { get; set; }
    public string Dir { get; set; } = "asc";
}

public static class DataTableExtensions
{
    /// <summary>
    /// مرتب‌سازی داینامیک بر اساس ستون‌های مجاز دیتاتیبل با EF.Property
    /// </summary>
    public static IQueryable<T> ApplyDataTableOrder<T>(
        this IQueryable<T> query,
        List<DataTableOrder>? orders,
        IReadOnlyDictionary<int, string> orderColumns,
        string defaultColumn,
        bool defaultDescending = true)
        where T : class
    {
        IOrderedQueryable<T>? ordered = null;

        if (orders is not null)
        {
            foreach (var order in orders)
            {
                if (!orderColumns.TryGetValue(order.Column, out var column))
                {
                    continue;
                }

                var descending = string.Equals(order.Dir, "desc", StringComparison.OrdinalIgnoreCase);

                if (ordered is null)
                {
                    ordered = descending
                        ? query.OrderByDescending(e => EF.Property<object>(e, column))
                        : query.OrderBy(e => EF.Property<object>(e, column));
                }
                else
                {
                    ordered = descending
                        ? ordered.ThenByDescending(e => EF.Property<object>(e, column))
                        : ordered.ThenBy(e => EF.Property<object>(e, column));
                }
            }
        }

        ordered ??= defaultDescending
            ? query.OrderByDescending(e => EF.Property<object>(e, defaultColumn))
            : query.OrderBy(e => EF.Property<object>(e, defaultColumn));

        return ordered;
    }
}
