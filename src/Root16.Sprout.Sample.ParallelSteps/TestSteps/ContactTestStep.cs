using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.Sample.ParallelSteps.Models;
using Root16.Sprout.DataStores;
using Root16.Sprout.Dataverse.DataStores;

namespace Root16.Sprout.Sample;

internal class ContactTestStep : BatchIntegrationStep<Contact,Entity>
{
    private readonly DataverseDataStore dataverseDataSource;
    private readonly EntityOperationReducer reducer;
    private readonly BatchProcessor batchProcessor;
    private MemoryDataStore<Contact> memoryDS;

    public ContactTestStep(MemoryDataStore<Contact> memoryDS, DataverseDataStore dataverseDataSource, EntityOperationReducer reducer, BatchProcessor batchProcessor)
    {
        this.dataverseDataSource = dataverseDataSource;
        this.reducer = reducer;
        this.batchProcessor = batchProcessor;
        this.memoryDS = memoryDS;
        DryRun = false;
        BatchSize = 50;
    }

    public override async Task<IReadOnlyList<Contact>> OnBeforeMapAsync(IReadOnlyList<Contact> batch)
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

    public override IReadOnlyList<DataOperation<Entity>> OnBeforeDelivery(IReadOnlyList<DataOperation<Entity>> batch)
    {
        return reducer.ReduceOperations(batch, entity => string.Concat(
                entity.GetAttributeValue<string>("firstname"),
                "|",
                entity.GetAttributeValue<string>("lastname")
        ));
    }

    public override async Task RunAsync()
    {
        await batchProcessor.ProcessAllBatchesAsync(this);
    }

    public override IDataStore<Entity> OutputDataStore => dataverseDataSource;

    public override IPagedQuery<Contact> GetInputQuery()
    {
        return memoryDS.CreatePagedQuery();
    }

    public override IReadOnlyList<DataOperation<Entity>> MapRecord(Contact source)
    {
        var result = new Entity("contact")
        {
            Attributes =
            {
                {"firstname", source.FirstName },
                {"lastname", source.LastName },
            }
        };

        return new[] { new DataOperation<Entity>("Create", result) };
    }

}
