namespace HamgamTransport.Server.Services;

/// <summary>
/// تولید کدهای خودکار تولید: پیشوند + شناسه با حداقل ۴ رقم (مثلاً HMP0001)
/// </summary>
public static class ProductionCodeHelper
{
    public const string BatchPrefix = "HMP";

    public static string ForBatch(int batchId) => $"{BatchPrefix}{batchId:D4}";
}
