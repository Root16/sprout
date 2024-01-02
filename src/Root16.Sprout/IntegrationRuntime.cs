using Microsoft.Extensions.DependencyInjection;
using Root16.Sprout.DependencyInjection;
using Root16.Sprout.Progress;

namespace Root16.Sprout;

public class IntegrationRuntime : IIntegrationRuntime
{
    private readonly IEnumerable<StepRegistration> stepRegistrations;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IProgressListener progressListener;

    public IntegrationRuntime(IEnumerable<StepRegistration> stepRegistrations, IServiceScopeFactory serviceScopeFactory, IProgressListener progressListener)
    {
        this.stepRegistrations = stepRegistrations;
        this.serviceScopeFactory = serviceScopeFactory;
        this.progressListener = progressListener;
    }

    public async Task<string> RunStepAsync(string name)
    {
        var reg = stepRegistrations.FirstOrDefault(step => step.Name == name);
        if (reg is null) throw new InvalidOperationException($"Step named '{name}' is not registered.");
        await RunStepAsync(reg);
        return reg.Name;
    }

    public async Task<string> RunStepAsync<TStep>() where TStep : class, IIntegrationStep
    {
        var reg = stepRegistrations.FirstOrDefault(step => step.StepType == typeof(TStep));
        if (reg is null) throw new InvalidOperationException($"Step of type '{typeof(TStep)}' is not registered.");
        await RunStepAsync(reg);
        return reg.Name;
    }

    private async Task<string> RunStepAsync(StepRegistration reg)
    {
        progressListener.OnStepStart(reg.Name);
        using var scope = serviceScopeFactory.CreateScope();
        var step = (IIntegrationStep)scope.ServiceProvider.GetRequiredService(reg.StepType);
        await step.RunAsync();
        progressListener.OnStepComplete(reg.Name);
        return reg.Name;
    }

    public IEnumerable<string> GetStepNames() => stepRegistrations.Select(reg => reg.Name);

    public async Task RunAllStepsAsync()
    {
        progressListener.OnRunStart();
        foreach(var reg in stepRegistrations)
        {
            await RunStepAsync(reg);
        }
        progressListener.OnRunComplete();
    }
}