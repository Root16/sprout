using System;
using System.Collections.Generic;
using System.Text;

namespace Root16.Sprout.Progress;

public class ConsoleProgressListener : IProgressListener
{
	public void OnRunStart(IIntegrationRuntime runtime)
	{
	}

	public void OnStepStart(IIntegrationRuntime runtime, string name)
	{
	}

	public void OnProgressChange(IIntegrationRuntime runtime, IntegrationProgress progress)
	{
		Console.CursorLeft = 0;
		Console.Write(progress.ToString());
	}

	public void OnStepComplete(IIntegrationRuntime runtime, string name)
	{
		Console.WriteLine();
	}

	public void OnRunComplete(IIntegrationRuntime runtime)
	{
	}
}
