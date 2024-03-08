using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using Root16.Sprout.BatchProcessing;
using System.Text;
using Root16.Sprout.Sample.ParallelSteps.Models;
using Root16.Sprout.DataStores;
using Root16.Sprout.Dataverse.DataStores;

namespace Root16.Sprout.Sample.ParallelSteps.TestSteps;

internal class LetterTestStep : BatchIntegrationStep<Letter, Entity>
{
    private readonly DataverseDataStore dataverseDataSource;
    private readonly EntityOperationReducer reducer;
    private readonly BatchProcessor batchProcessor;
    private MemoryDataStore<Letter> memoryDS;

    public LetterTestStep(MemoryDataStore<Letter> memoryDS, DataverseDataStore dataverseDataSource, EntityOperationReducer reducer, BatchProcessor batchProcessor)
    {
        this.dataverseDataSource = dataverseDataSource;
        this.reducer = reducer;
        this.batchProcessor = batchProcessor;
        this.memoryDS = memoryDS;
        DryRun = false;
        BatchSize = 50;
    }

    public override async Task<IReadOnlyList<Letter>> OnBeforeMapAsync(IReadOnlyList<Letter> batch)
    {
        var LetterSubjects = batch.Select(b => b.LetterSubject).Distinct(StringComparer.OrdinalIgnoreCase).Aggregate(new StringBuilder(), (current, x) => current.Append($"{x}</value><value>"));

        var matches = await dataverseDataSource.CrmServiceClient.RetrieveMultipleAsync(new FetchExpression($@"
                <fetch>
                  <entity name=""letter"">
                    <attribute name=""subject"" />
                    <filter>
                      <condition attribute=""subject"" operator=""in"">
                        <value>{LetterSubjects}</value>
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

    public override async Task RunAsync()
    {
        await batchProcessor.ProcessAllBatchesAsync(this);
    }

    public override IDataStore<Entity> OutputDataStore => dataverseDataSource;

    public override IPagedQuery<Letter> GetInputQuery()
    {
        return memoryDS.CreatePagedQuery();
    }

    public override IReadOnlyList<DataOperation<Entity>> MapRecord(Letter source)
    {
        var result = new Entity("letter")
        {
            Attributes =
            {
                {"subject", source.LetterSubject }
            }
        };

        return new[] { new DataOperation<Entity>("Create", result) };
    }

}
