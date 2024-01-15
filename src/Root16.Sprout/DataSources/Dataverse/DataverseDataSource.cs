using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.ServiceModel;

namespace Root16.Sprout.DataSources.Dataverse;

public class DataverseDataSource : IDataSource<Entity>
{
    private readonly ILogger<DataverseDataSource> logger;

    public DataverseDataSource(ServiceClient crmServiceClient, ILogger<DataverseDataSource> logger)
    {
        CrmServiceClient = crmServiceClient;
        ServiceClient.MaxConnectionTimeout = TimeSpan.FromMinutes(11);
        this.logger = logger;
    }

    public ServiceClient CrmServiceClient { get; }

    public async Task<IReadOnlyList<DataOperationResult<Entity>>> PerformOperationsAsync(IEnumerable<DataOperation<Entity>> operations, bool dryRun, IEnumerable<string> dataOperationFlags)
    {
        var requests = new OrganizationRequestCollection();

        requests.AddRange(operations.Select(c => CreateOrganizationRequest(c, dataOperationFlags)));
        return await ExecuteMultipleAsync(requests, dryRun);
    }

    public async Task<IReadOnlyList<DataOperationResult<Entity>>> ExecuteMultipleAsync(
        OrganizationRequestCollection requests,
        bool dryRun)
    {
        var results = new List<DataOperationResult<Entity>>();

        if (requests.Count == 1)
        {
            try
            {
                if (!dryRun)
                {
                    var response = await CrmServiceClient.ExecuteAsync(requests[0]);
                }
                results.Add(ResultFromRequestType(requests[0], true));
            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                logger.LogError(e.Message);
                results.Add(ResultFromRequestType(requests[0], false));
            }
        }
        else if (requests.Count > 1)
        {
            var executeMultiple = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ContinueOnError = true,
                },
                Requests = requests
            };

            if (dryRun)
            {
                for (var i = 0; i < requests.Count; i++)
                {
                    results.Add(ResultFromRequestType(requests[i], true));
                }
            }
            else
            {
                var executeMultipleResponse = (ExecuteMultipleResponse)await CrmServiceClient.ExecuteAsync(executeMultiple);
                var responses = executeMultipleResponse.Responses;
                for (var i = 0; i < requests.Count; i++)
                {
                    var response = responses.FirstOrDefault(r => r.RequestIndex == i);
                    if (response?.Fault is not null)
                    {
                        results.Add(ResultFromRequestType(requests[i], false));
                        if (response?.Fault?.InnerFault?.InnerFault?.Message is not null && response.Fault.InnerFault.InnerFault is OrganizationServiceFault)
                        {
                            logger.LogError(response.Fault.InnerFault.InnerFault.Message);
                        }
                        else
                        {
                            logger.LogError(response.Fault.Message);
                        }
                    }
                    else
                    {
                        results.Add(ResultFromRequestType(requests[i], true));
                    }
                }
            }
        }

        return results;
    }

    private static DataOperationResult<Entity> ResultFromRequestType(OrganizationRequest request, bool wasSuccessful)
    {
        return new DataOperationResult<Entity>(new DataOperation<Entity>(request.RequestName, (Entity)request.Parameters["Target"]), wasSuccessful);
    }

    public IPagedQuery<Entity> CreateFetchXmlQuery(string fetchXml)
    {
        return new DataverseFetchXmlPagedQuery(this, fetchXml);
    }

    protected OrganizationRequest CreateOrganizationRequest(DataOperation<Entity> change, IEnumerable<string> dataOperationFlags)
    {
        OrganizationRequest request;
        if (change.OperationType == "Create")
        {
            request = new CreateRequest
            {
                Target = change.Data,
            };
        }
        else if (change.OperationType == "Update")
        {
            request = new UpdateRequest
            {
                Target = change.Data,
            };
        }
        else
        {
            throw new NotImplementedException("Unknown operation type.");
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
