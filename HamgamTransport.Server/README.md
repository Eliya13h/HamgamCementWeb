# HamgamTransport

پروژهٔ مستقل **همگام ترانسپورت** داخل همان Solution — جدا از همگام سیمان.

## ساختار

| پروژه | نقش |
|-------|-----|
| `HamgamTransport.Server` | بک‌اند ASP.NET Core + حسابداری دابل‌انتری |
| `hamgamtransport.client` | فرانت React (همان استایل و مدال‌های سیمان) |

## تفاوت با سیمان

- دیتابیس جدا: `HamgamTransport`
- کوکی لاگین جدا: `HamgamTransport.Auth`
- پورت dev: بک‌اند `7295` / فرانت `61830`
- منوی متمرکز بر حمل‌ونقل + حسابداری

## اجرای dev

در Visual Studio پروژهٔ **HamgamTransport.Server** را Startup Project بگذارید و با پروفایل **https** اجرا کنید (F5). SpaProxy خودکار `npm run dev` را روی پورت `61830` بالا می‌آورد و مرورگر را باز می‌کند.

```powershell
# از CLI
dotnet run --project HamgamTransport.Server --launch-profile https
```

اگر فرانت بالا نیامد، یک‌بار `npm install` داخل `hamgamtransport.client` بزنید.

## دیتابیس

اولین اجرا migration و seed را اعمال می‌کند. کاربر پیش‌فرض: `admin` / `Admin@123`

## مراحل بعدی

- پیاده‌سازی کشنده/بونکر/سرویس/مالک
- گزارش سود و زیان هر کشنده
- حذف ماژول‌های سیمانی از بک‌اند (در صورت نیاز)
