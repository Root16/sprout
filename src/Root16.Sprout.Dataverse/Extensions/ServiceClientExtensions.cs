using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;

namespace Root16.Sprout.DataSources.Dataverse;

public static class ServiceClientExtensions
{
    public static async Task<EntityCollection> RetrieveMultipleWithRetryAsync(this ServiceClient serviceClient, QueryBase query, int retryCount = 5)
    {
        var retryAfter = TimeSpan.FromSeconds(0);
        var retry = 0;
        do
        {
            try
            {
                Thread.Sleep(retryAfter);
                return await serviceClient.RetrieveMultipleAsync(query);
            }
            catch (FaultException<OrganizationServiceFault>) { throw; }
            catch (CommunicationException)
            {
                if (retry > retryCount) throw;
                retry++;
                var retrypause = serviceClient.RetryPauseTime;
                if (retrypause.TotalSeconds > 0)
                    retryAfter = serviceClient.RetryPauseTime;
                else retryAfter = TimeSpan.FromSeconds(5 * retryCount);
            }
        } while (retry < retryCount);
        throw new Exception($"{nameof(RetrieveMultipleWithRetryAsync)} : {serviceClient.LastException.Message}");
    }
}
