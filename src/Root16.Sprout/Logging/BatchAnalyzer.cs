namespace Root16.Sprout.Logging;


public abstract class BatchAnalyzer<T> where T : class
{
    public abstract string FormatValue(object value);
    public abstract Audit GetDifference(string key, T data, T? previousData = null);
}