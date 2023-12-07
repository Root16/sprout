using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Root16.Sprout.Data;
using Root16.Sprout.Processors;
using Root16.Sprout.Query;
using Root16.Sprout.Step;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Root16.Sprout.Sample
{
    internal class TestStep : BatchIntegrationStep<Contact,Entity>
    {
        private readonly DataverseDataSource dataverseDataSource;
        private readonly EntityReducer entityReducer;
        private readonly BatchRunner runner;
        private MemoryDataSource<Contact> memoryDS;

        public TestStep(MemoryDataSource<Contact> memoryDS, DataverseDataSource dataverseDataSource, EntityReducer entityReducer, BatchRunner runner)
        {
            this.dataverseDataSource = dataverseDataSource;
            this.entityReducer = entityReducer;
            this.runner = runner;
            this.memoryDS = memoryDS;
        }

        public override async Task<IReadOnlyList<Contact>> OnBeforeMapAsync(IReadOnlyList<Contact> batch)
        {
            var firstNameValues = string.Join("</value><value>", batch.Select(b => b.FirstName).Distinct(StringComparer.OrdinalIgnoreCase));
            var lastNameValues = string.Join("</value><value>", batch.Select(b => b.LastName).Distinct(StringComparer.OrdinalIgnoreCase));

            var matches = await dataverseDataSource.CrmServiceClient.RetrieveMultipleAsync(new FetchExpression($@"
                <fetch>
                    <entity name='contact'>
                        <attribute name='firstname' />
                        <attribute name='lastname' />
                        <filter>
                            <condition attribute='firstname' operator='in'>
                                <value>{firstNameValues}</value>
                            </condition>
                            <condition attribute='lastname' operator='in'>
                                <value>{lastNameValues}</value>
                            </condition>
                        </filter>
                    </entity>
                </fetch>"));

            entityReducer.SetPotentialMatches(matches.Entities);

            return batch;
        }

        public override IReadOnlyList<DataOperation<Entity>> OnBeforeDelivery(IReadOnlyList<DataOperation<Entity>> batch)
        {
            return entityReducer.ReduceChanges(batch, entity => string.Concat(
                    entity.GetAttributeValue<string>("firstname"),
                    "|",
                    entity.GetAttributeValue<string>("lastname")
            ));
        }

        public override async Task RunAsync()
        {
            await runner.ProcessBatchAsync(this, null);
        }

        public override IDataSource<Entity> OutputDataSource => dataverseDataSource;

        public override IPagedQuery<Contact> GetSourceQuery()
        {
            return memoryDS.CreatePagedQuery();
        }

        public override IReadOnlyList<DataOperation<Entity>> MapRecord(Contact source)
        {
            var result = new Entity("contact")
            {
                Attributes =
                {
                    {"firstname", source.FirstName },
                    {"lastname", source.LastName },
                }
            };

            return new[] { new DataOperation<Entity>("Create", result) };
        }

    }

    internal class Contact
    {
        internal string? FirstName { get; set; }
        internal string? LastName { get; set; }
    }
}
