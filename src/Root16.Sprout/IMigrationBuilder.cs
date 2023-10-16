using Microsoft.Extensions.Logging;
using Root16.Sprout.Data;
using Root16.Sprout.Progress;
using Root16.Sprout.Step;
using Root16.Sprout.Strategy;
using System;

namespace Root16.Sprout;

public interface IMigrationBuilder
{
	IMigrationBuilder AddDataSource(string name, IDataSource dataSource);
	IMigrationBuilder ClearProgressListeners();
	IMigrationBuilder AddProgressListener(IProgressListener listener);
	IMigrationBuilder AddStep(string name, IMigrationStep step);
	IMigrationRuntime Create();
	ILogger<T> CreateLogger<T>();
	IMigrationBuilder UseDefaultStrategy<T>() where T : IMigrationStrategy;
	IMigrationBuilder UseDefaultStrategy<T>(Action<T> configure) where T : IMigrationStrategy;
	IMigrationBuilder UseLoggerFactory(ILoggerFactory loggerFactory);
	IMigrationBuilder UseLoggerFactory(Action<ILoggingBuilder> configure);
}
