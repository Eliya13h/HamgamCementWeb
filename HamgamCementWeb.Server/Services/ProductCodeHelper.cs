namespace HamgamCementWeb.Server.Services;

public static class ProductCodeHelper
{
    public const string Prefix = "HMP";

    public static string ForProduct(int productId) => $"{Prefix}{productId:D4}";
}
