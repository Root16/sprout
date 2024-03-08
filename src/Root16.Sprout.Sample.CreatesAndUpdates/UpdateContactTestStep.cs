using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.DataStores;
using Root16.Sprout.Dataverse.BatchProcessing;
using Root16.Sprout.Dataverse.DataStores;

namespace Root16.Sprout.Sample.CreatesAndUpdates;

internal class UpdateContactTestStep : DataverseBatchIntegrationStep<UpdateContact>
{
    private readonly DataverseDataStore dataverseDataSource;
    private readonly EntityOperationReducer reducer;
    private readonly IBatchProcessor batchProcessor;
    private MemoryDataStore<UpdateContact> memoryDS;

    public override IDataStore<OrganizationRequest, DataverseDataStoreOptions> OutputDataStore => dataverseDataSource;

    public override IBatchProcessor BatchProcessor => batchProcessor;

    public UpdateContactTestStep(MemoryDataStore<UpdateContact> memoryDS, DataverseDataStore dataverseDataSource, EntityOperationReducer reducer, IBatchProcessor batchProcessor)
    {
        this.dataverseDataSource = dataverseDataSource;
        this.reducer = reducer;
        this.batchProcessor = batchProcessor;
        this.memoryDS = memoryDS;
    }

    public override async Task<IReadOnlyList<UpdateContact>> OnBeforeMapAsync(IReadOnlyList<UpdateContact> batch)
    {
        var firstNameValues = string.Join("</value><value>", batch.Select(b => b.FirstName).Distinct(StringComparer.OrdinalIgnoreCase));
        var lastNameValues = string.Join("</value><value>", batch.Select(b => b.LastName).Distinct(StringComparer.OrdinalIgnoreCase));

        var matches = await dataverseDataSource.CrmServiceClient.RetrieveMultipleAsync(new FetchExpression($@"
                <fetch>
                    <entity name='contact'>
                        <attribute name='firstname' />
                        <attribute name='lastname' />
                        <attribute name='emailaddress1' />
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

    public override IPagedQuery<UpdateContact> GetInputQuery()
    {
        return memoryDS.CreatePagedQuery();
    }

    public override IReadOnlyList<Entity> MapEntity(UpdateContact source)
    {
        var result = new Entity("contact")
        {
            Attributes =
            {
                {"firstname", source.FirstName },
                {"lastname", source.LastName },
                {"emailaddress1", source.EmailAddress }
            }
        };

        return [result];
    }
}
