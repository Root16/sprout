using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Root16.Sprout.Data;
using Root16.Sprout.Processors;
using Root16.Sprout.Step;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Root16.Sprout.Sample
{
    internal class TestStep : IIntegrationStep
    {
        private readonly BatchProcessBuilder batchProcessBuilder;
        private readonly DataverseDataSource dataverseDataSource;

        public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public TestStep(BatchProcessBuilder batchProcessBuilder, DataverseDataSource dataverseDataSource)
        {
            this.batchProcessBuilder = batchProcessBuilder;
            this.dataverseDataSource = dataverseDataSource;
        }
        
        public async Task RunAsync()
        {
            var memoryDS = new MemoryDataSource<Contact>(new[]
            {
                new Contact { FirstName = "Corey", LastName = "Test" },
                new Contact { FirstName = "Corey", LastName = "Test2" },
            });

            var response = (WhoAmIResponse)await dataverseDataSource.CrmServiceClient.ExecuteAsync(new WhoAmIRequest());

            var query = memoryDS.CreatePagedQuery();

            var dataverseDS = new DataverseDataSink(this.dataverseDataSource);

            var batchProcessor = batchProcessBuilder
                .CreateProcessor<Contact, Entity>(query)
                .UseMapper(Map)
                .UseDataOperationEndpoint(dataverseDS)
                .Build();

            var result = await batchProcessor.ProcessBatchAsync(null);
        }

        private IEnumerable<DataOperation<Entity>> Map(Contact contact)
        {
            var result = new Entity("contact")
            {
                Attributes =
                {
                    {"firstname", contact.FirstName },
                    {"lastname", contact.LastName },
                }
            };

            yield return new DataOperation<Entity>("Create", result);
        }
    }

    internal class Contact
    {
        internal string FirstName { get; set; }
        internal string LastName { get; set; }
    }
}
