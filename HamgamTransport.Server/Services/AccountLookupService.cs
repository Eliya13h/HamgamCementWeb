using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using HamgamTransport.Server.Data.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Services;

public interface IAccountLookupService
{
    Task<Account> GetBySystemCodeAsync(string systemCode, CancellationToken cancellationToken = default);
    Task<Account> EnsureCustomerAccountAsync(int customerId, string customerName, CancellationToken cancellationToken = default);
    Task<Account> EnsureSupplierAccountAsync(int supplierId, string supplierName, CancellationToken cancellationToken = default);
    Task<Account> EnsureCashBoxAccountAsync(string cashBoxCode, string cashBoxName, CancellationToken cancellationToken = default);
    Task<Account> EnsureBankAccountAsync(string code, string name, CancellationToken cancellationToken = default);
    Task<Account> EnsureShareholderAccountAsync(int shareholderId, string shareholderName, CancellationToken cancellationToken = default);
    Task<Account> EnsureVehicleOwnerAccountAsync(int vehicleOwnerId, string ownerName, CancellationToken cancellationToken = default);
    Task<Account> EnsureDriverAccountAsync(int driverId, string driverName, CancellationToken cancellationToken = default);
    Task<int> ResolveInventoryAccountIdAsync(WarehouseType warehouseType, CancellationToken cancellationToken = default);
    Task<Account> ResolveRetainedEarningsPostableAsync(CancellationToken cancellationToken = default);
}

public class AccountLookupService : IAccountLookupService
{
    private readonly AppDbContext _db;

    public AccountLookupService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Account> GetBySystemCodeAsync(string systemCode, CancellationToken cancellationToken = default)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.SystemCode == systemCode && a.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException($"حساب سیستمی «{systemCode}» در کدینگ یافت نشد.");

        return account;
    }

    public async Task<Account> EnsureCustomerAccountAsync(
        int customerId,
        string customerName,
        CancellationToken cancellationToken = default)
    {
        var code = $"121-{customerId:D5}";
        var existing = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Code == code && a.IsDeleted != true, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var parent = await GetBySystemCodeAsync(AccountSystemCode.CustomersAr, cancellationToken);
        var account = new Account
        {
            Code = code,
            Name = customerName,
            Level = AccountLevel.Tafsili,
            ParentAccountId = parent.AccountID,
            AccountType = AccountType.Asset,
            Nature = AccountNature.Debit,
            IsPostable = true,
            IsSystem = true,
            SystemCode = $"CUST_{customerId}",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
        };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<Account> EnsureSupplierAccountAsync(
        int supplierId,
        string supplierName,
        CancellationToken cancellationToken = default)
    {
        var code = $"211-{supplierId:D5}";
        var existing = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Code == code && a.IsDeleted != true, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var parent = await GetBySystemCodeAsync(AccountSystemCode.SuppliersAp, cancellationToken);
        var account = new Account
        {
            Code = code,
            Name = supplierName,
            Level = AccountLevel.Tafsili,
            ParentAccountId = parent.AccountID,
            AccountType = AccountType.Liability,
            Nature = AccountNature.Credit,
            IsPostable = true,
            IsSystem = true,
            SystemCode = $"SUPP_{supplierId}",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
        };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<Account> EnsureCashBoxAccountAsync(
        string cashBoxCode,
        string cashBoxName,
        CancellationToken cancellationToken = default)
    {
        // تفصیلی صندوق زیر معین 111 — الگوی ثابت 111-XXXXX مثل مشتریان/تأمین‌کنندگان
        var seq = int.TryParse(cashBoxCode, out var n) ? n : 0;
        var code = seq > 0 ? $"111-{seq:D5}" : $"111-{cashBoxCode.Trim()}";
        var systemCode = seq > 0 ? $"CASH_{seq:D5}" : $"CASH_{cashBoxCode.Trim()}";

        var existing = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Code == code && a.IsDeleted != true, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var parent = await GetBySystemCodeAsync(AccountSystemCode.CashBoxes, cancellationToken);
        var account = new Account
        {
            Code = code,
            Name = cashBoxName,
            Level = AccountLevel.Tafsili,
            ParentAccountId = parent.AccountID,
            AccountType = AccountType.Asset,
            Nature = AccountNature.Debit,
            IsPostable = true,
            IsSystem = true,
            SystemCode = systemCode,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
        };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<Account> EnsureBankAccountAsync(
        string code,
        string name,
        CancellationToken cancellationToken = default)
    {
        // تفصیلی بانک زیر معین 112 — الگوی ثابت 112-XXXXX
        var seq = int.TryParse(code, out var n) ? n : 0;
        var glCode = seq > 0 ? $"112-{seq:D5}" : $"112-{code.Trim()}";
        var systemCode = seq > 0 ? $"BANK_{seq:D5}" : $"BANK_{code.Trim()}";

        var existing = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Code == glCode && a.IsDeleted != true, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var parent = await GetBySystemCodeAsync(AccountSystemCode.Banks, cancellationToken);
        if (parent.IsPostable)
        {
            parent.IsPostable = false;
        }

        var account = new Account
        {
            Code = glCode,
            Name = name,
            Level = AccountLevel.Tafsili,
            ParentAccountId = parent.AccountID,
            AccountType = AccountType.Asset,
            Nature = AccountNature.Debit,
            IsPostable = true,
            IsSystem = true,
            SystemCode = systemCode,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
        };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<Account> EnsureShareholderAccountAsync(
        int shareholderId,
        string shareholderName,
        CancellationToken cancellationToken = default)
    {
        var code = $"311-{shareholderId:D5}";
        var existing = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Code == code && a.IsDeleted != true, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.Name, shareholderName, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(shareholderName))
            {
                existing.Name = shareholderName;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return existing;
        }

        var parent = await GetBySystemCodeAsync(AccountSystemCode.CapitalMoein, cancellationToken);
        var account = new Account
        {
            Code = code,
            Name = shareholderName,
            Level = AccountLevel.Tafsili,
            ParentAccountId = parent.AccountID,
            AccountType = AccountType.Equity,
            Nature = AccountNature.Credit,
            IsPostable = true,
            IsSystem = true,
            SystemCode = $"SH_{shareholderId}",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
        };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<Account> EnsureVehicleOwnerAccountAsync(
        int vehicleOwnerId,
        string ownerName,
        CancellationToken cancellationToken = default)
    {
        var code = $"213-{vehicleOwnerId:D5}";
        var existing = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Code == code && a.IsDeleted != true, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var parent = await GetBySystemCodeAsync(AccountSystemCode.OwnerPayable, cancellationToken);
        var account = new Account
        {
            Code = code,
            Name = ownerName,
            Level = AccountLevel.Tafsili,
            ParentAccountId = parent.AccountID,
            AccountType = AccountType.Liability,
            Nature = AccountNature.Credit,
            IsPostable = true,
            IsSystem = true,
            SystemCode = $"OWNER_{vehicleOwnerId}",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
        };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<Account> EnsureDriverAccountAsync(
        int driverId,
        string driverName,
        CancellationToken cancellationToken = default)
    {
        var code = $"214-{driverId:D5}";
        var existing = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Code == code && a.IsDeleted != true, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var parent = await GetBySystemCodeAsync(AccountSystemCode.DriverPayable, cancellationToken);
        var account = new Account
        {
            Code = code,
            Name = driverName,
            Level = AccountLevel.Tafsili,
            ParentAccountId = parent.AccountID,
            AccountType = AccountType.Liability,
            Nature = AccountNature.Credit,
            IsPostable = true,
            IsSystem = true,
            SystemCode = $"DRIVER_{driverId}",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
        };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<Account> ResolveRetainedEarningsPostableAsync(CancellationToken cancellationToken = default)
    {
        var root = await GetBySystemCodeAsync(AccountSystemCode.RetainedEarnings, cancellationToken);
        if (root.IsPostable)
        {
            return root;
        }

        var postable = await _db.Accounts
            .Where(a => a.ParentAccountId == root.AccountID && a.IsDeleted != true && a.IsPostable)
            .OrderBy(a => a.Code)
            .FirstOrDefaultAsync(cancellationToken);

        return postable
            ?? throw new InvalidOperationException(
                "حساب قابل‌ثبت سود انباشته (زیر SYS_RETAINED) در کدینگ یافت نشد.");
    }

    public async Task<int> ResolveInventoryAccountIdAsync(
        WarehouseType warehouseType,
        CancellationToken cancellationToken = default)
    {
        var systemCode = warehouseType switch
        {
            WarehouseType.RawMaterials => AccountSystemCode.InventoryRaw,
            WarehouseType.SemiFinished => AccountSystemCode.InventorySemi,
            WarehouseType.Processed => AccountSystemCode.InventoryFg,
            _ => AccountSystemCode.InventoryFg,
        };

        var account = await GetBySystemCodeAsync(systemCode, cancellationToken);
        return account.AccountID;
    }
}
