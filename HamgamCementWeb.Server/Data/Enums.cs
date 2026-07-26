using System.ComponentModel.DataAnnotations;

namespace HamgamCementWeb.Server.Data
{
    public enum PersonType
    {
        [Display(Name = "حقیقی")]
        NaturalPerson = 1,

        [Display(Name = "حقوقی")]
        LegalEntity = 2
    }
    public enum PersonTitle
    {
        [Display(Name = "آقا")]
        Mr,

        [Display(Name = "خانم")]
        Mrs,
    }

    // وضعیت سند انبارگردانی
    public enum StocktakingStatus
    {
        [Display(Name = "پیش‌نویس")]
        Draft = 1,

        [Display(Name = "تأیید شده")]
        Confirmed = 2,

        [Display(Name = "لغو شده")]
        Cancelled = 3
    }

    // نوع انبار
    public enum WarehouseType
    {
        [Display(Name = "مواد خام")]
        RawMaterials = 1,

        [Display(Name = "مواد نیمه‌خام")]
        SemiFinished = 2,

        [Display(Name = "مواد پردازش‌شده")]
        Processed = 3
    }

    // وضعیت سفر حمل و نقل
    public enum TripStatus
    {
        [Display(Name = "برنامه‌ریزی شده")]
        Planned = 1,

        [Display(Name = "در مسیر")]
        InTransit = 2,

        [Display(Name = "تکمیل شده")]
        Completed = 3,

        [Display(Name = "لغو شده")]
        Cancelled = 4
    }

    // نوع کرایه حمل روی فاکتور خرید/فروش
    public enum FreightMode
    {
        [Display(Name = "بدون حمل")]
        None = 0,

        [Display(Name = "ناوگان خودی")]
        OwnFleet = 1,

        [Display(Name = "کرایه‌ای")]
        Hired = 2,
    }

    // هدف سفر حمل — برای گزارش‌گیری
    public enum TripPurpose
    {
        [Display(Name = "باربری تجاری")]
        CommercialHaul = 0,

        [Display(Name = "ورود خرید")]
        PurchaseInbound = 1,

        [Display(Name = "تحویل فروش")]
        SaleDelivery = 2,
    }

    public enum InvoiceStatus
    {
        [Display(Name ="استعلام قیمت")]
        Quotation = 1,
        [Display(Name ="پیش فاکتور")]
        Proforma = 2,
        [Display(Name = "آردر")]
        Order = 3,
        [Display(Name = "فاکتور")]
        Invoice = 4,
    }

    // نوع سند فاکتور — فاکتور عادی یا برگشت
    public enum InvoiceDocumentType
    {
        [Display(Name = "فاکتور")]
        Invoice = 1,

        [Display(Name = "برگشت از خرید")]
        PurchaseReturn = 2,

        [Display(Name = "برگشت از فروش")]
        SaleReturn = 3,
    }

    // منبع ورود کالا در فاکتور خرید — بازار یا تولید داخلی
    public enum PurchaseEntrySource
    {
        [Display(Name = "خرید از بازار")]
        Market = 1,

        [Display(Name = "ورود از تولید")]
        Production = 2,
    }

    // وضعیت سند تولید
    public enum ProductionBatchStatus
    {
        [Display(Name = "پیش‌نویس")]
        Draft = 1,

        [Display(Name = "ثبت‌شده")]
        Posted = 2,
    }

    // نوع فرمول تولید — ثابت: خطوط قفل؛ متغیر: در سند قابل ویرایش
    public enum ProductionFormulaMode
    {
        [Display(Name = "ثابت")]
        Fixed = 1,

        [Display(Name = "متغیر")]
        Variable = 2,
    }

    // نوع هزینه در فرمول/سند تولید
    public enum ProductionCostType
    {
        [Display(Name = "دستمزد مستقیم")]
        DirectWage = 1,

        [Display(Name = "سربار تولید")]
        Overhead = 2,

        [Display(Name = "هزینه جانبی")]
        Ancillary = 3,

        [Display(Name = "هزینه ثابت")]
        Fixed = 4,
    }

    // نحوه محاسبه مبلغ هزینه نسبت به مقدار پایه فرمول
    public enum ProductionCostAmountMode
    {
        [Display(Name = "به ازای مقدار پایه")]
        PerBase = 1,

        [Display(Name = "مبلغ ثابت هر تولید")]
        Flat = 2,
    }

    // منبع ثبت مصرف یا عاید در حسابداری
    public enum FinancialEntrySource
    {
        [Display(Name = "خرید محصولات")]
        ProductPurchase = 1,

        [Display(Name = "فروش محصولات")]
        ProductSale = 2,

        [Display(Name = "متفرقه")]
        Miscellaneous = 3,

        [Display(Name = "برگشت از خرید")]
        PurchaseReturn = 4,

        [Display(Name = "برگشت از فروش")]
        SaleReturn = 5,

        // اضافه شد برای اتصال فاکتور مصارف حمل‌ونقل به حسابداری
        [Display(Name = "هزینه حمل‌ونقل")]
        TransportExpense = 6,

        // درآمد کرایه حمل روی فروش / باربری
        [Display(Name = "درآمد حمل‌ونقل")]
        TransportRevenue = 7,
    }

    // سطح کدینگ حساب: گروه / کل / معین / تفصیلی
    public enum AccountLevel
    {
        [Display(Name = "گروه")]
        Group = 1,

        [Display(Name = "کل")]
        Kol = 2,

        [Display(Name = "معین")]
        Moein = 3,

        [Display(Name = "تفصیلی")]
        Tafsili = 4,
    }

    public enum AccountType
    {
        [Display(Name = "دارایی")]
        Asset = 1,

        [Display(Name = "بدهی")]
        Liability = 2,

        [Display(Name = "حقوق مالکانه")]
        Equity = 3,

        [Display(Name = "درآمد")]
        Revenue = 4,

        [Display(Name = "بهای تمام‌شده")]
        Cogs = 5,

        [Display(Name = "هزینه")]
        Expense = 6,
    }

    // ماهیت مانده حساب
    public enum AccountNature
    {
        [Display(Name = "بدهکار")]
        Debit = 1,

        [Display(Name = "بستانکار")]
        Credit = 2,
    }

    // منبع سند دفترروزنامه
    public enum JournalSource
    {
        [Display(Name = "فاکتور خرید")]
        PurchaseInvoice = 1,

        [Display(Name = "فاکتور فروش")]
        SaleInvoice = 2,

        [Display(Name = "مصرف")]
        Expense = 3,

        [Display(Name = "عاید")]
        Revenue = 4,

        [Display(Name = "تولید")]
        Production = 5,

        [Display(Name = "انتقال صندوق")]
        CashTransfer = 6,

        [Display(Name = "دستی")]
        Manual = 7,

        // پرداخت حقوق کارمندان
        [Display(Name = "حقوق")]
        SalaryPayment = 8,

        // تأیید انبارگردانی (کسری/اضافی موجودی)
        [Display(Name = "انبارگردانی")]
        Stocktaking = 9,

        // انتقال کالا بین انبارها
        [Display(Name = "انتقال انبار")]
        WarehouseTransfer = 10,

        // سند اختتام سال مالی
        [Display(Name = "اختتام سال مالی")]
        YearEndClosing = 11,

        // معکوس اختتام هنگام بازگشایی سال
        [Display(Name = "معکوس اختتام سال")]
        YearEndReversal = 12,

        // خرید / تملک دارایی ثابت
        [Display(Name = "خرید دارایی ثابت")]
        FixedAssetAcquire = 13,

        // استهلاک دوره‌ای دارایی ثابت
        [Display(Name = "استهلاک دارایی ثابت")]
        FixedAssetDepreciation = 14,

        // فروش / اسقاط دارایی ثابت
        [Display(Name = "فروش/اسقاط دارایی ثابت")]
        FixedAssetDispose = 15,

        // آورده / افزایش سرمایه سهامدار
        [Display(Name = "آورده سرمایه")]
        EquityCapitalContribution = 16,

        // برداشت / کاهش سرمایه سهامدار
        [Display(Name = "برداشت سرمایه")]
        EquityCapitalWithdrawal = 17,

        // توزیع سود بین سهامداران
        [Display(Name = "توزیع سود")]
        EquityProfitDistribution = 18,

        // مانده اولیه سرمایه سهامدار
        [Display(Name = "مانده اولیه سرمایه")]
        EquityOpeningBalance = 19,

        // تخصیص سود/زیان پایان سال به سرمایه سهامداران
        [Display(Name = "تخصیص سرمایه پایان سال")]
        EquityYearAllocation = 20,

        // معکوس تخصیص سرمایه هنگام بازگشایی سال
        [Display(Name = "معکوس تخصیص سرمایه")]
        EquityYearAllocationReversal = 21,
    }

    // نوع سند عملیاتی حقوق صاحبان سهام
    public enum ShareholderEquityTxnType
    {
        [Display(Name = "آورده سرمایه")]
        CapitalContribution = 1,

        [Display(Name = "برداشت سرمایه")]
        CapitalWithdrawal = 2,

        [Display(Name = "توزیع سود")]
        ProfitDistribution = 3,

        [Display(Name = "مانده اولیه")]
        OpeningBalance = 4,
    }

    // نحوه تسویه توزیع سود
    public enum EquitySettlementMode
    {
        [Display(Name = "نقدی (صندوق)")]
        Cash = 1,

        [Display(Name = "بدهی (پرداختنی)")]
        Payable = 2,
    }

    // وضعیت کارت دارایی ثابت
    public enum FixedAssetStatus
    {
        [Display(Name = "فعال")]
        Active = 1,

        [Display(Name = "کاملاً مستهلک")]
        FullyDepreciated = 2,

        [Display(Name = "فروخته/اسقاط")]
        Disposed = 3,
    }

    // روش محاسبه استهلاک
    public enum DepreciationMethod
    {
        [Display(Name = "خط مستقیم")]
        StraightLine = 1,
    }

    // وضعیت سال مالی شمسی
    public enum FiscalYearStatus
    {
        [Display(Name = "باز")]
        Open = 1,

        [Display(Name = "بسته")]
        Closed = 2,
    }

    // وضعیت سند انتقال انبار
    public enum WarehouseTransferStatus
    {
        [Display(Name = "پیش‌نویس")]
        Draft = 1,

        [Display(Name = "ثبت‌شده")]
        Posted = 2,

        [Display(Name = "لغو شده")]
        Cancelled = 3,
    }

    public enum CashShiftStatus
    {
        [Display(Name = "باز")]
        Open = 1,

        [Display(Name = "بسته")]
        Closed = 2,
    }

}
