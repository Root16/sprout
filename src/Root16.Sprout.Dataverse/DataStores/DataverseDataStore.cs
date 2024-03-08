using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Root16.Sprout.DataStores;
using System.ServiceModel;

namespace Root16.Sprout.DataSources.Dataverse;

public class DataverseDataStoreOptions
{
    public bool DryRun { get; set; }
    public bool BypassCustomPluginExecution { get; set; }
    public bool SuppressCallbackRegistrationExpanderJob { get; set; }
}

public class DataverseDataStore : IDataStore<OrganizationRequest, DataverseDataStoreOptions>
{
    private readonly ILogger<DataverseDataStore> logger;
    public string? ImpersonateUsingAttribute { get; set; }

    public DataverseDataStore(ServiceClient crmServiceClient, ILogger<DataverseDataStore> logger)
    {
        CrmServiceClient = crmServiceClient;
        ServiceClient.MaxConnectionTimeout = TimeSpan.FromMinutes(11);
        this.logger = logger;
    }

    public ServiceClient CrmServiceClient { get; }


    public async Task<IReadOnlyList<OperationResult<OrganizationRequest>>> PerformOperationsAsync(IEnumerable<OrganizationRequest> operations, DataverseDataStoreOptions? options = null)
    {
        CleanUpOverriddenCreatedOn(operations);

        IEnumerable<IGrouping<Guid?, OrganizationRequest>> groups;
        if (ImpersonateUsingAttribute is not null)
        {
            groups = operations
                .GroupBy(op =>
                {
                    if (op.Parameters.TryGetValue<Entity>("Target", out var target))
                    {
                        var entityRef = target.GetAttributeValue<EntityReference>(ImpersonateUsingAttribute);
                        if (entityRef?.LogicalName == "systemuser")
                        {
                            return entityRef?.Id;
                        }

                    }
                    return null;
                })
                .ToArray();
        }
        else
        {
            groups = operations.GroupBy(op => (Guid?)null).ToArray();
        }

        var results = new List<OperationResult<OrganizationRequest>>();
        foreach (var group in groups)
        {
            RemoveAttribute(group, ImpersonateUsingAttribute);

            var requests = new OrganizationRequestCollection();

            requests.AddRange(group
                .Where(r => r is not null)
            );

            CrmServiceClient.CallerId = group.Key ?? Guid.Empty;
            results.AddRange(await ExecuteMultipleAsync(requests, options?.DryRun ?? false));
            CrmServiceClient.CallerId = Guid.Empty;
        }

        return results;
    }

    private void CleanUpOverriddenCreatedOn(IEnumerable<OrganizationRequest> operations)
    {
        foreach(var op in operations)
        {
            if (op is UpdateRequest updateRequest && updateRequest.Target.Contains("overriddencreatedon"))
            {
                updateRequest.Target.Attributes.Remove("overriddencreatedon");
            }
        }
    }

    private void RemoveAttribute(IEnumerable<OrganizationRequest> operations, string? attributeName)
    {
        if (attributeName is not null)
        {
            foreach (var op in operations)
            {
                if (op.Parameters.TryGetValue<Entity>("Target", out var target) && target.Contains(attributeName))
                {
                    target.Attributes.Remove(attributeName);
                }
            }
        }
    }

    public async Task<IReadOnlyList<OperationResult<OrganizationRequest>>> ExecuteMultipleAsync(
        OrganizationRequestCollection requestCollection,
        bool dryRun)
    {
        var results = new List<OperationResult<OrganizationRequest>>();

        if (requestCollection.Count == 1)
        {
            try
            {
                if (!dryRun)
                {
                    var response = await CrmServiceClient.ExecuteAsync(requestCollection[0]);
                }
                results.Add(new(requestCollection[0], true));
            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                logger.LogError(e.Message);
                results.Add(new(requestCollection[0], false));
            }
        }
        else if (requestCollection.Count > 1)
        {
            if (dryRun)
            {
                for (var i = 0; i < requestCollection.Count; i++)
                {
                    results.Add(new(requestCollection[i], true));
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
                            results.Add(new(matchingRequests[k], false));
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
                            results.Add(new(matchingRequests[k], true));
                        }
                    }
                }
            }
        }
        return results;
    }

    public IPagedQuery<Entity> CreateFetchXmlQuery(string fetchXml)
    {
        return new DataverseFetchXmlPagedQuery(this, fetchXml);
    }

    protected OrganizationRequest? AddOptionParameters(OrganizationRequest request, DataverseDataStoreOptions? options)
    {
        if (options?.BypassCustomPluginExecution == true)
        {
            request.Parameters.Add(nameof(DataverseDataStoreOptions.BypassCustomPluginExecution), true);
        }

        if (options?.SuppressCallbackRegistrationExpanderJob == true)
        {
            request.Parameters.Add(nameof(DataverseDataStoreOptions.SuppressCallbackRegistrationExpanderJob), true);
        }

        return request;
    }

    public string GetOperationName(OrganizationRequest operation) => operation.RequestName;
}
