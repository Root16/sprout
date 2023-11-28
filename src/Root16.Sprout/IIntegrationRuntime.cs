using Root16.Sprout.Data;
using Root16.Sprout.Progress;
using System;
using System.Collections.Generic;

namespace Root16.Sprout;

public interface IIntegrationRuntime
{
	Task RunAllStepsAsync();
	Task RunStepAsync(string name);
	IEnumerable<string> GetStepNames();
	
}
