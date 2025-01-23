using Microsoft.Xrm.Sdk;
using System.Data;

namespace Root16.Sprout.DataSources.Sql;

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


    /// <summary>
    /// Set Entity's string attribute values from the datarow's corresponding column. Datarow field and entity field types must match. Datarow field name or alias and entity field name must be the same. 
    /// </summary>
    /// <returns>Microsoft.Xrm.Sdk.Entity</returns>
    public static Entity MapStrings(this DataRow row, Entity entity, params string[] attributes)
    {
        foreach (var attribute in attributes)
            entity[attribute.ToLower()] = row.GetValue<string>(attribute);
        return entity;
    }

    /// <summary>
    /// Set Entity's boolean attribute values from the datarow's corresponding column. Datarow field and entity field types must match. Datarow field name or alias and entity field name must be the same. 
    /// </summary>
    /// <returns>Microsoft.Xrm.Sdk.Entity</returns>
    public static Entity MapBooleans<T>(this DataRow row, Entity entity, params string[] attributes)
    {
        foreach (var attribute in attributes)
            entity[attribute.ToLower()] = row.GetValue<bool?>(attribute);
        return entity;
    }

    /// <summary>
    /// Set Entity's integer attribute values from the datarow's corresponding column. Datarow field and entity field types must match. Datarow field name or alias and entity field name must be the same. 
    /// </summary>
    /// <returns>Microsoft.Xrm.Sdk.Entity</returns>
    public static Entity MapIntegers(this DataRow row, Entity entity, params string[] attributes)
    {
        foreach (var attribute in attributes)
            entity[attribute.ToLower()] = row.GetValue<int>(attribute);
        return entity;

    }

    /// <summary>
    /// Set Entity's decimal attribute values from the datarow's corresponding column. Datarow field and entity field types must match. Datarow field name or alias and entity field name must be the same. 
    /// </summary>
    /// <returns>Microsoft.Xrm.Sdk.Entity</returns>
    public static Entity MapDecimals(this DataRow row, Entity entity, params string[] attributes)
    {
        foreach (var attribute in attributes)
            entity[attribute.ToLower()] = row.GetValue<decimal?>(attribute);

        return entity;

    }

    /// <summary>
    /// Set Entity's money attribute values from the datarow's corresponding column. Datarow field and entity field types must match. Datarow field name or alias and entity field name must be the same. 
    /// </summary>
    /// <returns>Microsoft.Xrm.Sdk.Entity</returns>
    public static Entity MapMonies(this DataRow row, Entity entity, params string[] attributes)
    {
        foreach (var attribute in attributes)
            entity[attribute.ToLower()] = new Money(row.GetValue<decimal>(attribute));
        return entity;
    }

    /// <summary>
    /// Set Entity's datetime attribute values from the datarow's corresponding column. Datarow field and entity field types must match. Datarow field name or alias and entity field name must be the same. 
    /// </summary>
    /// <returns>Microsoft.Xrm.Sdk.Entity</returns>
    public static Entity MapDateTimes(this DataRow row, Entity entity, params string[] attributes)
    {
        foreach (var attribute in attributes)
            entity[attribute.ToLower()] = row.GetValue<DateTime?>(attribute);
        return entity;
    }
}