using System.ComponentModel.DataAnnotations;
using HamgamTransport.Server.Authorization;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Controllers.Finance;

[ApiController]
[Route("api/finance/recurring-journals")]
[Authorize]
public class RecurringJournalController : FinanceControllerBase
{
    private readonly IJournalPostingService _journal;
    private readonly ICurrencyConversionService _currency;

    public RecurringJournalController(
        AppDbContext db,
        IJournalPostingService journal,
        ICurrencyConversionService currency) : base(db)
    {
        _journal = journal;
        _currency = currency;
    }

    [HttpGet]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var rows = await Db.RecurringJournalTemplates.AsNoTracking()
            .Where(t => t.IsDeleted != true)
            .OrderBy(t => t.Code)
            .Select(t => new
            {
                recurringJournalTemplateId = t.RecurringJournalTemplateID,
                code = t.Code,
                name = t.Name,
                description = t.Description,
                isActive = t.IsActive,
                lineCount = t.Lines.Count(l => l.IsDeleted != true),
            })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("{id:int}")]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var t = await Db.RecurringJournalTemplates.AsNoTracking()
            .Include(x => x.Lines.Where(l => l.IsDeleted != true))
            .FirstOrDefaultAsync(x => x.RecurringJournalTemplateID == id && x.IsDeleted != true, cancellationToken);
        if (t is null) return NotFound(new { message = "قالب یافت نشد." });

        return Ok(new
        {
            recurringJournalTemplateId = t.RecurringJournalTemplateID,
            code = t.Code,
            name = t.Name,
            description = t.Description,
            isActive = t.IsActive,
            lines = t.Lines.OrderBy(l => l.LineNo).Select(l => new
            {
                accountId = l.AccountId,
                description = l.Description,
                debitInBaseCurrency = l.DebitInBaseCurrency,
                creditInBaseCurrency = l.CreditInBaseCurrency,
                costCenterId = l.CostCenterId,
            }),
        });
    }

    [HttpPost]
    [HasPermission("accounting.expenses.create")]
    public async Task<IActionResult> Create(
        [FromBody] RecurringTemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (request.Lines is null || request.Lines.Count < 2)
        {
            return BadRequest(new { message = "قالب باید حداقل دو ردیف داشته باشد." });
        }

        var debit = request.Lines.Sum(l => l.DebitInBaseCurrency);
        var credit = request.Lines.Sum(l => l.CreditInBaseCurrency);
        if (Math.Abs(debit - credit) > 0.01m)
        {
            return BadRequest(new { message = "مجموع دیبت و کریدیت قالب باید برابر باشد." });
        }

        var code = request.Code.Trim();
        if (await Db.RecurringJournalTemplates.AnyAsync(t => t.IsDeleted != true && t.Code == code, cancellationToken))
        {
            return BadRequest(new { message = "کد قالب تکراری است." });
        }

        var now = DateTime.Now;
        var userId = ResolveCurrentUserId();
        var entity = new RecurringJournalTemplate
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedAt = now,
            CreatedBy = userId,
        };

        var lineNo = 1;
        foreach (var line in request.Lines)
        {
            entity.Lines.Add(new RecurringJournalTemplateLine
            {
                LineNo = lineNo++,
                AccountId = line.AccountId,
                Description = line.Description?.Trim(),
                DebitInBaseCurrency = line.DebitInBaseCurrency,
                CreditInBaseCurrency = line.CreditInBaseCurrency,
                CostCenterId = line.CostCenterId,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = userId,
            });
        }

        Db.RecurringJournalTemplates.Add(entity);
        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "قالب ثبت شد.", recurringJournalTemplateId = entity.RecurringJournalTemplateID });
    }

    [HttpPost("{id:int}/generate")]
    [HasPermission("accounting.expenses.create")]
    public async Task<IActionResult> Generate(
        int id,
        [FromBody] GenerateRecurringRequest? request,
        CancellationToken cancellationToken)
    {
        var template = await Db.RecurringJournalTemplates
            .Include(t => t.Lines.Where(l => l.IsDeleted != true))
            .FirstOrDefaultAsync(t => t.RecurringJournalTemplateID == id && t.IsDeleted != true, cancellationToken);
        if (template is null) return NotFound(new { message = "قالب یافت نشد." });
        if (template.IsActive != true)
        {
            return BadRequest(new { message = "قالب غیرفعال است." });
        }

        try
        {
            var entryDate = DateTime.TryParse(request?.EntryDate, out var d) ? d.Date : DateTime.Today;
            var baseCurrency = await _currency.GetBaseCurrencyAsync(cancellationToken);
            var drafts = template.Lines.OrderBy(l => l.LineNo).Select(l => new JournalLineDraft(
                l.AccountId,
                l.DebitInBaseCurrency,
                l.CreditInBaseCurrency,
                l.DebitInBaseCurrency,
                l.CreditInBaseCurrency,
                baseCurrency.CurrencyID,
                l.Description,
                CostCenterId: l.CostCenterId)).ToList();

            var entry = await _journal.PostAsync(
                entryDate,
                string.IsNullOrWhiteSpace(template.Description) ? template.Name : template.Description,
                JournalSource.Manual,
                template.RecurringJournalTemplateID,
                baseCurrency.CurrencyID,
                drafts,
                ResolveCurrentUserId(),
                cancellationToken);

            return Ok(new
            {
                message = "سند از قالب صادر شد.",
                journalEntryId = entry.JournalEntryID,
                entryNumber = entry.EntryNumber,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [HasPermission("accounting.expenses.delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await Db.RecurringJournalTemplates
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.RecurringJournalTemplateID == id && t.IsDeleted != true, cancellationToken);
        if (entity is null) return NotFound(new { message = "یافت نشد." });

        var now = DateTime.Now;
        var userId = ResolveCurrentUserId();
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = now;
        entity.DeletedBy = userId;
        foreach (var line in entity.Lines)
        {
            line.IsDeleted = true;
            line.IsActive = false;
            line.DeletedAt = now;
            line.DeletedBy = userId;
        }

        await Db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "قالب حذف شد." });
    }
}

public class RecurringTemplateRequest
{
    [Required, MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public List<RecurringLineRequest> Lines { get; set; } = [];
}

public class RecurringLineRequest
{
    public int AccountId { get; set; }
    public string? Description { get; set; }
    public decimal DebitInBaseCurrency { get; set; }
    public decimal CreditInBaseCurrency { get; set; }
    public int? CostCenterId { get; set; }
}

public class GenerateRecurringRequest
{
    public string? EntryDate { get; set; }
}
