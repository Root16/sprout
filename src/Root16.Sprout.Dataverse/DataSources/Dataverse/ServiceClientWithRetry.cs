using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using AuthenticationType = Microsoft.PowerPlatform.Dataverse.Client.AuthenticationType;

namespace Root16.Sprout.Dataverse.DataSources.Dataverse;

public class ServiceClientWithRetry : IOrganizationServiceAsync2
{
    public ServiceClient InnerClient { get; private set; }
    private ILogger? Logger { get; set; }

    public ServiceClientWithRetry(string dataverseConnectionString, ILogger? logger = null)
    {
        Logger = logger;
        InnerClient = new ServiceClient(dataverseConnectionString, logger ?? null);
    }

    public ServiceClientWithRetry(ServiceClient serviceClient, ILogger? logger = null)
    {
        Logger = logger;
        InnerClient = serviceClient;
    }

    /// <summary>
    /// Defaults to 10
    /// </summary>
    public int MaxRetryCount
    {
        get => InnerClient.MaxRetryCount;
        set => InnerClient.MaxRetryCount = value;
    }
    /// <summary>
    /// Defaults to 5 seconds
    /// </summary>
    public TimeSpan RetryPauseTime
    {
        get => InnerClient.RetryPauseTime;
        set => InnerClient.RetryPauseTime = value;
    }

    public AuthenticationType ActiveAuthenticationType => InnerClient.ActiveAuthenticationType;
    public string Authority => InnerClient.Authority;
    public Guid? CallerAADObjectId
    {
        get => InnerClient.CallerAADObjectId;
        set => InnerClient.CallerAADObjectId = value;
    }
    public Guid CallerId
    {
        get => InnerClient.CallerId;
        set => InnerClient.CallerId = value;
    }
    public string CurrentAccessToken => InnerClient.CurrentAccessToken;
    public bool EnableAffinityCookie
    {
        get => InnerClient.EnableAffinityCookie;
        set => InnerClient.EnableAffinityCookie = value;
    }
    public string EnvironmentId => InnerClient.EnvironmentId;
    public string OAuthUserId => InnerClient.OAuthUserId;
    public int RecommendedDegreesOfParallelism => InnerClient.RecommendedDegreesOfParallelism;
    public Guid TenantId => InnerClient.TenantId;
    public bool UseWebApi
    {
        get => InnerClient.UseWebApi;
        set => InnerClient.UseWebApi = value;
    }

    public ServiceClientWithRetry? Clone()
    {
        ServiceClient cloned = InnerClient.Clone();
        if (cloned == null)
        {
            return null;
        }
        return new(cloned, Logger);
    }

    private OrganizationResponse ExecuteWithRetry(OrganizationRequest request)
    {
        int retryCount = 0;
        Exception? lastException = null;

        do
        {
            try
            {
                if (retryCount > 0)
                {
                    Task.Delay(retryCount * RetryPauseTime).Wait();
                }
                return InnerClient.Execute(request);
            }
            catch (FaultException fault)
            when (fault.Message.Contains("Database is currently unavailable", StringComparison.OrdinalIgnoreCase))
            {
                WriteExceptionToLog(fault, lastException);
                lastException = fault;
            }
            catch (FaultException<OrganizationServiceFault>) { throw; }
            catch (Exception ex)
            {
                WriteExceptionToLog(ex, lastException);
                lastException = ex;
            }
        } while (retryCount++ < MaxRetryCount);

        throw lastException;
    }

    private async Task<OrganizationResponse> ExecuteWithRetryAsync(OrganizationRequest request, CancellationToken cancellationToken)
    {
        int retryCount = 0;
        Exception? lastException = null;

        do
        {
            try
            {
                if (retryCount > 0)
                {
                    await Task.Delay(retryCount * RetryPauseTime, cancellationToken);
                }
                return await InnerClient.ExecuteAsync(request, cancellationToken);
            }
            catch (FaultException fault)
            when (fault.Message.Contains("Database is currently unavailable", StringComparison.OrdinalIgnoreCase))
            {
                WriteExceptionToLog(fault, lastException);
                lastException = fault;
            }
            catch (FaultException<OrganizationServiceFault>) { throw; }
            catch (Exception ex)
            {
                WriteExceptionToLog(ex, lastException);
                lastException = ex;
            }
        } while (retryCount++ < MaxRetryCount);

        throw lastException!;
    }

    private void WriteExceptionToLog(Exception ex, Exception? lastException)
    {
        if (lastException is null || !ex.Message.Equals(lastException.Message, StringComparison.OrdinalIgnoreCase))
        {
            if (Logger != null && Logger.IsEnabled(LogLevel.Error))
            {
                Logger.LogError(ex, ex.Message);
            }
        }
    }

    public EntityMetadata GetEntityMetadata(string entityLogicalName, EntityFilters queryFilter = EntityFilters.Default)
    {
        RetrieveEntityResponse resp = (RetrieveEntityResponse)Execute(new RetrieveEntityRequest
        {
            LogicalName = entityLogicalName,
            EntityFilters = queryFilter,
        });
        return resp.EntityMetadata;
    }

    public Task AssociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities, CancellationToken cancellationToken)
    {
        return ExecuteWithRetryAsync(new AssociateRequest
        {
            Target = new EntityReference(entityName, entityId),
            Relationship = relationship,
            RelatedEntities = relatedEntities
        }, cancellationToken);
    }

    public async Task<Guid> CreateAsync(Entity entity, CancellationToken cancellationToken)
    {
        CreateResponse resp = (CreateResponse)await ExecuteWithRetryAsync(new CreateRequest
        {
            Target = entity
        }, cancellationToken);
        return resp.id;
    }

    public async Task<Entity> CreateAndReturnAsync(Entity entity, CancellationToken cancellationToken)
    {
        Guid id = await CreateAsync(entity, cancellationToken);
        return await RetrieveAsync(entity.LogicalName, id, new ColumnSet(true), cancellationToken);
    }

    public Task DeleteAsync(string entityName, Guid id, CancellationToken cancellationToken)
    {
        return ExecuteWithRetryAsync(new DeleteRequest
        {
            Target = new EntityReference(entityName, id)
        }, cancellationToken);
    }

    public Task DisassociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities, CancellationToken cancellationToken)
    {
        return ExecuteWithRetryAsync(new DisassociateRequest
        {
            Target = new EntityReference(entityName, entityId),
            Relationship = relationship,
            RelatedEntities = relatedEntities
        }, cancellationToken);
    }

    public Task<OrganizationResponse> ExecuteAsync(OrganizationRequest request, CancellationToken cancellationToken)
    {
        return ExecuteWithRetryAsync(request, cancellationToken);
    }

    public async Task<Entity> RetrieveAsync(string entityName, Guid id, ColumnSet columnSet, CancellationToken cancellationToken)
    {
        RetrieveResponse resp = (RetrieveResponse)await ExecuteWithRetryAsync(new RetrieveRequest
        {
            Target = new EntityReference(entityName, id),
            ColumnSet = columnSet,
        }, cancellationToken);
        return resp.Entity;
    }

    public async Task<EntityCollection> RetrieveMultipleAsync(QueryBase query, CancellationToken cancellationToken)
    {
        RetrieveMultipleResponse resp = (RetrieveMultipleResponse)await ExecuteWithRetryAsync(new RetrieveMultipleRequest
        {
            Query = query
        }, cancellationToken);
        return resp.EntityCollection;
    }

    public Task UpdateAsync(Entity entity, CancellationToken cancellationToken)
    {
        return ExecuteWithRetryAsync(new UpdateRequest
        {
            Target = entity
        }, cancellationToken);
    }

    public Task<Guid> CreateAsync(Entity entity)
    {
        return CreateAsync(entity, CancellationToken.None);
    }

    public Task<Entity> RetrieveAsync(string entityName, Guid id, ColumnSet columnSet)
    {
        return RetrieveAsync(entityName, id, columnSet, CancellationToken.None);
    }

    public Task UpdateAsync(Entity entity)
    {
        return UpdateAsync(entity, CancellationToken.None);
    }

    public Task DeleteAsync(string entityName, Guid id)
    {
        return DeleteAsync(entityName, id, CancellationToken.None);
    }

    public Task<OrganizationResponse> ExecuteAsync(OrganizationRequest request)
    {
        return ExecuteAsync(request, CancellationToken.None);
    }

    public Task AssociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
    {
        return AssociateAsync(entityName, entityId, relationship, relatedEntities, CancellationToken.None);
    }

    public Task DisassociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
    {
        return DisassociateAsync(entityName, entityId, relationship, relatedEntities, CancellationToken.None);
    }

    public Task<EntityCollection> RetrieveMultipleAsync(QueryBase query)
    {
        return RetrieveMultipleAsync(query, CancellationToken.None);
    }

    public Guid Create(Entity entity)
    {
        CreateResponse resp = (CreateResponse)ExecuteWithRetry(new CreateRequest
        {
            Target = entity
        });
        return resp.id;
    }

    public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
    {
        RetrieveResponse resp = (RetrieveResponse)ExecuteWithRetry(new RetrieveRequest
        {
            Target = new EntityReference(entityName, id),
            ColumnSet = columnSet
        });
        return resp.Entity;
    }

    public void Update(Entity entity)
    {
        ExecuteWithRetry(new UpdateRequest
        {
            Target = entity
        });
    }

    public void Delete(string entityName, Guid id)
    {
        ExecuteWithRetry(new DeleteRequest
        {
            Target = new EntityReference(entityName, id),
        });
    }

    public OrganizationResponse Execute(OrganizationRequest request)
    {
        return ExecuteWithRetry(request);
    }

    public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
    {
        ExecuteWithRetry(new AssociateRequest
        {
            Target = new EntityReference(entityName, entityId),
            Relationship = relationship,
            RelatedEntities = relatedEntities
        });
    }

    public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
    {
        ExecuteWithRetry(new DisassociateRequest
        {
            Target = new EntityReference(entityName, entityId),
            Relationship = relationship,
            RelatedEntities = relatedEntities
        });
    }

    public EntityCollection RetrieveMultiple(QueryBase query)
    {
        RetrieveMultipleResponse resp = (RetrieveMultipleResponse)ExecuteWithRetry(new RetrieveMultipleRequest
        {
            Query = query
        });
        return resp.EntityCollection;
    }
}
