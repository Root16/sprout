using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Root16.Sprout.BatchProcessing;
using System.Text;
using Root16.Sprout.Sample.ParallelSteps.Models;
using Root16.Sprout.DataStores;
using Root16.Sprout.Dataverse.DataStores;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.Dataverse.BatchProcessing;

namespace Root16.Sprout.Sample.TestSteps;

internal class AccountInvalidDependencyTestStep : DataverseBatchIntegrationStep<Account>
{
    private readonly DataverseDataStore dataverseDataSource;
    private readonly EntityOperationReducer reducer;
    private readonly IBatchProcessor batchProcessor;
    private MemoryDataStore<Account> memoryDS;

    public AccountInvalidDependencyTestStep(MemoryDataStore<Account> memoryDS, DataverseDataStore dataverseDataSource, EntityOperationReducer reducer, IBatchProcessor batchProcessor)
    {
        this.dataverseDataSource = dataverseDataSource;
        this.reducer = reducer;
        this.batchProcessor = batchProcessor;
        this.memoryDS = memoryDS;
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

    public override IReadOnlyList<OrganizationRequest> OnBeforeDelivery(IReadOnlyList<OrganizationRequest> batch)
    {
        return reducer.ReduceOperations(batch, entity => entity.GetAttributeValue<string>("name"));
    }

    public override IDataStore<OrganizationRequest,DataverseDataStoreOptions> OutputDataStore => dataverseDataSource;

    public override IBatchProcessor BatchProcessor => batchProcessor;

    public override IPagedQuery<Account> GetInputQuery()
    {
        return memoryDS.CreatePagedQuery();
    }

    public override IReadOnlyList<Entity> MapEntity(Account source)
    {
        var result = new Entity("account")
        {
            Attributes =
            {
                {"name", source.AccountName }
            }
        };

        return [result];
    }

}
