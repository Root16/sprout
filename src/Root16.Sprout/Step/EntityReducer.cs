using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Root16.Sprout.Extensions;
using Root16.Sprout.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Root16.Sprout.Step;

public class EntityReducer
{
    private IEnumerable<Entity>? entities;
    private readonly ILogger<EntityReducer> logger;

    public EntityReducer(ILogger<EntityReducer> logger)
    {
        this.logger = logger;
    }

    public void SetPotentialMatches(IEnumerable<Entity> entities)
    {
        this.entities = entities;
    }

    private Entity ReduceEntityChanges(Entity updates, Entity? original)
    {
        if (original == null)
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

    public IReadOnlyList<DataOperation<Entity>> ReduceChanges(IEnumerable<DataOperation<Entity>> changes, Func<Entity, string> keySelector)
    {
        return ReduceChanges(changes, (e1, e2) => StringComparer.OrdinalIgnoreCase.Equals(keySelector(e1), keySelector(e2)));
    }

    public IReadOnlyList<DataOperation<Entity>> ReduceChanges(IEnumerable<DataOperation<Entity>> changes, Func<Entity,Entity,bool> entityEqualityComparer)
    {
        if (entities == null)
        {
            return changes.ToList();
        }

        var results = new List<DataOperation<Entity>>();
        
        StringBuilder sb = new();

        foreach (var change in changes)
        {
            if (change == null) continue;

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
                if (delta != null && delta.Attributes.Count > 0)
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

                if (delta != null && delta.Attributes.Count > 0)
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
