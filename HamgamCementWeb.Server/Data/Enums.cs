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
    }

}
