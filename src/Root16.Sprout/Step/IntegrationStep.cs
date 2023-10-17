using Microsoft.Extensions.Logging;
using Root16.Sprout.Data;
using Root16.Sprout.Progress;
using Root16.Sprout.Query;
using System;
using System.Collections.Generic;

namespace Root16.Sprout.Step;

public interface IIntegrationStep
{
	string Name { get; set; }

	void Run(IIntegrationRuntime runtime);
}

public abstract class IntegrationStep : IIntegrationStep
{
	protected ILogger<IntegrationStep> Logger { get; }

	public IntegrationStep(ILogger<IntegrationStep> logger)
	{
		Name = GetType().Name;
		Logger = logger;
	}

	public string Name { get; set; }

	public abstract void Run(IIntegrationRuntime runtime);
}

public interface IIntegrationStep<TSource, TDest> : IIntegrationStep
{
	IPagedQuery<TSource> GetSourceQuery(IIntegrationRuntime runtime);
	IDataSink<TDest> GetDataSink(IIntegrationRuntime runtime);
	IReadOnlyList<DataChange<TDest>> MapRecord(TSource source);
	void OnBeforeMap(IReadOnlyList<TSource> sourceRecords);
	IReadOnlyList<DataChange<TDest>> OnBeforeUpdate(IReadOnlyList<DataChange<TDest>> destRecords);
	void OnAfterUpdate(IReadOnlyList<TSource> sourceRecords, IReadOnlyList<DataChange<TDest>> errors);
}

public abstract class IntegrationStep<TSource, TDest> : IntegrationStep, IIntegrationStep<TSource, TDest>
{
	private readonly ILogger<IntegrationStep<TSource, TDest>> logger;

	protected IntegrationStep(ILogger<IntegrationStep<TSource, TDest>> logger) : base(logger)
	{
		this.logger = logger;
	}

	public override void Run(IIntegrationRuntime runtime)
	{
		runtime.DefaultStrategy.Migrate(runtime, this);
	}

	public abstract IDataSink<TDest> GetDataSink(IIntegrationRuntime runtime);

	public abstract IPagedQuery<TSource> GetSourceQuery(IIntegrationRuntime runtime);

	public abstract IReadOnlyList<DataChange<TDest>> MapRecord(TSource source);

	public virtual void OnBeforeMap(IReadOnlyList<TSource> sourceRecords)
	{
	}

	public virtual IReadOnlyList<DataChange<TDest>> OnBeforeUpdate(IReadOnlyList<DataChange<TDest>> destRecords)
	{
		return destRecords;
	}

	public virtual void OnAfterUpdate(IReadOnlyList<TSource> sourceRecords, IReadOnlyList<DataChange<TDest>> errors)
	{
	}
}

