namespace HamgamTransport.Server.Services;

public static class PermissionService
{
    public static bool HasPermission(bool hasFullAccess, IReadOnlyCollection<string> permissionKeys, string key)
    {
        if (hasFullAccess)
        {
            return true;
        }

        return permissionKeys.Contains(key);
    }

    public static bool IsValidPermissionKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 120)
        {
            return false;
        }

        var parts = key.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        return parts.All(part => part.Length > 0 && part.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'));
    }
}
