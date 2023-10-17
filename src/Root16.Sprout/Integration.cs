using Microsoft.Extensions.Logging;
using Root16.Sprout.Data;
using Root16.Sprout.Extensions;
using Root16.Sprout.Progress;
using Root16.Sprout.Step;
using Root16.Sprout.Strategy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Root16.Sprout;

internal class Integration : IIntegrationRuntime, IIntegrationBuilder
{
	private OrderedDictionary steps;
	private Dictionary<string, IDataSource> dataSources;
	private List<IProgressListener> progressListeners;
	private ILoggerFactory loggerFactory;
	private bool disposeLoggerFactory;

	internal Integration()
	{
		steps = new OrderedDictionary();
		dataSources = new Dictionary<string, IDataSource>(StringComparer.CurrentCultureIgnoreCase);
		progressListeners = new List<IProgressListener>();
		Variables = new Dictionary<String, Object>(StringComparer.CurrentCultureIgnoreCase);
	}

	public IIntegationStrategy DefaultStrategy { get; private set; }

	public IDictionary<string, object> Variables { get; }

	public IIntegrationRuntime Create()
	{
		if (DefaultStrategy == null)
		{
			DefaultStrategy = new BulkIntegrationStrategy(CreateLogger<BulkIntegrationStrategy>());
		}
		return this;
	}

	public ILogger<T> CreateLogger<T>()
	{
		if (loggerFactory == null)
		{
			loggerFactory = LoggerFactory.Create(ConfigureDefaultLogging);
			disposeLoggerFactory = true;
			OnLoggingConfigured();
		}

		return loggerFactory.CreateLogger<T>();
	}

	private void ConfigureDefaultLogging(ILoggingBuilder builder)
	{
		builder
			.AddFilter("Microsoft", LogLevel.Warning)
			.AddFilter("System", LogLevel.Warning)
			.AddFilter("Root16.Sprout", LogLevel.Information)
			.AddConsole();
	}

	public IIntegrationBuilder UseLoggerFactory(ILoggerFactory loggerFactory)
	{
		this.loggerFactory = loggerFactory;
		disposeLoggerFactory = false;
		OnLoggingConfigured();
		return this;
	}

	public IIntegrationBuilder UseLoggerFactory(Action<ILoggingBuilder> configure)
	{
		loggerFactory = LoggerFactory.Create(builder =>
		{
			ConfigureDefaultLogging(builder);
			configure(builder);
		});

		disposeLoggerFactory = true;
		OnLoggingConfigured();

		return this;
	}

	private void OnLoggingConfigured()
	{
		if (progressListeners.Count == 0)
		{
			this.AddConsoleProgressListener();
			this.AddLoggingProgressListener();
		}
	}

	public IIntegrationBuilder UseDefaultStrategy<T>() where T : IIntegationStrategy
	{
		return UseDefaultStrategy<T>(null);
	}

	public IIntegrationBuilder UseDefaultStrategy<T>(Action<T> configure) where T : IIntegationStrategy
	{
		DefaultStrategy = (T)Activator.CreateInstance(typeof(T), CreateLogger<T>());

		configure?.Invoke((T)DefaultStrategy);

		return this;
	}

	public IIntegrationBuilder AddDataSource(string name, IDataSource dataSource)
	{
		dataSources.Add(name, dataSource);
		return this;
	}

	public IIntegrationBuilder AddStep(string name, IIntegrationStep step)
	{
		step.Name = name;
		steps.Add(name, step);
		return this;
	}

	public void RunAllSteps()
	{
		ReportRunStart();
		foreach (DictionaryEntry entry in steps)
		{
			var step = ((IIntegrationStep)entry.Value);
			ReportStepStart(step.Name);
			step.Run(this);
			ReportStepComplete(step.Name);
		}
		ReportRunComplete();
	}

	public T GetDataSource<T>(string name) where T : IDataSource
	{
		return (T)dataSources[name];
	}

	public T GetDataSource<T>() where T : IDataSource
	{
		return dataSources.Values.OfType<T>().Single();
	}

	private void ReportRunStart()
	{
		foreach (var listener in progressListeners)
		{
			listener.OnRunStart(this);
		}
	}

	public void ReportProgress(IntegrationProgress progress)
	{
		foreach (var listener in progressListeners)
		{
			listener.OnProgressChange(this, progress);
		}

	}

	private void ReportStepStart(string name)
	{
		foreach (var listener in progressListeners)
		{
			listener.OnStepStart(this, name);
		}
	}

	private void ReportStepComplete(string name)
	{
		foreach (var listener in progressListeners)
		{
			listener.OnStepComplete(this, name);
		}
	}

	private void ReportRunComplete()
	{
		foreach (var listener in progressListeners)
		{
			listener.OnRunComplete(this);
		}
	}

	public IIntegrationBuilder AddProgressListener(IProgressListener listener)
	{
		progressListeners.Add(listener);
		return this;
	}

	public void Dispose()
	{
		if (disposeLoggerFactory)
		{
			loggerFactory.Dispose();
		}
	}

	public IIntegrationBuilder ClearProgressListeners()
	{
		progressListeners.Clear();
		return this;
	}
}
