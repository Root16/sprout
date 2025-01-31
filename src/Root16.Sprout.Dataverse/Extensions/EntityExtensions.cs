using Microsoft.Xrm.Sdk;
using System.Text;

namespace Root16.Sprout.DataSources.Dataverse;

public static class EntityExtensions
{
    public static Entity CloneWithModifiedAttributes(this Entity updates, Entity original)
    {
        Entity delta = new(original.LogicalName, original.Id);
        foreach (var attribute in updates.Attributes)
        {
            bool different = false;
            original.Attributes.TryGetValue(attribute.Key, out object originalValue);
            var updateValue = attribute.Value;

            if (updateValue is EntityReference || originalValue is EntityReference)
            {
                var originalLookup = (EntityReference)originalValue;
                var updateLookup = (EntityReference)updateValue;

                if (updateLookup?.Id != originalLookup?.Id ||
                    updateLookup?.LogicalName != originalLookup?.LogicalName)
                {
                    different = true;
                }
            }
            else if (updateValue is Money || originalValue is Money)
            {
                var originalMoney = (Money)originalValue;
                var updateMoney = (Money)updateValue;

                if (updateMoney?.Value != originalMoney?.Value)
                {
                    different = true;
                }
            }
            else if (updateValue is OptionSetValue || originalValue is OptionSetValue)
            {
                var originalOptionSetValue = (OptionSetValue)originalValue;
                var updateOptionSetValue = (OptionSetValue)updateValue;

                if (updateOptionSetValue?.Value != originalOptionSetValue?.Value)
                {
                    different = true;
                }
            }
            else if (updateValue is OptionSetValueCollection || originalValue is OptionSetValueCollection)
            {
                var originalOptionSetValue = (OptionSetValueCollection)originalValue;
                var updateOptionSetValue = (OptionSetValueCollection)updateValue;
                var originalOptions = originalOptionSetValue?.Select(o => o.Value)?.ToArray() ?? [];
                var updateOptions = updateOptionSetValue?.Select(o => o.Value)?.ToArray() ?? [];

                if (originalOptions.Length != updateOptions.Length ||
                    originalOptions.Intersect(updateOptions).Count() != originalOptions.Length)
                {
                    different = true;
                }
            }
            else if (updateValue is string || originalValue is string)
            {
                if ((string)updateValue == "" && originalValue is null)
                {
                    different = false;
                }
                else if (!Equals(updateValue, originalValue))
                {
                    different = true;
                }
            }
            else
            {
                if (updateValue is DateTime dt)
                {
                    updateValue = (new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind)).ToUniversalTime();
                    if (originalValue is DateTime time)
                    {
                        originalValue = time.ToUniversalTime();
                    }
                }

                if (!Equals(updateValue, originalValue))
                {
                    different = true;
                }
            }


            if (different)
            {
                delta[attribute.Key] = updateValue;
            }
        }
        return delta;
    }

    public static string FormatChanges(this Entity entity, Entity previousValues)
    {
        StringBuilder sb = new();
        sb.AppendLine($"updating ({entity.LogicalName}, {entity.Id}):");
        foreach (var attribute in entity.Attributes)
        {
            previousValues.Attributes.TryGetValue(attribute.Key, out object matchValue);
            sb.AppendLine($"    {attribute.Key}: {DisplayAttributeValue(matchValue)} => {DisplayAttributeValue(attribute.Value)}");
        }

        return sb.ToString();
    }

    public static object DisplayAttributeValue(object attributeValue)
    {
        if (attributeValue is null)
        {
            return "(null)";
        }
        else if (attributeValue is EntityReference entityRef)
        {
            return $"({entityRef.LogicalName}, {entityRef.Id})";
        }
        else if (attributeValue is Money money)
        {
            return money.Value;
        }
        else if (attributeValue is OptionSetValue optionSetValue)
        {
            return optionSetValue.Value;
        }
        else if (attributeValue is string)
        {
            return $"'{attributeValue}'";
        }
        else
        {
            return attributeValue;
        }
    }

    public static string GetFormattedValue(this Entity entity, string attributeKey)
    {
        string result = string.Empty;
        if (entity.FormattedValues.ContainsKey(attributeKey))
            result = entity.FormattedValues[attributeKey];

        return result;
    }

    public static T GetAliasedAttributeValue<T>(this Entity entity, string attributeKey)
    {
        var aliasedValue = entity.GetAttributeValue<AliasedValue>(attributeKey);
        if (aliasedValue?.Value is null)
        {
            return default!;
        }

        return (T)aliasedValue.Value;
    }

    public static bool TryGetAliasedAttributeValue<T>(this Entity entity, string attributeKey, out T result)
    {
        try
        {
            AliasedValue aliasedValue = entity.GetAttributeValue<AliasedValue>(attributeKey);
            if (aliasedValue?.Value is null)
            {
                result = default!;
                return false;
            }

            if (aliasedValue.Value is T val)
            {
                result = val;
                return true;
            }

            System.ComponentModel.TypeConverter converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(T));
            result = (T)converter.ConvertFrom(aliasedValue.Value)!;
            return true;
        }
        catch { }

        result = default!;
        return false;
    }
}
