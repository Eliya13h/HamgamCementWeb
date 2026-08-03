using System.Data;
using System.Data.Common;
using Dapper;

namespace HamgamCementWeb.Server.Services;

public interface IEmployeeReadService
{
    Task<EmployeeDataTableResult> QueryDataTableAsync(
        EmployeeDataTableQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartmentOptionRow>> ListActiveDepartmentsAsync(
        CancellationToken cancellationToken = default);
}

public sealed class EmployeeReadService : IEmployeeReadService
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = "LTRIM(RTRIM(CONCAT(e.Name, N' ', e.Family)))",
        [2] = "e.NationalCode",
        [3] = "e.Mobile",
        [4] = "d.Name",
        [5] = "e.Sallary",
        [6] = "e.IsActive",
    };

    private readonly ISqlConnectionFactory _sql;

    public EmployeeReadService(ISqlConnectionFactory sql)
    {
        _sql = sql;
    }

    public async Task<EmployeeDataTableResult> QueryDataTableAsync(
        EmployeeDataTableQuery query,
        CancellationToken cancellationToken = default)
    {
        var start = Math.Max(query.Start, 0);
        var length = query.Length <= 0 ? 10 : Math.Min(query.Length, 100);
        var search = query.Search?.Trim() ?? string.Empty;
        var hasSearch = search.Length > 0;

        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        const string baseFrom = """
            FROM dbo.Employees e
            LEFT JOIN dbo.Departments d ON d.DepartmentID = e.DepartmentId
            WHERE ISNULL(e.IsDeleted, 0) = 0
            """;

        var where = string.Empty;
        var p = new DynamicParameters();
        if (hasSearch)
        {
            where = """
                 AND (
                    e.Name LIKE @Search
                    OR e.Family LIKE @Search
                    OR e.NationalCode LIKE @Search
                    OR e.Mobile LIKE @Search
                    OR ISNULL(d.Name, N'') LIKE @Search
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
            "e.CreatedAt DESC");

        p.Add("Start", start);
        p.Add("Length", length);

        var rows = (await connection.QueryAsync<EmployeeTableRow>(
            new CommandDefinition(
                $"""
                 SELECT
                     e.EmployeeID AS EmployeeId,
                     CAST(e.Title AS int) AS Title,
                     e.Name,
                     e.FatherName,
                     e.Family,
                     LTRIM(RTRIM(CONCAT(e.Name, N' ', e.Family))) AS FullName,
                     e.NationalCode,
                     e.Mobile,
                     e.Address,
                     e.Sallary,
                     e.DepartmentId,
                     ISNULL(d.Name, N'') AS DepartmentName,
                     CAST(CASE WHEN e.IsActive = 1 THEN 1 ELSE 0 END AS bit) AS IsActive
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

        return new EmployeeDataTableResult
        {
            RecordsTotal = total,
            RecordsFiltered = filtered,
            Rows = rows,
        };
    }

    public async Task<IReadOnlyList<DepartmentOptionRow>> ListActiveDepartmentsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<DepartmentOptionRow>(
            new CommandDefinition(
                """
                SELECT
                    d.DepartmentID AS DepartmentId,
                    d.Name
                FROM dbo.Departments d
                WHERE ISNULL(d.IsDeleted, 0) = 0
                  AND ISNULL(d.IsActive, 0) = 1
                ORDER BY d.Name
                """,
                cancellationToken: cancellationToken));

        return rows.AsList();
    }
}

public sealed class EmployeeDataTableQuery
{
    public int Start { get; init; }
    public int Length { get; init; }
    public string? Search { get; init; }
    public IReadOnlyList<DataTableOrderItem>? Order { get; init; }
}

public sealed class EmployeeDataTableResult
{
    public int RecordsTotal { get; init; }
    public int RecordsFiltered { get; init; }
    public IReadOnlyList<EmployeeTableRow> Rows { get; init; } = [];
}

public sealed class EmployeeTableRow
{
    public int RowNumber { get; set; }
    public int EmployeeId { get; init; }
    public int Title { get; init; }
    public string Name { get; init; } = string.Empty;
    public string FatherName { get; init; } = string.Empty;
    public string Family { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string NationalCode { get; init; } = string.Empty;
    public string Mobile { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public decimal Sallary { get; init; }
    public int DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class DepartmentOptionRow
{
    public int DepartmentId { get; init; }
    public string Name { get; init; } = string.Empty;
}
