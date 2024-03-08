using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.DataStores;
using Root16.Sprout.Dataverse.BatchProcessing;
using Root16.Sprout.Dataverse.DataStores;

namespace Root16.Sprout.Sample.CreatesAndUpdates;

internal class CreateContactTestStep : DataverseBatchIntegrationStep<CreateContact>
{
    private readonly DataverseDataStore dataverseDataSource;
    private readonly EntityOperationReducer reducer;
    private readonly IBatchProcessor batchProcessor;
    private MemoryDataStore<CreateContact> memoryDS;

    public CreateContactTestStep(MemoryDataStore<CreateContact> memoryDS, DataverseDataStore dataverseDataSource, EntityOperationReducer reducer, IBatchProcessor batchProcessor)
    {
        this.dataverseDataSource = dataverseDataSource;
        this.reducer = reducer;
        this.batchProcessor = batchProcessor;
        this.memoryDS = memoryDS;
        DryRun = false;
        BatchSize = 2000;
    }

    public override async Task<IReadOnlyList<CreateContact>> OnBeforeMapAsync(IReadOnlyList<CreateContact> batch)
    {
        var firstNameValues = string.Join("</value><value>", batch.Select(b => b.FirstName).Distinct(StringComparer.OrdinalIgnoreCase));
        var lastNameValues = string.Join("</value><value>", batch.Select(b => b.LastName).Distinct(StringComparer.OrdinalIgnoreCase));

        var matches = await dataverseDataSource.CrmServiceClient.RetrieveMultipleAsync(new FetchExpression($@"
                <fetch>
                    <entity name='contact'>
                        <attribute name='firstname' />
                        <attribute name='lastname' />
                        <filter>
                            <condition attribute='firstname' operator='in'>
                                <value>{firstNameValues}</value>
                            </condition>
                            <condition attribute='lastname' operator='in'>
                                <value>{lastNameValues}</value>
                            </condition>
                        </filter>
                    </entity>
                </fetch>"));

        reducer.SetPotentialMatches(matches.Entities);

        return batch;
    }

    public override IReadOnlyList<OrganizationRequest> OnBeforeDelivery(IReadOnlyList<OrganizationRequest> batch)
    {
        return reducer.ReduceOperations(batch, entity => string.Concat(
                entity.GetAttributeValue<string>("firstname"),
                "|",
                entity.GetAttributeValue<string>("lastname")
        ));
    }

    public override IDataStore<OrganizationRequest, DataverseDataStoreOptions> OutputDataStore => dataverseDataSource;

    public override IPagedQuery<CreateContact> GetInputQuery()
    {
        return memoryDS.CreatePagedQuery();
    }

    public override IReadOnlyList<Entity> MapEntity(CreateContact source)
    {
        var result = new Entity("contact")
        {
            Attributes =
            {
                {"firstname", source.FirstName },
                {"lastname", source.LastName },
            }
        };

        return [result];
    }
}
