namespace HamgamTransport.Server.Services;

public static class TransportCodeHelper
{
    public static string Vehicle(int id) => $"VH-{id:D5}";
    public static string Pair(int id) => $"VP-{id:D5}";
}
