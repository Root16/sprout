namespace Root16.Sprout;

public interface IIntegrationRuntime
{
    IEnumerable<string> GetStepNames();
    Task<string> RunStepAsync(string name, Action<IIntegrationStep>? stepConfigurator = null);
    Task<string> RunStepAsync<TStep>(Action<IIntegrationStep>? stepConfigurator = null) where TStep : class, IIntegrationStep;
    Task RunAllStepsAsync(int maxDegreesOfParallelism = 1, Action<string>? completionHandler = null);
}
