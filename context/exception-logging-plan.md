# پلان لاگینگ Exception و عملیات — سرور لوکال داخل شبکه

**وضعیت:** منتظر اتمام دولوپ — وقتی کاربر گفت «پیاده‌سازی کن»، این پلان را اجرا کن.  
**تاریخ ثبت:** 2026-07-22

## وضعیت فعلی (قبل از پیاده‌سازی)

- Serilog / NLog / Sentry / App Insights وجود ندارد.
- جدول AuditLog / AppErrorLog وجود ندارد.
- Middleware سراسری Exception وجود ندارد.
- فقط `ILogger` پیش‌فرض ASP.NET در چند نقطه پراکنده (`Program.cs` seed retry، `CurrencyConversionService`).
- IIS stdout در `web.config` فعال است (`stdoutLogEnabled` → `.\logs\stdout`).
- `BaseEntity` فقط `CreatedBy` / `UpdatedBy` دارد — لاگ کامل عملیات نیست.

## هدف استقرار

سرور on-prem داخل LAN — بدون وابستگی به کلود/اینترنت.

## محدوده پیاده‌سازی (وقتی درخواست شد)

### فاز ۱ — Exception logging (اولویت)

1. **Serilog** با `Serilog.AspNetCore` + `Serilog.Sinks.File`
   - مسیر: مثلاً `logs/hamgam-.log` با rolling روزانه
   - Retention حدود ۳۰ روز
   - Production: `Warning`/`Error`؛ Development: می‌تواند `Information` باشد
2. **Exception middleware** (`UseExceptionHandler` یا middleware اختصاصی)
   - Exceptionهای unhandled را `LogError` کند
   - به کلاینت فقط پیام عمومی برگردد (بدون stack trace)
3. **اختیاری ولی توصیه‌شده:** Sink به SQL Server
   - جدول ساده `AppErrorLog` برای جستجوی سریع روی همان SQL موجود
4. پیکربندی در `appsettings.json` / `appsettings.Production.json` و اتصال در `Program.cs`
5. اطمینان از وجود پوشه `logs` در deploy و (در صورت نیاز) همسویی با stdout IIS

### فاز ۲ — Audit عملیات کسب‌وکاری (جدا از Exception؛ فقط اگر خواسته شد)

- جدول `AuditLog`: `UserId`, `Action`, `Entity`, `EntityId`, `At`, جزئیات اختیاری
- با Serilog یکی نیست؛ برای «چه کسی چه کاری کرد»

## آنچه عمداً انجام نشود مگر درخواست صریح

- Sentry / Application Insights / Seq / ELK (برای LAN فعلی لازم نیست)
- لاگ کردن همه Request/Response به‌صورت verbose در Production
- افشای stack trace به کلاینت

## نقاط ورود کد

| بخش | مسیر |
|-----|------|
| بک‌اند | `HamgamCementWeb.Server/Program.cs` |
| تنظیمات | `HamgamCementWeb.Server/appsettings*.json` |
| پکیج‌ها | `HamgamCementWeb.Server.csproj` |
| IIS | `HamgamCementWeb.Server/web.config` |
| Entity خطا (اختیاری) | `HamgamCementWeb.Server/Data/Models/` + migration |
