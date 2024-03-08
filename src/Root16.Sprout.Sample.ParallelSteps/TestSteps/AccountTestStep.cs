using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Root16.Sprout.BatchProcessing;
using System.Text;
using Root16.Sprout.Sample.ParallelSteps.Models;
using Root16.Sprout.DataStores;
using Root16.Sprout.Dataverse.DataStores;

namespace Root16.Sprout.Sample.TestSteps;

internal class AccountTestStep : BatchIntegrationStep<Account, Entity>
{
    private readonly DataverseDataStore dataverseDataSource;
    private readonly EntityOperationReducer reducer;
    private readonly BatchProcessor batchProcessor;
    private MemoryDataStore<Account> memoryDS;

    public AccountTestStep(MemoryDataStore<Account> memoryDS, DataverseDataStore dataverseDataSource, EntityOperationReducer reducer, BatchProcessor batchProcessor)
    {
        this.dataverseDataSource = dataverseDataSource;
        this.reducer = reducer;
        this.batchProcessor = batchProcessor;
        this.memoryDS = memoryDS;
        DryRun = false;
        BatchSize = 50;
    }

    public override async Task<IReadOnlyList<Account>> OnBeforeMapAsync(IReadOnlyList<Account> batch)
    {
        var accountNames = batch.Select(b => b.AccountName).Distinct(StringComparer.OrdinalIgnoreCase).Aggregate(new StringBuilder(), (current, x) => current.Append($"{x}</value><value>"));

        var matches = await dataverseDataSource.CrmServiceClient.RetrieveMultipleAsync(new FetchExpression($@"
                <fetch>
                  <entity name=""account"">
                    <attribute name=""name"" />
                    <filter>
                      <condition attribute=""name"" operator=""in"">
                        <value>{accountNames}</value>
                      </condition>
                    </filter>
                  </entity>
                </fetch>"));

        reducer.SetPotentialMatches(matches.Entities);

        return batch;
    }

    public override IReadOnlyList<DataOperation<Entity>> OnBeforeDelivery(IReadOnlyList<DataOperation<Entity>> batch)
    {
        return reducer.ReduceOperations(batch, entity => entity.GetAttributeValue<string>("name"));
    }

    public override async Task RunAsync()
    {
        await batchProcessor.ProcessAllBatchesAsync(this);
    }

    public override IDataStore<Entity> OutputDataStore => dataverseDataSource;

    public override IPagedQuery<Account> GetInputQuery()
    {
        return memoryDS.CreatePagedQuery();
    }

    public override IReadOnlyList<DataOperation<Entity>> MapRecord(Account source)
    {
        var result = new Entity("account")
        {
            Attributes =
            {
                {"name", source.AccountName }
            }
        };

        return new[] { new DataOperation<Entity>("Create", result) };
    }

}
