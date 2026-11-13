namespace Root16.Sprout.Extensions;

public static class StringExtension
{
    public static string? RemoveNewLines(this string? value)
    {
        return value?.Replace("\r\n", string.Empty, StringComparison.Ordinal)?.Replace("\n", string.Empty, StringComparison.Ordinal);
    }
}
