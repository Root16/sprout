using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DataSources;
using System.Data;
using Microsoft.Xrm.Sdk;
using Root16.Sprout.DataSources.Dataverse;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Extensions.Logging;
using Root16.Sprout.Logging;

namespace Root16.Sprout.Sample.CreatesAndUpdates;
internal class ReportErrorsStep : BatchIntegrationStep<Entity, Entity>
{
	private readonly DataverseDataSource dataverseDataSource;
	private readonly BatchProcessor batchProcessor;
	EntityOperationReducer reducer;
    BatchLogger batchLogger;
	private IList<Entity> matches;
	private readonly ILogger<ReportErrorsStep> logger;
    public ReportErrorsStep(
		BatchProcessor batchProcessor,
		DataverseDataSource dataverseDataSource,
		EntityOperationReducer reducer,
		ILogger<ReportErrorsStep> logger,
		BatchLogger analyzer
		)
	{
		this.batchProcessor = batchProcessor;
		this.dataverseDataSource = dataverseDataSource;
		BatchSize = 10;
		this.reducer = reducer;
		this.logger = logger;
		this.batchLogger = analyzer;
		//DryRun = true; // false - errors will occur, true - no errors will occur
		this.KeySelector = e => e.GetAttributeValue<string>("name") ?? e.Id.ToString();
		AddDataOperationFlag(DataverseDataSourceFlags.BypassBusinessLogicExecutionSync);
		AddDataOperationFlag(DataverseDataSourceFlags.SuppressCallbackRegistrationExpanderJob);
	}

	public override IDataSource<Entity> OutputDataSource => dataverseDataSource;

	public override IPagedQuery<Entity> GetInputQuery()
	{
		return dataverseDataSource.CreateFetchXmlQuery(
			$@"<fetch>
              <entity name='account'>
					<filter>
						<condition attribute='name' operator='like' value='%a%' />
					</filter>
              </entity>
            </fetch>");
	}

	public override async Task<IReadOnlyList<Entity>> OnBeforeMapAsync(IReadOnlyList<Entity> batch)
	{
		var formatted =
			batch.Select(r => $"<value>{r.Id}</value>");

		if (formatted.Count() > 0)
		{
			var fetchXml = $@"
                <fetch>
                    <entity name='account'>
						<attribute name='statecode' />
						<attribute name='statuscode' />
                        <filter>
                            <condition attribute='accountid' operator='in'>
                                {string.Join("", formatted)}
                            </condition>
                        </filter>
                    </entity>
                </fetch>";

			var potentialMatches = await dataverseDataSource.CrmServiceClient.RetrieveMultipleWithRetryAsync(new FetchExpression(fetchXml));

			logger.LogInformation($"Potentional matches for: {batch.First()}: {potentialMatches.Entities.Count()}");

			reducer.SetPotentialMatches(potentialMatches.Entities);
        }
		else
		{
			reducer.SetPotentialMatches(new List<Entity>());
		}
		return batch;
	}
	public override IReadOnlyList<DataOperation<Entity>> OnBeforeDelivery(IReadOnlyList<DataOperation<Entity>> batch)
	{
		return reducer.ReduceOperations(batch, this.KeySelector!);
	}

	public override IReadOnlyList<DataOperation<Entity>> MapRecord(Entity source)
	{
		var accountUpdate = new Entity("account", source.Id);
		accountUpdate["statecode"] = new OptionSetValue(-1);
		accountUpdate["statuscode"] = new OptionSetValue(-1);

		return [new DataOperation<Entity>("Update", accountUpdate)];
    }
    public override Task OnAfterDeliveryAsync(IReadOnlyList<DataOperationResult<Entity>> results)
    {
        batchLogger.ReportFailuresToFile($"./report/{nameof(ReportErrorsStep)}-error-{DateTime.Now:yyyy-MM-dd}.txt", results, this.KeySelector!);

        // Relies on EntityOperationReducer or user to set the DataOperation.Change (and DataSource to not wipe out DataOperation.Change)
		// record before calling ReportDifference
		batchLogger.ReportDifferencesToFile($"./report/{nameof(ReportErrorsStep)}-diff-{DateTime.Now:yyyy-MM-dd}.txt", results, this.KeySelector!);

		return base.OnAfterDeliveryAsync(results);

    }
    public override async Task RunAsync(string stepName)
    {
        await batchProcessor.ProcessBatchesAsync(this, stepName, 1);
    }
}
