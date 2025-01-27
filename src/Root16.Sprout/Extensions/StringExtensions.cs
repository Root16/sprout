using System.Text.RegularExpressions;

namespace Root16.Sprout.Extensions;

public static partial class StringExtensions
{
    public static string? ToMaxLength(this string? value, int maxLength)
    {
        return !string.IsNullOrEmpty(value)
            ? new string(value.Take(maxLength).ToArray())
            : null;
    }

    public static string? FormatForSharepoint(this string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        var replacedSPChars = "[\\~#%&*.{}/:<>?|\"]";
        var formattedName = Regex.Replace(name.Trim(), replacedSPChars, "-").Replace(@"\", "-").Trim();

        return formattedName;
    }
}
