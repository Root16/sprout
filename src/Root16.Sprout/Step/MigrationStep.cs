using Microsoft.Extensions.Logging;
using Root16.Sprout.Data;
using Root16.Sprout.Progress;
using Root16.Sprout.Query;
using System;
using System.Collections.Generic;

namespace Root16.Sprout.Step;

public interface IMigrationStep
{
	string Name { get; set; }

	void Run(IMigrationRuntime runtime);
}

public abstract class MigrationStep : IMigrationStep
{
	protected ILogger<MigrationStep> Logger { get; }

	public MigrationStep(ILogger<MigrationStep> logger)
	{
		Name = GetType().Name;
		Logger = logger;
	}

	public string Name { get; set; }

	public abstract void Run(IMigrationRuntime runtime);
}

public interface IMigrationStep<TSource, TDest> : IMigrationStep
{
	IPagedQuery<TSource> GetSourceQuery(IMigrationRuntime runtime);
	IDataSink<TDest> GetDataSink(IMigrationRuntime runtime);
	IReadOnlyList<DataChange<TDest>> MapRecord(TSource source);
	void OnBeforeMap(IReadOnlyList<TSource> sourceRecords);
	IReadOnlyList<DataChange<TDest>> OnBeforeUpdate(IReadOnlyList<DataChange<TDest>> destRecords);
	void OnAfterUpdate(IReadOnlyList<TSource> sourceRecords, IReadOnlyList<DataChange<TDest>> errors);
}

public abstract class MigrationStep<TSource, TDest> : MigrationStep, IMigrationStep<TSource, TDest>
{
	private readonly ILogger<MigrationStep<TSource, TDest>> logger;

	protected MigrationStep(ILogger<MigrationStep<TSource, TDest>> logger) : base(logger)
	{
		this.logger = logger;
	}

	public override void Run(IMigrationRuntime runtime)
	{
		runtime.DefaultStrategy.Migrate(runtime, this);
	}

	public abstract IDataSink<TDest> GetDataSink(IMigrationRuntime runtime);

	public abstract IPagedQuery<TSource> GetSourceQuery(IMigrationRuntime runtime);

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

