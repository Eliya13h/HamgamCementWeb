using HamgamCementWeb.Server.Data.Models;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Data.Models.Inventory;
using HamgamCementWeb.Server.Data.Models.People;
using HamgamCementWeb.Server.Data.Models.Product;
using HamgamCementWeb.Server.Data.Models.Transport;
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

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        var department = await EnsureDepartmentAsync(db, cancellationToken);
        var employee = await EnsureEmployeeAsync(db, department, cancellationToken);
        var role = await EnsureRoleAsync(db, cancellationToken);
        await EnsureUserAsync(db, passwordHasher, employee, role, cancellationToken);
        await SeedData(db, cancellationToken);
        await EnsureMeaurmentsAsync(db, cancellationToken);
        await EnsureProductCategoriesAsync(db, cancellationToken);
        await EnsureWarehousesAsync(db, cancellationToken);

        var financeCategories = scope.ServiceProvider.GetRequiredService<IFinanceCategoryService>();
        await financeCategories.EnsureSystemCategoriesAsync(cancellationToken);
        await ChartOfAccountsSeeder.EnsureAsync(db, cancellationToken);
        await ProductionSchemaSeeder.EnsureAsync(db, cancellationToken);
        await InventorySchemaSeeder.EnsureAsync(db, cancellationToken);
        await FiscalYearSchemaSeeder.EnsureAsync(db, cancellationToken);
        await CashSchemaSeeder.EnsureAsync(db, cancellationToken);
        var cashBoxService = scope.ServiceProvider.GetRequiredService<ICashBoxService>();
        await CashSchemaSeeder.EnsureDefaultCashBoxAsync(db, cashBoxService, cancellationToken);
        await FixedAssetSchemaSeeder.EnsureAsync(db, cancellationToken);
        await EquitySchemaSeeder.EnsureAsync(db, cancellationToken);
        await EnsureGeneralSettingsAsync(db, cancellationToken);
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
        var VehiclesTypes = new[]
        {
            new VehicleType{Name="بونکر",Description="",CreatedBy = 1},
            new VehicleType{Name="کشنده",Description="",CreatedBy = 1},
            new VehicleType{Name="اسب",Description="",CreatedBy = 1}
        };
        var drivers = new[]
        {
            new Driver
            {
                Name = "علی احمد",
                FatherName = "غلام سخی",
                Address = "هرات",
                Title = PersonTitle.Mr,
                CreatedBy = 1
            },
            new Driver
            {
                Name = "علی اصغر",
                FatherName = "غلام سخی",
                Address = "هرات",
                Title = PersonTitle.Mr,
                CreatedBy = 1
            },
            new Driver
            {
                Name = "فاروق کریمی",
                FatherName = "محمد کریم",
                Address = "کابل",
                Title = PersonTitle.Mr,
                CreatedBy = 1
            },
            new Driver
            {
                Name = "سید حسین",
                FatherName = "سید رحمان",
                Address = "مزار شریف",
                Title = PersonTitle.Mr,
                CreatedBy = 1
            },
            new Driver
            {
                Name = "زهرا نوری",
                FatherName = "عبدالله نوری",
                Address = "قندهار",
                Title = PersonTitle.Mrs,
                CreatedBy = 1
            },
            new Driver
            {
                Name = "جعفر رضایی",
                FatherName = "کریم رضایی",
                Address = "بلخ",
                Title = PersonTitle.Mr,
                CreatedBy = 1
            }
};

        var vehicles = new[]
        {
            new Vehicle
            {
                Code = "HMV0001",
                PlateNumber = "AF-12345",
                VehicleTypeId = 1,
                Brand = "Volvo FH16",
                ModelYear = 2020,
                Color = "White",
                ChassisNumber = "CHS001",
                EngineNumber = "ENG001",
                FuelTankCapacity = 600,
                DefaultDriverId = 1,
                VehicleOwnerId = 1,
                CreatedBy = 1
            },
            new Vehicle
            {
                Code = "HMV0002",
                PlateNumber = "AF-54321",
                VehicleTypeId = 2,
                Brand = "Mercedes Actros",
                ModelYear = 2019,
                Color = "Blue",
                ChassisNumber = "CHS002",
                EngineNumber = "ENG002",
                FuelTankCapacity = 500,
                DefaultDriverId = 2,
                VehicleOwnerId = 2,
                CreatedBy = 1
            },
            new Vehicle
            {
                Code = "HMV0003",
                PlateNumber = "AF-67890",
                VehicleTypeId = 1,
                Brand = "Scania R500",
                ModelYear = 2021,
                Color = "Red",
                ChassisNumber = "CHS003",
                EngineNumber = "ENG003",
                FuelTankCapacity = 650,
                DefaultDriverId = 3,
                VehicleOwnerId = 3,
                CreatedBy = 1
            },
            new Vehicle
            {
                Code = "HMV0004",
                PlateNumber = "AF-98765",
                VehicleTypeId = 3,
                Brand = "MAN TGX",
                ModelYear = 2018,
                Color = "Black",
                ChassisNumber = "CHS004",
                EngineNumber = "ENG004",
                FuelTankCapacity = 550,
                DefaultDriverId = 4,
                VehicleOwnerId = 4,
                CreatedBy = 1
            },
            new Vehicle
            {
                Code = "HMV0005",
                PlateNumber = "AF-11223",
                VehicleTypeId = 2,
                Brand = "DAF XF",
                ModelYear = 2022,
                Color = "Silver",
                ChassisNumber = "CHS005",
                EngineNumber = "ENG005",
                FuelTankCapacity = 700,
                DefaultDriverId = 5,
                VehicleOwnerId = 3,
                CreatedBy = 1
            }
};
        var vehiclesOwner = new[]
        {
            new VehicleOwner{Name = "عبدالستار",Address = "هرات", Family= "اصغری" , FatherName = "عبدالعزیز",Mobile = "93123554580", NationalCode = "21254558", DefaultShare  = 20.5m , Title = PersonTitle.Mr, CreatedBy = 1},
            new VehicleOwner{Name = "عابدین",Address = "قندهار", Family= "اکبری" , FatherName = "عبدالعزیر",Mobile = "93123454580", NationalCode = "212545458", DefaultShare  = 25.5m , Title = PersonTitle.Mr, CreatedBy = 1},
            new VehicleOwner{Name = "گلبهار",Address = "هرات", Family= "شفایی" , FatherName = "سخی داد",Mobile = "93123554580", NationalCode = "21255458", DefaultShare  = 28.5m , Title = PersonTitle.Mr, CreatedBy = 1},
            new VehicleOwner{Name = "ننگیالی",Address = "هرات", Family= "احمدی" , FatherName = "اکبر",Mobile = "931234854580", NationalCode = "21253458", DefaultShare  = 21.5m , Title = PersonTitle.Mr, CreatedBy = 1},
        };
        var expenses = new[] {
        new ExpensesCategory{Name = "ویزا", CreatedBy = 1},
        new ExpensesCategory{Name = "خرج راه", CreatedBy = 1},
        new ExpensesCategory{Name = "شاگردانگی", CreatedBy = 1},
        new ExpensesCategory{Name = "رودپاس", CreatedBy = 1},
        };
        var routs = new[]
        {
            // کد مسیر با قالب TransportCodeHelper.ForRoute (HMR + شناسه ۴ رقمی) هماهنگ شد.
            new TransportRoute{Name = "کابل هرات", Code = "HMR0001" , DistanceKm = 250 , Destination = "کابل" , Description = "", EstimatedDays = 3 , Origin ="هرات", OriginCountry = "افغانستان", CreatedBy = 1},
            new TransportRoute{Name = "نمیروز هرات", Code = "HMR0002" , DistanceKm = 450 , Destination = "نیمروز" , Description = "", EstimatedDays = 3 , Origin ="هرات", OriginCountry = "افغانستان", CreatedBy = 1}
        };
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
        if (!await db.Vehicles.AnyAsync())
        {
            await db.AddRangeAsync(VehiclesTypes);
            await db.SaveChangesAsync();
            await db.AddRangeAsync(drivers);
            await db.AddRangeAsync(vehiclesOwner);
            await db.SaveChangesAsync();
            await db.AddRangeAsync(vehicles);
            await db.AddRangeAsync(routs);
            await db.AddRangeAsync(expenses);
            await db.AddRangeAsync(currencies);
            await db.AddRangeAsync(suppliers);
            await db.AddRangeAsync(customers);
            
            await db.SaveChangesAsync();

            // همگام‌سازی رابطه‌ی دوطرفه: علاوه بر Vehicle.DefaultDriverId، فیلد معکوس Driver.DefaultVehicleId
            // هم ست می‌شود (اولین وسیله‌ی ارجاع‌دهنده) تا داده‌ی seed سازگار باشد.
            foreach (var vehicle in vehicles)
            {
                if (vehicle.DefaultDriverId is int driverId)
                {
                    var driver = drivers.FirstOrDefault(d => d.DriverID == driverId);
                    if (driver is not null && driver.DefaultVehicleId is null)
                    {
                        driver.DefaultVehicleId = vehicle.VehicleID;
                    }
                }
            }

            await db.SaveChangesAsync();
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
        db.Meaurments.Add(kg);
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
}
