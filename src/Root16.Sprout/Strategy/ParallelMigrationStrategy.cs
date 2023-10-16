using Microsoft.Extensions.Logging;
using Root16.Sprout.Progress;
using Root16.Sprout.Step;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Root16.Sprout.Strategy;

public class ParallelMigrationStrategy : IMigrationStrategy
{
	private readonly ILogger<ParallelMigrationStrategy> logger;

	public ParallelMigrationStrategy(ILogger<ParallelMigrationStrategy> logger)
	{
		this.logger = logger;
	}

	public int BatchSize { get; set; } = 200;
	public int MaxDegreeOfParallelism { get; set; } = 20;

	public void Migrate<TSource, TDest>(IMigrationRuntime runtime, IMigrationStep<TSource, TDest> step)
	{
		var query = step.GetSourceQuery(runtime);
		var dest = step.GetDataSink(runtime);

		var progress = new MigrationProgress(step.Name, query.GetTotalRecordCount());
		runtime.ReportProgress(progress);

		while (query.MoreRecords)
		{
			IReadOnlyList<TSource> page = query.GetNextPage(BatchSize);
			step.OnBeforeMap(page);
			var options = new ParallelOptions
			{
				MaxDegreeOfParallelism = MaxDegreeOfParallelism,
			};
			Parallel.ForEach(page, options, sourceRecord =>
			{
				var results = new List<DataChange<TDest>>();
				var destRecords = step.MapRecord(sourceRecord);
				if (destRecords == null || destRecords.Count == 0)
				{
					progress.AddSkippedRecords(1);
				}
				else
				{
					results.AddRange(destRecords);
				}

				var finalResults = step.OnBeforeUpdate(results);
				progress.AddSkippedRecords(results.Count - finalResults.Count);

				if (finalResults.Count > 0)
				{
					progress.AddResultRange(finalResults.Where(c => c.Type == DataChangeType.Error).Select(c => c.Type));
					foreach (var change in finalResults.Where(c => c.Type != DataChangeType.Error))
					{
						var updateResult = dest.Update(new[] { change });
						progress.AddResultRange(updateResult);
					}
				}
			});

			runtime.ReportProgress(progress);
		}
	}
}
