using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Root16.Sprout.Logging;

namespace Root16.Sprout.DataSources.Dataverse;

public class EntityBatchAnalyzer(
    ILogger<EntityBatchAnalyzer> logger
    ) : BatchAnalyzer<Entity>
{
    public override Audit GetDifference(string key, Entity data, Entity? previousData = null)
    {
        var changeRecord = new Audit(data.LogicalName, FormatValue(data.Id), key, []);
        foreach (var attribute in data.Attributes)
        {
            object previousValue = null;
            previousData?.Attributes.TryGetValue(attribute.Key, out previousValue);
            if (previousValue != data[attribute.Key])
                changeRecord.Changes.Add(attribute.Key, new(FormatValue(previousValue), FormatValue(attribute.Value)));
        }

        return changeRecord;
    }

    public override string? FormatValue(object? attributeValue)
    {
        return EntityExtensions.DisplayAttributeValue(attributeValue);
    }
}
