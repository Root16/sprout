using Microsoft.Xrm.Sdk;
using Root16.Sprout.Extensions;
using System.Data;

namespace Root16.Sprout.DataSources.Dataverse;

public static partial class DataRowExtensions
{
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
    public static Entity MapBooleans(this DataRow row, Entity entity, params string[] attributes)
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
        {
            var rowVal = row.GetValue(attribute) ?? 0m;
            if (rowVal is decimal d) entity[attribute.ToLower()] = new Money(d);
            else if (rowVal is int i) entity[attribute.ToLower()] = new Money(i);
        }
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

    /// <summary>
    /// Set Entity's optionset attribute values from the datarow's corresponding column. Datarow field and entity field types must match. Datarow field name or alias and entity field name must be the same. 
    /// </summary>
    /// <returns>Microsoft.Xrm.Sdk.Entity</returns>
    public static Entity MapOptionSets(this DataRow row, Entity entity, IOptionSetMapper optionSetMapper, params string[] attributes)
    {
        if (string.IsNullOrWhiteSpace(entity.LogicalName)) return entity;
        foreach (var attribute in attributes)
            entity[attribute.ToLower()] = optionSetMapper.MapOptionSetByLabel(entity.LogicalName, attribute, row.GetValue<string>(attribute)?.Trim()!);
        return entity;
    }
}