using System.Data;
using System.Data.Common;
using Dapper;
using HamgamCementWeb.Server.Data;

namespace HamgamCementWeb.Server.Services;

public interface IShareholderReadService
{
    Task<ShareholderDataTableResult> QueryDataTableAsync(
        ShareholderDataTableQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShareholderOptionRow>> ListActiveOptionsAsync(
        CancellationToken cancellationToken = default);
}

public sealed class ShareholderReadService : IShareholderReadService
{
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [1] = "LTRIM(RTRIM(CONCAT(s.FirstName, N' ', s.LastName)))",
        [3] = "s.ProfitShare",
        [4] = "s.LossShare",
        [5] = "s.InitialBalance",
        [6] = "s.IsActive",
    };

    private readonly ISqlConnectionFactory _sql;

    public ShareholderReadService(ISqlConnectionFactory sql)
    {
        _sql = sql;
    }

    public async Task<ShareholderDataTableResult> QueryDataTableAsync(
        ShareholderDataTableQuery query,
        CancellationToken cancellationToken = default)
    {
        var start = Math.Max(query.Start, 0);
        var length = query.Length <= 0 ? 10 : Math.Min(query.Length, 100);
        var search = query.Search?.Trim() ?? string.Empty;
        var hasSearch = search.Length > 0;

        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        const string baseFrom = """
            FROM dbo.Shareholders s
            LEFT JOIN dbo.Accounts a ON a.AccountID = s.AccountId AND ISNULL(a.IsDeleted, 0) = 0
            WHERE ISNULL(s.IsDeleted, 0) = 0
            """;

        var where = string.Empty;
        var p = new DynamicParameters();
        if (hasSearch)
        {
            where = """
                 AND (
                    s.FirstName LIKE @Search
                    OR s.LastName LIKE @Search
                    OR ISNULL(s.Description, N'') LIKE @Search
                )
                """;
            p.Add("Search", $"%{search}%");
        }

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                $"SELECT COUNT(1) FROM dbo.Shareholders s WHERE ISNULL(s.IsDeleted, 0) = 0",
                cancellationToken: cancellationToken));

        var filtered = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                $"SELECT COUNT(1) {baseFrom}{where}",
                p,
                cancellationToken: cancellationToken));

        var orderClause = DataTableSqlHelper.BuildOrderClause(
            OrderColumns,
            query.Order,
            "s.LastName ASC, s.FirstName ASC");

        p.Add("Start", start);
        p.Add("Length", length);
        p.Add("OpeningTxnType", (int)ShareholderEquityTxnType.OpeningBalance);

        var rows = (await connection.QueryAsync<ShareholderTableRow>(
            new CommandDefinition(
                $"""
                 SELECT
                     s.ShareholderID AS ShareholderId,
                     CAST(s.Title AS int) AS Title,
                     s.FirstName,
                     s.LastName,
                     LTRIM(RTRIM(CONCAT(s.FirstName, N' ', s.LastName))) AS FullName,
                     s.InitialBalance,
                     ISNULL(s.Description, N'') AS Description,
                     s.ProfitShare,
                     s.LossShare,
                     CAST(CASE WHEN s.IsActive = 1 THEN 1 ELSE 0 END AS bit) AS IsActive,
                     s.AccountId,
                     a.Code AS AccountCode,
                     CAST(CASE WHEN EXISTS (
                         SELECT 1
                         FROM dbo.ShareholderEquityTxns t
                         WHERE t.ShareholderId = s.ShareholderID
                           AND ISNULL(t.IsDeleted, 0) = 0
                           AND t.TxnType = @OpeningTxnType
                     ) THEN 1 ELSE 0 END AS bit) AS HasOpeningBalance
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

        return new ShareholderDataTableResult
        {
            RecordsTotal = total,
            RecordsFiltered = filtered,
            Rows = rows,
        };
    }

    public async Task<IReadOnlyList<ShareholderOptionRow>> ListActiveOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<ShareholderOptionRow>(
            new CommandDefinition(
                """
                SELECT
                    s.ShareholderID AS Value,
                    LTRIM(RTRIM(CONCAT(s.FirstName, N' ', s.LastName))) AS Label,
                    s.ProfitShare,
                    s.LossShare,
                    s.AccountId
                FROM dbo.Shareholders s
                WHERE ISNULL(s.IsDeleted, 0) = 0
                  AND ISNULL(s.IsActive, 0) = 1
                ORDER BY s.LastName, s.FirstName
                """,
                cancellationToken: cancellationToken));

        return rows.AsList();
    }
}

public sealed class ShareholderDataTableQuery
{
    public int Start { get; init; }
    public int Length { get; init; }
    public string? Search { get; init; }
    public IReadOnlyList<DataTableOrderItem>? Order { get; init; }
}

public sealed class ShareholderDataTableResult
{
    public int RecordsTotal { get; init; }
    public int RecordsFiltered { get; init; }
    public IReadOnlyList<ShareholderTableRow> Rows { get; init; } = [];
}

public sealed class ShareholderTableRow
{
    public int RowNumber { get; set; }
    public int ShareholderId { get; init; }
    public int Title { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public decimal InitialBalance { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal ProfitShare { get; init; }
    public decimal LossShare { get; init; }
    public bool IsActive { get; init; }
    public int? AccountId { get; init; }
    public string? AccountCode { get; init; }
    public bool HasOpeningBalance { get; init; }
}

public sealed class ShareholderOptionRow
{
    public int Value { get; init; }
    public string Label { get; init; } = string.Empty;
    public decimal ProfitShare { get; init; }
    public decimal LossShare { get; init; }
    public int? AccountId { get; init; }
}
