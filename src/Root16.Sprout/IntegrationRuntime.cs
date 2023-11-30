using Microsoft.Extensions.DependencyInjection;
using Root16.Sprout.DependencyInjection;
using Root16.Sprout.Step;

namespace Root16.Sprout;

public class IntegrationRuntime : IIntegrationRuntime
{
    private readonly IEnumerable<StepRegistration> stepRegistrations;
    private readonly IServiceScopeFactory serviceScopeFactory;

    public IntegrationRuntime(IEnumerable<StepRegistration> stepRegistrations, IServiceScopeFactory serviceScopeFactory)
    {
        this.stepRegistrations = stepRegistrations;
        this.serviceScopeFactory = serviceScopeFactory;
    }

    public async Task RunStepAsync(string name)
    {
        var reg = stepRegistrations.FirstOrDefault(step => step.Name == name);
        if (reg == null) throw new InvalidOperationException($"Step named '{name}' is not registered.");

        await RunStepAsync(reg);
    }

    public async Task RunStepAsync<TStep>() where TStep : class, IIntegrationStep
    {
        var reg = stepRegistrations.FirstOrDefault(step => step.StepType == typeof(TStep));
        if (reg == null) throw new InvalidOperationException($"Step of type '{typeof(TStep)}' is not registered.");

        await RunStepAsync(reg);
    }

    private async Task RunStepAsync(StepRegistration reg)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var step = (IIntegrationStep)scope.ServiceProvider.GetRequiredService(reg.StepType);
        await step.RunAsync();
    }

    public IEnumerable<string> GetStepNames() => stepRegistrations.Select(reg => reg.Name);

    public async Task RunAllStepsAsync()
    {
        foreach(var reg in stepRegistrations)
        {
            await RunStepAsync(reg);
        }
    }

}