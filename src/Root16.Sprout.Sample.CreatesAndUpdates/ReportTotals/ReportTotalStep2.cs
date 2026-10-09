using Microsoft.Extensions.Logging;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.Logging;
using Root16.Sprout.Sample.CreatesAndUpdates;

namespace Root16.Sprout.Sample.CreateUpdateAndDelete.ReportTotals;

internal class ReportTotalStep2(
	BatchProcessor batchProcessor,
	DataverseDataSource dataverseDataSource,
	EntityOperationReducer reducer,
	ILogger<ReportErrorsStep> logger,
	BatchLogger analyzer
	) : ReportTotalsStep(batchProcessor, dataverseDataSource, reducer, logger, analyzer, 5)
{
}
