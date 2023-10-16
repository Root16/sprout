using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Root16.Sprout.Progress;


public class LoggingProgressListener : IProgressListener
{
	private readonly ILogger<LoggingProgressListener> logger;

	public LoggingProgressListener(ILogger<LoggingProgressListener> logger)
	{
		this.logger = logger;
	}

	public void OnRunStart(IMigrationRuntime runtime)
	{
		logger.LogInformation($"Starting run...");
	}

	public void OnStepStart(IMigrationRuntime runtime, string name)
	{
		logger.LogInformation($"Step {name} starting...");
	}

	public void OnProgressChange(IMigrationRuntime runtime, MigrationProgress progress)
	{
		logger.LogDebug(progress.ToString());
	}

	public void OnStepComplete(IMigrationRuntime runtime, string name)
	{
		logger.LogInformation($"Step {name} complete.");
	}

	public void OnRunComplete(IMigrationRuntime runtime)
	{
		logger.LogInformation($"Run complete.");
	}
}
