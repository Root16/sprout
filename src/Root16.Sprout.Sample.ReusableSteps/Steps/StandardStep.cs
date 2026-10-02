using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DataSources;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.Sample.StepRegistration.Models;

namespace Root16.Sprout.Sample.ReusableSteps.Steps;

public class StandardStep : BatchIntegrationStep<SampleClass, Entity>
{
    private readonly MemoryDataSource<SampleClass> memoryDS;
    private readonly DataverseDataSource dataverseDataSource;
    private readonly EntityOperationReducer reducer;
    private readonly BatchProcessor batchProcessor;

    public StandardStep(MemoryDataSource<SampleClass> memoryDS, DataverseDataSource dataverseDataSource, EntityOperationReducer reducer, BatchProcessor batchProcessor)
    {
        ArgumentNullException.ThrowIfNull(memoryDS);
        ArgumentNullException.ThrowIfNull(dataverseDataSource);
        ArgumentNullException.ThrowIfNull(reducer);
        ArgumentNullException.ThrowIfNull(batchProcessor);

        this.dataverseDataSource = dataverseDataSource;
        this.reducer = reducer;
        this.batchProcessor = batchProcessor;
        this.memoryDS = memoryDS;
        DryRun = true;
    }

    public override async Task RunAsync(string stepName)
    {
        await batchProcessor.ProcessBatchesAsync(this, stepName, 1000000);
    }

    public override IDataSource<Entity> OutputDataSource => dataverseDataSource;

    public override IPagedQuery<SampleClass> GetInputQuery()
    {
        return memoryDS.CreatePagedQuery();
    }

    public override IReadOnlyList<DataOperation<Entity>> MapRecord(SampleClass source)
    {
        return [];
    }
}
