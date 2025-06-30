using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.Collections.Concurrent;
using System.Net;
using System.ServiceModel;

namespace Root16.Sprout.DataSources.Dataverse;

public class DataverseDataSource : IDataSource<Entity>
{
    private readonly ILogger<DataverseDataSource> logger;
    public string? ImpersonateUsingAttribute { get; set; }

    const int MaxRetries = 10;

    public DataverseDataSource(
        ServiceClient crmServiceClient, 
        ILogger<DataverseDataSource> logger)
    {
        CrmServiceClient = crmServiceClient;
        ServiceClient.MaxConnectionTimeout = TimeSpan.FromMinutes(11);
        CrmServiceClient.EnableAffinityCookie = false;
        // TODO: Is there a better place to do this?
        ThreadPool.SetMinThreads(100, 100);
        ServicePointManager.DefaultConnectionLimit = 65000;
        ServicePointManager.Expect100Continue = false;
        ServicePointManager.UseNagleAlgorithm = false;
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

    private static void CleanUpOverriddenCreatedOn(IEnumerable<DataOperation<Entity>> operations)
    {
        foreach(var op in operations)
        {
            if (op.OperationType.Equals("Update", StringComparison.OrdinalIgnoreCase) && op.Data.Contains("overriddencreatedon"))
            {
                op.Data.Attributes.Remove("overriddencreatedon");
            }
        }
    }

    private static void RemoveAttribute(IEnumerable<DataOperation<Entity>> operations, string? attributeName)
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
            var request = requestCollection[0];
            try
            {
                if (!dryRun)
                {
                    var response = await TryExecuteRequestAsync(requestCollection[0]);
                }
                results.Add(ResultFromRequestType(requestCollection[0], true));
            }
            catch (Exception e)
            {
                LogRequestError(request, e);
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
                ConcurrentBag<DataOperationResult<Entity>> parallelResults = [];

                ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = CrmServiceClient.RecommendedDegreesOfParallelism };

                await Parallel.ForEachAsync(requestCollection.Chunk(10), parallelOptions, async (batch, token) =>
                {
                    ExecuteMultipleRequest request = new()
                    {
                        Settings = new ExecuteMultipleSettings
                        {
                            ContinueOnError = true,
                        },
                        Requests = []
                    };
                    request.Requests.AddRange(batch);

                    ExecuteMultipleResponse batchResponse = await TryExecuteRequestAsync<ExecuteMultipleResponse>(request, token);

                    for (var k = 0; k < batch.Length; k++)
                    {
                        var req = batch[k];
                        var response = batchResponse.Responses.FirstOrDefault(r => r.RequestIndex == k);
                        if (response?.Fault is not null)
                        {
                            parallelResults.Add(ResultFromRequestType(req, false));

                            var errorMessage = response.Fault.InnerFault?.InnerFault?.Message 
                                           ?? response.Fault.Message;

                            LogRequestError(req, new Exception(errorMessage));
                        }
                        else
                        {
                            parallelResults.Add(ResultFromRequestType(req, true));
                        }
                    }
                });

                results.AddRange(parallelResults);
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

    public IPagedQuery<Entity> CreateFetchXmlReducingQuery(string fetchXml, string? countByAttribute = null)
    {
        return new DataverseFetchXmlReducingQuery(this, fetchXml, countByAttribute);
    }

    protected static OrganizationRequest? CreateOrganizationRequest(DataOperation<Entity> change, IEnumerable<string> dataOperationFlags)
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

        if (dataOperationFlags.Contains(DataverseDataSourceFlags.BypassBusinessLogicExecution)
            || (dataOperationFlags.Contains(DataverseDataSourceFlags.BypassBusinessLogicExecutionAsync) && dataOperationFlags.Contains(DataverseDataSourceFlags.BypassBusinessLogicExecutionSync)))
        {
            request.Parameters.Add(DataverseDataSourceFlags.BypassBusinessLogicExecution, "CustomSync,CustomAsync");
        }
        else if(dataOperationFlags.Contains(DataverseDataSourceFlags.BypassBusinessLogicExecutionSync))
        {
            request.Parameters.Add(DataverseDataSourceFlags.BypassBusinessLogicExecution, "CustomSync");
        }
        else if (dataOperationFlags.Contains(DataverseDataSourceFlags.BypassBusinessLogicExecutionAsync))
        {
            request.Parameters.Add(DataverseDataSourceFlags.BypassBusinessLogicExecution, "CustomAsync");
        }

        return request;
    }

    private Task<OrganizationResponse> TryExecuteRequestAsync(OrganizationRequest request, CancellationToken token = default)
        => TryExecuteRequestAsync<OrganizationResponse>(request, token);

    private async Task<T> TryExecuteRequestAsync<T>(OrganizationRequest request, CancellationToken token = default)
        where T : OrganizationResponse
    {
        var retryCount = 0;
        Exception? lastException = null;
        do
        {
            try
            {
                return (T)await CrmServiceClient.ExecuteAsync(request, token);
            }
            catch (FaultException<OrganizationServiceFault>) { throw; }
            catch (Exception ex)
            {
                if (lastException is null || !ex.Message.Equals(lastException.Message, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogError(ex, ex.Message);
                }
                lastException = ex;
            }
        } while (retryCount++ < MaxRetries);
        
        throw lastException;
    }
	private void LogRequestError(OrganizationRequest request, Exception ex)
	{
		var target = request.Parameters.TryGetValue("Target", out var entityObj) && entityObj is Entity entity
			? $"{entity.LogicalName} (Id: {entity.Id})"
			: "Unknown target";

        if(logger.IsEnabled(LogLevel.Debug))
        {
		    logger.LogDebug("Request failed for target: {Target}. Exception: {Message}", target, ex.Message);
        }
	}
}
