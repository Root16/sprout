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
        var completedStepNames = new HashSet<string>(StringComparer.Ordinal);
        var queuedSteps = new List<DelayedStep>();
        var runningSteps = new List<Task<string>>();

        while (queuedSteps.Count != 0 || runningSteps.Count != 0 || waitingSteps.Count != 0)
        {
            queuedSteps.AddRange(waitingSteps.Where(s => s.StepRegistration.PrerequisteSteps.TrueForAll(preReq => completedStepNames.Contains(preReq))));
            waitingSteps = [.. waitingSteps.Except(queuedSteps)];
            int available = maxDegreesOfParallelism - runningSteps.Count;
            var newRunningSteps = queuedSteps
                .Take(available)
                .Select(x => maxDegreesOfParallelism > 1
                    ? Task.Run(() => x.StepRunner(x.StepRegistration))
                    : x.StepRunner(x.StepRegistration))
                .ToList();
            runningSteps.AddRange(newRunningSteps);
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
                var stepToUpdate = stepRegistrations.FirstOrDefault(x => x.Name.Equals(preRegStep));
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

    /// <summary>
    /// Checks for steps that will not run due to missing prerequisites or circular dependencies.
    /// </summary>
    /// <returns>A Hashset of all of the steps that will not run</returns>
    private HashSet<string> CheckForStepsThatWillNotRun()
    {
        HashSet<string> stepsThatWontRun = new(StringComparer.Ordinal);
        HashSet<string> registeredNames = new(stepRegistrations.Select(r => r.Name), StringComparer.Ordinal);

        // Check for steps with missing prerequisites
        foreach (var reg in stepRegistrations)
        {
            if (!reg.PrerequisteSteps.TrueForAll(p => registeredNames.Contains(p)))
            {
                stepsThatWontRun.Add(reg.Name);
            }
        }

        // Check for circular dependencies using BFS on a directed graph representation of the steps
        Dictionary<string, int> stepToDegree = new(StringComparer.Ordinal);
        foreach (var reg in stepRegistrations)
        {
            // Count the number of prerequisites for each step
            stepToDegree[reg.Name] = reg.PrerequisteSteps.Count(p => registeredNames.Contains(p));
        }

        // Initialize a queue with steps that have no prerequisites (degree 0)
        Queue<string> queue = new(stepToDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));
        HashSet<string> processedSteps = new(StringComparer.Ordinal);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            processedSteps.Add(current);

            // For each dependent step of the current step, reduce its degree and enqueue it if it becomes 0
            var currentReg = stepRegistrations.First(r => r.Name.Equals(current, StringComparison.Ordinal));
            foreach (var dependent in currentReg.DependentSteps)
            {
                if (stepToDegree.TryGetValue(dependent, out int value))
                {
                    stepToDegree[dependent] = --value;
                    if (stepToDegree[dependent] == 0)
                    {
                        queue.Enqueue(dependent);
                    }
                }
            }
        }

        // Any steps that were not processed are part of a circular dependency or have unmet prerequisites
        foreach (var reg in stepRegistrations)
        {
            if (!processedSteps.Contains(reg.Name))
            {
                stepsThatWontRun.Add(reg.Name);
            }
        }

        return stepsThatWontRun;
    }
}
