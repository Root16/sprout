using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Root16.Sprout.Data;
using Root16.Sprout.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Root16.Sprout.Step;

public abstract class DataverseDestinationStep<TSource> : MigrationStep<TSource, Entity>
{
	public DataverseDestinationStep(ILogger<DataverseDestinationStep<TSource>> logger) : base(logger)
	{

	}
	protected virtual DataverseDataSource GetDataverseDataSource(IMigrationRuntime runtime)
	{
		return runtime.GetDataverseDataSource();
	}

	public bool DryRun { get; set; }
	public bool BypassCustomPluginExecution { get; set; }
	public DataverseDataSink DataverseDataSink { get; protected set; }
	public DataverseDataSource DataverseDataSource { get; protected set; }

	public override IDataSink<Entity> GetDataSink(IMigrationRuntime runtime)
	{
		DataverseDataSink = DataverseDataSource.CreateDataSink();
		DataverseDataSink.DryRun = this.DryRun;
		DataverseDataSink.BypassCustomPluginExecution = BypassCustomPluginExecution;
		DataverseDataSink.OnError += OnDataverseDataSyncError;
		return DataverseDataSink;
	}

	protected virtual void OnDataverseDataSyncError(object sender, DataverseDataSinkError e)
	{
	}

	public override void Run(IMigrationRuntime runtime)
	{
		DataverseDataSource = GetDataverseDataSource(runtime);
		base.Run(runtime);
	}

	protected IDictionary<string, Entity[]> CreateLookupTable(IReadOnlyList<Entity> entities, Func<Entity, string> keySelector)
	{
		var groups = entities.GroupBy(keySelector, StringComparer.CurrentCultureIgnoreCase);
		return groups.ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.CurrentCultureIgnoreCase);
	}

}
