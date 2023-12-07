namespace Root16.Sprout;

public interface IIntegrationRuntime
{
	Task RunAllStepsAsync();
	Task RunStepAsync(string name);
	IEnumerable<string> GetStepNames();
    Task RunStepAsync<TStep>() where TStep : class, IIntegrationStep;
}
