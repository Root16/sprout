using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.DataStores;
using Root16.Sprout.Dataverse.DataStores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Root16.Sprout.Dataverse.BatchProcessing;

public abstract class DataverseBatchIntegrationStep<TInput> : BatchIntegrationStep<TInput, OrganizationRequest, DataverseDataStoreOptions>
{
    public override IReadOnlyList<OrganizationRequest> MapRecord(TInput source)
    {
        var entities = MapEntity(source);
        return entities
            .Select(e => new CreateRequest
            {
                Target = e,
            })
            .ToList();
    }

    public abstract IReadOnlyList<Entity> MapEntity(TInput source);
}
