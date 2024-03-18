using Microsoft.Extensions.Logging;
using Root16.Sprout.SpectreConsole.ProgressColumns;
using Spectre.Console;
using System;

namespace Root16.Sprout.Progress.SpectreConsole;

public class SpectreConsoleProgressListener(ILogger<SpectreConsoleProgressListener> logger) : IProgressListener
{
    private readonly ILogger<SpectreConsoleProgressListener> _logger = logger;
    private readonly List<KeyValuePair<string, ProgressTask>> _progressTasks = [];

    public Task OnRunStart(IList<string> stepNames)
    {
        CreateSpectreConsole(stepNames);
        return Task.CompletedTask;
    }

    public Task OnStepStart(string name)
    {
        var task = _progressTasks.FirstOrDefault(x => x.Key.Equals(name)).Value;
        if(!task.IsStarted)
        {
            task.StartTask();
        }
        return Task.CompletedTask;
    }

    public Task OnProgressChange(IntegrationProgress progress)
    {
        var progressTask = _progressTasks.First(x => x.Key.Equals(progress.StepName)).Value;
        progressTask.MaxValue = progress.TotalRecordCount is null ? default : (double)progress.TotalRecordCount;
        progressTask.Value = progress.ProcessedRecordCount;
        return Task.CompletedTask;
    }

    public Task OnStepComplete(string name)
    {
        var progressTask = _progressTasks.First(x => x.Key.Equals(name)).Value;
        if (!progressTask.IsFinished)
        {
            progressTask.StopTask();
        }
        return Task.CompletedTask;
    }

    public async Task OnRunComplete()
    {
        _progressTasks.First(x => x.Key.Equals("migration", StringComparison.OrdinalIgnoreCase)).Value.StopTask();
    }

    private void CreateSpectreConsole(IList<string> stepNames)
    {
        //Only create console the first time this is called
        if (_progressTasks.Count != 0) return;
        AnsiConsole.Progress()
            .AutoClear(false)
            .AutoRefresh(true)
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
                _progressTasks.Add(new KeyValuePair<string, ProgressTask>("migration", ctx.AddTask("Running Migration...", autoStart: true).IsIndeterminate()));
                _progressTasks.AddRange(stepNames.Select(x => new KeyValuePair<string, ProgressTask>(x, ctx.AddTask(x, false))));
                await Task.Run(() =>
                {
                    while (!ctx.IsFinished)
                    {

                    }
                });
            });
    }
}
