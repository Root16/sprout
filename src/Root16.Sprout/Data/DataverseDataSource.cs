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

namespace Root16.Sprout.Data;

public class DataverseDataSinkError
{
	public OrganizationServiceFault Fault { get; set; }
	public OrganizationRequest Request { get; set; }
}
public class DataverseDataSink : IDataOperationEndpoint<Entity>
{
	private readonly DataverseDataSource dataSource;
	public bool DryRun { get; set; }
	public bool BypassCustomPluginExecution { get; set; }
	public event EventHandler<DataverseDataSinkError> OnError;

	public DataverseDataSink(DataverseDataSource dataSource)
	{
		this.dataSource = dataSource;
	}

	public IReadOnlyList<DataChangeType> Update(IEnumerable<DataChange<Entity>> dests)
	{

		return dataSource.Update(dests, DryRun, BypassCustomPluginExecution, ReportError);
	}

	private void ReportError(OrganizationServiceFault error, OrganizationRequest request)
	{
		OnError?.Invoke(this, new DataverseDataSinkError
		{
			Fault = error,
			Request = request
		});
	}

    public Task<IReadOnlyList<DataOperationResult<Entity>>> PerformOperationsAsync(IEnumerable<DataOperation<Entity>> operations)
    {
		var results = (IReadOnlyList<DataOperationResult<Entity>>)operations
			.Select(o => new DataOperationResult<Entity>(o, true))
			.ToList();
		return Task.FromResult(results);
    }
}

public class DataverseDataSource : IDataSource
{
	private readonly ILogger<DataverseDataSource> logger;

	public DataverseDataSource(string crmConnectionString, ILogger<DataverseDataSource> logger) : this(new ServiceClient(crmConnectionString), logger)
	{
	}

	public DataverseDataSource(ServiceClient crmServiceClient, ILogger<DataverseDataSource> logger)
	{
		CrmServiceClient = crmServiceClient;
		//TODO: check this
		ServiceClient.MaxConnectionTimeout = TimeSpan.FromMinutes(11);
		this.logger = logger;
	}

	public ServiceClient CrmServiceClient { get; }

	public DataverseDataSink CreateDataSink()
	{
		return new DataverseDataSink(this);
	}

	public IReadOnlyList<DataChangeType> Update(IEnumerable<DataChange<Entity>> changes, bool dryRun, bool bypassCustomPluginExecution, Action<OrganizationServiceFault, OrganizationRequest> errorHandler)
	{
		var requests = new OrganizationRequestCollection();

		requests.AddRange(changes.Select(c => CreateOrganizationRequest(c, bypassCustomPluginExecution)));
		return ExecuteMultiple(requests, dryRun, errorHandler);
	}

	public IReadOnlyList<DataChangeType> ExecuteMultiple(OrganizationRequestCollection requests, bool dryRun, Action<OrganizationServiceFault, OrganizationRequest> errorHandler)
	{
		var results = new List<DataChangeType>();

		if (requests.Count == 1)
		{
			try
			{
				if (!dryRun)
				{
					var response = CrmServiceClient.Execute(requests[0]);
				}
				results.Add(ResultFromRequestType(requests[0]));
			}
			catch (FaultException<OrganizationServiceFault> e)
			{
				logger.LogError(e.Message);
				results.Add(DataChangeType.Error);
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

			ExecuteMultipleResponseItemCollection responses;
			for (var i = 0; i < requests.Count; i++)
			{
				results.Add(ResultFromRequestType(requests[i]));
			}

			if (!dryRun)
			{
				responses = ((ExecuteMultipleResponse)CrmServiceClient.Execute(executeMultiple)).Responses;
				foreach (var response in responses)
				{
					if (response.Fault != null)
					{
						results[response.RequestIndex] = DataChangeType.Error;
						logger.LogError(response.Fault.Message);
						errorHandler(response.Fault, requests[response.RequestIndex]);
					}
				}
			}
		}

		return results;
	}

	private static DataChangeType ResultFromRequestType(OrganizationRequest request)
	{
		return request.RequestName == "Create" ? DataChangeType.Create : DataChangeType.Update;
	}

	public IPagedQuery<Entity> CreateFetchXmlQuery(string fetchXml)
	{
		return new DataverseFetchXmlPagedQuery(this, fetchXml);
	}

	protected OrganizationRequest CreateOrganizationRequest(DataChange<Entity> change, bool bypassCustomPluginExecution)
	{
		OrganizationRequest request;
		if (change.Type == DataChangeType.Create)
		{
			request = new CreateRequest
			{
				Target = change.Target,
			};
		}
		else if (change.Type == DataChangeType.Update)
		{
			request = new UpdateRequest
			{
				Target = change.Target,
			};
		}
		else
		{
			throw new NotImplementedException("Unknown change type.");
		}

		if (bypassCustomPluginExecution)
		{
			request.Parameters.Add("BypassCustomPluginExecution", true);
		}

		return request;
	}
}
