using System;
using System.Collections.Generic;
using System.Text;

namespace Root16.Sprout.Progress;

public class ConsoleProgressListener : IProgressListener
{
	public void OnRunStart(IMigrationRuntime runtime)
	{
	}

	public void OnStepStart(IMigrationRuntime runtime, string name)
	{
	}

	public void OnProgressChange(IMigrationRuntime runtime, MigrationProgress progress)
	{
		Console.CursorLeft = 0;
		Console.Write(progress.ToString());
	}

	public void OnStepComplete(IMigrationRuntime runtime, string name)
	{
		Console.WriteLine();
	}

	public void OnRunComplete(IMigrationRuntime runtime)
	{
	}
}
