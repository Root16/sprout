using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DataSources;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.Sample.StepRegistration.Models;

namespace Root16.Sprout.Sample.StepRegistration.Steps;

public class ReusableStepWithMultipleParams : BatchIntegrationStep<SampleClass, Entity>
{
    private readonly MemoryDataSource<SampleClass> memoryDS;
    private readonly DataverseDataSource dataverseDataSource;
    private readonly EntityOperationReducer reducer;
    private readonly BatchProcessor batchProcessor;

    public ReusableStepWithMultipleParams(MemoryDataSource<SampleClass> memoryDS, string value1, DataverseDataSource dataverseDataSource, EntityOperationReducer reducer, string value2, BatchProcessor batchProcessor)
    {
        ArgumentNullException.ThrowIfNull(memoryDS);
        ArgumentNullException.ThrowIfNull(dataverseDataSource);
        ArgumentNullException.ThrowIfNull(reducer);
        ArgumentNullException.ThrowIfNull(batchProcessor);

        if (value1 != "Value1")
        {
            throw new ArgumentException($"Expected 'Value1', but received '{value1}'", nameof(value1));
        }

        if (value2 != "Value2")
        {
            throw new ArgumentException($"Expected 'Value2', but received '{value2}'", nameof(value2));
        }
        this.dataverseDataSource = dataverseDataSource;
        this.reducer = reducer;
        this.batchProcessor = batchProcessor;
        this.memoryDS = memoryDS;
        DryRun = true;
    }

    public override async Task RunAsync(string stepName)
    {
        await batchProcessor.ProcessBatchesAsync(this, stepName);
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
