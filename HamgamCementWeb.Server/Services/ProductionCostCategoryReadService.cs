using System.Data.Common;
using Dapper;
using HamgamCementWeb.Server.Data;

namespace HamgamCementWeb.Server.Services;

public interface IProductionCostCategoryReadService
{
    Task<(int Total, int Filtered, IReadOnlyList<ProductionCostCategoryListRow> Rows)> GetDataTableAsync(
        int start,
        int length,
        string? search,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductionCostCategoryOptionRow>> GetListAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    Task<ProductionCostCategoryDetailRow?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}

public class ProductionCostCategoryReadService : IProductionCostCategoryReadService
{
    private readonly ISqlConnectionFactory _sql;

    public ProductionCostCategoryReadService(ISqlConnectionFactory sql)
    {
        _sql = sql;
    }

    public async Task<(int Total, int Filtered, IReadOnlyList<ProductionCostCategoryListRow> Rows)> GetDataTableAsync(
        int start,
        int length,
        string? search,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        const string baseWhere = "WHERE c.IsDeleted = 0";
        var where = baseWhere;
        var p = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += " AND (c.Name LIKE @Search OR ISNULL(c.Description,'') LIKE @Search)";
            p.Add("Search", $"%{search.Trim()}%");
        }

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                $"SELECT COUNT(1) FROM dbo.ProductionCostCategories c {baseWhere}",
                cancellationToken: cancellationToken));

        var filtered = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                $"SELECT COUNT(1) FROM dbo.ProductionCostCategories c {where}",
                p,
                cancellationToken: cancellationToken));

        p.Add("Offset", start);
        p.Add("Fetch", length);

        var rows = (await connection.QueryAsync<ProductionCostCategoryListRow>(
            new CommandDefinition(
                $"""
                 SELECT
                     c.ProductionCostCategoryID AS ProductionCostCategoryId,
                     c.Name,
                     c.Code,
                     c.Description,
                     c.IsSystem,
                     CAST(c.CostType AS int) AS CostType,
                     c.SortOrder,
                     CAST(CASE WHEN c.IsActive = 1 OR c.IsActive IS NULL THEN 1 ELSE 0 END AS bit) AS IsActive,
                     (SELECT COUNT(1) FROM dbo.ProductionCostCategoryDepartments d
                      WHERE d.ProductionCostCategoryId = c.ProductionCostCategoryID) AS DepartmentsCount,
                     (SELECT STRING_AGG(dep.Name, N'، ') WITHIN GROUP (ORDER BY dep.Name)
                      FROM dbo.ProductionCostCategoryDepartments map
                      INNER JOIN dbo.Departments dep ON dep.DepartmentID = map.DepartmentId
                      WHERE map.ProductionCostCategoryId = c.ProductionCostCategoryID
                        AND (dep.IsDeleted IS NULL OR dep.IsDeleted = 0)) AS DepartmentNamesText,
                     (SELECT STRING_AGG(CAST(map.DepartmentId AS nvarchar(20)), N',')
                      FROM dbo.ProductionCostCategoryDepartments map
                      WHERE map.ProductionCostCategoryId = c.ProductionCostCategoryID) AS DepartmentIdsText
                 FROM dbo.ProductionCostCategories c
                 {where}
                 ORDER BY c.IsSystem DESC, c.SortOrder, c.Name
                 OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
                 """,
                p,
                cancellationToken: cancellationToken))).ToList();

        return (total, filtered, rows);
    }

    public async Task<IReadOnlyList<ProductionCostCategoryOptionRow>> GetListAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        var where = "WHERE c.IsDeleted = 0";
        if (activeOnly)
        {
            where += " AND (c.IsActive IS NULL OR c.IsActive = 1)";
        }

        var rows = await connection.QueryAsync<ProductionCostCategoryOptionRow>(
            new CommandDefinition(
                $"""
                 SELECT
                     c.ProductionCostCategoryID AS Value,
                     c.Name AS Label,
                     c.IsSystem,
                     CAST(c.CostType AS int) AS CostType,
                     c.Code
                 FROM dbo.ProductionCostCategories c
                 {where}
                 ORDER BY c.IsSystem DESC, c.SortOrder, c.Name
                 """,
                cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<ProductionCostCategoryDetailRow?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ProductionCostCategoryDetailRow>(
            new CommandDefinition(
                """
                SELECT
                    c.ProductionCostCategoryID AS ProductionCostCategoryId,
                    c.Name,
                    c.Code,
                    c.Description,
                    c.IsSystem,
                    CAST(c.CostType AS int) AS CostType,
                    c.SortOrder,
                    CAST(CASE WHEN c.IsActive = 1 OR c.IsActive IS NULL THEN 1 ELSE 0 END AS bit) AS IsActive
                FROM dbo.ProductionCostCategories c
                WHERE c.ProductionCostCategoryID = @Id AND c.IsDeleted = 0
                """,
                new { Id = id },
                cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        var departmentIds = (await connection.QueryAsync<int>(
            new CommandDefinition(
                """
                SELECT DepartmentId
                FROM dbo.ProductionCostCategoryDepartments
                WHERE ProductionCostCategoryId = @Id
                """,
                new { Id = id },
                cancellationToken: cancellationToken))).ToList();

        row.DepartmentIds = departmentIds;
        return row;
    }
}

public sealed class ProductionCostCategoryListRow
{
    public int ProductionCostCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public int CostType { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int DepartmentsCount { get; set; }
    public string? DepartmentNamesText { get; set; }
    public string? DepartmentIdsText { get; set; }
}

public sealed class ProductionCostCategoryOptionRow
{
    public int Value { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public int CostType { get; set; }
    public string? Code { get; set; }
}

public sealed class ProductionCostCategoryDetailRow
{
    public int ProductionCostCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public int CostType { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public List<int> DepartmentIds { get; set; } = [];
}
