using System.Data;
using System.Data.Common;
using Dapper;

namespace HamgamCementWeb.Server.Services;

public interface IDepartmentReadService
{
    Task<DepartmentDataTableResult> QueryDataTableAsync(
        DepartmentDataTableQuery query,
        CancellationToken cancellationToken = default);
}

public sealed class DepartmentReadService : IDepartmentReadService
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = "d.Name",
        [2] = "d.Description",
        [3] = "EmployeeCount",
    };

    private readonly ISqlConnectionFactory _sql;

    public DepartmentReadService(ISqlConnectionFactory sql)
    {
        _sql = sql;
    }

    public async Task<DepartmentDataTableResult> QueryDataTableAsync(
        DepartmentDataTableQuery query,
        CancellationToken cancellationToken = default)
    {
        var start = Math.Max(query.Start, 0);
        var length = query.Length <= 0 ? 10 : Math.Min(query.Length, 100);
        var search = query.Search?.Trim() ?? string.Empty;
        var hasSearch = search.Length > 0;

        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        const string baseFrom = """
            FROM dbo.Departments d
            WHERE ISNULL(d.IsDeleted, 0) = 0
            """;

        var where = string.Empty;
        var p = new DynamicParameters();
        if (hasSearch)
        {
            where = """
                 AND (
                    d.Name LIKE @Search
                    OR ISNULL(d.Description, N'') LIKE @Search
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
            "d.Name ASC");

        p.Add("Start", start);
        p.Add("Length", length);

        var rows = (await connection.QueryAsync<DepartmentTableRow>(
            new CommandDefinition(
                $"""
                 SELECT
                     d.DepartmentID AS DepartmentId,
                     d.Name,
                     ISNULL(d.Description, N'') AS Description,
                     (
                         SELECT COUNT(1)
                         FROM dbo.Employees e
                         WHERE e.DepartmentId = d.DepartmentID
                           AND ISNULL(e.IsDeleted, 0) = 0
                     ) AS EmployeeCount
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

        return new DepartmentDataTableResult
        {
            RecordsTotal = total,
            RecordsFiltered = filtered,
            Rows = rows,
        };
    }
}

public sealed class DepartmentDataTableQuery
{
    public int Start { get; init; }
    public int Length { get; init; }
    public string? Search { get; init; }
    public IReadOnlyList<DataTableOrderItem>? Order { get; init; }
}

public sealed class DepartmentDataTableResult
{
    public int RecordsTotal { get; init; }
    public int RecordsFiltered { get; init; }
    public IReadOnlyList<DepartmentTableRow> Rows { get; init; } = [];
}

public sealed class DepartmentTableRow
{
    public int RowNumber { get; set; }
    public int DepartmentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int EmployeeCount { get; init; }
}
