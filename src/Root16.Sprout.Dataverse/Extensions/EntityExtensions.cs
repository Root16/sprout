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
                var originalLookup = originalValue as EntityReference;
                var updateLookup = updateValue as EntityReference;

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
                var originalCollection = originalValue as EntityReferenceCollection;
                var updateCollection = updateValue as EntityReferenceCollection;

                if (originalCollection is null && updateCollection is null)
                {
                    continue;
                }
                if (originalCollection is null || updateCollection is null)
                {
                    logger.LogDebug("Attribute {AttributeKey} has different EntityReferenceCollection presence (one is null).", attribute.Key);
                    different = true;
                }
                else
                {
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
            }
            else if (updateValue is EntityCollection || originalValue is EntityCollection)
            {
                logger.LogDebug("Comparing EntityCollection attribute {AttributeKey}", attribute.Key);
                var originalCollection = originalValue as EntityCollection;
                var updateCollection = updateValue as EntityCollection;

                if (originalCollection is null && updateCollection is null)
                {
                    continue;
                }
                if (originalCollection is null || updateCollection is null)
                {
                    logger.LogDebug("Attribute {AttributeKey} has different EntityCollection presence (one is null).", attribute.Key);
                    different = true;
                }
                else
                {
                    var originalKeys = originalCollection.Entities.Select(GetActivityPartyAsStringForComparison).OrderBy(k => k).ToList();
                    var updateKeys = updateCollection.Entities.Select(GetActivityPartyAsStringForComparison).OrderBy(k => k).ToList();

                    if (!originalKeys.SequenceEqual(updateKeys))
                    {
                        logger.LogDebug("Attribute {AttributeKey} has different Activity Parties. Original: {OriginalParties}, Update: {UpdateParties}",
                            attribute.Key, string.Join(", ", originalKeys), string.Join(", ", updateKeys));
                        different = true;
                    }
                }
            }
            else if (updateValue is Money || originalValue is Money)
            {
                logger.LogDebug("Comparing Money attribute {AttributeKey}", attribute.Key);
                var originalMoney = originalValue as Money;
                var updateMoney = updateValue as Money;

                if (updateMoney?.Value != originalMoney?.Value)
                {
                    logger.LogDebug("Attribute {AttributeKey} has different Money values. Original: {OriginalValue}, Update: {UpdateValue}", attribute.Key, originalMoney?.Value, updateMoney?.Value);
                    different = true;
                }
            }
            else if (updateValue is OptionSetValue || originalValue is OptionSetValue)
            {
                logger.LogDebug("Comparing OptionSetValue attribute {AttributeKey}", attribute.Key);
                var originalOptionSetValue = originalValue as OptionSetValue;
                var updateOptionSetValue = updateValue as OptionSetValue;

                if (updateOptionSetValue?.Value != originalOptionSetValue?.Value)
                {
                    logger.LogDebug("Attribute {AttributeKey} has different OptionSetValue values. Original: {OriginalValue}, Update: {UpdateValue}", attribute.Key, originalOptionSetValue?.Value, updateOptionSetValue?.Value);
                    different = true;
                }
            }
            else if (updateValue is OptionSetValueCollection || originalValue is OptionSetValueCollection)
            {
                logger.LogDebug("Comparing OptionSetValueCollection attribute {AttributeKey}", attribute.Key);
                var originalOptionSetValue = originalValue as OptionSetValueCollection;
                var updateOptionSetValue = updateValue as OptionSetValueCollection;

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
                        logger.LogDebug("Attribute {AttributeKey} has different string values.", attribute.Key);
                        different = true;
                    }
                }
            }
            else if (updateValue is DateTime || originalValue is DateTime)
            {
                logger.LogDebug("Comparing DateTime attribute {AttributeKey}", attribute.Key);
                DateTime? dtUpdate = updateValue as DateTime?;
                DateTime? dtOriginal = originalValue as DateTime?;

                if (dtUpdate.HasValue && dtOriginal.HasValue)
                {
                    //Database doesn't have milliseconds, so we need to compare the values without milliseconds. We will convert both DateTime values to UTC to ensure accurate comparison across different time zones.
                    var uTime = new DateTime(dtUpdate.Value.Year, dtUpdate.Value.Month, dtUpdate.Value.Day,
                        dtUpdate.Value.Hour, dtUpdate.Value.Minute, dtUpdate.Value.Second, dtUpdate.Value.Kind).ToUniversalTime();

                    var oTime = new DateTime(dtOriginal.Value.Year, dtOriginal.Value.Month, dtOriginal.Value.Day,
                        dtOriginal.Value.Hour, dtOriginal.Value.Minute, dtOriginal.Value.Second, dtOriginal.Value.Kind).ToUniversalTime();

                    if (uTime != oTime)
                    {
                        logger.LogDebug("Attribute {AttributeKey} has different DateTime values. Original: {OriginalValue}, Update: {UpdateValue}", attribute.Key, oTime, uTime);
                        different = true;
                    }
                }
                else if (dtUpdate.HasValue != dtOriginal.HasValue)
                {
                    logger.LogDebug("Attribute {AttributeKey} has different DateTime presence. Original: {OriginalValue}, Update: {UpdateValue}", attribute.Key, dtOriginal, dtUpdate);
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

    public static string GetActivityPartyAsStringForComparison(Entity party)
    {
        var partyId = party.GetAttributeValue<EntityReference>("partyid");
        if (partyId is not null)
        {
            return $"{partyId.LogicalName}:{partyId.Id}";
        }

        var addressUsed = party.GetAttributeValue<string>("addressused");
        if (!string.IsNullOrWhiteSpace(addressUsed))
        {
            // Unresolved email address
            return $"unresolved:{addressUsed.ToLowerInvariant()}";
        }

        // Fallback for an empty party record
        return Guid.NewGuid().ToString();
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
