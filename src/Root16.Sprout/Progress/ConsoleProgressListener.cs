using System;
using System.Collections.Generic;
using System.Text;

namespace Root16.Sprout.Progress;

public class ConsoleProgressListener : IProgressListener
{
	public void OnRunStart()
	{
	}

	public void OnStepStart(string name)
	{
	}

	public void OnProgressChange(IntegrationProgress progress)
	{
		Console.CursorLeft = 0;
		Console.Write(progress.ToString());
	}

	public void OnStepComplete(string name)
	{
		Console.WriteLine();
	}

	public void OnRunComplete()
	{
	}
}
