# سیستم چندارزی HamgamCementWeb

این سند معماری سه جدول ارزی و نحوه استفاده از آن‌ها در معاملات (خرید، فروش، …) را توضیح می‌دهد.

---

## اصول کلی

| اصل | توضیح |
|-----|--------|
| **یک ارز پایه** | دقیقاً یک `Currency` با `IsBaseCurrency = true` (مثلاً ریال). |
| **نرخ فقط نسبت به پایه** | نرخ‌ها به‌صورت «ارز X نسبت به ارز پایه» ذخیره می‌شوند، نه ماتریس کامل N×N. |
| **تاریخچه زمانی** | هر تغییر نرخ در `CurrencyExchangeHistory` با بازه `[EffectiveFrom, EffectiveTo)` ثبت می‌شود. |
| **نرخ جاری** | `CurrencyExchangeRate` کش نرخ فعلی است؛ برای کوئری سریع «الان چقدر است؟». |
| **اسنپ‌شات معامله** | معاملات باید نرخ لحظه ثبت را نگه دارند تا گزارش‌های گذشته درست بمانند. |

---

## جداول

### 1. `Currency` — فهرست ارزها

| فیلد | نقش |
|------|-----|
| `CurrencyID` | کلید اصلی |
| `Name` | نام فارسی (ریال، دلار، …) |
| `Symbol` | نماد نمایشی (﷼، $) |
| `CurrencyCode` | ISO 4217 — یکتا (IRR، USD) |
| `IsBaseCurrency` | فقط یکی `true` |
| `DecimalPlaces` | رقم اعشار برای گرد کردن |

**نکته:** فیلد `ExchangeRate` از این جدول حذف شده؛ نرخ در جداول نرخ نگهداری می‌شود.

---

### 2. `CurrencyExchangeRate` — نرخ جاری

یک ردیف به ازای هر **ارز غیرپایه**.

| فیلد | نقش |
|------|-----|
| `CurrencyID` | ارز مبدأ (غیرپایه) |
| `BaseCurrencyID` | ارز پایه سیستم |
| `BaseUnitsPerUnit` | چند واحد از ارز پایه = ۱ واحد این ارز |
| `EffectiveFrom` | از چه زمانی این نرخ جاری است |
| `SourceHistoryID` | لینک به رکورد تاریخچه منبع |

**مثال:** پایه = ریال، دلار = ۵۰۰٬۰۰۰ ریال → `BaseUnitsPerUnit = 500000`

---

### 3. `CurrencyExchangeHistory` — تاریخچه تغییرات

هر بار نرخ عوض شود:

1. رکورد باز قبلی همان `CurrencyID` → `EffectiveTo = now`
2. رکورد جدید با `EffectiveFrom = now`، `EffectiveTo = null`
3. `CurrencyExchangeRate` همان ارز به‌روز شود

| فیلد | نقش |
|------|-----|
| `BaseUnitsPerUnit` | نرخ جدید |
| `PreviousBaseUnitsPerUnit` | نرخ قبل از تغییر |
| `EffectiveFrom` / `EffectiveTo` | بازه اعتبار |
| `ChangeReason` | دلیل تغییر (اختیاری) |

**یافتن نرخ در تاریخ مشخص `D`:**

```sql
WHERE CurrencyID = @currencyId
  AND EffectiveFrom <= @D
  AND (EffectiveTo IS NULL OR EffectiveTo > @D)
ORDER BY EffectiveFrom DESC
LIMIT 1
```

---

## تعریف رسمی نرخ

> `BaseUnitsPerUnit` = تعداد واحد **ارز پایه** که برابر **۱ واحد** ارز موردنظر است.

مثال با پایه = ریال:

| ارز | BaseUnitsPerUnit | معنی |
|-----|------------------|------|
| دلار | 500000 | ۱ دلار = ۵۰۰٬۰۰۰ ریال |
| یورو | 550000 | ۱ یورو = ۵۵۰٬۰۰۰ ریال |

---

## تبدیل مبلغ

### به ارز پایه

```
مبلغ_به_پایه = مبلغ_ارز × BaseUnitsPerUnit
```

**مثال:** ۱۰ دلار با نرخ ۵۰۰٬۰۰۰ → `10 × 500000 = 5,000,000` ریال

### از ارز پایه به ارز دیگر

```
مبلغ_ارز = مبلغ_پایه / BaseUnitsPerUnit
```

### بین دو ارز غیرپایه (در تاریخ D)

```
rateA = BaseUnitsPerUnit(A) در تاریخ D
rateB = BaseUnitsPerUnit(B) در تاریخ D

مبلغ_B = مبلغ_A × (rateA / rateB)
```

**مثال:** ۱۰۰ دلار → یورو؛ دلار=۵۰۰٬۰۰۰، یورو=۵۵۰٬۰۰۰:

```
100 × (500000 / 550000) ≈ 90.91 یورو
```

---

## اتصال به معاملات (خرید / فروش / …)

وقتی Entity معامله (فاکتور، سند، …) ساخته شود، **حداقل** این فیلدها را داشته باشد:

| فیلد پیشنهادی | توضیح |
|----------------|--------|
| `CurrencyID` | ارز مبلغ معامله |
| `Amount` | مبلغ به همان ارز |
| `BaseCurrencyID` | ارز پایه در زمان ثبت |
| `ExchangeHistoryID` | FK به `CurrencyExchangeHistory` — اسنپ‌شات نرخ |
| `BaseUnitsPerUnitAtTransaction` | کپی نرخ در لحظه ثبت (برای گزارش بدون JOIN) |
| `AmountInBaseCurrency` | مبلغ تبدیل‌شده به پایه |

**چرا اسنپ‌شات؟** اگر بعداً نرخ عوض شود، گزارش «ارزش معامله در زمان ثبت» همچنان درست است.

**ارزش نسبت به ارزهای دیگر در همان لحظه:** با خواندن `BaseUnitsPerUnit` از `CurrencyExchangeHistory` برای هر ارز دیگر در `TransactionDate` و فرمول تبدیل بالا محاسبه می‌شود.

---

## جریان به‌روزرسانی نرخ

```
[کاربر نرخ جدید دلار را ثبت می‌کند]
        │
        ▼
بستن رکورد باز History (EffectiveTo = now)
        │
        ▼
INSERT CurrencyExchangeHistory (نرخ جدید، Previous = قدیم)
        │
        ▼
UPSERT CurrencyExchangeRate (نرخ جاری + SourceHistoryID)
```

---

## قیود دیتابیس (Fluent API)

- `Currency.CurrencyCode` — یکتا
- `CurrencyExchangeRate.CurrencyID` — یکتا (یک نرخ جاری per ارز)
- ارز پایه نباید در `CurrencyExchangeRate` / `CurrencyExchangeHistory` به‌عنوان `CurrencyID` باشد
- حذف ارز: `Restrict` (اگر نرخ یا معامله دارد، حذف نشود)

---

## تغییرات نسبت به طراحی قبلی

| قبل | بعد | دلیل |
|-----|-----|------|
| `ExchangeRate` روی `Currency` | حذف | تک‌منبع حقیقت در History |
| `IsDefualtCurrency` | `IsBaseCurrency` | اصلاح املا و وضوح |
| `Symbole` | `Symbol` | اصلاح املا |
| `FromCurrency` / `ToCurrency` در Rate | `CurrencyID` + `BaseCurrencyID` | کاهش تکرار؛ تبدیل متقابل از فرمول |
| `CurrencyExchangeHistory` خالی | بازه زمانی + نرخ | پشتیبانی گزارش تاریخی |
| `CurrenciesExchangeRate` (DbSet) | `CurrencyExchangeRates` | نام‌گذاری استاندارد |

---

## چک‌لیست پیاده‌سازی بعدی

- [ ] API: CRUD ارزها + تعیین ارز پایه (تنها یکی)
- [ ] API: ثبت/ویرایش نرخ با بستن خودکار History
- [ ] سرویس `GetRateAt(currencyId, date)` و `Convert(amount, from, to, date)`
- [ ] صفحات `CurrenciesListPage` و `ExchangeHistoryPage`
- [ ] افزودن فیلدهای اسنپ‌شات به Entityهای معامله
- [ ] Migration EF Core

---

## اشتباهات رایج — اجتناب کن

1. ذخیره نرخ روی `Currency` — منبع حقیقت فقط History + Rate است.
2. فقط به‌روز کردن نرخ جاری بدون History — گزارش گذشته خراب می‌شود.
3. چند ارز پایه — همیشه یکی `IsBaseCurrency = true`.
4. حذف `ExchangeHistoryID` از معامله — بدون اسنپ‌شات، ارزش تاریخی قابل بازیابی نیست.
