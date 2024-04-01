using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Root16.Sprout.DataSources.Dataverse;

public class DataverseDataSource : IDataSource<Entity>
{
    private readonly ILogger<DataverseDataSource> logger;
    public string? ImpersonateUsingAttribute { get; set; }

    public DataverseDataSource(ServiceClient crmServiceClient, ILogger<DataverseDataSource> logger)
    {
        CrmServiceClient = crmServiceClient;
        ServiceClient.MaxConnectionTimeout = TimeSpan.FromMinutes(11);
        this.logger = logger;
    }

    public ServiceClient CrmServiceClient { get; }


    public async Task<IReadOnlyList<DataOperationResult<Entity>>> PerformOperationsAsync(IEnumerable<DataOperation<Entity>> operations, bool dryRun, IEnumerable<string> dataOperationFlags)
    {
        CleanUpOverriddenCreatedOn(operations);

        IEnumerable<IGrouping<Guid?, DataOperation<Entity>>> groups;
        if (ImpersonateUsingAttribute is not null)
        {
            groups = operations
                .GroupBy(op =>
                {
                    var entityRef = op.Data.GetAttributeValue<EntityReference>(ImpersonateUsingAttribute);
                    if (entityRef?.LogicalName == "systemuser")
                    {
                        return entityRef?.Id;
                    }
                    return null;
                })
                .ToArray();
        }
        else
        {
            groups = operations.GroupBy(op => (Guid?)null).ToArray();
        }

        var results = new List<DataOperationResult<Entity>>();
        foreach (var group in groups)
        {
            RemoveAttribute(group, ImpersonateUsingAttribute);

            var requests = new OrganizationRequestCollection();

            requests.AddRange(group
                .Select(c => CreateOrganizationRequest(c, dataOperationFlags))
                .Where(r => r is not null)
            );

            CrmServiceClient.CallerId = group.Key ?? Guid.Empty;
            results.AddRange(await ExecuteMultipleAsync(requests, dryRun));
            CrmServiceClient.CallerId = Guid.Empty;
        }

        return results;
    }

    private void CleanUpOverriddenCreatedOn(IEnumerable<DataOperation<Entity>> operations)
    {
        foreach(var op in operations)
        {
            if (op.OperationType.Equals("Update", StringComparison.OrdinalIgnoreCase) && op.Data.Contains("overriddencreatedon"))
            {
                op.Data.Attributes.Remove("overriddencreatedon");
            }
        }
    }

    private void RemoveAttribute(IEnumerable<DataOperation<Entity>> operations, string? attributeName)
    {
        if (attributeName is not null)
        {
            foreach (var op in operations)
            {
                if (op.Data.Contains(attributeName))
                {
                    op.Data.Attributes.Remove(attributeName);
                }
            }
        }
    }

    public async Task<IReadOnlyList<DataOperationResult<Entity>>> ExecuteMultipleAsync(
        OrganizationRequestCollection requestCollection,
        bool dryRun)
    {
        var results = new List<DataOperationResult<Entity>>();

        if (requestCollection.Count == 1)
        {
            try
            {
                if (!dryRun)
                {
                    var response = await CrmServiceClient.ExecuteAsync(requestCollection[0]);
                }
                results.Add(ResultFromRequestType(requestCollection[0], true));
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                results.Add(ResultFromRequestType(requestCollection[0], false));
            }
        }
        else if (requestCollection.Count > 1)
        {
            if (dryRun)
            {
                for (var i = 0; i < requestCollection.Count; i++)
                {
                    results.Add(ResultFromRequestType(requestCollection[i], true));
                }
            }
            else
            {
                List<OrganizationRequestCollection> ListofRequestCollections = new List<OrganizationRequestCollection>();

                foreach (var request in requestCollection.Chunk(1000))
                {
                    var orgRequestCollection = new OrganizationRequestCollection();
                    orgRequestCollection.AddRange(request);
                    ListofRequestCollections.Add(orgRequestCollection);
                }

                List<Task<OrganizationResponse>> requestTasks = new();

                foreach (var requests in ListofRequestCollections)
                {
                    requestTasks.Add(CrmServiceClient.ExecuteAsync(new ExecuteMultipleRequest
                    {
                        Settings = new ExecuteMultipleSettings
                        {
                            ContinueOnError = true,
                        },
                        Requests = requests
                    }));
                }

                OrganizationResponse[] organizationResponses = await Task.WhenAll(requestTasks);
                List<ExecuteMultipleResponse> executeMultipleResponses = organizationResponses.Select(x => (ExecuteMultipleResponse)x).ToList();

                for (var i = 0; i < executeMultipleResponses.Count; i++)
                {
                    var responses = executeMultipleResponses[i].Responses;
                    var matchingRequests = ListofRequestCollections[i];
                    for (var k = 0; k < matchingRequests.Count; k++)
                    {
                        var response = responses.FirstOrDefault(r => r.RequestIndex == k);
                        if (response?.Fault is not null)
                        {
                            results.Add(ResultFromRequestType(matchingRequests[k], false));
                            if (response?.Fault?.InnerFault?.InnerFault?.Message is not null
                                && response.Fault.InnerFault.InnerFault is OrganizationServiceFault innermostFault)
                            {
                                logger.LogError(innermostFault.Message);
                            }
                            else
                            {
                                logger.LogError(response.Fault.Message);
                            }
                        }
                        else
                        {
                            results.Add(ResultFromRequestType(matchingRequests[k], true));
                        }
                    }
                }
            }
        }
        return results;
    }

    private static DataOperationResult<Entity> ResultFromRequestType(OrganizationRequest request, bool wasSuccessful)
    {
        Entity target;
        if (request.Parameters["Target"] is EntityReference entityRef)
        {
            target = new Entity(entityRef.LogicalName, entityRef.Id);
        }
        else
        {
            target = (Entity)request.Parameters["Target"];
        }
        return new DataOperationResult<Entity>(new DataOperation<Entity>(request.RequestName, target), wasSuccessful);
    }

    public IPagedQuery<Entity> CreateFetchXmlQuery(string fetchXml)
    {
        return new DataverseFetchXmlPagedQuery(this, fetchXml);
    }

    protected OrganizationRequest? CreateOrganizationRequest(DataOperation<Entity> change, IEnumerable<string> dataOperationFlags)
    {
        OrganizationRequest request;
        if (change.OperationType.Equals("Create", StringComparison.OrdinalIgnoreCase) && change.Data.Attributes.Count > 0)
        {
            request = new CreateRequest
            {
                Target = change.Data,
            };
        }
        else if (change.OperationType.Equals("Update", StringComparison.OrdinalIgnoreCase) && change.Data.Attributes.Count > 0)
        {
            request = new UpdateRequest
            {
                Target = change.Data,
            };
        }
        else if (change.OperationType.Equals("Delete", StringComparison.OrdinalIgnoreCase))
        {
            request = new DeleteRequest
            {
                Target = change.Data.ToEntityReference(),
            };
        }
        else
        {
            return null;
        }

        if (dataOperationFlags.Contains(DataverseDataSourceFlags.BypassCustomPluginExecution) == true)
        {
            request.Parameters.Add(DataverseDataSourceFlags.BypassCustomPluginExecution, true);
        }

        if (dataOperationFlags.Contains(DataverseDataSourceFlags.SuppressCallbackRegistrationExpanderJob) == true)
        {
            request.Parameters.Add(DataverseDataSourceFlags.SuppressCallbackRegistrationExpanderJob, true);
        }

        return request;
    }
}
