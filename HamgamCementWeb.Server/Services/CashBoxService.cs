using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Services;

public record CashAmountLine(int CurrencyId, decimal Amount);

public interface ICashBoxService
{
    Task<int?> ResolveUserCashBoxIdAsync(int? userId, CancellationToken cancellationToken = default);
    Task<CashBox> CreateAsync(string? code, string name, int? parentCashBoxId, IReadOnlyList<int> userIds, string? description, int? createdBy, CancellationToken cancellationToken = default);
    Task UpdateAsync(int cashBoxId, string name, int? parentCashBoxId, IReadOnlyList<int> userIds, string? description, bool isActive, int? updatedBy, CancellationToken cancellationToken = default);
    Task<CashShift> OpenShiftAsync(int cashBoxId, int userId, IReadOnlyList<CashAmountLine> openingLines, string? notes, CancellationToken cancellationToken = default);
    Task<CashShift> CloseShiftAsync(int cashShiftId, int userId, IReadOnlyList<CashAmountLine> transferLines, string? notes, CancellationToken cancellationToken = default);
}

public class CashBoxService : ICashBoxService
{
    private readonly AppDbContext _db;
    private readonly IAccountLookupService _accounts;
    private readonly IOperationalGlService _gl;
    private readonly ICashBalanceService _balances;
    private readonly ICurrencyConversionService _currencies;

    public CashBoxService(
        AppDbContext db,
        IAccountLookupService accounts,
        IOperationalGlService gl,
        ICashBalanceService balances,
        ICurrencyConversionService currencies)
    {
        _db = db;
        _accounts = accounts;
        _gl = gl;
        _balances = balances;
        _currencies = currencies;
    }

    public async Task<int?> ResolveUserCashBoxIdAsync(int? userId, CancellationToken cancellationToken = default)
    {
        if (userId is not int uid)
        {
            return null;
        }

        var openShift = await _db.CashShifts
            .AsNoTracking()
            .Where(s => s.UserId == uid && s.Status == CashShiftStatus.Open && s.IsDeleted != true)
            .OrderByDescending(s => s.OpenedAt)
            .Select(s => (int?)s.CashBoxId)
            .FirstOrDefaultAsync(cancellationToken);

        if (openShift is not null)
        {
            return openShift;
        }

        return await _db.CashBoxUsers
            .AsNoTracking()
            .Where(u => u.UserId == uid && u.IsDeleted != true)
            .Join(_db.CashBoxes.Where(c => c.IsDeleted != true && c.IsActive == true),
                u => u.CashBoxId,
                c => c.CashBoxID,
                (_, c) => (int?)c.CashBoxID)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CashBox> CreateAsync(
        string? code,
        string name,
        int? parentCashBoxId,
        IReadOnlyList<int> userIds,
        string? description,
        int? createdBy,
        CancellationToken cancellationToken = default)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("نام صندوق الزامی است.");
        }

        // کد صندوق به‌صورت خودکار تولید می‌شود تا با کدینگ حساب‌ها تداخل نکند
        code = string.IsNullOrWhiteSpace(code)
            ? await GenerateNextCodeAsync(cancellationToken)
            : code.Trim();

        if (await _db.CashBoxes.AnyAsync(c => c.Code == code && c.IsDeleted != true, cancellationToken))
        {
            throw new InvalidOperationException("کد صندوق تکراری است.");
        }

        if (parentCashBoxId is int parentId)
        {
            var parentExists = await _db.CashBoxes
                .AnyAsync(c => c.CashBoxID == parentId && c.IsDeleted != true, cancellationToken);
            if (!parentExists)
            {
                throw new InvalidOperationException("صندوق والد یافت نشد.");
            }
        }

        var account = await _accounts.EnsureCashBoxAccountAsync(code, name, cancellationToken);
        var now = DateTime.Now;
        var box = new CashBox
        {
            Code = code,
            Name = name,
            ParentCashBoxId = parentCashBoxId,
            AccountId = account.AccountID,
            Description = description?.Trim(),
            IsActive = true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedBy = createdBy,
        };

        _db.CashBoxes.Add(box);
        await _db.SaveChangesAsync(cancellationToken);

        await SyncUsersAsync(box.CashBoxID, userIds, createdBy, cancellationToken);
        return box;
    }

    public async Task UpdateAsync(
        int cashBoxId,
        string name,
        int? parentCashBoxId,
        IReadOnlyList<int> userIds,
        string? description,
        bool isActive,
        int? updatedBy,
        CancellationToken cancellationToken = default)
    {
        var box = await _db.CashBoxes
            .FirstOrDefaultAsync(c => c.CashBoxID == cashBoxId && c.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("صندوق یافت نشد.");

        if (parentCashBoxId == cashBoxId)
        {
            throw new InvalidOperationException("صندوق نمی‌تواند والد خودش باشد.");
        }

        if (parentCashBoxId is int parentId)
        {
            var parentExists = await _db.CashBoxes
                .AnyAsync(c => c.CashBoxID == parentId && c.IsDeleted != true, cancellationToken);
            if (!parentExists)
            {
                throw new InvalidOperationException("صندوق والد یافت نشد.");
            }
        }

        box.Name = name.Trim();
        box.ParentCashBoxId = parentCashBoxId;
        box.Description = description?.Trim();
        box.IsActive = isActive;
        box.IsUpdated = true;
        box.UpdatedAt = DateTime.Now;
        box.UpdatedBy = updatedBy;

        var account = await _db.Accounts.FirstAsync(a => a.AccountID == box.AccountId, cancellationToken);
        account.Name = box.Name;
        account.IsUpdated = true;
        account.UpdatedAt = DateTime.Now;

        await SyncUsersAsync(box.CashBoxID, userIds, updatedBy, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CashShift> OpenShiftAsync(
        int cashBoxId,
        int userId,
        IReadOnlyList<CashAmountLine> openingLines,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var box = await _db.CashBoxes
            .FirstOrDefaultAsync(c => c.CashBoxID == cashBoxId && c.IsDeleted != true && c.IsActive == true, cancellationToken)
            ?? throw new InvalidOperationException("صندوق یافت نشد یا غیرفعال است.");

        var assigned = await _db.CashBoxUsers
            .AnyAsync(u => u.CashBoxId == cashBoxId && u.UserId == userId && u.IsDeleted != true, cancellationToken);
        if (!assigned)
        {
            throw new InvalidOperationException("کاربر به این صندوق متصل نیست.");
        }

        var hasOpen = await _db.CashShifts
            .AnyAsync(s => s.CashBoxId == cashBoxId && s.Status == CashShiftStatus.Open && s.IsDeleted != true, cancellationToken);
        if (hasOpen)
        {
            throw new InvalidOperationException("این صندوق شیفت باز دارد.");
        }

        var userHasOpen = await _db.CashShifts
            .AnyAsync(s => s.UserId == userId && s.Status == CashShiftStatus.Open && s.IsDeleted != true, cancellationToken);
        if (userHasOpen)
        {
            throw new InvalidOperationException("کاربر شیفت باز دیگری دارد.");
        }

        var normalized = NormalizeLines(openingLines, allowZero: true);
        var now = DateTime.Now;
        decimal openingBase = 0m;

        foreach (var line in normalized)
        {
            var snapshot = await _currencies.GetSnapshotAsync(line.CurrencyId, now, cancellationToken);
            openingBase += _currencies.ConvertToBase(line.Amount, snapshot);
        }

        var shift = new CashShift
        {
            CashBoxId = cashBoxId,
            UserId = userId,
            Status = CashShiftStatus.Open,
            OpenedAt = now,
            OpeningBalanceInBase = openingBase,
            Notes = notes?.Trim(),
            IsActive = true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedBy = userId,
        };

        _db.CashShifts.Add(shift);
        await _db.SaveChangesAsync(cancellationToken);

        foreach (var line in normalized)
        {
            _db.CashShiftOpeningLines.Add(new CashShiftOpeningLine
            {
                CashShiftId = shift.CashShiftID,
                CurrencyId = line.CurrencyId,
                Amount = line.Amount,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = userId,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return shift;
    }

    public async Task<CashShift> CloseShiftAsync(
        int cashShiftId,
        int userId,
        IReadOnlyList<CashAmountLine> transferLines,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var shift = await _db.CashShifts
            .Include(s => s.CashBox)
            .FirstOrDefaultAsync(s => s.CashShiftID == cashShiftId && s.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("شیفت یافت نشد.");

        if (shift.Status != CashShiftStatus.Open)
        {
            throw new InvalidOperationException("این شیفت قبلاً بسته شده است.");
        }

        if (shift.UserId != userId)
        {
            throw new InvalidOperationException("فقط کاربر بازکننده شیفت می‌تواند آن را ببندد.");
        }

        if (shift.CashBox.ParentCashBoxId is not int parentId)
        {
            throw new InvalidOperationException("برای این صندوق والد تعریف نشده؛ تحویل پایان شیفت ممکن نیست.");
        }

        var parent = await _db.CashBoxes
            .FirstOrDefaultAsync(c => c.CashBoxID == parentId && c.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("صندوق والد یافت نشد.");

        var normalized = NormalizeLines(transferLines, allowZero: false);
        var now = DateTime.Now;
        CashTransfer? transfer = null;
        decimal totalBase = 0m;

        if (normalized.Count > 0)
        {
            foreach (var line in normalized)
            {
                await _balances.EnsureSufficientBalanceAsync(
                    shift.CashBoxId,
                    line.CurrencyId,
                    line.Amount,
                    cancellationToken);
            }

            transfer = new CashTransfer
            {
                FromCashBoxId = shift.CashBoxId,
                ToCashBoxId = parent.CashBoxID,
                CashShiftId = shift.CashShiftID,
                TransferDate = now,
                AmountInBaseCurrency = 0,
                Description = $"تحویل شیفت {shift.CashShiftID} — {shift.CashBox.Name} به {parent.Name}",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = userId,
            };

            _db.CashTransfers.Add(transfer);
            await _db.SaveChangesAsync(cancellationToken);

            foreach (var line in normalized)
            {
                var snapshot = await _currencies.GetSnapshotAsync(line.CurrencyId, now, cancellationToken);
                var amountBase = _currencies.ConvertToBase(line.Amount, snapshot);
                totalBase += amountBase;

                _db.CashTransferLines.Add(new CashTransferLine
                {
                    CashTransferId = transfer.CashTransferID,
                    CurrencyId = line.CurrencyId,
                    Amount = line.Amount,
                    AmountInBaseCurrency = amountBase,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    CreatedBy = userId,
                });
            }

            transfer.AmountInBaseCurrency = totalBase;
            await _db.SaveChangesAsync(cancellationToken);

            // بارگذاری خطوط برای Posting
            await _db.Entry(transfer).Collection(t => t.Lines).LoadAsync(cancellationToken);

            var fromAccount = await _db.Accounts.FirstAsync(a => a.AccountID == shift.CashBox.AccountId, cancellationToken);
            var toAccount = await _db.Accounts.FirstAsync(a => a.AccountID == parent.AccountId, cancellationToken);
            var journal = await _gl.PostCashTransferAsync(transfer, fromAccount, toAccount, userId, cancellationToken);
            transfer.JournalEntryId = journal.JournalEntryID;
        }

        shift.Status = CashShiftStatus.Closed;
        shift.ClosedAt = now;
        shift.ClosingTransferAmountInBase = totalBase;
        shift.CashTransferId = transfer?.CashTransferID;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            shift.Notes = string.IsNullOrWhiteSpace(shift.Notes) ? notes.Trim() : $"{shift.Notes}\n{notes.Trim()}";
        }

        shift.IsUpdated = true;
        shift.UpdatedAt = now;
        shift.UpdatedBy = userId;
        await _db.SaveChangesAsync(cancellationToken);
        return shift;
    }

    private async Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken)
    {
        var codes = await _db.CashBoxes
            .IgnoreQueryFilters()
            .Select(c => c.Code)
            .ToListAsync(cancellationToken);

        var maxSequence = codes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return (maxSequence + 1).ToString("D5");
    }

    private static List<CashAmountLine> NormalizeLines(IReadOnlyList<CashAmountLine>? lines, bool allowZero)
    {
        if (lines is null || lines.Count == 0)
        {
            if (allowZero)
            {
                return [];
            }

            return [];
        }

        var grouped = lines
            .Where(l => l.CurrencyId > 0)
            .GroupBy(l => l.CurrencyId)
            .Select(g => new CashAmountLine(g.Key, g.Sum(x => x.Amount)))
            .ToList();

        if (grouped.Any(l => l.Amount < 0))
        {
            throw new InvalidOperationException("مبلغ نمی‌تواند منفی باشد.");
        }

        if (!allowZero)
        {
            grouped = grouped.Where(l => l.Amount > 0).ToList();
        }

        var currencyIds = grouped.Select(l => l.CurrencyId).ToList();
        if (currencyIds.Count != currencyIds.Distinct().Count())
        {
            throw new InvalidOperationException("ارز تکراری در خطوط مجاز نیست.");
        }

        return grouped;
    }

    private async Task SyncUsersAsync(
        int cashBoxId,
        IReadOnlyList<int> userIds,
        int? actorId,
        CancellationToken cancellationToken)
    {
        var distinct = userIds.Distinct().ToList();
        var existing = await _db.CashBoxUsers
            .Where(u => u.CashBoxId == cashBoxId && u.IsDeleted != true)
            .ToListAsync(cancellationToken);

        foreach (var row in existing.Where(e => !distinct.Contains(e.UserId)))
        {
            row.IsDeleted = true;
            row.DeletedAt = DateTime.Now;
            row.DeletedBy = actorId;
        }

        var currentIds = existing.Where(e => e.IsDeleted != true).Select(e => e.UserId).ToHashSet();
        foreach (var uid in distinct.Where(id => !currentIds.Contains(id)))
        {
            var userExists = await _db.Users.AnyAsync(u => u.UserID == uid && u.IsDeleted != true, cancellationToken);
            if (!userExists)
            {
                throw new InvalidOperationException($"کاربر {uid} یافت نشد.");
            }

            _db.CashBoxUsers.Add(new CashBoxUser
            {
                CashBoxId = cashBoxId,
                UserId = uid,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreatedBy = actorId,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
