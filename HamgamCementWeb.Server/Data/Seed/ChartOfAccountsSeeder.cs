using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Data.Seed;

// بذر کدینگ چهارسطحی کارخانه — حساب‌های سیستمی با SystemCode ثابت
public static class ChartOfAccountsSeeder
{
    public static async Task EnsureAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Accounts.AnyAsync(a => a.IsDeleted != true, cancellationToken))
        {
            await EnsureProductionCostAccountsAsync(db, cancellationToken);
            await EnsureFixedAssetAccountsAsync(db, cancellationToken);
            await EnsureEquityAccountsAsync(db, cancellationToken);
            await EnsureTransportRevenueAccountAsync(db, cancellationToken);
            await MapCategoryAccountsAsync(db, cancellationToken);
            return;
        }

        var now = DateTime.Now;

        Account Add(
            string code,
            string name,
            AccountLevel level,
            AccountType type,
            AccountNature nature,
            Account? parent,
            string? systemCode,
            bool postable = false)
        {
            var account = new Account
            {
                Code = code,
                Name = name,
                Level = level,
                ParentAccountId = parent?.AccountID,
                AccountType = type,
                Nature = nature,
                IsPostable = postable,
                IsSystem = true,
                SystemCode = systemCode,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
            };
            db.Accounts.Add(account);
            return account;
        }

        // گروه ۱ دارایی‌ها
        var assets = Add("1", "دارایی‌ها", AccountLevel.Group, AccountType.Asset, AccountNature.Debit, null, AccountSystemCode.Assets);
        await db.SaveChangesAsync(cancellationToken);

        var cashBank = Add("11", "نقد و بانک", AccountLevel.Kol, AccountType.Asset, AccountNature.Debit, assets, AccountSystemCode.CashAndBank);
        var receivablesKol = Add("12", "حساب‌های دریافتنی", AccountLevel.Kol, AccountType.Asset, AccountNature.Debit, assets, AccountSystemCode.Receivables);
        var inventoryKol = Add("13", "موجودی کالا", AccountLevel.Kol, AccountType.Asset, AccountNature.Debit, assets, AccountSystemCode.Inventory);
        var otherCa = Add("14", "سایر دارایی‌های جاری", AccountLevel.Kol, AccountType.Asset, AccountNature.Debit, assets, AccountSystemCode.OtherCurrentAssets);
        var fixedAssetsKol = Add("15", "دارایی‌های ثابت", AccountLevel.Kol, AccountType.Asset, AccountNature.Debit, assets, AccountSystemCode.FixedAssets);
        var accumDeprKol = Add("16", "استهلاک انباشته دارایی ثابت", AccountLevel.Kol, AccountType.Asset, AccountNature.Credit, assets, AccountSystemCode.AccumulatedDepreciationKol);
        await db.SaveChangesAsync(cancellationToken);

        var cashBoxes = Add("111", "صندوق‌ها", AccountLevel.Moein, AccountType.Asset, AccountNature.Debit, cashBank, AccountSystemCode.CashBoxes);
        var banks = Add("112", "بانک‌ها", AccountLevel.Moein, AccountType.Asset, AccountNature.Debit, cashBank, AccountSystemCode.Banks, postable: true);
        var ar = Add("121", "مشتریان", AccountLevel.Moein, AccountType.Asset, AccountNature.Debit, receivablesKol, AccountSystemCode.CustomersAr);
        var invRaw = Add("131", "مواد اولیه", AccountLevel.Moein, AccountType.Asset, AccountNature.Debit, inventoryKol, AccountSystemCode.InventoryRaw, postable: true);
        var invSemi = Add("132", "نیمه‌ساخته", AccountLevel.Moein, AccountType.Asset, AccountNature.Debit, inventoryKol, AccountSystemCode.InventorySemi, postable: true);
        var invFg = Add("133", "محصول ساخته", AccountLevel.Moein, AccountType.Asset, AccountNature.Debit, inventoryKol, AccountSystemCode.InventoryFg, postable: true);
        Add("141", "سایر دارایی جاری", AccountLevel.Moein, AccountType.Asset, AccountNature.Debit, otherCa, null, postable: true);
        Add("151", "ماشین‌آلات و تجهیزات", AccountLevel.Moein, AccountType.Asset, AccountNature.Debit, fixedAssetsKol, AccountSystemCode.FixedAssetMachinery, postable: true);
        Add("152", "وسایل نقلیه", AccountLevel.Moein, AccountType.Asset, AccountNature.Debit, fixedAssetsKol, AccountSystemCode.FixedAssetVehicles, postable: true);
        Add("153", "اثاثیه و منصوبات", AccountLevel.Moein, AccountType.Asset, AccountNature.Debit, fixedAssetsKol, AccountSystemCode.FixedAssetFurniture, postable: true);
        Add("154", "ساختمان", AccountLevel.Moein, AccountType.Asset, AccountNature.Debit, fixedAssetsKol, AccountSystemCode.FixedAssetBuildings, postable: true);
        Add("161", "استهلاک انباشته", AccountLevel.Moein, AccountType.Asset, AccountNature.Credit, accumDeprKol, AccountSystemCode.AccumulatedDepreciation, postable: true);
        await db.SaveChangesAsync(cancellationToken);

        // گروه ۲ بدهی‌ها
        var liabilities = Add("2", "بدهی‌ها", AccountLevel.Group, AccountType.Liability, AccountNature.Credit, null, AccountSystemCode.Liabilities);
        await db.SaveChangesAsync(cancellationToken);

        var payablesKol = Add("21", "حساب‌های پرداختنی", AccountLevel.Kol, AccountType.Liability, AccountNature.Credit, liabilities, AccountSystemCode.Payables);
        var otherLiab = Add("22", "سایر بدهی‌ها", AccountLevel.Kol, AccountType.Liability, AccountNature.Credit, liabilities, AccountSystemCode.OtherLiabilities);
        await db.SaveChangesAsync(cancellationToken);

        var ap = Add("211", "تأمین‌کنندگان", AccountLevel.Moein, AccountType.Liability, AccountNature.Credit, payablesKol, AccountSystemCode.SuppliersAp);
        Add("221", "سایر بدهی‌ها", AccountLevel.Moein, AccountType.Liability, AccountNature.Credit, otherLiab, null, postable: true);
        Add("222", "سود سهام پرداختنی", AccountLevel.Moein, AccountType.Liability, AccountNature.Credit, otherLiab, AccountSystemCode.DividendPayable, postable: true);
        await db.SaveChangesAsync(cancellationToken);

        // گروه ۳ حقوق مالکانه
        var equity = Add("3", "حقوق مالکانه", AccountLevel.Group, AccountType.Equity, AccountNature.Credit, null, AccountSystemCode.Equity);
        await db.SaveChangesAsync(cancellationToken);

        var capitalKol = Add("31", "سرمایه", AccountLevel.Kol, AccountType.Equity, AccountNature.Credit, equity, AccountSystemCode.Capital);
        var retainedKol = Add("32", "سود انباشته", AccountLevel.Kol, AccountType.Equity, AccountNature.Credit, equity, AccountSystemCode.RetainedEarnings);
        var openingKol = Add("33", "افتتاحیه سرمایه", AccountLevel.Kol, AccountType.Equity, AccountNature.Debit, equity, null);
        await db.SaveChangesAsync(cancellationToken);

        // معین سرمایه والد تفصیلی سهامداران — غیرقابل‌ثبت مستقیم
        Add("311", "سرمایه سهامداران", AccountLevel.Moein, AccountType.Equity, AccountNature.Credit, capitalKol, AccountSystemCode.CapitalMoein, postable: false);
        Add("321", "سود و زیان انباشته", AccountLevel.Moein, AccountType.Equity, AccountNature.Credit, retainedKol, null, postable: true);
        Add("331", "حساب افتتاحیه سرمایه", AccountLevel.Moein, AccountType.Equity, AccountNature.Debit, openingKol, AccountSystemCode.EquityOpening, postable: true);
        await db.SaveChangesAsync(cancellationToken);

        // گروه ۴ درآمدها
        var revenues = Add("4", "درآمدها", AccountLevel.Group, AccountType.Revenue, AccountNature.Credit, null, AccountSystemCode.Revenues);
        await db.SaveChangesAsync(cancellationToken);

        var salesKol = Add("41", "فروش محصولات", AccountLevel.Kol, AccountType.Revenue, AccountNature.Credit, revenues, null);
        var otherRevKol = Add("42", "سایر درآمدها", AccountLevel.Kol, AccountType.Revenue, AccountNature.Credit, revenues, null);
        await db.SaveChangesAsync(cancellationToken);

        Add("411", "فروش کالا", AccountLevel.Moein, AccountType.Revenue, AccountNature.Credit, salesKol, AccountSystemCode.ProductSales, postable: true);
        Add("421", "سایر درآمدها", AccountLevel.Moein, AccountType.Revenue, AccountNature.Credit, otherRevKol, AccountSystemCode.OtherRevenue, postable: true);
        Add("422", "سود فروش دارایی ثابت", AccountLevel.Moein, AccountType.Revenue, AccountNature.Credit, otherRevKol, AccountSystemCode.FixedAssetDisposalGain, postable: true);
        Add("423", "درآمد حمل‌ونقل", AccountLevel.Moein, AccountType.Revenue, AccountNature.Credit, otherRevKol, AccountSystemCode.TransportRevenue, postable: true);
        await db.SaveChangesAsync(cancellationToken);

        // گروه ۵ بهای تمام‌شده
        var cogsGroup = Add("5", "بهای تمام‌شده", AccountLevel.Group, AccountType.Cogs, AccountNature.Debit, null, AccountSystemCode.CogsGroup);
        await db.SaveChangesAsync(cancellationToken);

        var cogsKol = Add("51", "بهای تمام‌شده کالای فروش‌رفته", AccountLevel.Kol, AccountType.Cogs, AccountNature.Debit, cogsGroup, null);
        var adjKol = Add("52", "تعدیل و ضایعات موجودی", AccountLevel.Kol, AccountType.Cogs, AccountNature.Debit, cogsGroup, null);
        await db.SaveChangesAsync(cancellationToken);

        Add("511", "بهای تمام‌شده فروش", AccountLevel.Moein, AccountType.Cogs, AccountNature.Debit, cogsKol, AccountSystemCode.Cogs, postable: true);
        Add("521", "ضایعات و تعدیل", AccountLevel.Moein, AccountType.Cogs, AccountNature.Debit, adjKol, AccountSystemCode.InventoryAdjustment, postable: true);
        await db.SaveChangesAsync(cancellationToken);

        // گروه ۶ هزینه‌ها
        var expenses = Add("6", "هزینه‌ها", AccountLevel.Group, AccountType.Expense, AccountNature.Debit, null, AccountSystemCode.Expenses);
        await db.SaveChangesAsync(cancellationToken);

        var opexKol = Add("61", "هزینه‌های عملیاتی", AccountLevel.Kol, AccountType.Expense, AccountNature.Debit, expenses, null);
        var transportKol = Add("62", "حمل‌ونقل", AccountLevel.Kol, AccountType.Expense, AccountNature.Debit, expenses, null);
        var miscKol = Add("69", "متفرقه", AccountLevel.Kol, AccountType.Expense, AccountNature.Debit, expenses, null);
        await db.SaveChangesAsync(cancellationToken);

        var opex = Add("611", "هزینه عملیاتی", AccountLevel.Moein, AccountType.Expense, AccountNature.Debit, opexKol, AccountSystemCode.OperatingExpense, postable: true);
        var transport = Add("621", "هزینه حمل‌ونقل", AccountLevel.Moein, AccountType.Expense, AccountNature.Debit, transportKol, AccountSystemCode.TransportExpense, postable: true);
        var misc = Add("691", "هزینه متفرقه", AccountLevel.Moein, AccountType.Expense, AccountNature.Debit, miscKol, AccountSystemCode.MiscExpense, postable: true);
        Add("612", "دستمزد مستقیم تولید", AccountLevel.Moein, AccountType.Expense, AccountNature.Debit, opexKol, AccountSystemCode.ProductionWage, postable: true);
        Add("613", "سربار تولید", AccountLevel.Moein, AccountType.Expense, AccountNature.Debit, opexKol, AccountSystemCode.ProductionOverhead, postable: true);
        Add("614", "هزینه جانبی تولید", AccountLevel.Moein, AccountType.Expense, AccountNature.Debit, opexKol, AccountSystemCode.ProductionAncillary, postable: true);
        Add("615", "هزینه ثابت تولید", AccountLevel.Moein, AccountType.Expense, AccountNature.Debit, opexKol, AccountSystemCode.ProductionFixed, postable: true);
        Add("616", "حقوق و دستمزد کارکنان", AccountLevel.Moein, AccountType.Expense, AccountNature.Debit, opexKol, AccountSystemCode.SalaryExpense, postable: true);
        Add("617", "هزینه استهلاک دارایی ثابت", AccountLevel.Moein, AccountType.Expense, AccountNature.Debit, opexKol, AccountSystemCode.DepreciationExpense, postable: true);
        Add("692", "زیان فروش دارایی ثابت", AccountLevel.Moein, AccountType.Expense, AccountNature.Debit, miscKol, AccountSystemCode.FixedAssetDisposalLoss, postable: true);
        await db.SaveChangesAsync(cancellationToken);

        // معین‌های والد برای تفصیلی اشخاص/صندوق باید IsPostable=false بمانند
        cashBoxes.IsPostable = false;
        ar.IsPostable = false;
        ap.IsPostable = false;
        await db.SaveChangesAsync(cancellationToken);

        await MapCategoryAccountsAsync(db, cancellationToken);

        _ = (opex, transport, misc, banks, invRaw, invSemi, invFg);
    }

    // برای دیتابیس‌هایی که قبلاً کدینگ دارند — حساب‌های هزینه تولید را در صورت نبود اضافه می‌کند
    private static async Task EnsureProductionCostAccountsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var opexKol = await db.Accounts
            .FirstOrDefaultAsync(
                a => a.Code == "61" && a.Level == AccountLevel.Kol && a.IsDeleted != true,
                cancellationToken);
        if (opexKol is null)
        {
            return;
        }

        async Task Ensure(string code, string name, string systemCode)
        {
            var exists = await db.Accounts.AnyAsync(
                a => (a.SystemCode == systemCode || a.Code == code) && a.IsDeleted != true,
                cancellationToken);
            if (exists)
            {
                return;
            }

            db.Accounts.Add(new Account
            {
                Code = code,
                Name = name,
                Level = AccountLevel.Moein,
                ParentAccountId = opexKol.AccountID,
                AccountType = AccountType.Expense,
                Nature = AccountNature.Debit,
                IsPostable = true,
                IsSystem = true,
                SystemCode = systemCode,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
            });
        }

        await Ensure("612", "دستمزد مستقیم تولید", AccountSystemCode.ProductionWage);
        await Ensure("613", "سربار تولید", AccountSystemCode.ProductionOverhead);
        await Ensure("614", "هزینه جانبی تولید", AccountSystemCode.ProductionAncillary);
        await Ensure("615", "هزینه ثابت تولید", AccountSystemCode.ProductionFixed);
        await db.SaveChangesAsync(cancellationToken);
    }

    // برای دیتابیس‌هایی که قبلاً کدینگ دارند — حساب‌های دارایی ثابت را در صورت نبود اضافه می‌کند
    private static async Task EnsureFixedAssetAccountsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var assetsGroup = await db.Accounts
            .FirstOrDefaultAsync(
                a => (a.SystemCode == AccountSystemCode.Assets || a.Code == "1")
                     && a.Level == AccountLevel.Group
                     && a.IsDeleted != true,
                cancellationToken);
        if (assetsGroup is null)
        {
            return;
        }

        async Task<Account> EnsureKol(string code, string name, string systemCode, AccountNature nature)
        {
            var existing = await db.Accounts.FirstOrDefaultAsync(
                a => (a.SystemCode == systemCode || a.Code == code) && a.IsDeleted != true,
                cancellationToken);
            if (existing is not null)
            {
                return existing;
            }

            var account = new Account
            {
                Code = code,
                Name = name,
                Level = AccountLevel.Kol,
                ParentAccountId = assetsGroup.AccountID,
                AccountType = AccountType.Asset,
                Nature = nature,
                IsPostable = false,
                IsSystem = true,
                SystemCode = systemCode,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
            };
            db.Accounts.Add(account);
            await db.SaveChangesAsync(cancellationToken);
            return account;
        }

        var faKol = await EnsureKol("15", "دارایی‌های ثابت", AccountSystemCode.FixedAssets, AccountNature.Debit);
        var accumKol = await EnsureKol("16", "استهلاک انباشته دارایی ثابت", AccountSystemCode.AccumulatedDepreciationKol, AccountNature.Credit);

        async Task EnsureMoein(string code, string name, string systemCode, Account parent, AccountNature nature, AccountType type)
        {
            var exists = await db.Accounts.AnyAsync(
                a => (a.SystemCode == systemCode || a.Code == code) && a.IsDeleted != true,
                cancellationToken);
            if (exists)
            {
                return;
            }

            db.Accounts.Add(new Account
            {
                Code = code,
                Name = name,
                Level = AccountLevel.Moein,
                ParentAccountId = parent.AccountID,
                AccountType = type,
                Nature = nature,
                IsPostable = true,
                IsSystem = true,
                SystemCode = systemCode,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
            });
        }

        await EnsureMoein("151", "ماشین‌آلات و تجهیزات", AccountSystemCode.FixedAssetMachinery, faKol, AccountNature.Debit, AccountType.Asset);
        await EnsureMoein("152", "وسایل نقلیه", AccountSystemCode.FixedAssetVehicles, faKol, AccountNature.Debit, AccountType.Asset);
        await EnsureMoein("153", "اثاثیه و منصوبات", AccountSystemCode.FixedAssetFurniture, faKol, AccountNature.Debit, AccountType.Asset);
        await EnsureMoein("154", "ساختمان", AccountSystemCode.FixedAssetBuildings, faKol, AccountNature.Debit, AccountType.Asset);
        await EnsureMoein("161", "استهلاک انباشته", AccountSystemCode.AccumulatedDepreciation, accumKol, AccountNature.Credit, AccountType.Asset);

        var opexKol = await db.Accounts
            .FirstOrDefaultAsync(a => a.Code == "61" && a.Level == AccountLevel.Kol && a.IsDeleted != true, cancellationToken);
        if (opexKol is not null)
        {
            await EnsureMoein("617", "هزینه استهلاک دارایی ثابت", AccountSystemCode.DepreciationExpense, opexKol, AccountNature.Debit, AccountType.Expense);
        }

        var otherRevKol = await db.Accounts
            .FirstOrDefaultAsync(a => a.Code == "42" && a.Level == AccountLevel.Kol && a.IsDeleted != true, cancellationToken);
        if (otherRevKol is not null)
        {
            await EnsureMoein("422", "سود فروش دارایی ثابت", AccountSystemCode.FixedAssetDisposalGain, otherRevKol, AccountNature.Credit, AccountType.Revenue);
        }

        var miscKol = await db.Accounts
            .FirstOrDefaultAsync(a => a.Code == "69" && a.Level == AccountLevel.Kol && a.IsDeleted != true, cancellationToken);
        if (miscKol is not null)
        {
            await EnsureMoein("692", "زیان فروش دارایی ثابت", AccountSystemCode.FixedAssetDisposalLoss, miscKol, AccountNature.Debit, AccountType.Expense);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    // حساب درآمد حمل‌ونقل برای دیتابیس‌های از قبل seedشده
    private static async Task EnsureTransportRevenueAccountAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var otherRevKol = await db.Accounts
            .FirstOrDefaultAsync(
                a => a.Code == "42" && a.Level == AccountLevel.Kol && a.IsDeleted != true,
                cancellationToken);
        if (otherRevKol is null)
        {
            return;
        }

        var exists = await db.Accounts.AnyAsync(
            a => (a.SystemCode == AccountSystemCode.TransportRevenue || a.Code == "423")
                 && a.IsDeleted != true,
            cancellationToken);
        if (exists)
        {
            return;
        }

        db.Accounts.Add(new Account
        {
            Code = "423",
            Name = "درآمد حمل‌ونقل",
            Level = AccountLevel.Moein,
            ParentAccountId = otherRevKol.AccountID,
            AccountType = AccountType.Revenue,
            Nature = AccountNature.Credit,
            IsPostable = true,
            IsSystem = true,
            SystemCode = AccountSystemCode.TransportRevenue,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    // همگام‌سازی حساب‌های حقوق مالکانه و سود پرداختنی برای دیتابیس‌های از قبل seedشده
    private static async Task EnsureEquityAccountsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var equity = await db.Accounts
            .FirstOrDefaultAsync(
                a => (a.SystemCode == AccountSystemCode.Equity || a.Code == "3")
                     && a.Level == AccountLevel.Group
                     && a.IsDeleted != true,
                cancellationToken);
        if (equity is null)
        {
            return;
        }

        var capitalKol = await db.Accounts
            .FirstOrDefaultAsync(
                a => (a.SystemCode == AccountSystemCode.Capital || a.Code == "31")
                     && a.IsDeleted != true,
                cancellationToken);
        var otherLiab = await db.Accounts
            .FirstOrDefaultAsync(
                a => (a.SystemCode == AccountSystemCode.OtherLiabilities || a.Code == "22")
                     && a.IsDeleted != true,
                cancellationToken);

        async Task<Account> EnsureKol(string code, string name, string? systemCode, AccountNature nature)
        {
            var existing = await db.Accounts
                .FirstOrDefaultAsync(
                    a => ((systemCode != null && a.SystemCode == systemCode) || a.Code == code)
                         && a.IsDeleted != true,
                    cancellationToken);
            if (existing is not null)
            {
                if (systemCode is not null && string.IsNullOrEmpty(existing.SystemCode))
                {
                    existing.SystemCode = systemCode;
                }

                return existing;
            }

            var account = new Account
            {
                Code = code,
                Name = name,
                Level = AccountLevel.Kol,
                ParentAccountId = equity.AccountID,
                AccountType = AccountType.Equity,
                Nature = nature,
                IsPostable = false,
                IsSystem = true,
                SystemCode = systemCode,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
            };
            db.Accounts.Add(account);
            await db.SaveChangesAsync(cancellationToken);
            return account;
        }

        async Task EnsureMoein(
            string code,
            string name,
            string systemCode,
            Account parent,
            AccountNature nature,
            AccountType type,
            bool postable)
        {
            var existing = await db.Accounts
                .FirstOrDefaultAsync(
                    a => (a.SystemCode == systemCode || a.Code == code) && a.IsDeleted != true,
                    cancellationToken);
            if (existing is not null)
            {
                if (string.IsNullOrEmpty(existing.SystemCode))
                {
                    existing.SystemCode = systemCode;
                }

                existing.IsPostable = postable;
                if (existing.Name != name && existing.Code == code)
                {
                    existing.Name = name;
                }

                return;
            }

            db.Accounts.Add(new Account
            {
                Code = code,
                Name = name,
                Level = AccountLevel.Moein,
                ParentAccountId = parent.AccountID,
                AccountType = type,
                Nature = nature,
                IsPostable = postable,
                IsSystem = true,
                SystemCode = systemCode,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
            });
        }

        if (capitalKol is not null)
        {
            await EnsureMoein(
                "311",
                "سرمایه سهامداران",
                AccountSystemCode.CapitalMoein,
                capitalKol,
                AccountNature.Credit,
                AccountType.Equity,
                postable: false);
        }

        var openingKol = await EnsureKol("33", "افتتاحیه سرمایه", null, AccountNature.Debit);
        await EnsureMoein(
            "331",
            "حساب افتتاحیه سرمایه",
            AccountSystemCode.EquityOpening,
            openingKol,
            AccountNature.Debit,
            AccountType.Equity,
            postable: true);

        if (otherLiab is not null)
        {
            await EnsureMoein(
                "222",
                "سود سهام پرداختنی",
                AccountSystemCode.DividendPayable,
                otherLiab,
                AccountNature.Credit,
                AccountType.Liability,
                postable: true);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task MapCategoryAccountsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        async Task MapExpense(string categoryCode, string accountSystemCode)
        {
            var category = await db.ExpenseCategories
                .FirstOrDefaultAsync(c => c.Code == categoryCode && c.IsDeleted != true, cancellationToken);
            if (category is null || category.AccountId is > 0)
            {
                return;
            }

            var accountId = await db.Accounts
                .Where(a => a.SystemCode == accountSystemCode && a.IsDeleted != true && a.IsPostable)
                .Select(a => a.AccountID)
                .FirstOrDefaultAsync(cancellationToken);
            if (accountId == 0)
            {
                return;
            }

            category.AccountId = accountId;
        }

        async Task MapRevenue(string categoryCode, string accountSystemCode)
        {
            var category = await db.RevenueCategories
                .FirstOrDefaultAsync(c => c.Code == categoryCode && c.IsDeleted != true, cancellationToken);
            if (category is null || category.AccountId is > 0)
            {
                return;
            }

            var accountId = await db.Accounts
                .Where(a => a.SystemCode == accountSystemCode && a.IsDeleted != true && a.IsPostable)
                .Select(a => a.AccountID)
                .FirstOrDefaultAsync(cancellationToken);
            if (accountId == 0)
            {
                return;
            }

            category.AccountId = accountId;
        }

        // خرید محصولات به موجودی محصول ساخته نگاشت می‌شود (نه هزینه) — دسته برای لایه عملیاتی می‌ماند
        await MapExpense(FinanceCategoryCode.MiscellaneousExpense, AccountSystemCode.MiscExpense);
        await MapExpense(FinanceCategoryCode.TransportExpense, AccountSystemCode.TransportExpense);
        await MapRevenue(FinanceCategoryCode.ProductSale, AccountSystemCode.ProductSales);
        await MapRevenue(FinanceCategoryCode.MiscellaneousRevenue, AccountSystemCode.OtherRevenue);
        await MapRevenue(FinanceCategoryCode.TransportRevenue, AccountSystemCode.TransportRevenue);

        // دسته خرید: حساب موجودی FG به‌عنوان مرجع پیش‌فرض (ثبت واقعی در Posting جداگانه است)
        var purchaseCat = await db.ExpenseCategories
            .FirstOrDefaultAsync(c => c.Code == FinanceCategoryCode.ProductPurchase && c.IsDeleted != true, cancellationToken);
        if (purchaseCat is not null && purchaseCat.AccountId is null or 0)
        {
            purchaseCat.AccountId = await db.Accounts
                .Where(a => a.SystemCode == AccountSystemCode.InventoryFg && a.IsDeleted != true)
                .Select(a => (int?)a.AccountID)
                .FirstOrDefaultAsync(cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
