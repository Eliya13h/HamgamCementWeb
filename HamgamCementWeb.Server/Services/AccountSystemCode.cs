namespace HamgamCementWeb.Server.Services;

// کدهای ثابت حساب‌های سیستمی — Posting فقط با این کدها حساب را پیدا می‌کند
public static class AccountSystemCode
{
    public const string Assets = "SYS_ASSETS";
    public const string CashAndBank = "SYS_CASH_BANK";
    public const string CashBoxes = "SYS_CASH_BOXES";
    public const string Banks = "SYS_BANKS";
    public const string Receivables = "SYS_RECEIVABLES";
    public const string CustomersAr = "SYS_AR";
    public const string Inventory = "SYS_INVENTORY";
    public const string InventoryRaw = "SYS_INVENTORY_RAW";
    public const string InventorySemi = "SYS_INVENTORY_SEMI";
    public const string InventoryFg = "SYS_INVENTORY_FG";
    public const string OtherCurrentAssets = "SYS_OTHER_CA";

    // دارایی‌های ثابت و استهلاک
    public const string FixedAssets = "SYS_FIXED_ASSETS";
    public const string FixedAssetMachinery = "SYS_FA_MACHINERY";
    public const string FixedAssetVehicles = "SYS_FA_VEHICLES";
    public const string FixedAssetFurniture = "SYS_FA_FURNITURE";
    public const string FixedAssetBuildings = "SYS_FA_BUILDINGS";
    public const string AccumulatedDepreciationKol = "SYS_ACCUM_DEPR_KOL";
    public const string AccumulatedDepreciation = "SYS_ACCUM_DEPR";
    public const string DepreciationExpense = "SYS_DEPR_EXP";
    public const string FixedAssetDisposalGain = "SYS_FA_GAIN";
    public const string FixedAssetDisposalLoss = "SYS_FA_LOSS";

    // سود/زیان تسعیر ارز در خرید و فروش ارز
    public const string FxGain = "SYS_FX_GAIN";
    public const string FxLoss = "SYS_FX_LOSS";

    public const string Liabilities = "SYS_LIABILITIES";
    public const string Payables = "SYS_PAYABLES";
    public const string SuppliersAp = "SYS_AP";
    public const string OtherLiabilities = "SYS_OTHER_LIAB";

    public const string Equity = "SYS_EQUITY";
    public const string Capital = "SYS_CAPITAL";
    // معین سرمایه — والد تفصیلی هر سهامدار
    public const string CapitalMoein = "SYS_CAPITAL_MOEIN";
    public const string RetainedEarnings = "SYS_RETAINED";
    // طرف مقابل مانده اولیهٔ سرمایه سهامدار
    public const string EquityOpening = "SYS_EQUITY_OPENING";
    // بدهی سود سهام پرداختنی (توزیع غیرنقدی)
    public const string DividendPayable = "SYS_DIVIDEND_PAYABLE";

    public const string Revenues = "SYS_REVENUES";
    public const string ProductSales = "SYS_SALES";
    public const string OtherRevenue = "SYS_OTHER_REV";

    public const string CogsGroup = "SYS_COGS_GROUP";
    public const string Cogs = "SYS_COGS";
    public const string InventoryAdjustment = "SYS_INV_ADJ";

    public const string Expenses = "SYS_EXPENSES";
    public const string OperatingExpense = "SYS_OPEX";
    public const string MiscExpense = "SYS_MISC_EXP";
    // هزینه حقوق و دستمزد کارکنان اداری/عملیاتی
    public const string SalaryExpense = "SYS_SALARY_EXP";
    // کسورات حقوق (طرف کریدیت در سند پرداخت)
    public const string SalaryDeductions = "SYS_SALARY_DEDUCTIONS";

    // مالیات پیش‌نویس
    public const string TaxPayable = "SYS_TAX_PAYABLE";
    public const string TaxReceivable = "SYS_TAX_RECEIVABLE";

    // ذخیره مطالبات مشکوک
    public const string DoubtfulDebtExpense = "SYS_DOUBTFUL_EXP";
    public const string DoubtfulDebtAllowance = "SYS_DOUBTFUL_ALLOW";

    // هزینه‌های ساخت در تولید — کریدیت در سند تولید (سرمایه‌گذاری در موجودی FG)
    public const string ProductionWage = "SYS_PROD_WAGE";
    public const string ProductionOverhead = "SYS_PROD_OVERHEAD";
    public const string ProductionAncillary = "SYS_PROD_ANCILLARY";
    public const string ProductionFixed = "SYS_PROD_FIXED";
}
