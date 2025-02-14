using Microsoft.Extensions.DependencyInjection;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DependencyInjection;
using Root16.Sprout.Progress;

namespace Root16.Sprout;


public class IntegrationRuntime : IIntegrationRuntime
{
    private readonly IEnumerable<StepRegistration> stepRegistrations = [];
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IProgressListener progressListener;
    private readonly IEnumerable<IIntegrationStep> _steps;

    public IntegrationRuntime(IEnumerable<StepRegistration> stepRegistrations, IEnumerable<IIntegrationStep> steps, IServiceScopeFactory serviceScopeFactory, IProgressListener progressListener)
    {
        this.stepRegistrations = stepRegistrations;
        _steps = steps;
        this.serviceScopeFactory = serviceScopeFactory;
        this.progressListener = progressListener;
        CheckRegistrations();
        BuildDependencyTree();
    }

    public async Task<string> RunStepAsync(string name, Action<IIntegrationStep>? stepConfigurator = null)
    {
        var reg = stepRegistrations.FirstOrDefault(step => step.Name == name) ?? throw new InvalidOperationException($"Step named '{name}' is not registered.");
        await RunStepAsync(reg, stepConfigurator);
        return reg.Name;
    }

    public async Task<string> RunStepAsync<TStep>(Action<IIntegrationStep>? stepConfigurator = null) where TStep : class, IIntegrationStep
    {
        var reg = stepRegistrations.FirstOrDefault(step => step.StepType == typeof(TStep)) ?? throw new InvalidOperationException($"Step of type '{typeof(TStep)}' is not registered.");
        await RunStepAsync(reg, stepConfigurator);
        return reg.Name;
    }

    private async Task<string> RunStepAsync(StepRegistration reg, Action<IIntegrationStep>? stepConfigurator = null)
    {
        progressListener.OnStepStart(reg.Name);
        using var scope = serviceScopeFactory.CreateScope();
        var step = (IIntegrationStep)scope.ServiceProvider.GetRequiredKeyedService(reg.StepType, reg.Name);
        stepConfigurator?.Invoke(step);
        await step.RunAsync(reg.Name);
        progressListener.OnStepComplete(reg.Name);
        return reg.Name;
    }

    public IEnumerable<string> GetStepNames() => stepRegistrations.Select(reg => reg.Name);

    public async Task RunAllStepsAsync(int maxDegreesOfParallelism = 1, Action<string>? completionHandler = null)
    {
        CheckStepDependencyTree();
        progressListener.OnRunStart();
        var waitingSteps = stepRegistrations.Select(reg => new DelayedStep(reg, RunStepAsync)).ToList();
        var completedStepNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queuedSteps = new List<DelayedStep>();
        var runningSteps = new List<Task<string>>();

        while (queuedSteps.Count != 0 || runningSteps.Count != 0 || waitingSteps.Count != 0)
        {
            queuedSteps.AddRange(waitingSteps.Where(s => s.StepRegistration.PrerequisteSteps.TrueForAll(preReq => completedStepNames.Contains(preReq))));
            waitingSteps = waitingSteps.Except(queuedSteps).ToList();
            int available = maxDegreesOfParallelism - runningSteps.Count;
            runningSteps.AddRange(queuedSteps.Take(available).Select(x => x.StepRunner(x.StepRegistration)));
            queuedSteps.RemoveRange(0, Math.Min(queuedSteps.Count, available));
            var finishedFunction = await Task.WhenAny(runningSteps);
            runningSteps.Remove(finishedFunction);
            var stepName = await finishedFunction;
            completedStepNames.Add(stepName);
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

    private void BuildDependencyTree()
    {
        foreach (var stepReg in stepRegistrations)
        {
            var preRegSteps = stepReg.PrerequisteSteps;

            foreach (var preRegStep in preRegSteps)
            {
                var stepToUpdate = stepRegistrations.FirstOrDefault(x => x.Name.Equals(preRegStep, StringComparison.OrdinalIgnoreCase));
                stepToUpdate?.DependentSteps.Add(stepReg.Name);
            }
        }
    }

    private void CheckStepDependencyTree()
    {
        var stepsThatWontRun = CheckForStepsThatWillNotRun();

        if (stepsThatWontRun.Count != 0)
        {
            throw new InvalidDataException($"Unreachable steps found: {string.Join(", ", stepsThatWontRun)}");
        }
    }

    private HashSet<string> CheckForStepsThatWillNotRun()
    {
        List<string> stepsThatWontRun =
        [
            .. stepRegistrations.Where(x => x.PrerequisteSteps.Intersect(x.DependentSteps).Any()).Select(x => x.Name),
        ];
        stepsThatWontRun.AddRange(GetAllStepsThatWontRun(stepsThatWontRun));
        stepsThatWontRun.AddRange(stepRegistrations.Where(x => !x.PrerequisteSteps.TrueForAll(x => stepRegistrations.Select(x => x.Name).Contains(x))).Select(x => x.Name));
        return [.. stepsThatWontRun];
    }

    private List<string> GetAllStepsThatWontRun(List<string> stepsThatWontRun)
    {
        if (stepsThatWontRun.Count == 0)
        {
            return [];
        }
        var steps = new List<string>();
        var newStepsThatWontRun = stepRegistrations
            .Where(x => !stepsThatWontRun.Contains(x.Name))
            .Where(x => x.PrerequisteSteps.Intersect(stepsThatWontRun).Any())
            .Select(x => x.Name)
            .ToList();

        steps.AddRange(newStepsThatWontRun);
        steps.AddRange([.. GetAllStepsThatWontRun(steps)]);
        return steps;
    }
}
