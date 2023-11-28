using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Root16.Sprout.Data;
using Root16.Sprout.Extensions;
using Root16.Sprout.Step;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Root16.Sprout.Processors;

//public class DataverseDestinationProcessor<TInput> : BatchProcessor<TInput,Entity>
//{
//    public DataverseDestinationProcessor(ILogger<DataverseDestinationProcessor<TInput>> logger) : base(logger)
//    {
//    }
//    protected virtual DataverseDataSource GetDataverseDataSource(IIntegrationRuntime runtime)
//    {
//        return runtime.GetDataverseDataSource();
//    }

//    public bool DryRun { get; set; }
//    public bool BypassCustomPluginExecution { get; set; }
//    public DataverseDataSink DataverseDataSink { get; protected set; }
//    public DataverseDataSource DataverseDataSource { get; protected set; }

//    public override IDataOperationEndpoint<Entity> GetDataSink(IIntegrationRuntime runtime)
//    {
//        DataverseDataSink = DataverseDataSource.CreateDataSink();
//        DataverseDataSink.DryRun = DryRun;
//        DataverseDataSink.BypassCustomPluginExecution = BypassCustomPluginExecution;
//        DataverseDataSink.OnError += OnDataverseDataSyncError;
//        return DataverseDataSink;
//    }

//    protected virtual void OnDataverseDataSyncError(object sender, DataverseDataSinkError e)
//    {
//    }

//    public override void Run(IIntegrationRuntime runtime)
//    {
//        DataverseDataSource = GetDataverseDataSource(runtime);
//        base.Run(runtime);
//    }

//    protected IDictionary<string, Entity[]> CreateLookupTable(IReadOnlyList<Entity> entities, Func<Entity, string> keySelector)
//    {
//        var groups = entities.GroupBy(keySelector, StringComparer.CurrentCultureIgnoreCase);
//        return groups.ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.CurrentCultureIgnoreCase);
//    }

//}
