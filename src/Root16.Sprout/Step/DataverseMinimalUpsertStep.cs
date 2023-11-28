using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Root16.Sprout.Extensions;
using Root16.Sprout.Processors;
using Root16.Sprout.Progress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Root16.Sprout.Step
{

 //   public abstract class DataverseMinimalUpsertStep<TSource> : DataverseDestinationProcessor<TSource>
	//{
	//	public DataverseMinimalUpsertStep(ILogger<DataverseMinimalUpsertStep<TSource>> logger) : base(logger)
	//	{
	//	}

	//	protected abstract IReadOnlyList<Entity> GetMatchingEntities(IReadOnlyCollection<TSource> sourceRecords);
	//	protected IReadOnlyList<Entity> MatchingEntities { get; set; }

	//	public virtual string MatchEntityOn(Entity entity)
	//	{
	//		return entity.Id.ToString();
	//	}

	//	public override void OnBeforeMap(IReadOnlyList<TSource> sourceRecords)
	//	{
	//		MatchingEntities = GetMatchingEntities(sourceRecords);
	//	}

	//	protected abstract Entity MapRecordToEntity(TSource record);

	//	public override IReadOnlyList<DataChange<Entity>> MapRecord(TSource source)
	//	{
	//		var target = MapRecordToEntity(source);
	//		if (target == null) return null;

	//		return new[] {
	//			new DataChange<Entity>
	//			{
	//				Type = DataChangeType.Create,
	//				Target = target
	//			}
	//		};
	//	}

	//	protected virtual IReadOnlyList<Entity> FindMatchingEntities(Entity change)
	//	{
	//		string key = MatchEntityOn(change);
	//		List<Entity> matches = new List<Entity>();
	//		if (key != null)
	//		{
	//			foreach (var potentialMatch in MatchingEntities)
	//			{
	//				if (StringComparer.CurrentCultureIgnoreCase.Equals(key, MatchEntityOn(potentialMatch)))
	//				{
	//					matches.Add(potentialMatch);
	//				}
	//			}
	//		}

	//		if (matches.Count > 1)
	//		{
	//			Logger.LogError($"Multiple matches for {change.LogicalName} ({key}).");
	//		}

	//		return matches;
	//	}

	//	public override IReadOnlyList<DataChange<Entity>> OnBeforeUpdate(IReadOnlyList<DataChange<Entity>> changes)
	//	{
	//		var results = new List<DataChange<Entity>>();

	//		StringBuilder sb = new StringBuilder();

	//		foreach (var change in changes)
	//		{
	//			var matches = FindMatchingEntities(change.Target);

	//			if (matches.Count > 0)
	//			{
	//				if (matches.Count > 1)
	//				{
	//					results.Add(new DataChange<Entity> { Type = DataChangeType.Error, Target = change.Target });
	//					continue;
	//				}

	//				var match = matches[0];
	//				change.Target.Id = match.Id;
	//				var delta = ReduceEntityChanges(change.Target, match);
	//				if (delta != null && delta.Attributes.Count > 0)
	//				{
	//					results.Add(new DataChange<Entity> { Type = DataChangeType.Update, Target = delta });
	//					if (Logger.IsEnabled(LogLevel.Debug))
	//					{
	//						Logger.LogDebug(delta.FormatChanges(match));
	//					}
	//				}
	//			}
	//			else
	//			{
	//				var delta = ReduceEntityChanges(change.Target, null);

	//				if (delta != null && delta.Attributes.Count > 0)
	//				{
	//					results.Add(new DataChange<Entity> { Type = DataChangeType.Create, Target = delta });

	//					if (Logger.IsEnabled(LogLevel.Debug))
	//					{
	//						sb.Clear();
	//						sb.AppendLine($"creating ({delta.LogicalName}):");
	//						foreach (var attribute in delta.Attributes)
	//						{
	//							sb.AppendLine($"    {attribute.Key}: - => {SproutExtensions.DisplayAttributeValue(attribute.Value)}");
	//						}
	//						Logger.LogDebug(sb.ToString());
	//					}
	//				}
	//			}
	//		}

	//		return results;
	//	}

	//	protected virtual Entity ReduceEntityChanges(Entity updates, Entity original)
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
	//		return updates.GetDelta(original);
	//	}
	//}
}
