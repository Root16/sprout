using Microsoft.Extensions.Logging;
using Root16.Sprout.DependencyInjection;
using Root16.Sprout.Progress;
using Root16.Sprout.SpectreConsole.ProgressColumns;
using Spectre.Console;
using System.Collections.Concurrent;

namespace Root16.Sprout.SpectreConsole;

public class SpectreConsoleProgressListener(ILogger<SpectreConsoleProgressListener> logger, IEnumerable<StepRegistration> stepRegistrations, TimeSpan? refreshRate = null) : IProgressListener
{
    private readonly ConcurrentDictionary<string, ProgressTask> _progressTasks = new(StringComparer.OrdinalIgnoreCase);

    // Always use RunContinuationsAsynchronously to prevent hijacking the setter's thread
    private readonly TaskCompletionSource _initialized = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _runCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private Task? _consoleTask;
    private ProgressContext? _progressContext;
    private readonly object _lock = new();

    public async Task OnRunStart(IList<string> stepNames)
    {
        CreateSpectreConsole(stepNames);
        await _initialized.Task;
    }

    public async Task OnStepStart(string name)
    {
        if (_consoleTask == null)
        {
            CreateSpectreConsole([]);
            await _initialized.Task;
        }

        // Dynamically add the step if it wasn't registered during OnRunStart
        if (!_progressTasks.TryGetValue(name, out var task))
        {
            lock (_lock)
            {
                // Since we are inside the lock, avoid the GetOrAdd delegate allocation
                if (!_progressTasks.TryGetValue(name, out task))
                {
                    task = _progressContext!.AddTask(name, autoStart: false);
                    _progressTasks.TryAdd(name, task);
                }
            }
        }

        if (!task.IsStarted)
        {
            task.StartTask();
        }
    }

    public Task OnProgressChange(IntegrationProgress progress)
    {
        if (_progressTasks.TryGetValue(progress.StepName, out var progressTask))
        {
            if (progress.TotalRecordCount.HasValue)
            {
                progressTask.IsIndeterminate = false;
                progressTask.MaxValue = progress.TotalRecordCount.Value;
            }
            else
            {
                progressTask.IsIndeterminate = true;
            }

            progressTask.Value = progress.ProcessedRecordCount;
        }
        return Task.CompletedTask;
    }

    public Task OnStepComplete(string name)
    {
        if (_progressTasks.TryGetValue(name, out var progressTask) && !progressTask.IsFinished)
        {
            progressTask.StopTask();
        }
        return Task.CompletedTask;
    }

    public async Task OnRunComplete()
    {
        // Stop any unfinished tasks so the console context can complete
        foreach (var task in _progressTasks.Values)
        {
            if (!task.IsFinished)
            {
                task.StopTask();
            }
        }

        // Instantly signal the StartAsync delegate to finish
        _runCompleted.TrySetResult();

        if (_consoleTask != null)
        {
            await _consoleTask;
        }
    }

    private void CreateSpectreConsole(IList<string> stepNames)
    {
        if (_consoleTask != null)
        {
            return;
        }

        lock (_lock)
        {
            if (_consoleTask != null)
            {
                return;
            }

            // If refreshRate has a value, we turn OFF AutoRefresh so we can control it manually.
            bool useAutoRefresh = !refreshRate.HasValue;

            _consoleTask = AnsiConsole.Progress()
                .AutoClear(false)
                .AutoRefresh(useAutoRefresh)
                .Columns(
                [
                    new StepDescriptionColumn(),    // Task description
                    new ProgressBarColumn(),        // Progress bar
                    new PercentageColumn(),         // Percentage
                    new RemainingTimeColumn(),      // Remaining time
                    new SpinnerColumn(),
                ])
                .StartAsync(async ctx =>
                {
                    _progressContext = ctx;
                    _progressTasks.TryAdd("migration", ctx.AddTask("Running Step(s)...", autoStart: true).IsIndeterminate());

                    foreach (var step in stepNames)
                    {
                        _progressTasks.TryAdd(step, ctx.AddTask(step, false));
                    }

                    _initialized.TrySetResult();

                    if (refreshRate.HasValue)
                    {
                        // Manual refresh loop using the provided TimeSpan
                        while (!_runCompleted.Task.IsCompleted)
                        {
                            ctx.Refresh();
                            await Task.WhenAny(Task.Delay(refreshRate.Value), _runCompleted.Task);
                        }

                        // Final refresh to ensure we display 100% completion
                        ctx.Refresh();
                    }
                    else
                    {
                        // Default Spectre background refresh behavior
                        await _runCompleted.Task;
                    }
                });
        }
    }
}