using Microsoft.Extensions.DependencyInjection;
using Root16.Sprout.DependencyInjection;
using Root16.Sprout.Progress;
using Root16.Sprout.Extensions;
using static Root16.Sprout.IntegrationRuntime;

namespace Root16.Sprout;


public class IntegrationRuntime : IIntegrationRuntime
{
    private readonly IEnumerable<StepRegistration> stepRegistrations = new List<StepRegistration>();
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IProgressListener progressListener;

    public IntegrationRuntime(IEnumerable<StepRegistration> stepRegistrations, IServiceScopeFactory serviceScopeFactory, IProgressListener progressListener)
    {
        this.stepRegistrations = stepRegistrations;
        this.serviceScopeFactory = serviceScopeFactory;
        this.progressListener = progressListener;
        CheckRegistrations();
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

    public async Task RunAllStepsAsync(int maxDegreesOfParallelism = 1, Action<string>? completionHandler = null)
    {
        progressListener.OnRunStart();

        var waitingSteps = stepRegistrations.Select(reg => new DelayedStep(reg, RunStepAsync)).ToList();

        var completedSteps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var readySteps = new List<DelayedStep>();

        var runningSteps = new List<Task<string>>();

        while (readySteps.Any() || runningSteps.Any() || waitingSteps.Any())
        {
            readySteps.AddRange(waitingSteps.Where(s => s.StepRegistration.PrerequisteSteps.All(preReq => completedSteps.Contains(preReq))));
            waitingSteps = waitingSteps.Except(readySteps).ToList();

            int available = maxDegreesOfParallelism - runningSteps.Count;
            runningSteps.AddRange(readySteps.Take(available).Select(x => x.StepRunner(x.StepRegistration)));
            readySteps.RemoveRange(0, Math.Min(readySteps.Count, available));

            if (!runningSteps.Any())
            {
                throw new InvalidOperationException($"Unreachable steps found: {string.Join(", ", waitingSteps.Select(s => s.StepRegistration.Name))}");
            }

            var finishedFunction = await Task.WhenAny(runningSteps);
            runningSteps.Remove(finishedFunction);

            var stepName = await finishedFunction;
            completedSteps.Add(stepName);
            completionHandler?.Invoke(stepName);
        }
        
        progressListener.OnRunComplete();
    }

    private void CheckRegistrations()
    {
        var duplicateRegistrations = string.Join(", ", stepRegistrations.GroupBy(x => x.Name).Where(x => x.Count() > 1).Select(x => x.Key));

        if (!string.IsNullOrEmpty(duplicateRegistrations))
        {
            throw new InvalidDataException($"Steps can only be registered once! Please remove duplication registrations. The below registrations are duplicated:\n\t{duplicateRegistrations}");
        }
    }
}