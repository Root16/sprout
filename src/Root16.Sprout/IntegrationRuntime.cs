using Microsoft.Extensions.DependencyInjection;
using Root16.Sprout.DependencyInjection;
using Root16.Sprout.Progress;
using Root16.Sprout.Extensions;

namespace Root16.Sprout;

public class IntegrationRuntime : IIntegrationRuntime
{
    private readonly IEnumerable<StepRegistration> stepRegistrations = new List<StepRegistration>();
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IProgressListener progressListener;
    private List<string> _finishedSteps = new();
    private List<string> _queuedOrRunningSteps = new();

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

    public async Task RunAllStepsAsync()
    {
        progressListener.OnRunStart();
        foreach(var reg in stepRegistrations)
        {
            await RunStepAsync(reg);
        }
        progressListener.OnRunComplete();
    }


    //Assumes you have no dependencies
    //We can probably have it do a check and throw if there are dependencies and this is called same with RunAllStepsAsync above
    public async IAsyncEnumerable<string> RunAllStepsAtTheSameTime()
    {
        //1) Start all of the steps
        //2) Pass the list of running tasks to extension method
        //3) Wait for value to be returned
        progressListener.OnRunStart();
        var runningTasks = stepRegistrations.Select(x => RunStepAsync(x)).ToList();
        await foreach (var finishedStep in runningTasks.StreamFinishedTasksAllRunning())
        {
            yield return finishedStep;
        }
        progressListener.OnRunComplete();
    }

    public async IAsyncEnumerable<string> RunAllStepsWithDependenciesOneAtATime()
    {
        // Call RunAllStepsWithDependenciesSetAmountAtATime passing 1 as the amount
        await foreach (var finishedStep in RunAllStepsWithDependenciesSetAmountAtATime(1))
        {
            yield return finishedStep;
        }
    }

    public async IAsyncEnumerable<string> RunAllStepsWithDependenciesAtTheSameTime()
    {
        //1) Get steps with no dependencies
        //2) Start those steps
        //3) Add those steps to list of steps that are running
        //4) Call Extension Method and wait for returned value
        //5) Add the step to the list of steps that are finished
        //6) Get steps that are not running, finished, and the dependent steps are all in the finished steps list
        //7) Start new steps
        //8) Add them to list of running steps
        //9) Add names to list of queuedOrRunning steps
        progressListener.OnRunStart();
        var initialStepsToRun = stepRegistrations.Where(x => !x.DependentSteps.Any());
        var runningTasks = initialStepsToRun.Select(x => RunStepAsync(x)).ToList();
        _queuedOrRunningSteps.AddRange(initialStepsToRun.Select(x => x.Name));
        await foreach (var finishedStep in runningTasks.StreamFinishedTasksAllRunning())
        {
            yield return finishedStep;
            _finishedSteps.Add(finishedStep);
            _queuedOrRunningSteps.Remove(finishedStep);
            //Get me all of the steps that are not finished, not running or queued, and all of the items in the dependentSteps list are in the finishedSteps list
            var nextSteps = stepRegistrations.Where(x => !_finishedSteps.Contains(x.Name) && !_queuedOrRunningSteps.Contains(x.Name) && x.DependentSteps.TrueForAll(x => _finishedSteps.Contains(x)));
            runningTasks.AddRange(nextSteps.Select(x => RunStepAsync(x)));
            _queuedOrRunningSteps.AddRange(nextSteps.Select(x => x.Name));
        }
        progressListener.OnRunComplete();
    }

    public async IAsyncEnumerable<string> RunAllStepsWithDependenciesSetAmountAtATime(int amount = default)
    {
        //1) Get steps with no dependencies
        //2) Pass the list to extension method and wait for returned value
        //3) Add returned value to list of finished steps
        //4) Get steps that are not running, or finished, and the dependent steps are all in the finished steps list
        //5) Add those steps to the list of steps to run
        if (amount == default) amount = 5;
        progressListener.OnRunStart();
        // Get all of the Steps that aren't dependent on anything create a delegate func that will call RunStepAsync when its the steps turn.
        List<KeyValuePair<StepRegistration, Func<StepRegistration, Task<string>>>> tasksToRun = stepRegistrations.Where(x => !x.DependentSteps.Any()).Select(x => CreateKeyValForRun(x)).ToList();
        await foreach (var finishedStep in tasksToRun.StreamFinishedTasksWithSpecificAmount(amount))
        {
            yield return finishedStep;
            _finishedSteps.Add(finishedStep);
            _queuedOrRunningSteps.Remove(finishedStep);
            //Get all of the steps that are not finished, not running or queued, and all of the items in the dependentSteps list are in the finishedSteps list
            var nextSteps = stepRegistrations.Where(x => !_finishedSteps.Contains(x.Name) && !_queuedOrRunningSteps.Contains(x.Name) && x.DependentSteps.TrueForAll(x => _finishedSteps.Contains(x)));
            tasksToRun.AddRange(nextSteps.Select(x => CreateKeyValForRun(x)));
        }
        progressListener.OnRunComplete();

        var stepsNotRun = stepRegistrations.Where(x => !_finishedSteps.Contains(x.Name)).Select(x => x.Name);

        Console.WriteLine($"Steps that were not run: {string.Join(", ", stepsNotRun)}");
    }

    private KeyValuePair<StepRegistration,Func<StepRegistration,Task<string>>> CreateKeyValForRun(StepRegistration registration)
    {
        //Add step name to queued list...probably should be somewhere else
        //Create key value pair that can be used to start step when the step is up to run
        _queuedOrRunningSteps.Add(registration.Name);
        return new KeyValuePair<StepRegistration, Func<StepRegistration,Task<string>>>(registration, RunStepAsync);
    }

    private void CheckRegistrations()
    {
        var duplicateRegistrations = string.Join(", ", stepRegistrations.GroupBy(x => x.Name).Where(x => x.Count()>1).Select(x => x.Key));

        if (!string.IsNullOrEmpty(duplicateRegistrations))
        {
            throw new InvalidDataException($"Steps can only be registered once! Please remove duplication registrations. The below registrations are duplicated:\n\t{duplicateRegistrations}");
        }
    }
}