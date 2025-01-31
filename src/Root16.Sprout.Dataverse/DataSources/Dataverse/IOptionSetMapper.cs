using Microsoft.Xrm.Sdk;

namespace Root16.Sprout.DataSources.Dataverse;

public interface IOptionSetMapper
{
    OptionSetValue? MapOptionSetByLabel(string entityLogicalName, string attributeLogicalName, string label, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase);
    OptionSetValueCollection? MapMultiSelectByLabels(string entityLogicalName, string attributeLogicalName, string labels, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase);
    OptionSetValue? MapStateByLabel(string entityLogicalName, string label, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase);
    OptionSetValue? MapStatusByLabel(string entityLogicalName, string label, int? state = null, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase);
    OptionSetValue? MapStateByStatusLabel(string entityLogicalName, string label, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase);
}