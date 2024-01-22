namespace Root16.Sprout;

public interface IIntegrationRuntime
{
    Task<string> RunStepAsync<TStep>() where TStep : class, IIntegrationStep;
	Task<string> RunStepAsync(string name);
	IEnumerable<string> GetStepNames();
    Task RunAllStepsAsync(int maxDegreesOfParallelism = 1, Action<string>? completionHandler = null);
}
