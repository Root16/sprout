using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Root16.Sprout.DataSources;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.BatchProcessing;

namespace Root16.Sprout.Sample.CreatesAndUpdates;

internal class DeleteTestStep : BatchIntegrationStep<Entity, Entity>
{
    private readonly DataverseDataSource _dataverseDataSource;
    private readonly BatchProcessor _batchProcessor;

    public DeleteTestStep(DataverseDataSource dataverseDataSource, EntityOperationReducer reducer, BatchProcessor batchProcessor)
    {
        _dataverseDataSource = dataverseDataSource;
        _batchProcessor = batchProcessor;
        DryRun = true;
        BatchSize = 500;
    }

    public override async Task RunAsync(string stepName)
    {
        await _batchProcessor.ProcessBatchesAsync(this, stepName, 22);
    }

    public override IDataSource<Entity> OutputDataSource => _dataverseDataSource;

    public override IPagedQuery<Entity> GetInputQuery()
    {
        return _dataverseDataSource.CreateFetchXmlQuery($@"
            <fetch version=""1.0"" mapping=""logical"">
                <entity name=""contact"">
                <attribute name=""contactid"" />
                </entity>
            </fetch>");
    }

    public override IReadOnlyList<DataOperation<Entity>> MapRecord(Entity source)
    {
        return [new DataOperation<Entity>("Delete", source)];
    }
}
