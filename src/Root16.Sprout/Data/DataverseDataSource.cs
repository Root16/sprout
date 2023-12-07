using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Root16.Sprout.Processors;
using Root16.Sprout.Progress;
using Root16.Sprout.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Root16.Sprout.Data;

public record DataverseDataSinkError(OrganizationServiceFault Fault, OrganizationRequest Request);

public class DataverseDataSource : IDataSource<Entity>
{
	private readonly ILogger<DataverseDataSource> logger;
	public string? Name { get; set; }
    public bool DryRun { get; set; }
    public bool BypassCustomPluginExecution { get; set; }
    public event EventHandler<DataverseDataSinkError>? OnError;

    public DataverseDataSource(ServiceClient crmServiceClient, ILogger<DataverseDataSource> logger)
	{
		CrmServiceClient = crmServiceClient;
		//TODO: check this
		ServiceClient.MaxConnectionTimeout = TimeSpan.FromMinutes(11);
		this.logger = logger;
	}

	public ServiceClient CrmServiceClient { get; }

	public async Task<IReadOnlyList<DataOperationResult<Entity>>> PerformOperationsAsync(IEnumerable<DataOperation<Entity>> changes, bool dryRun, bool bypassCustomPluginExecution, Action<OrganizationServiceFault, OrganizationRequest> errorHandler)
	{
		var requests = new OrganizationRequestCollection();

		requests.AddRange(changes.Select(c => CreateOrganizationRequest(c, bypassCustomPluginExecution)));
		return await ExecuteMultipleAsync(requests, dryRun, errorHandler);
	}

	public async Task<IReadOnlyList<DataOperationResult<Entity>>> ExecuteMultipleAsync(
		OrganizationRequestCollection requests, 
		bool dryRun, 
		Action<OrganizationServiceFault, OrganizationRequest>? errorHandler)
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
					if (response?.Fault != null)
					{
						results.Add(ResultFromRequestType(requests[i], false));
						logger.LogError(response.Fault.Message);
						errorHandler?.Invoke(response.Fault, requests[response.RequestIndex]);
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

	protected OrganizationRequest CreateOrganizationRequest(DataOperation<Entity> change, bool bypassCustomPluginExecution)
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

		if (bypassCustomPluginExecution)
		{
			request.Parameters.Add("BypassCustomPluginExecution", true);
		}

		return request;
	}

    public async Task<IReadOnlyList<DataOperationResult<Entity>>> PerformOperationsAsync(IEnumerable<DataOperation<Entity>> operations)
    {
        return await PerformOperationsAsync(operations, DryRun, BypassCustomPluginExecution, ReportError);
    }

    private void ReportError(OrganizationServiceFault error, OrganizationRequest request)
    {
        OnError?.Invoke(this, new DataverseDataSinkError
        (
            error,
            request
        ));
    }
}
