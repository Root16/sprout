namespace Root16.Sprout.Extensions;

public static partial class DictionaryExtensions
{
    public static TOut? GetValue<TOut>(this IDictionary<string, TOut> dict, string key)
    {
        if (string.IsNullOrEmpty(key)) return default;
        return dict.TryGetValue(key, out var value) ? value : default;
    }
}
