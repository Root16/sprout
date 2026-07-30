using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using System.Text;

namespace Root16.Sprout.DataSources.Dataverse;

public static class EntityExtensions
{
    public static Entity CloneWithModifiedAttributes(this Entity updates, Entity original, ILogger logger)
    {
        logger.LogDebug("Cloning entity {LogicalName} ({Id}) with modified attributes.", original?.LogicalName, original?.Id);
        Entity delta = new(original.LogicalName, original.Id);
        foreach (var attribute in updates.Attributes)
        {
            logger.LogDebug("Checking attribute {AttributeKey} for changes.", attribute.Key);
            bool different = false;
            original.Attributes.TryGetValue(attribute.Key, out object originalValue);
            var updateValue = attribute.Value;

            if (updateValue is EntityReference || originalValue is EntityReference)
            {
                logger.LogDebug("Comparing EntityReference attribute {AttributeKey}", attribute.Key);
                var originalLookup = (EntityReference)originalValue;
                var updateLookup = (EntityReference)updateValue;

                if (updateLookup?.Id != originalLookup?.Id ||
                    updateLookup?.LogicalName != originalLookup?.LogicalName)
                {
                    logger.LogDebug("Attribute {AttributeKey} has different EntityReference values.", attribute.Key);
                    different = true;
                }
            }
            else if (updateValue is EntityReferenceCollection || originalValue is EntityReferenceCollection)
            {
                logger.LogDebug("Comparing EntityReferenceCollection attribute {AttributeKey}", attribute.Key);
                var originalCollection = (EntityReferenceCollection)originalValue;
                var updateCollection = (EntityReferenceCollection)updateValue;

                List<IGrouping<string?, EntityReference>> groupedOriginalCollection = [.. originalCollection.GroupBy(o => o?.LogicalName).OrderBy(g => g.Key)];
                List<IGrouping<string?, EntityReference>> groupedUpdateCollection = [.. updateCollection.GroupBy(o => o?.LogicalName).OrderBy(g => g.Key)];

                // Check If Same Amount Of Groups
                if (groupedOriginalCollection.Count != groupedUpdateCollection.Count)
                {
                    logger.LogDebug("Attribute {AttributeKey} has different number of groups. Original: {OriginalCount}, Update: {UpdateCount}", attribute.Key, groupedOriginalCollection.Count, groupedUpdateCollection.Count);
                    different = true;
                }
                else
                {
                    var originalTypes = groupedOriginalCollection.Select(g => g.Key).Distinct();
                    var updateTypes = groupedUpdateCollection.Select(g => g.Key).Distinct();

                    // Check if the distinct record types are the same
                    if (!originalTypes.SequenceEqual(updateTypes))
                    {
                        logger.LogDebug("Attribute {AttributeKey} has different record types. Original: {OriginalTypes}, Update: {UpdateTypes}", attribute.Key, string.Join(", ", originalTypes.Select(g => g?.ToString() ?? "null")), string.Join(", ", updateTypes.Select(g => g?.ToString() ?? "null")));
                        different = true;
                    }
                    else
                    {
                        // Loop through each group and check if they have the same amount of records
                        foreach (var originalGroup in groupedOriginalCollection)
                        {
                            var updateGroup = groupedUpdateCollection.FirstOrDefault(g => g.Key == originalGroup.Key);
                            if (updateGroup is null || originalGroup.Count() != updateGroup.Count())
                            {
                                logger.LogDebug("Attribute {AttributeKey} has different number of records for type {RecordType}. Original: {OriginalCount}, Update: {UpdateCount}", attribute.Key, originalGroup.Key, originalGroup.Count(), updateGroup?.Count() ?? 0);
                                different = true;
                                break;
                            }

                            var originalIds = new HashSet<Guid?>(originalGroup.Select(o => o?.Id));
                            var updateIds = new HashSet<Guid?>(updateGroup.Select(u => u?.Id));

                            if(updateIds.Contains(null))
                            {
                                logger.LogDebug("Attribute {AttributeKey} has null record IDs for type {RecordType}. Original: {OriginalIds}, Update: {UpdateIds}", attribute.Key, originalGroup.Key, string.Join(", ", originalIds.Select(g => g?.ToString() ?? "null")), string.Join(", ", updateIds.Select(g => g?.ToString() ?? "null")));
                                different = true;
                                break;
                            }
                            if (!originalIds.SetEquals(updateIds))
                            {
                                logger.LogDebug("Attribute {AttributeKey} has different record IDs for type {RecordType}. Original: {OriginalIds}, Update: {UpdateIds}", attribute.Key, originalGroup.Key, string.Join(", ", originalIds.Select(g => g?.ToString() ?? "null")), string.Join(", ", updateIds.Select(g => g?.ToString() ?? "null")));
                                different = true;
                                break;
                            }
                        }
                    }
                }
            }
            else if (updateValue is EntityCollection || originalValue is EntityCollection)
            {
                logger.LogDebug("Comparing EntityCollection attribute {AttributeKey}", attribute.Key);
                var originalCollection = (EntityCollection)originalValue;
                var updateCollection = (EntityCollection)updateValue;

                var originalPartyIds = new HashSet<Guid?>(originalCollection.Entities.Select(e => e.GetAttributeValue<EntityReference>("partyid")?.Id));
                var updatePartyIds = new HashSet<Guid?>(updateCollection.Entities.Select(e => e.GetAttributeValue<EntityReference>("partyid")?.Id));

                if(updatePartyIds.Contains(null) || originalPartyIds.Contains(null))
                {
                    logger.LogDebug("Attribute {AttributeKey} has null party IDs. Original: {OriginalIds}, Update: {UpdateIds}", attribute.Key, string.Join(", ", originalPartyIds), string.Join(", ", updatePartyIds.Select(g => g?.ToString() ?? "null")));
                    different = true;
                }
                else if (!originalPartyIds.SetEquals(updatePartyIds))
                {
                    logger.LogDebug("Attribute {AttributeKey} has different party IDs. Original: {OriginalIds}, Update: {UpdateIds}", attribute.Key, string.Join(", ", originalPartyIds), string.Join(", ", updatePartyIds.Select(g => g?.ToString() ?? "null")));
                    different = true;
                }

            }
            else if (updateValue is Money || originalValue is Money)
            {
                logger.LogDebug("Comparing Money attribute {AttributeKey}", attribute.Key);
                var originalMoney = (Money)originalValue;
                var updateMoney = (Money)updateValue;

                if (updateMoney?.Value != originalMoney?.Value)
                {
                    logger.LogDebug("Attribute {AttributeKey} has different Money values. Original: {OriginalValue}, Update: {UpdateValue}", attribute.Key, originalMoney?.Value, updateMoney?.Value);
                    different = true;
                }
            }
            else if (updateValue is OptionSetValue || originalValue is OptionSetValue)
            {
                logger.LogDebug("Comparing OptionSetValue attribute {AttributeKey}", attribute.Key);
                var originalOptionSetValue = (OptionSetValue)originalValue;
                var updateOptionSetValue = (OptionSetValue)updateValue;

                if (updateOptionSetValue?.Value != originalOptionSetValue?.Value)
                {
                    logger.LogDebug("Attribute {AttributeKey} has different OptionSetValue values. Original: {OriginalValue}, Update: {UpdateValue}", attribute.Key, originalOptionSetValue?.Value, updateOptionSetValue?.Value);
                    different = true;
                }
            }
            else if (updateValue is OptionSetValueCollection || originalValue is OptionSetValueCollection)
            {
                logger.LogDebug("Comparing OptionSetValueCollection attribute {AttributeKey}", attribute.Key);
                var originalOptionSetValue = (OptionSetValueCollection)originalValue;
                var updateOptionSetValue = (OptionSetValueCollection)updateValue;
                var originalOptions = originalOptionSetValue?.Select(o => o.Value)?.ToArray() ?? [];
                var updateOptions = updateOptionSetValue?.Select(o => o.Value)?.ToArray() ?? [];

                if (originalOptions.Length != updateOptions.Length ||
                    originalOptions.Intersect(updateOptions).Count() != originalOptions.Length)
                {
                    logger.LogDebug("Attribute {AttributeKey} has different OptionSetValueCollection values. Original: {OriginalValues}, Update: {UpdateValues}", attribute.Key, string.Join(", ", originalOptions), string.Join(", ", updateOptions));
                    different = true;
                }
            }
            else if (updateValue is string || originalValue is string)
            {
                logger.LogDebug("Comparing string attribute {AttributeKey}", attribute.Key);
                string? strUpdate = updateValue as string;
                string? strOriginal = originalValue as string;

                if (strUpdate == "" && strOriginal is null)
                {
                    logger.LogDebug("Attribute {AttributeKey} is considered equal because update value is empty string and original value is null.", attribute.Key);
                    different = false;
                }
                else
                {
                    bool areEqual = string.Equals(strUpdate, strOriginal, StringComparison.OrdinalIgnoreCase);

                    if (!areEqual)
                    {
                        logger.LogDebug("Attribute {AttributeKey} has different string values,.", attribute.Key);
                        different = true;
                    } else
                    {
                        different = false;
                    }
                }
            }
            else if (updateValue is DateTime || originalValue is DateTime)
            {
                logger.LogDebug("Comparing DateTime attribute {AttributeKey}", attribute.Key);
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
                    logger.LogDebug("Attribute {AttributeKey} has different DateTime values. Original: {OriginalValue}, Update: {UpdateValue}", attribute.Key, originalValue, updateValue);
                    different = true;
                }
            }
            else
            {
                if (!Equals(updateValue, originalValue))
                {
                    logger.LogDebug("Attribute {AttributeKey} has different values. Original: {OriginalValue}, Update: {UpdateValue}", attribute.Key, originalValue, updateValue);
                    different = true;
                }
            }


            if (different)
            {
                logger.LogDebug("Attribute {AttributeKey} has changed.", attribute.Key);
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

    public static string DisplayAttributeValue(object attributeValue, string? defaultDateTimeFormat="u")
    {
        if (attributeValue is null)
        {
            return "(null)";
        }
        else if (attributeValue is EntityCollection entityCol)
        {
            return $"[{string.Join(",", entityCol.Entities.OrderBy(e => e.Id).Select(entity => $"{entity.LogicalName}({entity.Id})"))}]";
        }
        else if (attributeValue is EntityReferenceCollection entityRefCol) 
        {
            return $"[{string.Join(",", entityRefCol.OrderBy(e=>e.Id).Select(entityRef => $"{entityRef.LogicalName}({entityRef.Id})"))}]";
        }
        else if (attributeValue is EntityReference entityRef)
        {
            return $"{entityRef.LogicalName}({entityRef.Id})";
        }
        else if (attributeValue is Money money)
        {
            return money.Value.ToString();
        }
        else if (attributeValue is OptionSetValueCollection optionSetValueCol) 
        {
            return $"[{string.Join(",", optionSetValueCol.OrderBy(op=>op.Value).Select(op => op.Value))}]";
        }
        else if (attributeValue is DateTime dateTimeValue)
        {
            return dateTimeValue.ToString(defaultDateTimeFormat);
        }
        else if (attributeValue is OptionSetValue optionSetValue)
        {
            return optionSetValue.Value.ToString();
        }
        else if (attributeValue is string)
        {
            return $"{attributeValue}";
        }
        else
        {
            return $"{attributeValue}";
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
