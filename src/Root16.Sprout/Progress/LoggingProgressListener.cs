using Microsoft.Extensions.Logging;

namespace Root16.Sprout.Progress;


public class LoggingProgressListener(ILogger<LoggingProgressListener> logger) : IProgressListener
{
	private readonly ILogger<LoggingProgressListener> logger = logger;

    public Task OnRunStart(IList<string> stepNames)
	{
		logger.LogInformation($"Starting run...");
        return Task.CompletedTask;
    }

	public Task OnStepStart(string name)
	{
		logger.LogInformation($"Step {name} starting...");
        return Task.CompletedTask;
    }

	public Task OnProgressChange(IntegrationProgress progress)
	{
		logger.LogDebug(progress.ToString());
        return Task.CompletedTask;
    }

	public Task OnStepComplete(string name)
	{
		logger.LogInformation($"Step {name} complete.");
        return Task.CompletedTask;
    }

	public Task OnRunComplete()
	{
		logger.LogInformation($"Run complete.");
		return Task.CompletedTask;
	}
}
