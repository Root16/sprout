using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Root16.Sprout.Extensions;
using System.Text;

namespace Root16.Sprout.DataSources.Dataverse;

public class EntityOperationReducer(
    ILogger<EntityOperationReducer> logger,
    EntityBatchAnalyzer analyzer
    )
{
    private List<Entity>? potentialMatches = [];
    private readonly ILogger<EntityOperationReducer> logger = logger;

    public void SetPotentialMatches(IEnumerable<Entity> entities)
    {
        potentialMatches = [.. entities];
    }

    private Entity ReduceEntityChanges(Entity updates, Entity? original)
    {
        if (original is null)
        {
            if (updates.Attributes.ContainsKey("createdon"))
            {
                updates["overriddencreatedon"] = updates["createdon"];
                updates.Attributes.Remove("createdon");
            }
            return updates;
        }

        updates.Attributes.Remove("overriddencreatedon");
        return updates.CloneWithModifiedAttributes(original);
    }

    public IReadOnlyList<DataOperation<Entity>> ReduceOperations(IEnumerable<DataOperation<Entity>> changes, Func<Entity, string> keySelector, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
    {
        if (potentialMatches is null || !potentialMatches.Any())
        {
            return [..changes];
        }

        ILookup<string, Entity> potentialMatchLookup = potentialMatches.ToLookup(keySelector, StringComparer.FromComparison(stringComparison));

        var results = new List<DataOperation<Entity>>();

        StringBuilder sb = new();

        foreach (var change in changes)
        {
            if (change is null) continue;

            var altKey = keySelector(change.Data);
            var matches = potentialMatchLookup[altKey].ToList();

            if (change.OperationType.Equals("Update", StringComparison.OrdinalIgnoreCase)
                && (matches is null || matches.Count == 0 || change.Data.Id == Guid.Empty))
            {
                results.Add(new DataOperation<Entity>("Skip", change.Data));
            }
            else if (matches is not null && matches.Count != 0
                && (change.OperationType.Equals("Update", StringComparison.OrdinalIgnoreCase) || change.OperationType.Equals("Create", StringComparison.OrdinalIgnoreCase)))
            {
                if (matches.Count > 1)
                {
                    results.Add(new DataOperation<Entity>("Error", change.Data));
                    continue;
                }

                var match = matches[0];
                change.Data.Id = match.Id;
                var delta = ReduceEntityChanges(change.Data, match);
                var audit = analyzer.GetDifference(altKey, delta, match);
                if (delta is not null && (delta.Attributes.Count > 1 || (delta.Attributes.Count == 1 && !delta.Contains("createdon"))))
                {
                    results.Add(new DataOperation<Entity>("Update", delta, audit));
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug(delta.FormatChanges(match));
                    }
                }
                else if (delta is null || delta.Attributes.Count == 0)
                {
                    results.Add(new DataOperation<Entity>("Skip", change.Data, audit));
                }
            }
            else if (change.OperationType.Equals("Create", StringComparison.OrdinalIgnoreCase))
            {
                var delta = ReduceEntityChanges(change.Data, null);
                var audit = analyzer.GetDifference(altKey, delta);

                if (delta is not null && delta.Attributes.Count > 0)
                {
                    results.Add(new DataOperation<Entity>("Create", delta, audit));

                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        sb.Clear();
                        sb.AppendLine($"creating ({delta.LogicalName}):");
                        foreach (var attribute in delta.Attributes)
                        {
                            sb.AppendLine($"    {attribute.Key}: - => {EntityExtensions.DisplayAttributeValue(attribute.Value)}");
                        }
                        logger.LogDebug(sb.ToString());
                    }
                }
            }

        }

        return results;
    }
}