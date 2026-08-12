namespace HamgamTransport.Server.Services;

public static class InvoiceCodeHelper
{
    public const string PurchasePrefix = "HMPI";
    public const string PurchaseReturnPrefix = "HMPR";
    public const string SalePrefix = "HMPS";
    public const string SaleReturnPrefix = "HMSR";

    public static string ForPurchase(int id) => $"{PurchasePrefix}{id:D4}";

    public static string ForPurchaseReturn(int id) => $"{PurchaseReturnPrefix}{id:D4}";

    public static string ForSale(int id) => $"{SalePrefix}{id:D4}";

    public static string ForSaleReturn(int id) => $"{SaleReturnPrefix}{id:D4}";
}
