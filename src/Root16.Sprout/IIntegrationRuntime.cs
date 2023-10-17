using Root16.Sprout.Data;
using Root16.Sprout.Progress;
using Root16.Sprout.Strategy;
using System;
using System.Collections.Generic;

namespace Root16.Sprout;

public interface IIntegrationRuntime : IDisposable
{
	IIntegationStrategy DefaultStrategy { get; }
	T GetDataSource<T>() where T : IDataSource;
	T GetDataSource<T>(string name) where T : IDataSource;
	void ReportProgress(IntegrationProgress progress);
	void RunAllSteps();
	IDictionary<string, object> Variables { get; }
}
