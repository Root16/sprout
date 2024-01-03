namespace Root16.Sprout;

public interface IIntegrationRuntime
{
    Task<string> RunStepAsync<TStep>() where TStep : class, IIntegrationStep;
	Task<string> RunStepAsync(string name);
	IEnumerable<string> GetStepNames();
    Task RunAllStepsAsync();
    IAsyncEnumerable<string> RunAllStepsWithDependenciesOneAtATime();
    IAsyncEnumerable<string> RunAllStepsAtTheSameTime();
    IAsyncEnumerable<string> RunAllStepsWithDependenciesAtTheSameTime();
    IAsyncEnumerable<string> RunAllStepsWithDependenciesSetAmountAtATime(int amount = 5);
}
