using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Root16.Sprout.DataSources;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.BatchProcessing;
using System.Text;
using Root16.Sprout.Sample.ParallelSteps.Models;

namespace Root16.Sprout.Sample.ParallelSteps.TestSteps;

internal class EmailTestStep : BatchIntegrationStep<Email, Entity>
{
    private readonly DataverseDataSource dataverseDataSource;
    private readonly EntityOperationReducer reducer;
    private readonly BatchProcessor batchProcessor;
    private readonly MemoryDataSource<Email> memoryDS;

    public EmailTestStep(MemoryDataSource<Email> memoryDS, DataverseDataSource dataverseDataSource, EntityOperationReducer reducer, BatchProcessor batchProcessor)
    {
        this.dataverseDataSource = dataverseDataSource;
        this.reducer = reducer;
        this.batchProcessor = batchProcessor;
        this.memoryDS = memoryDS;
        DryRun = false;
        BatchSize = 50;
    }

    public override async Task<IReadOnlyList<Email>> OnBeforeMapAsync(IReadOnlyList<Email> batch)
    {
        var emailSubjects = batch.Select(b => b.EmailSubject).Distinct(StringComparer.OrdinalIgnoreCase).Aggregate(new StringBuilder(), (current, x) => current.Append($"{x}</value><value>"));

        var matches = await dataverseDataSource.CrmServiceClient.RetrieveMultipleAsync(new FetchExpression($@"
                <fetch>
                  <entity name=""email"">
                    <attribute name=""subject"" />
                    <filter>
                      <condition attribute=""subject"" operator=""in"">
                        <value>{emailSubjects}</value>
                      </condition>
                    </filter>
                  </entity>
                </fetch>"));

        reducer.SetPotentialMatches(matches.Entities);

        return batch;
    }

    public override IReadOnlyList<DataOperation<Entity>> OnBeforeDelivery(IReadOnlyList<DataOperation<Entity>> batch)
    {
        return reducer.ReduceOperations(batch, entity => entity.GetAttributeValue<string>("subject"));
    }

    public override async Task RunAsync(string stepName)
    {
        await batchProcessor.ProcessAllBatchesAsync(this, stepName);
    }

    public override IDataSource<Entity> OutputDataSource => dataverseDataSource;

    public override IPagedQuery<Email> GetInputQuery()
    {
        return memoryDS.CreatePagedQuery();
    }

    public override IReadOnlyList<DataOperation<Entity>> MapRecord(Email source)
    {
        var result = new Entity("email")
        {
            Attributes =
            {
                {"subject", source.EmailSubject }
            }
        };

        return [new DataOperation<Entity>("Create", result)];
    }

}
