using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Root16.Sprout.Data;
using Root16.Sprout.Extensions;
using Root16.Sprout.Processors;
using Root16.Sprout.Progress;
using Root16.Sprout.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Root16.Sprout.Step;


//public class DataverseIntegrationStep<TSource> : IBatchIntegrationStep<TSource,Entity>
//{
//    private readonly ILogger<DataverseIntegrationStep<TSource>> logger;
//    private IReadOnlyList<Entity>? matchingEntities;

//    public DataverseIntegrationStep(ILogger<DataverseIntegrationStep<TSource>> logger)
//	{
//        this.logger = logger;
//    }

//	protected IReadOnlyList<Entity> GetMatchingEntities(IReadOnlyCollection<TSource> sourceRecords)
//	{
//		throw new NotImplementedException();
//	}

//	public virtual string MatchEntityOn(Entity entity)
//	{
//		return entity.Id.ToString();
//	}

//	public void OnBeforeMap(IReadOnlyList<TSource> sourceRecords)
//	{
//		matchingEntities = GetMatchingEntities(sourceRecords);
//	}

//	protected Entity MapRecordToEntity(TSource record)
//	{
//		throw new NotImplementedException();
//	}

//	public IReadOnlyList<DataOperation<Entity>> MapRecord(TSource source)
//	{
//		var target = MapRecordToEntity(source);
//		if (target == null) return Array.Empty<DataOperation<Entity>>();

//		return new[] {
//			new DataOperation<Entity>(
//				"Create",
//				target
//			)
//		};
//	}

//	protected virtual IReadOnlyList<Entity> FindMatchingEntities(Entity change)
//	{
//		string key = MatchEntityOn(change);
//		List<Entity> matches = new();
//		if (key != null)
//		{
//			foreach (var potentialMatch in matchingEntities)
//			{
//				if (StringComparer.CurrentCultureIgnoreCase.Equals(key, MatchEntityOn(potentialMatch)))
//				{
//					matches.Add(potentialMatch);
//				}
//			}
//		}

//		if (matches.Count > 1)
//		{
//			logger.LogError($"Multiple matches for {change.LogicalName} ({key}).");
//		}

//		return matches;
//	}

//	public IReadOnlyList<DataOperation<Entity>> OnBeforeUpdate(IReadOnlyList<DataOperation<Entity>> changes)
//	{
//		var results = new List<DataOperation<Entity>>();

//		StringBuilder sb = new();

//		foreach (var change in changes)
//		{
//			if (change == null) continue;

//			var matches = FindMatchingEntities(change.Data);

//			if (matches.Count > 0)
//			{
//				if (matches.Count > 1)
//				{
//					results.Add(new DataOperation<Entity>("Error", change.Data));
//					continue;
//				}

//				var match = matches[0];
//				change.Data.Id = match.Id;
//				var delta = ReduceEntityChanges(change.Data, match);
//				if (delta != null && delta.Attributes.Count > 0)
//				{
//					results.Add(new DataOperation<Entity>("Update", delta));
//					if (logger.IsEnabled(LogLevel.Debug))
//					{
//						logger.LogDebug(delta.FormatChanges(match));
//					}
//				}
//			}
//			else
//			{
//				var delta = ReduceEntityChanges(change.Data, null);

//				if (delta != null && delta.Attributes.Count > 0)
//				{
//					results.Add(new DataOperation<Entity>("Create", delta));

//					if (logger.IsEnabled(LogLevel.Debug))
//					{
//						sb.Clear();
//						sb.AppendLine($"creating ({delta.LogicalName}):");
//						foreach (var attribute in delta.Attributes)
//						{
//							sb.AppendLine($"    {attribute.Key}: - => {SproutExtensions.DisplayAttributeValue(attribute.Value)}");
//						}
//						logger.LogDebug(sb.ToString());
//					}
//				}
//			}
//		}

//		return results;
//	}

//	protected virtual Entity ReduceEntityChanges(Entity updates, Entity? original)
//	{
//		if (original == null)
//		{
//			if (updates.Attributes.ContainsKey("createdon"))
//			{
//				updates["overriddencreatedon"] = updates["createdon"];
//				updates.Attributes.Remove("createdon");
//			}
//			return updates;
//		}

//		updates.Attributes.Remove("overriddencreatedon");
//		return updates.CloneWithModifiedAttributes(original);
//	}

//    public IPagedQuery<TSource> GetSourceQuery()
//    {
//        throw new NotImplementedException();
//    }

//    public IDataOperationEndpoint<Entity> GetDataSink()
//    {
//		// TODO: find the default dataverse datasource
//        throw new NotImplementedException();
//    }

//    public void OnAfterUpdate(IReadOnlyList<TSource> sourceRecords, IReadOnlyList<DataOperation<Entity>> errors)
//    {
//    }
//}
