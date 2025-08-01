using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Root16.Sprout.Extensions;
using System.Text;

namespace Root16.Sprout.DataSources.Dataverse;

public class EntityOperationReducer(ILogger<EntityOperationReducer> logger)
{
    private IEnumerable<Entity>? potentialMatches;
    private readonly ILogger<EntityOperationReducer> logger = logger;

    public void SetPotentialMatches(IEnumerable<Entity> entities)
    {
        this.potentialMatches = entities;
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
            return changes.ToList();
        }

        Dictionary<string, List<Entity>> potentialMatchDict = potentialMatches.GroupBy(x => keySelector(x)).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.FromComparison(stringComparison));

        var results = new List<DataOperation<Entity>>();

        StringBuilder sb = new();

        foreach (var change in changes)
        {
            if (change is null) continue;

            var matches = potentialMatchDict.GetValue(keySelector(change.Data));

            if (matches is not null && matches.Count != 0 && (change.OperationType.Equals("Update", StringComparison.OrdinalIgnoreCase) || change.OperationType.Equals("Create", StringComparison.OrdinalIgnoreCase)))
            {
                if (matches.Count > 1)
                {
                    results.Add(new DataOperation<Entity>("Error", change.Data));
                    continue;
                }

                var match = matches[0];
                change.Data.Id = match.Id;
                var delta = ReduceEntityChanges(change.Data, match);
                if (delta is not null && (delta.Attributes.Count > 1
                    || (delta.Attributes.Count == 1 && !delta.Attributes.Contains("createdon"))))
                {
                    results.Add(new DataOperation<Entity>("Update", delta));
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug(delta.FormatChanges(match));
                    }
                }
            }
            else if (change.OperationType.Equals("Create", StringComparison.OrdinalIgnoreCase))
            {
                var delta = ReduceEntityChanges(change.Data, null);

                if (delta is not null && delta.Attributes.Count > 0)
                {
                    results.Add(new DataOperation<Entity>("Create", delta));

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