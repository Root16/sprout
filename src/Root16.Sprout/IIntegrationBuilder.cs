using Microsoft.Extensions.Logging;
using Root16.Sprout.Data;
using Root16.Sprout.Progress;
using Root16.Sprout.Step;
using Root16.Sprout.Strategy;
using System;

namespace Root16.Sprout;

public interface IIntegrationBuilder
{
	IIntegrationBuilder AddDataSource(string name, IDataSource dataSource);
	IIntegrationBuilder ClearProgressListeners();
	IIntegrationBuilder AddProgressListener(IProgressListener listener);
	IIntegrationBuilder AddStep(string name, IIntegrationStep step);
	IIntegrationRuntime Create();
	ILogger<T> CreateLogger<T>();
	IIntegrationBuilder UseDefaultStrategy<T>() where T : IIntegationStrategy;
	IIntegrationBuilder UseDefaultStrategy<T>(Action<T> configure) where T : IIntegationStrategy;
	IIntegrationBuilder UseLoggerFactory(ILoggerFactory loggerFactory);
	IIntegrationBuilder UseLoggerFactory(Action<ILoggingBuilder> configure);
}
