using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Root16.Sprout.Data;

public class ExecuteParallelResult
{
	public IEnumerable<OrganizationServiceFault> Faults { get; set; }
}

public class ExecuteParallelResult<T> : ExecuteParallelResult
{
	public IEnumerable<T> Results { get; set; }
}

public class ParallelServiceClient : IDisposable
{
	private ServiceClient Client { get; }
	private int MaxDegreeOfParallelism { get; }

	private readonly int oldDefaultConnectionLimit;
	private readonly int oldMinWorkerThreads;
	private readonly int oldMinCompletionPortThreads;
	private readonly bool oldExpect100Continue;
	private readonly bool oldUseNagleAlgorithm;

	/// <summary>
	/// A simple implementation of CrmServiceClient + Task Parallel Library. Be sure to add 
	/// key="PreferConnectionAffinity" value="false" to the AppSettings node in your App.config file.
	/// More info: https://github.com/microsoft/PowerApps-Samples/tree/master/cds/Xrm%20Tooling/TPLCrmServiceClient
	/// </summary>
	/// <param name="client"></param>
	/// <param name="maxDegreeOfParallelism">The max number of concurrent tasks</param>
	/// <param name="defaultConnectionLimit">Change max connections from .NET to a remote service default: 2</param>
	/// <param name="minThreads">Bump up the min threads reserved for this app to ramp connections faster - minWorkerThreads defaults to 4, minIOCP defaults to 4</param>
	/// <param name="expect100Continue">Turn off the Expect 100 to continue message - 'true' will cause the caller to wait until it round-trip confirms a connection to the server</param>
	/// <param name="useNagleAlgorithm">Can decrease overall transmission overhead but can cause delay in data packet arrival</param>
	public ParallelServiceClient(ServiceClient client, int maxDegreeOfParallelism = 10000, int defaultConnectionLimit = 65000, int minThreads = 100, bool expect100Continue = false, bool useNagleAlgorithm = false)
	{
		Client = client;
		MaxDegreeOfParallelism = maxDegreeOfParallelism;

		oldDefaultConnectionLimit = System.Net.ServicePointManager.DefaultConnectionLimit;
		System.Net.ServicePointManager.DefaultConnectionLimit = defaultConnectionLimit;

		System.Threading.ThreadPool.GetMinThreads(out oldMinWorkerThreads, out oldMinCompletionPortThreads);
		System.Threading.ThreadPool.SetMinThreads(minThreads, minThreads);

		oldExpect100Continue = System.Net.ServicePointManager.Expect100Continue;
		System.Net.ServicePointManager.Expect100Continue = expect100Continue;

		oldUseNagleAlgorithm = System.Net.ServicePointManager.UseNagleAlgorithm;
		System.Net.ServicePointManager.UseNagleAlgorithm = useNagleAlgorithm;
	}

	public ExecuteParallelResult<Guid> CreateParallel(IEnumerable<Entity> entities)
	{
		return ExecuteParallel(entities, (svc, e) => svc.Create(e));
	}

	public ExecuteParallelResult UpdateParallel(IEnumerable<Entity> entities)
	{
		return ExecuteParallel(entities, (svc, e) => svc.Update(e));
	}

	public ExecuteParallelResult DeleteParallel(IEnumerable<EntityReference> entities)
	{
		return ExecuteParallel(entities, (svc, e) => svc.Delete(e.LogicalName, e.Id));
	}

	public ExecuteParallelResult ExecuteParallel<T>(IEnumerable<T> items, Action<ServiceClient, T> operation)
	{
		var faults = new ConcurrentBag<OrganizationServiceFault>();
		var options = new ParallelOptions
		{
			MaxDegreeOfParallelism = MaxDegreeOfParallelism
		};

		Parallel.ForEach(items, options,
			() => Client.Clone(),
			(T item, ParallelLoopState loopState, long index, ServiceClient client) =>
			{
				try
				{
					operation(client, item);
				}
				catch (FaultException<OrganizationServiceFault> e)
				{
					faults.Add(e.Detail);
				}
				return client;
			},
			svc => svc?.Dispose());

		return new ExecuteParallelResult
		{
			Faults = faults
		};
	}

	public ExecuteParallelResult<TResult> ExecuteParallel<T, TResult>(IEnumerable<T> items, Func<ServiceClient, T, TResult> operation)
	{
		var faults = new ConcurrentBag<OrganizationServiceFault>();
		var results = new ConcurrentBag<TResult>();
		var options = new ParallelOptions
		{
			MaxDegreeOfParallelism = MaxDegreeOfParallelism
		};

		Parallel.ForEach(items, options,
			() => Client.Clone(),
			(T item, ParallelLoopState loopState, long index, ServiceClient client) =>
			{
				try
				{
					results.Add(operation(client, item));
				}
				catch (FaultException<OrganizationServiceFault> e)
				{
					faults.Add(e.Detail);
				}
				return client;
			},
			svc => svc?.Dispose());

		return new ExecuteParallelResult<TResult>
		{
			Faults = faults,
			Results = results
		};
	}

	public void Dispose()
	{
		Client?.Dispose();
		System.Net.ServicePointManager.DefaultConnectionLimit = oldDefaultConnectionLimit;
		System.Threading.ThreadPool.SetMinThreads(oldMinWorkerThreads, oldMinCompletionPortThreads);
		System.Net.ServicePointManager.Expect100Continue = oldExpect100Continue;
		System.Net.ServicePointManager.UseNagleAlgorithm = oldUseNagleAlgorithm;
	}
}
