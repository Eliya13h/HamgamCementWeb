using System.Data;
using System.Data.Common;
using System.Globalization;
using Dapper;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Production;

namespace HamgamCementWeb.Server.Services;

public interface IProductionFormulaReadService
{
    Task<object> GetSystemCostHintsAsync(CancellationToken cancellationToken = default);

    Task<(int Total, int Filtered, IReadOnlyList<ProductionFormulaListRow> Rows)> GetDataTableAsync(
        int start,
        int length,
        string? search,
        string orderColumn,
        bool ascending,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductionFormulaOptionRow>> GetListAsync(
        int? productId,
        CancellationToken cancellationToken = default);

    Task<ProductionFormulaDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>
/// خواندن فرمول‌های تولید و پیشنهاد هزینه‌های سیستمی با Dapper.
/// </summary>
public class ProductionFormulaReadService : IProductionFormulaReadService
{
    private static readonly HashSet<string> AllowedOrderColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Name", "ProductName", "BaseQuantity", "Mode", "IsDefault",
    };

    private readonly ISqlConnectionFactory _sql;

    public ProductionFormulaReadService(ISqlConnectionFactory sql)
    {
        _sql = sql;
    }

    public async Task<object> GetSystemCostHintsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        // جمع پایه حقوق (Sallary) بر اساس بخش‌های وصل‌شده به دسته‌های سیستمی — بدون حضور و غیاب
        const string sql = """
            SELECT
                c.Code AS CategoryCode,
                c.Name AS CategoryName,
                c.ProductionCostCategoryID AS CategoryId,
                ISNULL(SUM(e.Sallary), 0) AS Amount,
                COUNT(e.EmployeeID) AS EmployeeCount
            FROM dbo.ProductionCostCategories c
            LEFT JOIN dbo.ProductionCostCategoryDepartments cd
                ON cd.ProductionCostCategoryId = c.ProductionCostCategoryID
            LEFT JOIN dbo.Departments d
                ON d.DepartmentID = cd.DepartmentId
               AND (d.IsDeleted IS NULL OR d.IsDeleted = 0)
            LEFT JOIN dbo.Employees e
                ON e.DepartmentId = d.DepartmentID
               AND (e.IsDeleted IS NULL OR e.IsDeleted = 0)
            WHERE c.IsDeleted = 0
              AND c.Code IN (N'DIRECT_WAGE', N'OVERHEAD')
            GROUP BY c.Code, c.Name, c.ProductionCostCategoryID
            """;

        var rows = (await connection.QueryAsync<SystemCostHintRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken))).ToList();

        var deptSql = """
            SELECT
                c.Code AS CategoryCode,
                d.Name AS DepartmentName
            FROM dbo.ProductionCostCategories c
            INNER JOIN dbo.ProductionCostCategoryDepartments cd
                ON cd.ProductionCostCategoryId = c.ProductionCostCategoryID
            INNER JOIN dbo.Departments d
                ON d.DepartmentID = cd.DepartmentId
               AND (d.IsDeleted IS NULL OR d.IsDeleted = 0)
            WHERE c.IsDeleted = 0
              AND c.Code IN (N'DIRECT_WAGE', N'OVERHEAD')
            ORDER BY d.Name
            """;

        var deptRows = (await connection.QueryAsync<CategoryDepartmentNameRow>(
            new CommandDefinition(deptSql, cancellationToken: cancellationToken))).ToList();

        var direct = rows.FirstOrDefault(r => r.CategoryCode == ProductionCostCategoryCode.DirectWage);
        var overhead = rows.FirstOrDefault(r => r.CategoryCode == ProductionCostCategoryCode.Overhead);

        var directDepts = deptRows
            .Where(r => r.CategoryCode == ProductionCostCategoryCode.DirectWage)
            .Select(r => r.DepartmentName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .ToList();
        var overheadDepts = deptRows
            .Where(r => r.CategoryCode == ProductionCostCategoryCode.Overhead)
            .Select(r => r.DepartmentName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .ToList();

        // هزینه روزانه = جمع حقوق ماهانه ÷ تعداد روز ماه شمسی جاری
        var calendar = new PersianCalendar();
        var today = DateTime.Now;
        var solarYear = calendar.GetYear(today);
        var solarMonth = calendar.GetMonth(today);
        var daysInSolarMonth = calendar.GetDaysInMonth(solarYear, solarMonth);
        if (daysInSolarMonth <= 0)
        {
            daysInSolarMonth = 30;
        }

        static decimal ToDailyAmount(decimal monthlyTotal, int days) =>
            days <= 0 ? 0m : Math.Round(monthlyTotal / days, 4, MidpointRounding.AwayFromZero);

        static string DailyWageDescription(IReadOnlyList<string> depts) =>
            depts.Count == 0
                ? "هزینه دستمزد روزانه کارمندان (بخشی انتخاب نشده)"
                : $"هزینه دستمزد روزانه کارمندان بخش {string.Join("، ", depts)}";

        var directMonthly = direct?.Amount ?? 0m;
        var overheadMonthly = overhead?.Amount ?? 0m;

        return new
        {
            daysInSolarMonth,
            solarYear,
            solarMonth,
            directWage = new
            {
                costType = (int)ProductionCostType.DirectWage,
                productionCostCategoryId = direct?.CategoryId,
                amount = ToDailyAmount(directMonthly, daysInSolarMonth),
                monthlyAmount = directMonthly,
                employeeCount = direct?.EmployeeCount ?? 0,
                departmentNames = directDepts,
                description = DailyWageDescription(directDepts),
                amountMode = (int)ProductionCostAmountMode.Flat,
                isSystemCalculated = true,
            },
            overhead = new
            {
                costType = (int)ProductionCostType.Overhead,
                productionCostCategoryId = overhead?.CategoryId,
                amount = ToDailyAmount(overheadMonthly, daysInSolarMonth),
                monthlyAmount = overheadMonthly,
                employeeCount = overhead?.EmployeeCount ?? 0,
                departmentNames = overheadDepts,
                description = DailyWageDescription(overheadDepts),
                amountMode = (int)ProductionCostAmountMode.Flat,
                isSystemCalculated = true,
            },
        };
    }

    public async Task<(int Total, int Filtered, IReadOnlyList<ProductionFormulaListRow> Rows)> GetDataTableAsync(
        int start,
        int length,
        string? search,
        string orderColumn,
        bool ascending,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        // فقط ستون‌های whitelist در ORDER BY قرار می‌گیرند
        var orderBy = AllowedOrderColumns.Contains(orderColumn) ? orderColumn : "Name";
        var dir = ascending ? "ASC" : "DESC";
        var orderSql = orderBy switch
        {
            "ProductName" => $"p.Name {dir}",
            "BaseQuantity" => $"f.BaseQuantity {dir}",
            "Mode" => $"f.Mode {dir}",
            "IsDefault" => $"f.IsDefault {dir}",
            _ => $"f.Name {dir}",
        };

        const string baseWhere = "WHERE f.IsDeleted = 0";
        var where = baseWhere;
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            where += """
                 AND (f.Name LIKE @Search OR p.Name LIKE @Search OR ISNULL(f.Notes,'') LIKE @Search)
                """;
            p.Add("Search", $"%{search.Trim()}%");
        }

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                $"SELECT COUNT(1) FROM dbo.ProductionFormulas f {baseWhere}",
                cancellationToken: cancellationToken));

        var filtered = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                $"""
                 SELECT COUNT(1)
                 FROM dbo.ProductionFormulas f
                 INNER JOIN dbo.Products p ON p.ProductID = f.ProductId
                 {where}
                 """,
                p,
                cancellationToken: cancellationToken));

        p.Add("Offset", start);
        p.Add("Fetch", length);

        var rows = (await connection.QueryAsync<ProductionFormulaListRow>(
            new CommandDefinition(
                $"""
                 SELECT
                     f.ProductionFormulaID AS ProductionFormulaId,
                     f.Name,
                     f.ProductId,
                     p.Name AS ProductName,
                     f.MeaurmentId,
                     m.Name AS MeaurmentName,
                     f.BaseQuantity,
                     CAST(f.Mode AS int) AS Mode,
                     f.IsDefault,
                     (SELECT COUNT(1) FROM dbo.ProductionFormulaMaterialLines ml
                      WHERE ml.ProductionFormulaId = f.ProductionFormulaID AND ml.IsDeleted = 0) AS MaterialLinesCount,
                     (SELECT COUNT(1) FROM dbo.ProductionFormulaCostLines cl
                      WHERE cl.ProductionFormulaId = f.ProductionFormulaID AND cl.IsDeleted = 0) AS CostLinesCount,
                     f.Notes
                 FROM dbo.ProductionFormulas f
                 INNER JOIN dbo.Products p ON p.ProductID = f.ProductId
                 INNER JOIN dbo.Meaurments m ON m.MeaurmentID = f.MeaurmentId
                 {where}
                 ORDER BY {orderSql}, f.ProductionFormulaID DESC
                 OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
                 """,
                p,
                cancellationToken: cancellationToken))).ToList();

        return (total, filtered, rows);
    }

    public async Task<IReadOnlyList<ProductionFormulaOptionRow>> GetListAsync(
        int? productId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        var where = "WHERE f.IsDeleted = 0 AND (f.IsActive IS NULL OR f.IsActive = 1)";
        var p = new DynamicParameters();
        if (productId is > 0)
        {
            where += " AND f.ProductId = @ProductId";
            p.Add("ProductId", productId.Value);
        }

        var rows = await connection.QueryAsync<ProductionFormulaOptionRow>(
            new CommandDefinition(
                $"""
                 SELECT
                     f.ProductionFormulaID AS Value,
                     CASE WHEN f.IsDefault = 1 THEN f.Name + N' (پیش‌فرض)' ELSE f.Name END AS Label,
                     f.ProductId,
                     p.Name AS ProductName,
                     f.MeaurmentId,
                     f.BaseQuantity,
                     CAST(f.Mode AS int) AS Mode,
                     f.IsDefault
                 FROM dbo.ProductionFormulas f
                 INNER JOIN dbo.Products p ON p.ProductID = f.ProductId
                 {where}
                 ORDER BY f.IsDefault DESC, f.Name
                 """,
                p,
                cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<ProductionFormulaDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        var header = await connection.QuerySingleOrDefaultAsync<ProductionFormulaHeaderRow>(
            new CommandDefinition(
                """
                SELECT
                    f.ProductionFormulaID AS ProductionFormulaId,
                    f.Name,
                    f.ProductId,
                    p.Name AS ProductName,
                    f.MeaurmentId,
                    m.Name AS MeaurmentName,
                    f.BaseQuantity,
                    CAST(f.Mode AS int) AS Mode,
                    f.IsDefault,
                    f.Notes
                FROM dbo.ProductionFormulas f
                INNER JOIN dbo.Products p ON p.ProductID = f.ProductId
                INNER JOIN dbo.Meaurments m ON m.MeaurmentID = f.MeaurmentId
                WHERE f.ProductionFormulaID = @Id AND f.IsDeleted = 0
                """,
                new { Id = id },
                cancellationToken: cancellationToken));

        if (header is null)
        {
            return null;
        }

        var materials = (await connection.QueryAsync<ProductionFormulaMaterialRow>(
            new CommandDefinition(
                """
                SELECT
                    ml.ProductionFormulaMaterialLineID AS ProductionFormulaMaterialLineId,
                    ml.ProductId,
                    p.Name AS ProductName,
                    ml.MeaurmentId,
                    m.Name AS MeaurmentName,
                    ml.Quantity,
                    ml.DefaultWarehouseId,
                    w.Name AS DefaultWarehouseName
                FROM dbo.ProductionFormulaMaterialLines ml
                INNER JOIN dbo.Products p ON p.ProductID = ml.ProductId
                INNER JOIN dbo.Meaurments m ON m.MeaurmentID = ml.MeaurmentId
                LEFT JOIN dbo.Warehouses w ON w.WarehouseID = ml.DefaultWarehouseId
                WHERE ml.ProductionFormulaId = @Id AND ml.IsDeleted = 0
                ORDER BY ml.ProductionFormulaMaterialLineID
                """,
                new { Id = id },
                cancellationToken: cancellationToken))).ToList();

        var costs = (await connection.QueryAsync<ProductionFormulaCostRow>(
            new CommandDefinition(
                """
                SELECT
                    cl.ProductionFormulaCostLineID AS ProductionFormulaCostLineId,
                    CAST(cl.CostType AS int) AS CostType,
                    cl.ProductionCostCategoryId,
                    c.Name AS CostCategoryName,
                    cl.Description,
                    CAST(cl.AmountMode AS int) AS AmountMode,
                    cl.Amount,
                    cl.AccountId
                FROM dbo.ProductionFormulaCostLines cl
                LEFT JOIN dbo.ProductionCostCategories c
                    ON c.ProductionCostCategoryID = cl.ProductionCostCategoryId
                WHERE cl.ProductionFormulaId = @Id AND cl.IsDeleted = 0
                ORDER BY cl.ProductionFormulaCostLineID
                """,
                new { Id = id },
                cancellationToken: cancellationToken))).ToList();

        return new ProductionFormulaDetailDto
        {
            ProductionFormulaId = header.ProductionFormulaId,
            Name = header.Name,
            ProductId = header.ProductId,
            ProductName = header.ProductName,
            MeaurmentId = header.MeaurmentId,
            MeaurmentName = header.MeaurmentName,
            BaseQuantity = header.BaseQuantity,
            Mode = header.Mode,
            ModeLabel = header.Mode == (int)ProductionFormulaMode.Fixed ? "ثابت" : "متغیر",
            IsDefault = header.IsDefault,
            Notes = header.Notes,
            MaterialLines = materials,
            CostLines = costs,
        };
    }

    private sealed class SystemCostHintRow
    {
        public string CategoryCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public int EmployeeCount { get; set; }
    }

    private sealed class CategoryDepartmentNameRow
    {
        public string CategoryCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
    }

    private sealed class ProductionFormulaHeaderRow
    {
        public int ProductionFormulaId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int MeaurmentId { get; set; }
        public string MeaurmentName { get; set; } = string.Empty;
        public decimal BaseQuantity { get; set; }
        public int Mode { get; set; }
        public bool IsDefault { get; set; }
        public string? Notes { get; set; }
    }
}

public sealed class ProductionFormulaListRow
{
    public int ProductionFormulaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int MeaurmentId { get; set; }
    public string MeaurmentName { get; set; } = string.Empty;
    public decimal BaseQuantity { get; set; }
    public int Mode { get; set; }
    public bool IsDefault { get; set; }
    public int MaterialLinesCount { get; set; }
    public int CostLinesCount { get; set; }
    public string? Notes { get; set; }
}

public sealed class ProductionFormulaOptionRow
{
    public int Value { get; set; }
    public string Label { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int MeaurmentId { get; set; }
    public decimal BaseQuantity { get; set; }
    public int Mode { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class ProductionFormulaMaterialRow
{
    public int ProductionFormulaMaterialLineId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int MeaurmentId { get; set; }
    public string MeaurmentName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public int? DefaultWarehouseId { get; set; }
    public string? DefaultWarehouseName { get; set; }
}

public sealed class ProductionFormulaCostRow
{
    public int ProductionFormulaCostLineId { get; set; }
    public int CostType { get; set; }
    public int? ProductionCostCategoryId { get; set; }
    public string? CostCategoryName { get; set; }
    public string? Description { get; set; }
    public int AmountMode { get; set; }
    public decimal Amount { get; set; }
    public int? AccountId { get; set; }
}

public sealed class ProductionFormulaDetailDto
{
    public int ProductionFormulaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int MeaurmentId { get; set; }
    public string MeaurmentName { get; set; } = string.Empty;
    public decimal BaseQuantity { get; set; }
    public int Mode { get; set; }
    public string ModeLabel { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string? Notes { get; set; }
    public List<ProductionFormulaMaterialRow> MaterialLines { get; set; } = [];
    public List<ProductionFormulaCostRow> CostLines { get; set; } = [];
}
