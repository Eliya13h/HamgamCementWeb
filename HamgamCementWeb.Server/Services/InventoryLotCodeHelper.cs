namespace HamgamCementWeb.Server.Services;

public static class InventoryLotCodeHelper
{
    public const string Prefix = "HML";

    public static string ForLot(int lotId) => $"{Prefix}{lotId:D4}";
}
