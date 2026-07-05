# برنامه رفع مشکلات پروژه HamgamCementWeb

> این فایل نقشه‌ی راه رفع مشکلات شناسایی‌شده در بررسی جامع برنامه است.
> کار به‌صورت **مرحله‌به‌مرحله** انجام می‌شود؛ هر آیتم با تیک `[ ]` مشخص شده و پس از اتمام به `[x]` تغییر می‌کند.
> ترتیب فازها بر اساس **شدت و ریسک** چیده شده است.

## راهنمای شدت

| نماد | معنی |
|------|------|
| 🔴 | بحرانی — صحت داده یا امنیت |
| 🟠 | مهم — منطق کسب‌وکار ناقص |
| 🟡 | متوسط/جزئی — کیفیت و نگهداری |

## وضعیت کلی فازها

- [x] فاز ۱ — امنیت و دسترسی
- [x] فاز ۲ — صحت مالی و ارز
- [x] فاز ۳ — صحت موجودی و FIFO
- [x] فاز ۴ — یکپارچگی تراکنش (Transaction) و Post
- [~] فاز ۵ — تکمیل ماژول ترابری (۵.۱/۵.۲/۵.۴/۵.۷ انجام شد؛ ۵.۳/۵.۵/۵.۶ به‌عنوان ویژگی بعدی)
- [~] فاز ۶ — تکمیل ماژول تولید (۶.۱/۶.۲/۶.۴/۶.۵ انجام شد؛ ۶.۳ به‌عنوان ویژگی بعدی)
- [x] فاز ۷ — پاکسازی، تایپو و کیفیت کد (۷.۱/۷.۲/۷.۳/۷.۴ انجام شد)

---

## فاز ۱ — امنیت و دسترسی 🔴

هدف: بستن حفره‌های امنیتی و اعمال کنترل دسترسی در سمت بک‌اند (نه فقط فرانت).

### ۱.۱ 🔴 بستن ثبت‌نام عمومی (register)
- [x] مشکل: `POST /api/auth/register` با `[AllowAnonymous]` است؛ هر کسی می‌تواند کاربر بسازد.
- **فایل:** `HamgamCementWeb.Server/Controllers/User/AuthController.cs` (خطوط ۶۲–۱۵۰)
- **راه‌حل:** حذف `[AllowAnonymous]` و افزودن `[Authorize]` + بررسی permission مدیریت کاربر؛ یا حذف کامل endpoint و ساخت کاربر فقط از `UsersController`.
- **پذیرش:** کاربر ناشناس نتواند register کند؛ فقط مدیر مجاز باشد.

### ۱.۲ 🔴 اعمال Permission در بک‌اند
- [x] مشکل: اکثر کنترلرها فقط `[Authorize]` دارند؛ کاربر لاگین‌شده به همه‌ی APIهای مالی/ارز/محصول/ترابری دسترسی کامل دارد. کنترل فقط در فرانت است.
- **فایل‌ها:** همه‌ی کنترلرها، به‌ویژه `Controllers/Finance/*`, `Controllers/Invoice/*`, `Controllers/Transport/*`, `Controllers/Product/*`, `Controllers/Production/*`
- **راه‌حل:**
  1. ساخت یک `PermissionAttribute` یا `IAuthorizationHandler` که کلید permission را چک کند (مثلاً `finance.expenses.edit`).
  2. استفاده از `PermissionService` موجود برای بررسی دسترسی کاربر جاری.
  3. اعمال روی اکشن‌های حساس (create/update/delete/post).
- **پذیرش:** درخواست به اکشن بدون permission → `403`.

### ۱.۳ 🟡 هماهنگی پیام و قاعده رمز عبور
- [x] مشکل: در `RegisterRequest` پیام «۶ کاراکتر» ولی attribute `MinLength(4)` است.
- **فایل:** `AuthController.cs` (خط ۲۶۲)
- **راه‌حل:** یکسان‌سازی حداقل طول و پیام.

---

## فاز ۲ — صحت مالی و ارز 🔴

هدف: جلوگیری از اثرات جانبی ناخواسته روی نرخ ارز و اصلاح ثبت مبالغ.

### ۲.۱ 🔴 حذف تغییر نرخ سراسری از فاکتور خرید
- [x] مشکل: `RecordInvoiceExchangeHistoryAsync` هنگام ثبت فاکتور با نرخ دستی، `ApplyRateChangeAsync` را صدا می‌زند و **نرخ ارز کل سیستم** را عوض می‌کند.
- **فایل:** `HamgamCementWeb.Server/Controllers/Invoice/PurchaseInvoiceController.cs` (خطوط ۵۶۴–۵۹۰) و `Services/CurrencyExchangeRateService.cs`
- **راه‌حل:** فاکتور فقط باید **اسنپ‌شات نرخ محلی** خود را ذخیره کند (`BaseUnitsPerUnitAtTransaction`)، نه اینکه تاریخچه‌ی نرخ سیستم را تغییر دهد. تغییر نرخ فقط از صفحه‌ی مدیریت ارز مجاز باشد.
- **پذیرش:** ثبت فاکتور با نرخ دستی، `CurrencyExchangeRate`/`History` سیستم را تغییر ندهد.

### ۲.۲ 🟠 اصلاح علامت مبالغ در اسناد برگشت
- [x] مشکل (تصمیم: ثبت برگشت به‌صورت قلم کاهنده/منفی): `Expense` برگشت خرید و `Revenue` برگشت فروش با مبلغ **مثبت** ثبت می‌شوند؛ گزارش‌هایی که بر اساس جمع کل `Amount` هستند، ارقام را inflate می‌کنند.
- **فایل:** `Services/InvoicePostingService.cs` (خطوط ۵۱۳–۵۳۱ و ۶۷۴–۶۹۳)
- **راه‌حل:** یا مبلغ برگشت را **منفی** ذخیره کنیم، یا همه‌ی گزارش‌ها را ملزم به تفکیک بر اساس `FinancialEntrySource` کنیم. تصمیم باید یکپارچه در کل گزارش‌گیری اعمال شود.
- **پذیرش:** جمع مصارف/درآمد خالص با احتساب برگشت‌ها درست باشد.

### ۲.۳ 🟠 تعیین‌تکلیف `PaidAmount` در حسابداری
- [x] مشکل (تأیید مدل تعهدی + کامنت مستند): مبلغ `Expense`/`Revenue` همیشه کل فاکتور است، نه `PaidAmount`؛ مانده بدهی فقط در تراز طرف‌حساب دیده می‌شود.
- **فایل:** `Services/InvoicePostingService.cs` (خطوط ۱۷۲–۱۷۳ و ۳۶۵–۳۹۵)
- **راه‌حل:** روشن‌سازی مدل حسابداری: آیا مبنای تعهدی (کل مبلغ) درست است یا باید پرداخت/مانده جدا مدیریت شود. در صورت نیاز، مدل پرداخت (Payment) جدا اضافه شود.
- **پذیرش:** رفتار مصرف/درآمد با نقدی/نسیه شفاف و مستند باشد.

### ۲.۴ 🟡 fallback نرخ ارز شفاف شود
- [x] مشکل: اگر History برای تاریخ نباشد، بی‌صدا از نرخ امروز استفاده می‌شود.
- **فایل:** `Services/CurrencyConversionService.cs` (خطوط ۷۱–۸۷)
- **راه‌حل:** لاگ/هشدار هنگام fallback یا سیاست صریح (خطا در صورت نبود نرخ برای تاریخ گذشته).

### ۲.۵ 🟡 اصلاح تغییر ارز پایه
- [x] مشکل (بازمحاسبه نرخ سایر ارزها + گارد امن): هنگام `set-base`، نرخ‌های سایر ارزها نسبت به پایه‌ی جدید بازمحاسبه نمی‌شوند.
- **فایل:** `Controllers/Finance/CurrencyController.cs` (خطوط ۳۹۶–۴۳۹)
- **راه‌حل:** هنگام تغییر ارز پایه، نرخ همه‌ی ارزها بازمحاسبه و تاریخچه به‌روزرسانی شود.

---

## فاز ۳ — صحت موجودی و FIFO 🔴

هدف: هماهنگ نگه‌داشتن `InventoryStock` و `InventoryLot` و رفع دوباره‌شماری.

### ۳.۱ 🔴 هماهنگ‌سازی Lot در انبارگردانی
- [x] مشکل (متد `AdjustToCountAsync`؛ Lot تعدیلی با میانگین وزنی): تأیید Stocktaking فقط `InventoryStock.QuantityInBase` را تنظیم می‌کند و `InventoryLot.RemainingQuantityInBase` دست‌نخورده می‌ماند → FIFO از موجودی مجازی تخصیص می‌دهد.
- **فایل:** `Controllers/Inventory/StocktakingController.cs` (خطوط ۲۳۶–۲۶۵)
- **راه‌حل:** هنگام Confirm، اختلاف را روی Lotها هم اعمال کنیم (کاهش از قدیمی‌ترین/جدیدترین Lot طبق سیاست، یا ساخت Lot تعدیلی). Stock و مجموع Lotها باید همیشه برابر بمانند.
- **پذیرش:** پس از انبارگردانی، `Sum(Lot.Remaining) == InventoryStock.Quantity`.

### ۳.۲ 🔴 رفع دوباره‌شماری موجودی تولید ↔ فاکتور خرید
- [x] مشکل (مصرف Lotهای همان batch از انبار خروجی قبل از Receive در انبار فاکتور): Post تولید یک Lot می‌سازد؛ Post فاکتور خرید با `EntrySource=Production` دوباره `ReceiveAsync` می‌کند → موجودی دو برابر.
- **فایل:** `Services/ProductionPostingService.cs` (۱۵۴–۱۶۵) و `Services/InvoicePostingService.cs` (۲۰۵–۲۱۸)
- **راه‌حل:** یک مدل واحد انتخاب شود:
  - **گزینه الف:** تولید Lot نسازد و فقط فاکتور خرید Lot بسازد.
  - **گزینه ب (پیشنهادی):** تولید Lot بسازد و فاکتور خرید به‌جای Receive مجدد، همان Lotهای تولید را مرجع بگیرد/مصرف کند.
- **پذیرش:** موجودی محصول تولیدی پس از انتقال به فروش، فقط یک‌بار شمرده شود.

### ۳.۳ 🟡 `ReceiptSequence` به‌ازای محصول/انبار
- [x] مشکل: `ReceiptSequence` سراسری است، نه per-product/warehouse.
- **فایل:** `Services/FifoInventoryService.cs` (خطوط ۸۹–۹۲)
- **راه‌حل:** محاسبه‌ی sequence در محدوده‌ی `(ProductId, WarehouseId)`.

### ۳.۴ 🟡 اصلاح تولید کد انبارگردانی
- [x] مشکل: کد از `Count` تولید می‌شود؛ پس از حذف احتمال تکرار دارد.
- **فایل:** `Controllers/Inventory/StocktakingController.cs` (خطوط ۲۹۹–۳۰۳)
- **راه‌حل:** استفاده از `Max(sequence)+1` مشابه سایر helperها.

---

## فاز ۴ — یکپارچگی تراکنش (Transaction) 🔴

هدف: اتمیک کردن عملیات Post تا از ناسازگاری داده جلوگیری شود.

### ۴.۱ 🔴 پیچیدن Postها در Transaction
- [x] مشکل (helper `RunInTransactionAsync` با گارد ownsTransaction؛ تولید و SetBase و create+post برگشت هم پوشش داده شد): سرویس‌های FIFO خودشان `SaveChangesAsync` دارند؛ اگر Post وسط کار fail شود، بخشی از تغییرات commit شده باقی می‌ماند.
- **فایل‌ها:** `Services/ProductionPostingService.cs`, `Services/InvoicePostingService.cs`, `Services/InvoiceReturnService.cs`, `Services/FifoInventoryService.cs`
- **راه‌حل:**
  1. `SaveChangesAsync`های داخلی FIFO حذف/یکپارچه شوند (فقط یک Save در انتهای عملیات).
  2. کل عملیات Post در `IDbContextTransaction` پیچیده شود (BeginTransaction/Commit/Rollback).
- **پذیرش:** در صورت خطا در هر مرحله، هیچ تغییری در دیتابیس باقی نماند.

### ۴.۲ 🟠 اعتبارسنجی وضعیت در endpoint `/post`
- [x] مشکل (خرید: فقط فاکتور نهایی؛ فروش: بلاک استعلام/برگشت/ثبت‌شده): endpoint `/post` وضعیت فاکتور را چک نمی‌کند؛ Proforma/Order هم Post می‌شود.
- **فایل:** `Controllers/Invoice/PurchaseInvoiceController.cs`, `SaleInvoiceController.cs`
- **راه‌حل:** فقط فاکتور با وضعیت مجاز (`Inoivce`) قابل Post نهایی باشد یا قوانین صریح تعریف شود.

---

## فاز ۵ — تکمیل ماژول ترابری 🟠

هدف: تبدیل ترابری از CRUD خام به ماژول حسابداری واقعی.

### ۵.۱ 🔴 تبدیل ارز در فاکتور ترابری
- [x] مشکل (اسنپ‌شات ارز روی TransportExpense + TotalAmountInBaseCurrency + migration): `TransportInvoice.TotalAmount` جمع خام `Amount` است حتی با ارزهای مختلف.
- **فایل:** `Controllers/Transport/TransportInvoiceController.cs` (خط ۱۶۳)، `Data/Models/Transport/TransportExpense.cs`
- **راه‌حل:** استفاده از `CurrencyConversionService` و افزودن فیلدهای اسنپ‌شات ارز به `TransportExpense`/`TransportInvoice`؛ جمع بر اساس ارز پایه.
- **پذیرش:** جمع فاکتور با ردیف‌های چندارزی درست باشد.

### ۵.۲ 🟠 اتصال ترابری به حسابداری
- [x] مشکل (افزودن `FinancialEntrySource.TransportExpense` + دسته `TRANSPORT_EXPENSE` + ساخت/به‌روزرسانی/حذف `Expense` لینک‌شده به فاکتور ترابری): هیچ اتصالی به `Expense`/`Revenue` وجود ندارد؛ `FinancialEntrySource` منبع Transport ندارد.
- **فایل:** `Data/Enums.cs` (`FinancialEntrySource`)، `Services/InvoicePostingService.cs` یا سرویس جدید ترابری
- **راه‌حل:** افزودن مقادیر منبع ترابری و ثبت مصرف/درآمد از فاکتور ترابری و `TripRevenue`.

### ۵.۳ 🟠 استفاده از `DefaultShare` (سهم راننده/مالک)
- [ ] مشکل: `DefaultShare` فقط ذخیره می‌شود و در تسهیم درآمد/هزینه استفاده نمی‌شود.
- **فایل:** `Data/Models/People/Driver.cs`, `VehicleOwner.cs`، سرویس محاسبه سود سفر
- **راه‌حل:** محاسبه سهم راننده/مالک از `TripRevenue` و نمایش در گزارش سود سفر.

### ۵.۴ 🟠 اعتبارسنجی FK و سازگاری در ترابری
- [x] مشکل (اعتبارسنجی در TransportTripController و TransportInvoiceController + ترتیب تاریخ/کیلومترشمار + سازگاری trip↔vehicle): وجود `VehicleId`/`RouteId`/`DriverId`/`TripId`/`CategoryId` و سازگاری trip↔vehicle چک نمی‌شود.
- **فایل:** `Controllers/Transport/TransportTripController.cs`, `TransportInvoiceController.cs`
- **راه‌حل:** اعتبارسنجی وجود موجودیت‌ها و سازگاری سفر با وسیله؛ اعتبارسنجی تاریخ‌ها و کیلومتر.

### ۵.۵ 🟠 اتصال نگهداری/قطعات
- [ ] مشکل: `VehicleMaintenance`/`VehiclePartReplacement` جدا از فاکتور/trip و بدون انعکاس مالی.
- **راه‌حل:** افزودن ارز و انعکاس هزینه در حسابداری/گزارش وسیله.

### ۵.۶ 🟠 گزارش ترابری
- [ ] مشکل: `TransportReportPage.jsx` فقط placeholder است.
- **راه‌حل:** پیاده‌سازی گزارش سود سفر (Revenue − Expenses − Maintenance) و گزارش هزینه‌ی هر وسیله.

### ۵.۷ 🟡 باگ‌های کوچک ترابری
- [x] کلید ستون تکراری `[2]` در مرتب‌سازی مسیر — `TransportRouteController.cs` (خط ۱۸–۲۰).
- [x] ناسازگاری پیشوند seed مسیر (`HMTR` در seed vs `HMR` در helper) — `DataSeeder.cs` (۲۸۲) و `TransportCodeHelper.cs`.
- [x] در seed، `Driver.DefaultVehicleId` ست نمی‌شود (فقط `Vehicle.DefaultDriverId`).

> **موارد ۵.۳ (سهم راننده/مالک)، ۵.۵ (اتصال نگهداری/قطعات به مالی) و ۵.۶ (گزارش ترابری) پیاده نشدند:** این‌ها افزودن ویژگی‌های نسبتاً بزرگ (شامل تغییرات فرانت و تصمیم‌های محصولی) هستند نه رفع باگ صحت. برای حفظ کیفیت و پرهیز از ویژگی نیمه‌کاره، به‌عنوان کار بعدی باقی ماندند. زیرساخت مالی (Expense با Source=TransportExpense و مبالغ ارز پایه) اکنون آماده است تا گزارش ۵.۶ و تسهیم ۵.۳ روی آن ساخته شوند.

---

## فاز ۶ — تکمیل ماژول تولید 🟠

### ۶.۱ 🟠 افزودن Unpost/برگشت تولید
- [x] مشکل (endpoint `/unpost` + `UnpostBatchAsync` با تراکنش؛ حذف Lot تولیدی و بازگرداندن دقیق مواد به Lot اصلی): هیچ راهی برای برگشت Post تولید نیست؛ Lotهای مصرف/تولید بازگردانده نمی‌شوند.
- **فایل:** `Services/ProductionPostingService.cs`, `Controllers/Production/ProductionBatchController.cs`
- **راه‌حل:** endpoint unpost که Lot تولیدی را حذف و مواد مصرفی را به Lot اصلی بازگرداند (مشابه منطق برگشت فروش).

### ۶.۲ 🟠 اعتبارسنجی خطوط تولید
- [x] مشکل (بررسی وجود/فعال بودن محصول و مجاز بودن واحد از ProductMeaurment؛ موجودی هنگام Post توسط FIFO اعتبارسنجی می‌شود): وجود/فعال بودن Product، سازگاری Meaurment با Product، و موجودی کافی قبل از Post چک نمی‌شود.
- **فایل:** `Controllers/Production/ProductionBatchController.cs`
- **راه‌حل:** افزودن اعتبارسنجی‌ها و پیش‌بررسی موجودی قبل از Post.

### ۶.۳ 🟡 اتصال `ProductionPlan` به اجرا
- [ ] مشکل: `ProductionPlan` کاملاً جدا از batch است؛ بدون مقایسه planned/actual.
- **راه‌حل:** افزودن ارتباط اختیاری plan↔batch و گزارش انحراف تولید.

### ۶.۴ 🟡 FK برای `ProductionOutputLine.InventoryLotId`
- [x] مشکل (رابطه رسمی + migration): ستون بدون relation در EF تعریف شده.
- **فایل:** `Data/AppDbContext.cs`، `Data/Models/Production/ProductionOutputLine.cs`
- **راه‌حل:** تعریف رابطه‌ی رسمی + migration.

### ۶.۵ 🟡 تکمیل Trace مصرف
- [x] مشکل (موجودیت `ProductionInputLotAllocation` + ذخیره تخصیص در Post + نمایش `ConsumedLots` در Trace): `GetTraceAsync` Lotهای مصرف‌شده (input) را نشان نمی‌دهد.

> **آیتم ۶.۳ (اتصال ProductionPlan به اجرا) پیاده نشد:** ویژگی گزارشی/محصولی است (مقایسه planned/actual) نه رفع باگ صحت؛ به‌عنوان کار بعدی باقی ماند.

---

## فاز ۷ — پاکسازی، تایپو و کیفیت کد 🟡

### ۷.۱ 🟡 اصلاح تایپو `Inoivce`
- [x] مشکل (rename به `Invoice` در enum + همه ۱۱ فایل سرور؛ enum به‌صورت int ذخیره می‌شود پس بدون migration و بدون تأثیر روی فرانت که از کد عددی استفاده می‌کند): `InvoiceStatus.Inoivce` در enum و کل پروژه نفوذ کرده.

### ۷.۲ 🟡 اصلاح ISO ارز افغانی
- [x] مشکل (`"AFs"` → `"AFN"` در seed): در seed `"AFs"` به‌جای `"AFN"`.

### ۷.۳ 🟡 همپوشانی `Status` و `IsPosted` در تولید
- [x] بررسی شد — تصمیم: `Status` منبع حقیقت (برای برچسب/توسعه‌پذیری) و `IsPosted` پرچم ماندگار برای کوئری‌ها؛ هر دو همیشه در `PostBatch`/`UnpostBatch` هم‌گام می‌مانند. حذف `IsPosted` نیازمند migration و تغییر ~۱۰ ارجاع بود و همپوشانی بی‌ضرر است، پس عمداً نگه داشته شد.

### ۷.۴ 🟡 موارد جزئی زیرساخت
- [x] ترتیب `UseSession` اصلاح شد (اکنون پیش از Authentication/Authorization) — `Program.cs`.
- [x] Stimulsoft license به تنظیمات منتقل شد (`Stimulsoft:LicenseKey` با fallback به مقدار پیش‌فرض) — `Program.cs`.
- [x] کد comment‌شده `CreatedBy` در register پاک شد و `CreatedBy` از کاربر جاری مقداردهی شد — `AuthController.cs`.
- [ ] به‌روزرسانی/حذف checklist قدیمی در `MULTI-CURRENCY-SYSTEM.md`. (اختیاری — مستندات)

---

## پیوست: نقشه فایل‌های کلیدی

| بخش | مسیر |
|-----|------|
| DbContext | `HamgamCementWeb.Server/Data/AppDbContext.cs` |
| Enums | `HamgamCementWeb.Server/Data/Enums.cs` |
| FIFO | `HamgamCementWeb.Server/Services/FifoInventoryService.cs` |
| Posting فاکتور | `HamgamCementWeb.Server/Services/InvoicePostingService.cs` |
| برگشت فاکتور | `HamgamCementWeb.Server/Services/InvoiceReturnService.cs` |
| Posting تولید | `HamgamCementWeb.Server/Services/ProductionPostingService.cs` |
| ارز | `HamgamCementWeb.Server/Services/CurrencyConversionService.cs`, `CurrencyExchangeRateService.cs` |
| دسترسی | `HamgamCementWeb.Server/Services/PermissionService.cs` |
| احراز هویت | `HamgamCementWeb.Server/Controllers/User/AuthController.cs` |
| Seed | `HamgamCementWeb.Server/Data/Seed/DataSeeder.cs` |
| راه‌اندازی | `HamgamCementWeb.Server/Program.cs` |
| فرانت | `hamgamcementweb.client/src/` |

---

## روش کار

1. هر بار یک آیتم (یا یک زیرفاز) را انتخاب و به من اعلام کنید.
2. من قبل از تغییر، فایل‌های مربوطه را دقیق می‌خوانم و راه‌حل را اجرا می‌کنم.
3. پس از هر تغییر Entity، migration ساخته می‌شود (طبق قاعده پروژه).
4. آیتم انجام‌شده در این فایل به `[x]` تغییر داده می‌شود.
