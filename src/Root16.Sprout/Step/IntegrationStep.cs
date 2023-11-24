using Microsoft.Extensions.Logging;
using Root16.Sprout.Data;
using Root16.Sprout.Progress;
using Root16.Sprout.Query;
using Root16.Sprout.Strategy;
using System;
using System.Collections.Generic;

namespace Root16.Sprout.Step;

public interface IIntegrationStep
{
	Task RunAsync();
}

public abstract class IntegrationStep : IIntegrationStep
{
	public IntegrationStep()
	{
	}

	public abstract Task RunAsync();
}

public interface IIntegrationStep<TSource, TDest> : IIntegrationStep
{
	IPagedQuery<TSource> GetSourceQuery();
	IDataSink<TDest> GetDataSink();
	IReadOnlyList<DataChange<TDest>> MapRecord(TSource source);
	void OnBeforeMap(IReadOnlyList<TSource> sourceRecords);
	IReadOnlyList<DataChange<TDest>> OnBeforeUpdate(IReadOnlyList<DataChange<TDest>> destRecords);
	void OnAfterUpdate(IReadOnlyList<TSource> sourceRecords, IReadOnlyList<DataChange<TDest>> errors);
}

public abstract class IntegrationStep<TSource, TDest> : IntegrationStep, IIntegrationStep<TSource, TDest>
{
    private readonly IIntegationStrategy integationStrategy;
    private readonly ILogger<IntegrationStep<TSource, TDest>> logger;

	protected IntegrationStep(IIntegationStrategy integationStrategy,  ILogger<IntegrationStep<TSource, TDest>> logger)
	{
        this.integationStrategy = integationStrategy;
        this.logger = logger;
	}

	public override async Task RunAsync()
	{
		integationStrategy.Migrate(this);
	}

	public abstract IDataSink<TDest> GetDataSink();

	public abstract IPagedQuery<TSource> GetSourceQuery();

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

