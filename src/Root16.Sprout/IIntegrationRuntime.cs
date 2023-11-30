using Root16.Sprout.Data;
using Root16.Sprout.Progress;
using Root16.Sprout.Step;
using System;
using System.Collections.Generic;

namespace Root16.Sprout;

public interface IIntegrationRuntime
{
	Task RunAllStepsAsync();
	Task RunStepAsync(string name);
	IEnumerable<string> GetStepNames();
    Task RunStepAsync<TStep>() where TStep : class, IIntegrationStep;
}
