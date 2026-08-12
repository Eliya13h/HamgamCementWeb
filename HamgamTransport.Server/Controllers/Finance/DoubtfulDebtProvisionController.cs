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
[Route("api/finance/doubtful-provisions")]
[Authorize]
public class DoubtfulDebtProvisionController : FinanceControllerBase
{
    private readonly IJournalPostingService _journal;
    private readonly IAccountLookupService _accounts;
    private readonly ICurrencyConversionService _currency;

    public DoubtfulDebtProvisionController(
        AppDbContext db,
        IJournalPostingService journal,
        IAccountLookupService accounts,
        ICurrencyConversionService currency) : base(db)
    {
        _journal = journal;
        _accounts = accounts;
        _currency = currency;
    }

    [HttpGet]
    [HasPermission("accounting.expenses.view")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var rows = await Db.DoubtfulDebtProvisions.AsNoTracking()
            .Where(p => p.IsDeleted != true)
            .OrderByDescending(p => p.ProvisionDate)
            .ThenByDescending(p => p.DoubtfulDebtProvisionID)
            .Select(p => new
            {
                doubtfulDebtProvisionId = p.DoubtfulDebtProvisionID,
                provisionDate = p.ProvisionDate,
                amountInBaseCurrency = p.AmountInBaseCurrency,
                description = p.Description,
                journalEntryId = p.JournalEntryId,
            })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpPost]
    [HasPermission("accounting.expenses.create")]
    public async Task<IActionResult> Create(
        [FromBody] DoubtfulProvisionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AmountInBaseCurrency <= 0)
        {
            return BadRequest(new { message = "مبلغ باید بزرگ‌تر از صفر باشد." });
        }

        try
        {
            var date = DateTime.TryParse(request.ProvisionDate, out var d) ? d.Date : DateTime.Today;
            var expense = await _accounts.GetBySystemCodeAsync(AccountSystemCode.DoubtfulDebtExpense, cancellationToken);
            var allowance = await _accounts.GetBySystemCodeAsync(AccountSystemCode.DoubtfulDebtAllowance, cancellationToken);
            var baseCurrency = await _currency.GetBaseCurrencyAsync(cancellationToken);
            var amount = request.AmountInBaseCurrency;

            var entity = new DoubtfulDebtProvision
            {
                ProvisionDate = date,
                AmountInBaseCurrency = amount,
                Description = request.Description?.Trim(),
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreatedBy = ResolveCurrentUserId(),
            };
            Db.DoubtfulDebtProvisions.Add(entity);
            await Db.SaveChangesAsync(cancellationToken);

            var entry = await _journal.PostAsync(
                date,
                string.IsNullOrWhiteSpace(request.Description)
                    ? "ذخیره مطالبات مشکوک"
                    : request.Description.Trim(),
                JournalSource.DoubtfulDebtProvision,
                entity.DoubtfulDebtProvisionID,
                baseCurrency.CurrencyID,
                [
                    new JournalLineDraft(expense.AccountID, amount, 0, amount, 0, baseCurrency.CurrencyID, "هزینه مطالبات مشکوک"),
                    new JournalLineDraft(allowance.AccountID, 0, amount, 0, amount, baseCurrency.CurrencyID, "ذخیره کاهش ارزش دریافتنی"),
                ],
                ResolveCurrentUserId(),
                cancellationToken);

            entity.JournalEntryId = entry.JournalEntryID;
            await Db.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                message = "ذخیره ثبت شد.",
                doubtfulDebtProvisionId = entity.DoubtfulDebtProvisionID,
                journalEntryId = entry.JournalEntryID,
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
        var entity = await Db.DoubtfulDebtProvisions.FirstOrDefaultAsync(
            p => p.DoubtfulDebtProvisionID == id && p.IsDeleted != true, cancellationToken);
        if (entity is null) return NotFound(new { message = "یافت نشد." });

        try
        {
            if (entity.JournalEntryId is int jeId)
            {
                await _journal.ReverseEntryAsync(jeId, ResolveCurrentUserId(), null, cancellationToken);
            }

            entity.IsDeleted = true;
            entity.IsActive = false;
            entity.DeletedAt = DateTime.Now;
            entity.DeletedBy = ResolveCurrentUserId();
            await Db.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "ذخیره ابطال شد." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class DoubtfulProvisionRequest
{
    public string? ProvisionDate { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal AmountInBaseCurrency { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}
