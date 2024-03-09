namespace Root16.Sprout.Progress;

public class ConsoleProgressListener : IProgressListener
{
	public Task OnRunStart(IList<string> stepNames)
	{
		return Task.CompletedTask;
	}

	public Task OnStepStart(string name)
	{
		return Task.CompletedTask;
	}

	public Task OnProgressChange(IntegrationProgress progress)
	{
		Console.CursorLeft = 0;
		Console.Write(progress.ToString());
		return Task.CompletedTask;
	}

	public Task OnStepComplete(string name)
	{
		Console.WriteLine();
		return Task.CompletedTask;
	}

	public Task OnRunComplete()
	{
		return Task.CompletedTask;
	}
}
