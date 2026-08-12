# گزارش‌های Stimulsoft تولید

این سند نحوهٔ کار دو گزارش چاپ تولید را توضیح می‌دهد تا در نگهداری بعدی گیج نشوید.

## دو گزارش جدا از «روزنامچه تولید»

| گزارش | قالب | مسیر Viewer | داده |
|--------|------|-------------|------|
| لیست بازه‌ای | `Reports/Production.mrt` | `/report-viewer/production?dateFrom&dateTo` | فقط بچ‌های **ثبت‌شده** در بازه |
| تفصیلی تک‌سند | `Reports/ProductionBatch.mrt` | `/report-viewer/production-batch?productionBatchId=` | یک بچ + مواد/هزینه/خروجی |
| روزنامچه تولید (قدیمی) | `Reports/Jurnal.mrt` | `/report-viewer/journal?type=production` | اسناد دفتر با `Source=Production` |

روزنامچه دست نخورده است؛ گزارش‌های جدید مخصوص صفحهٔ «گزارش تولیدات» هستند.

## جریان اجرا

```
ProductionReportPage
  → window.open(/report-viewer/production|production-batch)
  → ReportViewerController Session را ست می‌کند و View را برمی‌گرداند
  → StiNetCoreViewer AJAX → GetProductionReport / GetProductionBatchReport
  → ProductionReportService (Dapper + RegBusinessObject)
  → Render → Viewer / PDF
```

## سرویس

فایل: `HamgamTransport.Server/Services/ProductionReportService.cs`

- **Read با Dapper** از `ProductionBatches` و جداول مرتبط
- **Info شرکت** از `GeneralSettings` Id=1 (لوگو، نام فارسی/انگلیسی)
- **ارز پایه** از `Currencies.IsBaseCurrency` برای پسوند مبلغ
- قالب‌ها با **Clone کش‌شده** مثل فاکتور لود می‌شوند
- جابجایی لوگو: `CompanyLogo` ← ZmLogo و برعکس (قرارداد Jurnal/Invoice)
- `CalculationMode = Interpretation` + فونت Noto Nastaliq روی `Text1`

### Business Objects

**Production.mrt**

- `Info`: عنوان، بازه، جمع مواد/تبدیل/کل، تعداد سند
- `Batches`: ردیف‌های لیست

**ProductionBatch.mrt**

- `Info`: هدر شرکت + جمع‌ها
- `Batch`: هدر سند (لیست تک‌عضوی روی DataBand)
- `InputLines` / `CostLines` / `OutputLines`: سه بخش جدول

## فرانت

- صفحه: `hamgamtransport.client/src/Pages/Reporting/ProductionReportPage.jsx`
- URL helperها: `getProductionListReportUrl` / `getProductionBatchReportUrl` در `productionApi.js`
- فیلتر تاریخ پیش‌فرض: اول سال جلالی تا امروز
- چاپ لیست فقط از دکمهٔ بالای صفحه؛ چاپ تفصیلی از ردیف ثبت‌شده و مودال ردیابی

## ویرایش قالب

1. فایل `.mrt` را در Stimulsoft Designer باز کنید (یا XML را با دقت ویرایش کنید).
2. بعد از تغییر، `CopyToOutputDirectory` در csproj از قبل ثبت شده است.
3. کش قالب در سرویس با `LastWriteTimeUtc` باطل می‌شود — ری‌استارت سرور معمولاً لازم نیست.

اسکریپت اولیهٔ ساخت از روی Products/Jurnal: `scripts/patch-production-mrt.js` (فقط برای بازتولید پایه؛ بعد از ویرایش دستی دوباره اجرا نکنید مگر کپی تازه داشته باشید).
