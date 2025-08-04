using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Root16.Sprout.Logging;
using System.Data.Common;

namespace Root16.Sprout.DataSources.Dataverse;

public class EntityBatchAnalyzer(
    ILogger<EntityBatchAnalyzer> logger
    ) : BatchAnalyzer<Entity>
{
    protected override ChangeRecord GetDifference(string key, Entity data, Entity? previousData = null)
    {
        var changeRecord = new ChangeRecord(data.LogicalName, data.Id.ToString(), []);
        foreach (var attribute in data.Attributes)
        {
            object previousValue = null;
            previousData?.Attributes.TryGetValue(attribute.Key, out previousValue);
            if (previousValue != data[attribute.Key])
                changeRecord.Changes.Add(attribute.Key, new(FormatAttributeValue(previousValue), FormatAttributeValue(attribute.Value)));
        }

        return changeRecord;
    }

    private string? FormatAttributeValue(object? attributeValue)
    {
        if (attributeValue is null)
        {
            return null;
        }
        else if (attributeValue is EntityReference entityRef)
        {
            return $"{entityRef.LogicalName}({entityRef.Id})";
        }
        else if (attributeValue is Money money)
        {
            return money.Value.ToString();
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
}
