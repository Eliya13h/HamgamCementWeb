using System.ComponentModel.DataAnnotations;
using Dapper;
using HamgamCementWeb.Server.Authorization;
using HamgamCementWeb.Server.Controllers.Common;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/cash-boxes")]
[Authorize]
public class CashBoxController : FinanceControllerBase
{
    private readonly ICashBoxService _cashBoxes;
    private readonly ICashBalanceService _balances;
    private readonly ISqlConnectionFactory _sql;

    public CashBoxController(
        AppDbContext db,
        ICashBoxService cashBoxes,
        ICashBalanceService balances,
        ISqlConnectionFactory sql) : base(db)
    {
        _cashBoxes = cashBoxes;
        _balances = balances;
        _sql = sql;
    }

    [HttpPost("datatable")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> DataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);
        var search = request.Search?.Value?.Trim();

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);
        const string baseWhere = "WHERE c.IsDeleted = 0";
        var where = baseWhere;
        var parameters = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += " AND (c.Code LIKE @Search OR c.Name LIKE @Search)";
            parameters.Add("Search", $"%{search}%");
        }

        var recordsTotal = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM CashBoxes c {baseWhere}");
        var recordsFiltered = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM CashBoxes c {where}", parameters);

        parameters.Add("Offset", start);
        parameters.Add("Fetch", length);

        var rows = (await connection.QueryAsync(
            $"""
             SELECT c.CashBoxID AS cashBoxId,
                    c.Code AS code,
                    c.Name AS name,
                    c.ParentCashBoxId AS parentCashBoxId,
                    p.Name AS parentName,
                    c.Description AS description,
                    c.IsActive AS isActive,
                    (SELECT COUNT(1) FROM CashBoxUsers u WHERE u.CashBoxId = c.CashBoxID AND u.IsDeleted = 0) AS userCount,
                    (SELECT STRING_AGG(CAST(u.UserId AS varchar(20)), ',')
                     FROM CashBoxUsers u
                     WHERE u.CashBoxId = c.CashBoxID AND u.IsDeleted = 0) AS userIdsText,
                    (SELECT STRING_AGG(CONCAT(cur.CurrencyCode, ':', FORMAT(b.Amt, '0.####')), ' | ')
                     FROM (
                         SELECT jl.CurrencyId, SUM(jl.Debit - jl.Credit) AS Amt
                         FROM JournalLines jl
                         INNER JOIN JournalEntries je ON je.JournalEntryID = jl.JournalEntryId
                         WHERE jl.CashBoxId = c.CashBoxID
                           AND ISNULL(jl.IsDeleted, 0) = 0
                           AND ISNULL(je.IsDeleted, 0) = 0
                           AND je.IsPosted = 1
                         GROUP BY jl.CurrencyId
                         HAVING SUM(jl.Debit - jl.Credit) <> 0
                     ) b
                     INNER JOIN Currencies cur ON cur.CurrencyID = b.CurrencyId
                    ) AS balancesText
             FROM CashBoxes c
             LEFT JOIN CashBoxes p ON p.CashBoxID = c.ParentCashBoxId AND p.IsDeleted = 0
             {where}
             ORDER BY c.Code
             OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
             """, parameters)).ToList();

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal,
            recordsFiltered,
            data = rows.Select((r, i) =>
            {
                var dict = (IDictionary<string, object>)r;
                return new
                {
                    rowNumber = start + i + 1,
                    cashBoxId = dict["cashBoxId"],
                    code = dict["code"],
                    name = dict["name"],
                    parentCashBoxId = dict["parentCashBoxId"],
                    parentName = dict["parentName"],
                    description = dict["description"],
                    isActive = dict["isActive"],
                    userCount = dict["userCount"],
                    userIdsText = dict["userIdsText"]?.ToString() ?? string.Empty,
                    balancesText = dict["balancesText"]?.ToString() ?? string.Empty,
                };
            }),
        });
    }

    [HttpGet("user-options")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> UserOptions(CancellationToken cancellationToken)
    {
        var items = await Db.Users
            .AsNoTracking()
            .Where(u => u.IsDeleted != true && u.IsActive == true)
            .OrderBy(u => u.UserName)
            .Select(u => new { value = u.UserID, label = u.UserName + " — " + u.FullName })
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("options")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> Options(CancellationToken cancellationToken)
    {
        var items = await Db.CashBoxes
            .AsNoTracking()
            .Where(c => c.IsDeleted != true && c.IsActive == true)
            .OrderBy(c => c.Code)
            .Select(c => new { value = c.CashBoxID, label = c.Code + " — " + c.Name })
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    // خلاصه وضعیت و موجودی همه صندوق‌ها برای صفحه آمار و تحلیل
    [HttpGet("overview")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken)
    {
        var rows = await _balances.GetOverviewAsync(cancellationToken);
        return Ok(rows.Select(r => new
        {
            cashBoxId = r.CashBoxId,
            code = r.Code,
            name = r.Name,
            parentName = r.ParentName,
            isActive = r.IsActive,
            hasOpenShift = r.HasOpenShift,
            openShiftUserName = r.OpenShiftUserName,
            totalInBase = r.TotalInBase,
            balances = r.Balances.Select(b => new
            {
                currencyId = b.CurrencyId,
                currencyCode = b.CurrencyCode,
                symbol = b.Symbol,
                name = b.Name,
                isBaseCurrency = b.IsBaseCurrency,
                amount = b.Amount,
                amountInBase = b.AmountInBase,
            }),
        }));
    }

    [HttpGet("{id:int}")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var box = await Db.CashBoxes
            .AsNoTracking()
            .Where(c => c.CashBoxID == id && c.IsDeleted != true)
            .Select(c => new
            {
                cashBoxId = c.CashBoxID,
                code = c.Code,
                name = c.Name,
                parentCashBoxId = c.ParentCashBoxId,
                description = c.Description,
                isActive = c.IsActive == true,
                isPettyCash = c.IsPettyCash,
                ceilingAmountInBase = c.CeilingAmountInBase,
                userIds = c.Users.Where(u => u.IsDeleted != true).Select(u => u.UserId).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return box is null ? NotFound(new { message = "صندوق یافت نشد." }) : Ok(box);
    }

    [HttpGet("{id:int}/balances")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> Balances(int id, CancellationToken cancellationToken)
    {
        var exists = await Db.CashBoxes
            .AsNoTracking()
            .AnyAsync(c => c.CashBoxID == id && c.IsDeleted != true, cancellationToken);
        if (!exists)
        {
            return NotFound(new { message = "صندوق یافت نشد." });
        }

        var balances = await _balances.GetBalancesAsync(id, cancellationToken);
        return Ok(balances.Select(b => new
        {
            currencyId = b.CurrencyId,
            currencyCode = b.CurrencyCode,
            symbol = b.Symbol,
            name = b.Name,
            isBaseCurrency = b.IsBaseCurrency,
            amount = b.Amount,
            amountInBase = b.AmountInBase,
        }));
    }

    [HttpPost]
    [HasPermission("accounting.expenses.create")]
    public async Task<IActionResult> Create([FromBody] SaveCashBoxRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var box = await _cashBoxes.CreateAsync(
                request.Code,
                request.Name,
                request.ParentCashBoxId,
                request.UserIds ?? [],
                request.Description,
                request.IsPettyCash,
                request.CeilingAmountInBase ?? 0,
                ResolveCurrentUserId(),
                cancellationToken);
            return Ok(new { message = "صندوق ثبت شد.", cashBoxId = box.CashBoxID, code = box.Code });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [HasPermission("accounting.expenses.edit")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveCashBoxRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            await _cashBoxes.UpdateAsync(
                id,
                request.Name,
                request.ParentCashBoxId,
                request.UserIds ?? [],
                request.Description,
                request.IsActive ?? true,
                request.IsPettyCash,
                request.CeilingAmountInBase ?? 0,
                ResolveCurrentUserId(),
                cancellationToken);
            return Ok(new { message = "صندوق به‌روزرسانی شد." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // انتقال آزاد بین صندوق‌ها (بدون محدودیت والد/فرزند)
    [HttpPost("transfers")]
    [HasPermission("accounting.expenses.create")]
    public async Task<IActionResult> Transfer([FromBody] FreeCashTransferRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var lines = (request.Lines ?? [])
                .Where(l => l.CurrencyId > 0 && l.Amount > 0)
                .Select(l => new CashTransferLineInput(l.CurrencyId, l.Amount, l.AmountInBaseCurrency))
                .ToList();

            var transfer = await _cashBoxes.TransferAsync(
                request.FromCashBoxId,
                request.ToCashBoxId,
                request.TransferDate ?? DateTime.Now,
                request.Description,
                lines,
                ResolveCurrentUserId(),
                cancellationToken);

            return Ok(new
            {
                message = "انتقال صندوق ثبت شد.",
                cashTransferId = transfer.CashTransferID,
                journalEntryId = transfer.JournalEntryId,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/recharge")]
    [HasPermission("accounting.expenses.create")]
    public async Task<IActionResult> RechargePettyCash(
        int id,
        [FromBody] PettyCashRechargeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var lines = (request.Lines ?? [])
                .Where(l => l.CurrencyId > 0 && l.Amount > 0)
                .Select(l => new CashTransferLineInput(l.CurrencyId, l.Amount, l.AmountInBaseCurrency))
                .ToList();
            var transfer = await _cashBoxes.RechargePettyCashAsync(
                id, request.TransferDate ?? DateTime.Now, lines, ResolveCurrentUserId(), cancellationToken);
            return Ok(new { message = "تنخواه شارژ شد.", cashTransferId = transfer.CashTransferID, journalEntryId = transfer.JournalEntryId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("shifts/open")]
    [HasPermission("accounting.expenses.create")]
    public async Task<IActionResult> OpenShift([FromBody] OpenShiftRequest request, CancellationToken cancellationToken)
    {
        var userId = ResolveCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var shift = await _cashBoxes.OpenShiftAsync(
                request.CashBoxId,
                userId.Value,
                MapLines(request.OpeningLines),
                request.Notes,
                cancellationToken);
            return Ok(new { message = "شیفت باز شد.", cashShiftId = shift.CashShiftID });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("shifts/{id:int}/close")]
    [HasPermission("accounting.expenses.create")]
    public async Task<IActionResult> CloseShift(int id, [FromBody] CloseShiftRequest request, CancellationToken cancellationToken)
    {
        var userId = ResolveCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var shift = await _cashBoxes.CloseShiftAsync(
                id,
                userId.Value,
                MapLines(request.TransferLines),
                request.Notes,
                cancellationToken);
            return Ok(new
            {
                message = "شیفت بسته و مانده به صندوق بالاتر منتقل شد.",
                cashShiftId = shift.CashShiftID,
                cashTransferId = shift.CashTransferId,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("shifts/datatable")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> ShiftsDataTable(
        [FromBody] DataTableRequest request,
        CancellationToken cancellationToken)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);
        const string where = "WHERE s.IsDeleted = 0";
        var recordsTotal = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM CashShifts s {where}");

        var rows = (await connection.QueryAsync(
            """
            SELECT s.CashShiftID AS cashShiftId,
                   s.CashBoxId AS cashBoxId,
                   c.Name AS cashBoxName,
                   s.UserId AS userId,
                   u.UserName AS userName,
                   s.Status AS status,
                   s.OpenedAt AS openedAt,
                   s.ClosedAt AS closedAt,
                   s.OpeningBalanceInBase AS openingBalanceInBase,
                   s.ClosingTransferAmountInBase AS closingTransferAmountInBase,
                   (SELECT STRING_AGG(CONCAT(cur.CurrencyCode, ':', FORMAT(ol.Amount, '0.####')), ' | ')
                    FROM CashShiftOpeningLines ol
                    INNER JOIN Currencies cur ON cur.CurrencyID = ol.CurrencyId
                    WHERE ol.CashShiftId = s.CashShiftID AND ISNULL(ol.IsDeleted, 0) = 0
                      AND ol.Amount <> 0) AS openingLinesText,
                   (SELECT STRING_AGG(CONCAT(cur.CurrencyCode, ':', FORMAT(tl.Amount, '0.####')), ' | ')
                    FROM CashTransfers t
                    INNER JOIN CashTransferLines tl ON tl.CashTransferId = t.CashTransferID AND ISNULL(tl.IsDeleted, 0) = 0
                    INNER JOIN Currencies cur ON cur.CurrencyID = tl.CurrencyId
                    WHERE t.CashTransferID = s.CashTransferId AND tl.Amount <> 0) AS transferLinesText
            FROM CashShifts s
            INNER JOIN CashBoxes c ON c.CashBoxID = s.CashBoxId
            INNER JOIN Users u ON u.UserID = s.UserId
            WHERE s.IsDeleted = 0
            ORDER BY s.OpenedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
            """, new { Offset = start, Fetch = length })).ToList();

        return Ok(new
        {
            draw = request.Draw,
            recordsTotal,
            recordsFiltered = recordsTotal,
            data = rows.Select((r, i) =>
            {
                var d = (IDictionary<string, object>)r;
                var status = Convert.ToInt32(d["status"]);
                return new
                {
                    rowNumber = start + i + 1,
                    cashShiftId = d["cashShiftId"],
                    cashBoxId = d["cashBoxId"],
                    cashBoxName = d["cashBoxName"],
                    userId = d["userId"],
                    userName = d["userName"],
                    status,
                    statusLabel = status == 1 ? "باز" : "بسته",
                    openedAt = Convert.ToDateTime(d["openedAt"]).ToString("yyyy-MM-dd HH:mm"),
                    closedAt = d["closedAt"] is DateTime closed ? closed.ToString("yyyy-MM-dd HH:mm") : null,
                    openingBalanceInBase = d["openingBalanceInBase"],
                    closingTransferAmountInBase = d["closingTransferAmountInBase"],
                    openingLinesText = d["openingLinesText"]?.ToString() ?? string.Empty,
                    transferLinesText = d["transferLinesText"]?.ToString() ?? string.Empty,
                };
            }),
        });
    }

    private static IReadOnlyList<CashAmountLine> MapLines(IEnumerable<CashAmountLineRequest>? lines)
    {
        if (lines is null)
        {
            return [];
        }

        return lines
            .Where(l => l.CurrencyId > 0)
            .Select(l => new CashAmountLine(l.CurrencyId, l.Amount))
            .ToList();
    }
}

public class SaveCashBoxRequest
{
    // در ایجاد، کد به‌صورت خودکار تولید می‌شود؛ ارسال اختیاری است
    [MaxLength(30)]
    public string? Code { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int? ParentCashBoxId { get; set; }

    public List<int>? UserIds { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool? IsActive { get; set; } = true;

    public bool IsPettyCash { get; set; }

    // اختیاری؛ در صورت null به‌عنوان صفر در نظر گرفته می‌شود
    public decimal? CeilingAmountInBase { get; set; }
}

public class CashAmountLineRequest
{
    [Required]
    public int CurrencyId { get; set; }

    public decimal Amount { get; set; }
}

public class OpenShiftRequest
{
    [Required]
    public int CashBoxId { get; set; }

    public List<CashAmountLineRequest>? OpeningLines { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class CloseShiftRequest
{
    public List<CashAmountLineRequest>? TransferLines { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class FreeCashTransferRequest
{
    [Required]
    public int FromCashBoxId { get; set; }

    [Required]
    public int ToCashBoxId { get; set; }

    public DateTime? TransferDate { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public List<FreeCashTransferLineRequest>? Lines { get; set; }
}

public class FreeCashTransferLineRequest
{
    [Required]
    public int CurrencyId { get; set; }

    public decimal Amount { get; set; }

    public decimal? AmountInBaseCurrency { get; set; }
}

public class PettyCashRechargeRequest
{
    public DateTime? TransferDate { get; set; }
    public List<FreeCashTransferLineRequest>? Lines { get; set; }
}
