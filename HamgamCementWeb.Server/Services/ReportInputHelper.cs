namespace HamgamCementWeb.Server.Services;

public static class ReportInputHelper
{
    public static string NormalizeToLatinDigits(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        ReadOnlySpan<char> persian = "۰۱۲۳۴۵۶۷۸۹";
        ReadOnlySpan<char> arabic = "٠١٢٣٤٥٦٧٨٩";
        Span<char> buffer = stackalloc char[input.Length];
        var length = 0;

        foreach (var character in input)
        {
            var persianIndex = persian.IndexOf(character);
            if (persianIndex >= 0)
            {
                buffer[length++] = (char)('0' + persianIndex);
                continue;
            }

            var arabicIndex = arabic.IndexOf(character);
            if (arabicIndex >= 0)
            {
                buffer[length++] = (char)('0' + arabicIndex);
                continue;
            }

            buffer[length++] = character;
        }

        return new string(buffer[..length]);
    }

    public static bool TryParseReportDate(string? value, out DateTime date)
    {
        date = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = NormalizeToLatinDigits(value.Trim());
        return DateTime.TryParse(normalized, out date);
    }
}
