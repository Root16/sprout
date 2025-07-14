using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.ServiceModel;

namespace Root16.Sprout.DataSources.Dataverse;

public class OrganizationRequestDataSource(DataverseDataSource dataverseDataSource, ILogger<OrganizationRequestDataSource> logger) : IDataSource<OrganizationRequest>
{
    const int MaxRetries = 10;
    public ServiceClient CrmServiceClient { get { return dataverseDataSource.CrmServiceClient; } }

    public async Task<IReadOnlyList<DataOperationResult<OrganizationRequest>>> PerformOperationsAsync(IEnumerable<DataOperation<OrganizationRequest>> operations, bool dryRun, IEnumerable<string> dataOperationFlags)
    {
        var results = new List<DataOperationResult<OrganizationRequest>>();

        if (dryRun)
        {
            foreach (var operation in operations)
            {
                results.Add(new DataOperationResult<OrganizationRequest>(operation, true));
            }
            return results;
        }

        var executeMultipleRequest = new ExecuteMultipleRequest
        {
            Requests = new OrganizationRequestCollection(),
            Settings = new ExecuteMultipleSettings
            {
                ContinueOnError = true,
                ReturnResponses = true,
            }
        };
        executeMultipleRequest.Requests.AddRange(operations.Select(op => CreateOrganizationRequest(op, dataOperationFlags)));

        var executeMultipleResponse = (ExecuteMultipleResponse)await dataverseDataSource.CrmServiceClient.ExecuteAsync(executeMultipleRequest);

        foreach (var response in executeMultipleResponse.Responses)
        {
            if (response.Fault is not null)
            {
                logger.LogError(response.Fault.ToString());
            }

            results.Add(new DataOperationResult<OrganizationRequest>(
                operations.Skip(response.RequestIndex).First(),
                response.Fault is null));
        }
        return results;
    }

    private static OrganizationRequest? CreateOrganizationRequest(DataOperation<OrganizationRequest> change, IEnumerable<string> dataOperationFlags)
    {
        OrganizationRequest request = change.Data;

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
        else if (dataOperationFlags.Contains(DataverseDataSourceFlags.BypassBusinessLogicExecutionSync))
        {
            request.Parameters.Add(DataverseDataSourceFlags.BypassBusinessLogicExecution, "CustomSync");
        }
        else if (dataOperationFlags.Contains(DataverseDataSourceFlags.BypassBusinessLogicExecutionAsync))
        {
            request.Parameters.Add(DataverseDataSourceFlags.BypassBusinessLogicExecution, "CustomAsync");
        }

        var stepIds = dataOperationFlags
            .Where(flag =>
                new[]
                {
                    DataverseDataSourceFlags.BypassCustomPluginExecution,
                    DataverseDataSourceFlags.SuppressCallbackRegistrationExpanderJob,
                    DataverseDataSourceFlags.BypassBusinessLogicExecution,
                    DataverseDataSourceFlags.BypassBusinessLogicExecutionAsync,
                    DataverseDataSourceFlags.BypassBusinessLogicExecutionSync,
                    DataverseDataSourceFlags.BypassBusinessLogicExecutionStepIds
                }.All(param => param != flag))
            .SelectMany(flag =>
                flag.Split(',', StringSplitOptions.TrimEntries)
                    .Where(s => !string.IsNullOrWhiteSpace(s) && Guid.TryParse(s, out var _)))
            .ToArray();
        if (stepIds.Length > 0)
        {
            request.Parameters.Add(DataverseDataSourceFlags.BypassBusinessLogicExecutionStepIds, string.Join(",", stepIds));
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
}