using HamgamCementWeb.Server.Data.Models;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Data.Models.Inventory;
using HamgamCementWeb.Server.Data.Models.People;
using HamgamCementWeb.Server.Data.Models.Product;
using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Data.Seed;

/// <summary>
/// داده‌های پایه برای توسعه — اگر جدول خالی باشد یک رکورد پیش‌فرض می‌سازد.
/// </summary>
public static class DataSeeder
{
    public const string DefaultDepartmentName = "مدیریت";
    public const string DefaultRoleName = "مدیر سیستم";
    public const string DefaultUserName = "admin";
    public const string DefaultPassword = "admin";
    public const string DefaultEmail = "admin@hamgam.local";
    public const string DefaultShareholderFirstName = "صاحب";
    public const string DefaultShareholderLastName = "امتیاز";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        // موقت: فقط یوزر ادمین، واحدات و سیستم کدینگ
        var department = await EnsureDepartmentAsync(db, cancellationToken);
        var employee = await EnsureEmployeeAsync(db, department, cancellationToken);
        var role = await EnsureRoleAsync(db, cancellationToken);
        await EnsureUserAsync(db, passwordHasher, employee, role, cancellationToken);
        // await SeedData(db, cancellationToken);
        await EnsureMeaurmentsAsync(db, cancellationToken);
        // await EnsureProductCategoriesAsync(db, cancellationToken);
        // await EnsureWarehousesAsync(db, cancellationToken);

        var financeCategories = scope.ServiceProvider.GetRequiredService<IFinanceCategoryService>();
        await financeCategories.EnsureSystemCategoriesAsync(cancellationToken);
        await ChartOfAccountsSeeder.EnsureAsync(db, cancellationToken);
        // await UsersSchemaSeeder.EnsureAsync(db, cancellationToken);
        // دسته‌بندی‌های پیش‌فرض هزینه تولید (مستقیم / غیرمستقیم / …)
        await ProductionSchemaSeeder.EnsureAsync(db, cancellationToken);
        // await ProductSchemaSeeder.EnsureAsync(db, cancellationToken);
        // await InventorySchemaSeeder.EnsureAsync(db, cancellationToken);
        // await FiscalYearSchemaSeeder.EnsureAsync(db, cancellationToken);
        // await CashSchemaSeeder.EnsureAsync(db, cancellationToken);
        // var cashBoxService = scope.ServiceProvider.GetRequiredService<ICashBoxService>();
        // await CashSchemaSeeder.EnsureDefaultCashBoxAsync(db, cashBoxService, cancellationToken);
        // await BankSchemaSeeder.EnsureAsync(db, cancellationToken);
        await AccountingCompletenessSchemaSeeder.EnsureAsync(db, cancellationToken);
        // await CurrencyExchangeSchemaSeeder.EnsureAsync(db, cancellationToken);
        // await FixedAssetSchemaSeeder.EnsureAsync(db, cancellationToken);
        // await EquitySchemaSeeder.EnsureAsync(db, cancellationToken);
        // var accountLookup = scope.ServiceProvider.GetRequiredService<IAccountLookupService>();
        // await EnsureDefaultShareholderAsync(db, accountLookup, cancellationToken);
        await TransportRemovalSeeder.EnsureAsync(db, cancellationToken);
        // await EnsureGeneralSettingsAsync(db, cancellationToken);
    }

    private static async Task<Department> EnsureDepartmentAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var department = await db.Departments
            .FirstOrDefaultAsync(d => d.IsDeleted != true, cancellationToken);

        if (department is not null)
        {
            return department;
        }

        department = new Department
        {
            Name = DefaultDepartmentName,
            Description = "واحد پیش‌فرض سیستم",
            IsSelected = true,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
        };

        db.Departments.Add(department);
        await db.SaveChangesAsync(cancellationToken);

        return department;
    }

    private static async Task<Employee> EnsureEmployeeAsync(
        AppDbContext db,
        Department department,
        CancellationToken cancellationToken)
    {
        var employee = await db.Employees
            .FirstOrDefaultAsync(e => e.IsDeleted != true, cancellationToken);

        if (employee is not null)
        {
            return employee;
        }

        employee = new Employee
        {
            Title = PersonTitle.Mr,
            Name = "مدیر",
            FatherName = "سیستم",
            Family = "پیش‌فرض",
            NationalCode = "0000000000",
            Mobile = "0700000000",
            Address = string.Empty,
            Sallary = 0,
            DepartmentId = department.DepartmentID,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
        };

        db.Employees.Add(employee);
        await db.SaveChangesAsync(cancellationToken);

        return employee;
    }

    // نقش پیش‌فرض برای کاربر اول — User بدون Role قابل ذخیره نیست
    private static async Task<Role> EnsureRoleAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var role = await db.Roles
            .FirstOrDefaultAsync(r => r.IsDeleted != true, cancellationToken);

        if (role is not null)
        {
            return role;
        }

        role = new Role
        {
            Name = DefaultRoleName,
            Description = "دسترسی کامل مدیریت سیستم",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);

        return role;
    }
    private static async Task SeedData(AppDbContext db, CancellationToken cancellationToken)
    {
        var currencies = new[] {
            new Currency{Name = "افغانی", CurrencyCode = "AFN", DecimalPlaces = 2, Symbol = "؋", IsBaseCurrency = true, CreatedBy = 1},
        };
        var suppliers = new[]
        {
            new Supplier { Title = PersonTitle.Mr, Name = "شرکت مصالح ساختمانی همکار", PhoneNumber = "93701234501", Address = "جاده اسلام قلعه، ناحیه ۱۲", City = "هرات", Country = "افغانستان", InitialBalance = 0, SupplierType = PersonType.LegalEntity, CreatedBy = 1 },
            new Supplier { Title = PersonTitle.Mr, Name = "تأمین‌کننده فولاد افغان", PhoneNumber = "93701234502", Address = "شهرنو، کوچه ۸", City = "کابل", Country = "افغانستان", InitialBalance = 0, SupplierType = PersonType.LegalEntity, CreatedBy = 1 },
            new Supplier { Title = PersonTitle.Mr, Name = "محمد جواد اصغری", PhoneNumber = "93701234503", Address = "چهارراهی ملک، ناحیه ۳", City = "هرات", Country = "افغانستان", InitialBalance = 0, SupplierType = PersonType.NaturalPerson, CreatedBy = 1 },
            new Supplier { Title = PersonTitle.Mr, Name = "شرکت واردات مصالح شرق", PhoneNumber = "93701234504", Address = "کارته پروان، سرک ۱۵", City = "کابل", Country = "افغانستان", InitialBalance = 0, SupplierType = PersonType.LegalEntity, CreatedBy = 1 },
            new Supplier { Title = PersonTitle.Mr, Name = "احمد شاه صادقی", PhoneNumber = "93701234505", Address = "سراب، ناحیه ۵", City = "مزار شریف", Country = "افغانستان", InitialBalance = 0, SupplierType = PersonType.NaturalPerson, CreatedBy = 1 },
            new Supplier { Title = PersonTitle.Mr, Name = "شرکت سمنت و مصالح قندهار", PhoneNumber = "93701234506", Address = "ناحیه ۲، سرک اصلی", City = "قندهار", Country = "افغانستان", InitialBalance = 0, SupplierType = PersonType.LegalEntity, CreatedBy = 1 },
            new Supplier { Title = PersonTitle.Mr, Name = "عبدالرحمن کریمی", PhoneNumber = "93701234507", Address = "جاده ایار، ناحیه ۷", City = "هرات", Country = "افغانستان", InitialBalance = 0, SupplierType = PersonType.NaturalPerson, CreatedBy = 1 },
            new Supplier { Title = PersonTitle.Mr, Name = "مجموعه تجاری پدرام", PhoneNumber = "93701234508", Address = "خیرخانه، سرک ۲۲", City = "کابل", Country = "افغانستان", InitialBalance = 0, SupplierType = PersonType.LegalEntity, CreatedBy = 1 },
            new Supplier { Title = PersonTitle.Mr, Name = "ناصر احمدی", PhoneNumber = "93701234509", Address = "دهدشت، مرکز شهر", City = "بلخ", Country = "افغانستان", InitialBalance = 0, SupplierType = PersonType.NaturalPerson, CreatedBy = 1 },
            new Supplier { Title = PersonTitle.Mr, Name = "شرکت توزیع مصالح ساختمانی اعلم", PhoneNumber = "93701234510", Address = "بازار شاهد شمالی", City = "هرات", Country = "افغانستان", InitialBalance = 0, SupplierType = PersonType.LegalEntity, CreatedBy = 1 },
        };
        var customers = new[]
        {
            new Customer { Name = "محمد حسین کریمی", PhoneNumber = "93702345601", Address = "خیابان انقلاب، ناحیه ۳", City = "هرات", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.NaturalPerson, CreatedBy = 1 },
            new Customer { Name = "شرکت ساختمانی آریا", PhoneNumber = "93702345602", Address = "شهرنو، کوچه ۵", City = "کابل", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.LegalEntity, CreatedBy = 1 },
            new Customer { Name = "احمد نور احمدی", PhoneNumber = "93702345603", Address = "چهارراهی بلخ", City = "مزار شریف", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.NaturalPerson, CreatedBy = 1 },
            new Customer { Name = "فروشگاه مصالح ساختمانی پویا", PhoneNumber = "93702345604", Address = "جاده اسلام قلعه", City = "هرات", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.LegalEntity, CreatedBy = 1 },
            new Customer { Name = "عبدالله رحیمی", PhoneNumber = "93702345605", Address = "ناحیه ۲، سرک اصلی", City = "قندهار", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.NaturalPerson, CreatedBy = 1 },
            new Customer { Name = "شرکت راه‌سازی شمال", PhoneNumber = "93702345606", Address = "شهرک صنعتی", City = "بلخ", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.LegalEntity, CreatedBy = 1 },
            new Customer { Name = "فاطمه نوری", PhoneNumber = "93702345607", Address = "خیرخانه، سرک ۱۰", City = "کابل", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.NaturalPerson, CreatedBy = 1 },
            new Customer { Name = "سید جعفر موسوی", PhoneNumber = "93702345608", Address = "ناحیه ۱۰", City = "هرات", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.NaturalPerson, CreatedBy = 1 },
            new Customer { Name = "مجتمع مسکونی آسمان", PhoneNumber = "93702345609", Address = "کتی کابل", City = "کابل", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.LegalEntity, CreatedBy = 1 },
            new Customer { Name = "غلام عباس صفری", PhoneNumber = "93702345610", Address = "مرکز شهر زرنج", City = "نیمروز", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.NaturalPerson, CreatedBy = 1 },
            new Customer { Name = "شرکت بازرگانی صمد", PhoneNumber = "93702345611", Address = "بازار شاهد شمالی", City = "هرات", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.LegalEntity, CreatedBy = 1 },
            new Customer { Name = "زهرا محمدی", PhoneNumber = "93702345612", Address = "سراب، ناحیه ۴", City = "مزار شریف", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.NaturalPerson, CreatedBy = 1 },
            new Customer { Name = "نیک محمد اکبری", PhoneNumber = "93702345613", Address = "ناحیه ۷", City = "قندهار", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.NaturalPerson, CreatedBy = 1 },
            new Customer { Name = "شرکت پیمانکاری رضایی", PhoneNumber = "93702345614", Address = "جاده ایار", City = "هرات", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.LegalEntity, CreatedBy = 1 },
            new Customer { Name = "حسین علی شفیقی", PhoneNumber = "93702345615", Address = "کارته پروان", City = "کابل", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.NaturalPerson, CreatedBy = 1 },
            new Customer { Name = "تجارتخانه مصالح ساختمانی برادران", PhoneNumber = "93702345616", Address = "چهارراهی ملک", City = "هرات", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.LegalEntity, CreatedBy = 1 },
            new Customer { Name = "عبدالستار حیدری", PhoneNumber = "93702345617", Address = "دهدشت", City = "بلخ", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.NaturalPerson, CreatedBy = 1 },
            new Customer { Name = "رقیه سادات", PhoneNumber = "93702345618", Address = "کله حسین", City = "کابل", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.NaturalPerson, CreatedBy = 1 },
            new Customer { Name = "شرکت ساختمانی آفتاب", PhoneNumber = "93702345619", Address = "شهرک صنعتی", City = "مزار شریف", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.LegalEntity, CreatedBy = 1 },
            new Customer { Name = "علی احمدی", PhoneNumber = "93702345620", Address = "ناحیه ۱۵", City = "هرات", Country = "افغانستان", InitialBalance = 0, CustomerType = PersonType.NaturalPerson, CreatedBy = 1 },
        };
        if (!await db.Currencies.AnyAsync(cancellationToken))
        {
            await db.AddRangeAsync(currencies);
            await db.AddRangeAsync(suppliers);
            await db.AddRangeAsync(customers);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task EnsureMeaurmentsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Meaurments.AnyAsync(cancellationToken))
        {
            return;
        }

        var kg = new Meaurment
        {
            Name = "کیلوگرم",
            Symbol = "Kg",
            IsBaseUnit = true,
            FactorToBase = 1,
            CreatedBy = 1,
        };
        var unit = new Meaurment
        {
            Name = "عدد",
            Symbol = "ع",
            IsBaseUnit = true,
            FactorToBase = 1,
            CreatedBy = 1,
        };
        db.Meaurments.Add(kg);
        db.Meaurments.Add(unit);
        await db.SaveChangesAsync(cancellationToken);

        db.Meaurments.AddRange(
            new Meaurment
            {
                Name = "پاکت",
                Symbol = "پ",
                IsBaseUnit = false,
                BaseMeaurmentId = kg.MeaurmentID,
                FactorToBase = 50,
                CreatedBy = 1,
            },
            new Meaurment
            {
                Name = "تن",
                Symbol = "ت",
                IsBaseUnit = false,
                BaseMeaurmentId = kg.MeaurmentID,
                FactorToBase = 1000,
                CreatedBy = 1,
            });
        await db.SaveChangesAsync(cancellationToken);
    }

    // دسته‌بندی‌های پایه محصول: بدون دسته‌بندی / خام / پروسس شده
    private static async Task EnsureProductCategoriesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var names = new[] { "بدون دسته‌بندی", "خام", "پروسس شده" };
        var now = DateTime.Now;
        var added = false;

        foreach (var name in names)
        {
            var exists = await db.Categories
                .AnyAsync(c => c.Name == name && c.IsDeleted != true, cancellationToken);
            if (exists)
            {
                continue;
            }

            db.Categories.Add(new Category
            {
                Name = name,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = 1,
            });
            added = true;
        }

        if (added)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    // انبارهای پایه: سیلو مرکزی (پروسس‌شده) و سیلو کارگاه (خام)
    private static async Task EnsureWarehousesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var warehouses = new (string Name, WarehouseType Type, string Description)[]
        {
            ("سیلو مرکزی", WarehouseType.Processed, "انبار پروسس شده"),
            ("سیلو کارگاه", WarehouseType.RawMaterials, "انبار خام"),
        };
        var now = DateTime.Now;
        var added = false;

        foreach (var (name, type, description) in warehouses)
        {
            var exists = await db.Warehouses
                .AnyAsync(w => w.Name == name && w.IsDeleted != true, cancellationToken);
            if (exists)
            {
                continue;
            }

            db.Warehouses.Add(new Warehouse
            {
                Name = name,
                WarehouseType = type,
                Description = description,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                Capacity = 10000,
                CapacityMeaurmentId = 1,
                CreatedBy = 1,
            });
            added = true;
        }

        if (added)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task EnsureUserAsync(
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        Employee employee,
        Role role,
        CancellationToken cancellationToken)
    {
        var exists = await db.Users
            .AnyAsync(u => u.IsDeleted != true, cancellationToken);

        if (exists)
        {
            return;
        }

        var user = new User
        {
            Title = PersonTitle.Mr,
            FullName = "مدیر سیستم",
            Email = DefaultEmail,
            UserName = DefaultUserName,
            PasswordHash = passwordHasher.HashPassword(null!, DefaultPassword),
            RoleId = role.RoleID,
            EmployeeId = employee.EmployeeID,
            HasFullAccess = true,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureGeneralSettingsAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var exists = await db.GeneralSettings.AnyAsync(cancellationToken);
        if (exists)
        {
            return;
        }

        db.GeneralSettings.Add(new GeneralSettings
        {
            ZmLogoPath = "/zm_logo.jpg",
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    // حداقل یک سهامدار فعال برای تخصیص سود/زیان و اسناد سرمایه لازم است
    private static async Task EnsureDefaultShareholderAsync(
        AppDbContext db,
        IAccountLookupService accounts,
        CancellationToken cancellationToken)
    {
        var hasAny = await db.Shareholders
            .AnyAsync(s => s.IsDeleted != true, cancellationToken);
        if (hasAny)
        {
            return;
        }

        var createdBy = await db.Users
            .Where(u => u.IsDeleted != true)
            .Select(u => (int?)u.UserID)
            .FirstOrDefaultAsync(cancellationToken);

        var shareholder = new Shareholder
        {
            Title = PersonTitle.Mr,
            FirstName = DefaultShareholderFirstName,
            LastName = DefaultShareholderLastName,
            InitialBalance = 0,
            Description = "سهام‌دار پیش‌فرض سیستم",
            ProfitShare = 100m,
            LossShare = 100m,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            CreatedBy = createdBy,
        };

        db.Shareholders.Add(shareholder);
        await db.SaveChangesAsync(cancellationToken);

        var fullName = $"{shareholder.FirstName} {shareholder.LastName}".Trim();
        var account = await accounts.EnsureShareholderAccountAsync(
            shareholder.ShareholderID,
            fullName,
            cancellationToken);
        shareholder.AccountId = account.AccountID;
        await db.SaveChangesAsync(cancellationToken);
    }
}
