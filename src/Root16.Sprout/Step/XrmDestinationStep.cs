using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Root16.Sprout.Data;
using Root16.Sprout.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Root16.Sprout.Step;

public abstract class XrmDestinationStep<TSource> : MigrationStep<TSource, Entity>
{
	public XrmDestinationStep(ILogger<XrmDestinationStep<TSource>> logger) : base(logger)
	{

	}
	protected virtual XrmDataSource GetXrmDataSource(IMigrationRuntime runtime)
	{
		return runtime.GetXrmDataSource();
	}

	public bool DryRun { get; set; }
	public bool BypassCustomPluginExecution { get; set; }
	public XrmDataSink XrmDataSink { get; protected set; }
	public XrmDataSource XrmDataSource { get; protected set; }

	public override IDataSink<Entity> GetDataSink(IMigrationRuntime runtime)
	{
		XrmDataSink = XrmDataSource.CreateDataSink();
		XrmDataSink.DryRun = this.DryRun;
		XrmDataSink.BypassCustomPluginExecution = BypassCustomPluginExecution;
		XrmDataSink.OnError += OnXrmDataSyncError;
		return XrmDataSink;
	}

	protected virtual void OnXrmDataSyncError(object sender, XrmDataSinkError e)
	{
	}

	public override void Run(IMigrationRuntime runtime)
	{
		XrmDataSource = GetXrmDataSource(runtime);
		base.Run(runtime);
	}

	protected IDictionary<string, Entity[]> CreateLookupTable(IReadOnlyList<Entity> entities, Func<Entity, string> keySelector)
	{
		var groups = entities.GroupBy(keySelector, StringComparer.CurrentCultureIgnoreCase);
		return groups.ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.CurrentCultureIgnoreCase);
	}

}
