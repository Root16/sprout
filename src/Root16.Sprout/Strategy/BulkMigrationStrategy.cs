﻿using Microsoft.Extensions.Logging;
using Root16.Sprout.Progress;
using Root16.Sprout.Step;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Root16.Sprout.Strategy;

public class BulkMigrationStrategy : IMigrationStrategy
{
	private readonly ILogger<BulkMigrationStrategy> logger;

	public BulkMigrationStrategy(ILogger<BulkMigrationStrategy> logger)
	{
		this.logger = logger;
	}

	public int BatchSize { get; set; } = 200;

	public void Migrate<TSource, TDest>(IMigrationRuntime runtime, IMigrationStep<TSource, TDest> step)
	{
		var query = step.GetSourceQuery(runtime);
		var dest = step.GetDataSink(runtime);

		var progress = new MigrationProgress(step.Name, query.GetTotalRecordCount());
		runtime.ReportProgress(progress);

		int retryCount = 0;
		while (query.MoreRecords)
		{
			if (retryCount == 10) throw new InvalidOperationException("Retry count exceeded.");

			try
			{
				var results = new List<DataChange<TDest>>();
				IReadOnlyList<TSource> page = query.GetNextPage(BatchSize);
				step.OnBeforeMap(page);

				foreach (var sourceRecord in page)
				{
					var destRecords = step.MapRecord(sourceRecord);
					if (destRecords?.Count > 0)
					{
						results.AddRange(destRecords);
					}
					else
					{
						progress.AddSkippedRecords(1);
					}
				}

				var finalResults = step.OnBeforeUpdate(results);
				progress.AddSkippedRecords(results.Count - finalResults.Count);
				var errors = new List<DataChange<TDest>>(finalResults.Where(c => c.Type == DataChangeType.Error));
				if (finalResults.Count > 0)
				{
					progress.AddResultRange(finalResults.Where(c => c.Type == DataChangeType.Error).Select(c => c.Type));
					var updateResult = dest.Update(finalResults.Where(c => c.Type != DataChangeType.Error));

					for (int i = 0; i < updateResult.Count; i++)
					{
						if (updateResult[i] == DataChangeType.Error)
						{
							errors.Add(new DataChange<TDest>
							{
								Type = DataChangeType.Error,
								Target = finalResults[i].Target
							});
						}
					}
					progress.AddResultRange(updateResult);
				}

				step.OnAfterUpdate(page, errors);

				runtime.ReportProgress(progress);
				retryCount = 0;
			}
			catch (Exception e)
			{
				retryCount++;
				logger.LogError(e.ToString());
			}
		}
	}
}