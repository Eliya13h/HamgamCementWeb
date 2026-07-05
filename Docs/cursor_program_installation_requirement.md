# Program installation requirements
_Exported on 7/4/2026 at 11:13:42 GMT+4:30 from Cursor (3.7.19)_

---

**User**

خب الان بهم بگو برای اینکه این برنامه رو روی کامپیوتر دیگه ایی نصب کنم چه چیز هایی نیاز هست روی کامپیوتر مقصد نصب باشه

---

**Cursor**

برای پاسخ دقیق، ابتدا ساختار پروژه و وابستگی‌های runtime را بررسی می‌کنم.


برای نصب **HamgamCementWeb** روی کامپیوتر دیگر، بسته به اینکه فقط **اجرا** می‌کنید یا **توسعه** می‌دهید، نیازها فرق می‌کند.

---

## سناریو ۱: نصب برای استفاده (Production) — پیشنهادی

اگر برنامه را **روی یک ماشین build/publish** می‌کنید و خروجی را به کامپیوتر مقصد می‌برید:

### الزامی روی کامپیوتر مقصد

| نرم‌افزار | نسخه / توضیح |
|-----------|----------------|
| **Windows** | ویندوز ۱۰/۱۱ یا Windows Server (پروژه برای ویندوز طراحی شده) |
| **.NET 9 Runtime** | [ASP.NET Core Runtime 9.0](https://dotnet.microsoft.com/download/dotnet/9.0) — حداقل **ASP.NET Core Runtime** |
| **SQL Server** | SQL Server Express (رایگان) یا Standard/Enterprise — دیتابیس `HamgamNimroz` |
| **مرورگر وب** | Chrome، Edge یا Firefox |

### روی مقصد **نیاز نیست**

- Node.js / npm — فرانت React موقع `publish` داخل `wwwroot` کپی می‌شود
- .NET SDK — فقط Runtime کافی است
- Visual Studio

---

## سناریو ۲: نصب برای توسعه (Development)

اگر می‌خواهید سورس را روی ماشین دیگر clone کنید و توسعه دهید:

| نرم‌افزار | نسخه |
|-----------|------|
| **.NET 9 SDK** | برای build و migration |
| **Node.js LTS** | برای `npm install` و `npm run dev` |
| **SQL Server** | همانند بالا |
| **Visual Studio 2022** یا VS Code | اختیاری ولی مفید |

---

## جزئیات مهم

### ۱. SQL Server
برنامه از **Entity Framework + SQL Server** استفاده می‌کند. کانکشن پیش‌فرض در `appsettings.json`:

```9:11:d:\Mehraban Sources\HamgamCementWeb\HamgamCementWeb.Server\appsettings.json
  "ConnectionStrings": {
    "Local": "Server=.;Database=HamgamNimroz;Trusted_Connection=True;TrustServerCertificate=True; MultipleActiveResultSets=True;"
  }
```

روی مقصد باید:
- SQL Server نصب و **SQL Server Browser** (در صورت نیاز) فعال باشد
- دیتابیس `HamgamNimroz` ساخته شود
- **Migrationها** اجرا شوند — در کد فعلی migration خودکار در startup نیست؛ باید دستی انجام شود:

```powershell
dotnet ef database update --project HamgamCementWeb.Server
```

- `Trusted_Connection=True` یعنی **Windows Authentication** — حساب ویندوزی که برنامه با آن اجرا می‌شود باید به SQL Server دسترسی داشته باشد

### ۲. Publish و انتقال
روی ماشین build:

```powershell
cd "d:\Mehraban Sources\HamgamCementWeb\HamgamCementWeb.Server"
dotnet publish -c Release -o .\publish
```

خروجی پوشه `publish` را به مقصد کپی کنید. روی مقصد:

```powershell
dotnet HamgamCementWeb.Server.dll
```

یا با profile شبکه روی پورت **5085** (طبق `launchSettings.json`).

### ۳. تنظیمات روی مقصد
- `appsettings.json` (یا `appsettings.Production.json`) — connection string را برای SQL Server مقصد اصلاح کنید
- اگر از شبکه محلی استفاده می‌کنید: فایروال ویندوز را برای پورت **5085** (یا پورت IIS) باز کنید
- کاربر پیش‌فرض بعد از seed: `admin` / `admin` (در `DataSeeder.cs`)

### ۴. IIS (اختیاری)
می‌توانید به‌جای `dotnet run`:
- **IIS** + **ASP.NET Core Hosting Bundle 9.0** نصب کنید
- سایت را به پوشه publish اشاره دهید

### ۵. Stimulsoft Reports
گزارش‌ها با **Stimulsoft** کار می‌کنند؛ لایسنس در `Program.cs` تنظیم شده. فایل‌های گزارش (`Reports/*.mrt`) باید در publish باشند (در csproj تنظیم شده).

---

## خلاصه سریع

**حداقل برای یک کاربر نهایی روی PC دیگر:**

1. Windows  
2. ASP.NET Core Runtime 9.0  
3. SQL Server (Express کافی است)  
4. فایل‌های publish + migration دیتابیس  
5. مرورگر  

**Node.js و SDK فقط برای build/توسعه لازم است، نه برای اجرای نسخه publish‌شده.**

اگر بگویید مقصد **یک PC تکی** است یا **سرور شبکه**، می‌توانم مراحل دقیق‌تر (IIS، connection string، فایروال) را برای همان سناریو بنویسم.

---

## خطای 500.30 بعد از ریستارت ویندوز

### علت

بعد از **Restart** ویندوز، **IIS** معمولاً **زودتر از SQL Server** بالا می‌آید. برنامه در startup به دیتابیس وصل می‌شود (`DataSeeder`)؛ اگر SQL هنوز آماده نباشد، process کرش می‌کند و IIS خطای **HTTP 500.30** نشان می‌دهد.

وقتی **App Pool** یا **Site** را دستی Stop/Start می‌کنید، SQL Server دیگر آماده است و سایت درست کار می‌کند.

### راه‌حل سریع (روی ماشین مقصد)

PowerShell را **Run as Administrator** باز کنید:

```powershell
cd "مسیر\HamgamCementWeb\scripts"
.\fix-iis-reboot-startup.ps1
```

این اسکریپت:
1. SQL Server را روی **Automatic** می‌گذارد
2. یک **Scheduled Task** می‌سازد که ~۹۰ ثانیه بعد از boot، App Pool را restart می‌کند

اگر بعد از reboot خطای **connection refused** گرفتید (IIS اصلاً listen نمی‌کند):

```powershell
.\repair-iis-connection.ps1
```

بعد از reboot، حدود **۱–۲ دقیقه** صبر کنید و سایت را باز کنید.

### تشخیص دقیق‌تر

```powershell
.\diagnose-iis.ps1
```

لاگ startup در این مسیر است:

```
C:\inetpub\HamgamCementWeb\logs\stdout_*.log
```

اگر پیام شبیه `Cannot open database` یا `A network-related error` دیدید، همان مشکل ترتیب start است.

### نسخه جدید برنامه

در نسخه‌های جدید، startup چند بار (تا ~۱ دقیقه) برای اتصال SQL تلاش می‌کند. برای اعمال آن باید دوباره **publish** کنید و فایل‌ها را روی مقصد کپی کنید.

