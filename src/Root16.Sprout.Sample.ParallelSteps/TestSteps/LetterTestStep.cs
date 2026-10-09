using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.DataSources;
using System.Text;
using Root16.Sprout.Sample.ParallelSteps.Models;

namespace Root16.Sprout.Sample.ParallelSteps.TestSteps;

internal class LetterTestStep : BatchIntegrationStep<Letter, Entity>
{
    private readonly DataverseDataSource dataverseDataSource;
    private readonly EntityOperationReducer reducer;
    private readonly BatchProcessor batchProcessor;
    private readonly MemoryDataSource<Letter> memoryDS;

    public LetterTestStep(MemoryDataSource<Letter> memoryDS, DataverseDataSource dataverseDataSource, EntityOperationReducer reducer, BatchProcessor batchProcessor)
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

    public override async Task RunAsync(string stepName)
    {
        await batchProcessor.ProcessBatchesAsync(this, stepName);
    }

    public override IDataSource<Entity> OutputDataSource => dataverseDataSource;

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

        return [new DataOperation<Entity>("Create", result)];
    }

}
