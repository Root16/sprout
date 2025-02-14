using Microsoft.Extensions.Caching.Memory;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Root16.Sprout.DataSources.Dataverse;

public class OptionSetMapper(DataverseDataSource dataverseDataSource, IMemoryCache memoryCache) : IOptionSetMapper
{
    private EntityMetadata GetEntityMetadata(string entityLogicalName)
    {
        var metadata = memoryCache.GetOrCreate($"dataverse-metadata-{dataverseDataSource.CrmServiceClient.EnvironmentId}-{entityLogicalName}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);

            var request = new RetrieveEntityRequest
            {
                LogicalName = entityLogicalName,
                EntityFilters = EntityFilters.Attributes
            };
            var response = (RetrieveEntityResponse)dataverseDataSource.CrmServiceClient.Execute(request);

            return response.EntityMetadata;
        });

        return metadata;
    }

    public OptionSetValue? MapOptionSetByLabel(string entityLogicalName, string attributeLogicalName, string label, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
    {
        if (string.IsNullOrWhiteSpace(label)) return null;

        var metadata = GetEntityMetadata(entityLogicalName);
        var picklistMetadata = (PicklistAttributeMetadata)metadata.Attributes.Where(a => a.AttributeType == AttributeTypeCode.Picklist && a.LogicalName == attributeLogicalName).First();
        var option = picklistMetadata.OptionSet.Options.Where(o => string.Equals(o.Label.UserLocalizedLabel.Label, label, stringComparison)).FirstOrDefault();

        if (option != null && option.Value != null)
        {
            return new OptionSetValue
            {
                Value = option.Value.Value
            };
        }
        else
            return null;
    }

    public OptionSetValueCollection? MapMultiSelectByLabels(string entityLogicalName, string attributeLogicalName, string labels, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
    {
        if (string.IsNullOrWhiteSpace(labels)) return null;

        var metadata = GetEntityMetadata(entityLogicalName);
        var picklistMetaData = (MultiSelectPicklistAttributeMetadata)metadata.Attributes.Where(a => a.GetType() == typeof(MultiSelectPicklistAttributeMetadata) && a.LogicalName == attributeLogicalName).First();
        var labelList = labels.Split(';', StringSplitOptions.TrimEntries).Where(l => !string.IsNullOrWhiteSpace(l));
        List<OptionSetValue> values = new List<OptionSetValue>();

        foreach (var l in labelList)
        {
            var option = picklistMetaData.OptionSet.Options.Where(o => string.Equals(o.Label.UserLocalizedLabel.Label, l, stringComparison)).FirstOrDefault();
            if (option != null && option.Value != null)
            {
                values.Add(new OptionSetValue(option.Value.Value));
            }
        }

        if (values.Count > 0)
        {
            return new OptionSetValueCollection(values);
        }
        else
            return null;
    }

    public OptionSetValue? MapStateByLabel(string entityLogicalName, string label, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
    {
        var metadata = GetEntityMetadata(entityLogicalName);
        var stateMetaData = (StateAttributeMetadata)metadata.Attributes.Where(a => a.AttributeType == AttributeTypeCode.State && a.LogicalName == "statecode").First();
        var option = stateMetaData.OptionSet.Options.Where(o => string.Equals(o.Label.UserLocalizedLabel.Label, label, stringComparison)).FirstOrDefault();

        if (option != null && option.Value != null)
        {
            return new OptionSetValue
            {
                Value = option.Value.Value
            };
        }
        else
            return null;
    }

    public OptionSetValue? MapStatusByLabel(string entityLogicalName, string label, int? state = null, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
    {
        if (string.IsNullOrWhiteSpace(label)) return null;
        var metadata = GetEntityMetadata(entityLogicalName);
        var statusMetaData = (StatusAttributeMetadata)metadata.Attributes.Where(a => a.AttributeType == AttributeTypeCode.Status && a.LogicalName == "statuscode").First();
        var option = statusMetaData.OptionSet.Options.Where(o => string.Equals(o.Label.UserLocalizedLabel.Label, label, stringComparison)).FirstOrDefault(o => state is null || (state.HasValue && state.Value == ((StatusOptionMetadata)o).State));

        if (option != null && option.Value != null)
        {
            return new OptionSetValue
            {
                Value = option.Value.Value
            };
        }
        else
            return null;
    }

    public OptionSetValue? MapStateByStatusLabel(string entityLogicalName, string label, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
    {
        if (string.IsNullOrWhiteSpace(label)) return null;
        var metadata = GetEntityMetadata(entityLogicalName);
        var statusMetaData = (StatusAttributeMetadata)metadata.Attributes.Where(a => a.AttributeType == AttributeTypeCode.Status && a.LogicalName == "statuscode").First();
        var option = statusMetaData.OptionSet.Options.Where(o => string.Equals(o.Label.UserLocalizedLabel.Label, label, stringComparison)).FirstOrDefault() as StatusOptionMetadata;

        if (option != null && option.Value != null && option.State != null)
        {
            return new OptionSetValue
            {
                Value = (int)option.State
            };
        }
        else
            return null;
    }
}
