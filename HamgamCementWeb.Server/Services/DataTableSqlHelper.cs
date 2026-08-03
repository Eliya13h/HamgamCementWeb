namespace HamgamCementWeb.Server.Services;

internal static class DataTableSqlHelper
{
    public static string BuildOrderClause(
        IReadOnlyDictionary<int, string> columns,
        IReadOnlyList<DataTableOrderItem>? orders,
        string defaultOrder)
    {
        if (orders is null || orders.Count == 0)
        {
            return $"ORDER BY {defaultOrder}";
        }

        var parts = new List<string>();
        foreach (var order in orders)
        {
            if (!columns.TryGetValue(order.Column, out var column))
            {
                continue;
            }

            var direction = string.Equals(order.Dir, "desc", StringComparison.OrdinalIgnoreCase)
                ? "DESC"
                : "ASC";
            parts.Add($"{column} {direction}");
        }

        return parts.Count > 0
            ? "ORDER BY " + string.Join(", ", parts)
            : $"ORDER BY {defaultOrder}";
    }
}

public sealed class DataTableOrderItem
{
    public int Column { get; init; }
    public string Dir { get; init; } = "asc";
}
