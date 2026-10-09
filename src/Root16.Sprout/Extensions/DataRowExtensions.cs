using System.Data;

namespace Root16.Sprout.Extensions;

public static partial class DataRowExtensions
{
    public static object GetValue(this DataRow row, string column)
    {
        return row.Table.Columns.Contains(column) ? row[column] : null!;
    }

    public static TOut GetValue<TOut>(this DataRow row, string column)
    {
        if (row.Table.Columns.Contains(column) && row[column] != null && row[column] is not DBNull)
        {
            return (TOut)row[column];
        }

        return default!;
    }
    
    public static bool TryGetValue<TOut>(this DataRow row, string column, out TOut value)
    {
        if (row.Table.Columns.Contains(column) && row[column] != null && row[column] is not DBNull)
        {
            value = (TOut)row[column];
            return true;
        }

        value = default(TOut)!;
        return false;
    }
}