using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.Text;

namespace Root16.Sprout.Dataverse.DataStores;

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

    public IReadOnlyList<OrganizationRequest> ReduceOperations(IEnumerable<OrganizationRequest> changes, Func<Entity, string> keySelector)
    {
        return ReduceOperations(changes, (e1, e2) => StringComparer.OrdinalIgnoreCase.Equals(keySelector(e1), keySelector(e2)));
    }

    private Entity? GetEntity(OrganizationRequest organizationRequest)
    {
        if (organizationRequest is CreateRequest createRequest)
        {
            return createRequest.Target;
        }
        else if (organizationRequest is UpdateRequest updateRequest)
        {
            return updateRequest.Target;
        }

        return null;
    }
    public IReadOnlyList<OrganizationRequest> ReduceOperations(IEnumerable<OrganizationRequest> changes, Func<Entity, Entity, bool> entityEqualityComparer)
    {
        if (entities is null)
        {
            return changes.ToList();
        }

        var results = new List<OrganizationRequest>();

        StringBuilder sb = new();

        foreach (var change in changes)
        {
            var entity = GetEntity(change);

            if (entity is null) continue;

            var matches = entities.Where(e => entityEqualityComparer(e, entity)).ToList();

            if (matches.Any() && (change is UpdateRequest || change is CreateRequest))
            {
                if (matches.Count > 1)
                {
                    logger.LogError($"Multiple matches found for entity {entity.LogicalName}, {entity.Id}");
                    continue;
                }

                var match = matches[0];
                entity.Id = match.Id;
                var delta = ReduceEntityChanges(entity, match);
                if (delta is not null && delta.Attributes.Count > 0)
                {
                    results.Add(new UpdateRequest { Target = delta });
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug(delta.FormatChanges(match));
                    }
                }
            }
            else if (change is CreateRequest)
            {
                var delta = ReduceEntityChanges(entity, null);

                if (delta is not null && delta.Attributes.Count > 0)
                {
                    results.Add(new CreateRequest { Target = delta });

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