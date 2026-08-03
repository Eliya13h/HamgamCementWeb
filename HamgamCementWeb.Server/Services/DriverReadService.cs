using System.Data;
using System.Data.Common;
using Dapper;

namespace HamgamCementWeb.Server.Services;

public interface IDriverReadService
{
    Task<DriverDataTableResult> QueryDataTableAsync(
        DriverDataTableQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DriverOptionRow>> ListActiveAsync(CancellationToken cancellationToken = default);
}

public sealed class DriverReadService : IDriverReadService
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = "LTRIM(RTRIM(CONCAT(d.Name, N' ', d.Family)))",
        [2] = "d.NationalCode",
        [3] = "d.Mobile",
        [4] = "d.DefaultShare",
        [5] = "d.IsActive",
    };

    private readonly ISqlConnectionFactory _sql;

    public DriverReadService(ISqlConnectionFactory sql)
    {
        _sql = sql;
    }

    public async Task<DriverDataTableResult> QueryDataTableAsync(
        DriverDataTableQuery query,
        CancellationToken cancellationToken = default)
    {
        var start = Math.Max(query.Start, 0);
        var length = query.Length <= 0 ? 10 : Math.Min(query.Length, 100);
        var search = query.Search?.Trim() ?? string.Empty;
        var hasSearch = search.Length > 0;

        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        const string baseFrom = """
            FROM dbo.Drivers d
            WHERE ISNULL(d.IsDeleted, 0) = 0
            """;

        var where = string.Empty;
        var p = new DynamicParameters();
        if (hasSearch)
        {
            where = """
                 AND (
                    d.Name LIKE @Search
                    OR d.Family LIKE @Search
                    OR d.NationalCode LIKE @Search
                    OR d.Mobile LIKE @Search
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
            "d.CreatedAt DESC");

        p.Add("Start", start);
        p.Add("Length", length);

        var rows = (await connection.QueryAsync<DriverTableRow>(
            new CommandDefinition(
                $"""
                 SELECT
                     d.DriverID AS DriverId,
                     CAST(d.Title AS int) AS Title,
                     d.Name,
                     d.FatherName,
                     d.Family,
                     LTRIM(RTRIM(CONCAT(d.Name, N' ', d.Family))) AS FullName,
                     d.NationalCode,
                     d.Mobile,
                     d.Address,
                     d.DefaultShare,
                     CAST(CASE WHEN d.IsActive = 1 THEN 1 ELSE 0 END AS bit) AS IsActive
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

        return new DriverDataTableResult
        {
            RecordsTotal = total,
            RecordsFiltered = filtered,
            Rows = rows,
        };
    }

    public async Task<IReadOnlyList<DriverOptionRow>> ListActiveAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<DriverOptionRow>(
            new CommandDefinition(
                """
                SELECT
                    d.DriverID AS Value,
                    LTRIM(RTRIM(CONCAT(d.Name, N' ', d.Family))) AS Label,
                    d.DefaultVehicleId
                FROM dbo.Drivers d
                WHERE ISNULL(d.IsDeleted, 0) = 0
                  AND ISNULL(d.IsActive, 0) = 1
                ORDER BY d.Name, d.Family
                """,
                cancellationToken: cancellationToken));

        return rows.AsList();
    }
}

public sealed class DriverDataTableQuery
{
    public int Start { get; init; }
    public int Length { get; init; }
    public string? Search { get; init; }
    public IReadOnlyList<DataTableOrderItem>? Order { get; init; }
}

public sealed class DriverDataTableResult
{
    public int RecordsTotal { get; init; }
    public int RecordsFiltered { get; init; }
    public IReadOnlyList<DriverTableRow> Rows { get; init; } = [];
}

public sealed class DriverTableRow
{
    public int RowNumber { get; set; }
    public int DriverId { get; init; }
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

public sealed class DriverOptionRow
{
    public int Value { get; init; }
    public string Label { get; init; } = string.Empty;
    public int? DefaultVehicleId { get; init; }
}
