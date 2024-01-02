using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using System.Text;

namespace Root16.Sprout.DataSources.Dataverse;

public class EntityOperationReducer
{
    private IEnumerable<Entity>? entities;
    private readonly ILogger<EntityOperationReducer> logger;

    public EntityOperationReducer(ILogger<EntityOperationReducer> logger)
    {
        this.logger = logger;
    }

    public void SetPotentialMatches(IEnumerable<Entity> entities)
    {
        this.entities = entities;
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

    public IReadOnlyList<DataOperation<Entity>> ReduceOperations(IEnumerable<DataOperation<Entity>> changes, Func<Entity, string> keySelector)
    {
        return ReduceOperations(changes, (e1, e2) => StringComparer.OrdinalIgnoreCase.Equals(keySelector(e1), keySelector(e2)));
    }

    public IReadOnlyList<DataOperation<Entity>> ReduceOperations(IEnumerable<DataOperation<Entity>> changes, Func<Entity,Entity,bool> entityEqualityComparer)
    {
        if (entities is null)
        {
            return changes.ToList();
        }

        var results = new List<DataOperation<Entity>>();
        
        StringBuilder sb = new();

        foreach (var change in changes)
        {
            if (change is null) continue;

            var matches = entities.Where(e => entityEqualityComparer(e, change.Data)).ToList();

            if (matches.Count > 0)
            {
                if (matches.Count > 1)
                {
                    results.Add(new DataOperation<Entity>("Error", change.Data));
                    continue;
                }

                var match = matches[0];
                change.Data.Id = match.Id;
                var delta = ReduceEntityChanges(change.Data, match);
                if (delta is not null && delta.Attributes.Count > 0)
                {
                    results.Add(new DataOperation<Entity>("Update", delta));
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug(delta.FormatChanges(match));
                    }
                }
            }
            else
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
