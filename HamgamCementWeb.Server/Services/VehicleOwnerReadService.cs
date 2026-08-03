using System.Data;
using System.Data.Common;
using Dapper;

namespace HamgamCementWeb.Server.Services;

public interface IVehicleOwnerReadService
{
    Task<VehicleOwnerDataTableResult> QueryDataTableAsync(
        VehicleOwnerDataTableQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehicleOwnerOptionRow>> ListActiveAsync(
        CancellationToken cancellationToken = default);
}

public sealed class VehicleOwnerReadService : IVehicleOwnerReadService
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = "LTRIM(RTRIM(CONCAT(v.Name, N' ', v.Family)))",
        [2] = "v.NationalCode",
        [3] = "v.Mobile",
        [4] = "v.DefaultShare",
        [5] = "v.IsActive",
    };

    private readonly ISqlConnectionFactory _sql;

    public VehicleOwnerReadService(ISqlConnectionFactory sql)
    {
        _sql = sql;
    }

    public async Task<VehicleOwnerDataTableResult> QueryDataTableAsync(
        VehicleOwnerDataTableQuery query,
        CancellationToken cancellationToken = default)
    {
        var start = Math.Max(query.Start, 0);
        var length = query.Length <= 0 ? 10 : Math.Min(query.Length, 100);
        var search = query.Search?.Trim() ?? string.Empty;
        var hasSearch = search.Length > 0;

        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        const string baseFrom = """
            FROM dbo.VehicleOwners v
            WHERE ISNULL(v.IsDeleted, 0) = 0
            """;

        var where = string.Empty;
        var p = new DynamicParameters();
        if (hasSearch)
        {
            where = """
                 AND (
                    v.Name LIKE @Search
                    OR v.Family LIKE @Search
                    OR v.NationalCode LIKE @Search
                    OR v.Mobile LIKE @Search
                )
                """;
            p.Add("Search", $"%{search}%");
        }

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                $"SELECT COUNT(1) {baseFrom}",
                cancellationToken: cancellationToken));

        var filtered = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                $"SELECT COUNT(1) {baseFrom}{where}",
                p,
                cancellationToken: cancellationToken));

        var orderClause = DataTableSqlHelper.BuildOrderClause(
            OrderColumns,
            query.Order,
            "v.CreatedAt DESC");

        p.Add("Start", start);
        p.Add("Length", length);

        var rows = (await connection.QueryAsync<VehicleOwnerTableRow>(
            new CommandDefinition(
                $"""
                 SELECT
                     v.VehicleOwnerID AS VehicleOwnerId,
                     CAST(v.Title AS int) AS Title,
                     v.Name,
                     v.FatherName,
                     v.Family,
                     LTRIM(RTRIM(CONCAT(v.Name, N' ', v.Family))) AS FullName,
                     v.NationalCode,
                     v.Mobile,
                     v.Address,
                     v.DefaultShare,
                     CAST(CASE WHEN v.IsActive = 1 THEN 1 ELSE 0 END AS bit) AS IsActive
                 {baseFrom}
                 {where}
                 {orderClause}
                 OFFSET @Start ROWS FETCH NEXT @Length ROWS ONLY
                 """,
                p,
                cancellationToken: cancellationToken))).ToList();

        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].RowNumber = start + i + 1;
        }

        return new VehicleOwnerDataTableResult
        {
            RecordsTotal = total,
            RecordsFiltered = filtered,
            Rows = rows,
        };
    }

    public async Task<IReadOnlyList<VehicleOwnerOptionRow>> ListActiveAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<VehicleOwnerOptionRow>(
            new CommandDefinition(
                """
                SELECT
                    v.VehicleOwnerID AS Value,
                    LTRIM(RTRIM(CONCAT(v.Name, N' ', v.Family))) AS Label
                FROM dbo.VehicleOwners v
                WHERE ISNULL(v.IsDeleted, 0) = 0
                  AND ISNULL(v.IsActive, 0) = 1
                ORDER BY v.Name, v.Family
                """,
                cancellationToken: cancellationToken));

        return rows.AsList();
    }
}

public sealed class VehicleOwnerDataTableQuery
{
    public int Start { get; init; }
    public int Length { get; init; }
    public string? Search { get; init; }
    public IReadOnlyList<DataTableOrderItem>? Order { get; init; }
}

public sealed class VehicleOwnerDataTableResult
{
    public int RecordsTotal { get; init; }
    public int RecordsFiltered { get; init; }
    public IReadOnlyList<VehicleOwnerTableRow> Rows { get; init; } = [];
}

public sealed class VehicleOwnerTableRow
{
    public int RowNumber { get; set; }
    public int VehicleOwnerId { get; init; }
    public int Title { get; init; }
    public string Name { get; init; } = string.Empty;
    public string FatherName { get; init; } = string.Empty;
    public string Family { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string NationalCode { get; init; } = string.Empty;
    public string Mobile { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public decimal DefaultShare { get; init; }
    public bool IsActive { get; init; }
}

public sealed class VehicleOwnerOptionRow
{
    public int Value { get; init; }
    public string Label { get; init; } = string.Empty;
}
