﻿using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Root16.Sprout.DataSources;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.BatchProcessing;

namespace Root16.Sprout.Sample.CreatesAndUpdates;

internal class CreateContactTestStep : BatchIntegrationStep<CreateContact, Entity>
{
    private readonly DataverseDataSource dataverseDataSource;
    private readonly EntityOperationReducer reducer;
    private readonly BatchProcessor batchProcessor;
    private readonly MemoryDataSource<CreateContact> memoryDS;

    public CreateContactTestStep(MemoryDataSource<CreateContact> memoryDS, DataverseDataSource dataverseDataSource, EntityOperationReducer reducer, BatchProcessor batchProcessor)
    {
        this.dataverseDataSource = dataverseDataSource;
        this.reducer = reducer;
        this.batchProcessor = batchProcessor;
        this.memoryDS = memoryDS;
        DryRun = true;
        BatchSize = 200;
    }

    public override async Task<IReadOnlyList<CreateContact>> OnBeforeMapAsync(IReadOnlyList<CreateContact> batch)
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

        reducer.SetPotentialMatches(matches.Entities);

        return batch;
    }

    public override IReadOnlyList<DataOperation<Entity>> OnBeforeDelivery(IReadOnlyList<DataOperation<Entity>> batch)
    {
        return reducer.ReduceOperations(batch, entity => string.Concat(
                entity.GetAttributeValue<string>("firstname"),
                "|",
                entity.GetAttributeValue<string>("lastname")
        ));
    }

    public override async Task RunAsync(string stepName)
    {
        await batchProcessor.ProcessBatchesAsync(this, stepName, 5);
    }

    public override IDataSource<Entity> OutputDataSource => dataverseDataSource;

    public override IPagedQuery<CreateContact> GetInputQuery()
    {
        return memoryDS.CreatePagedQuery();
    }

    public override IReadOnlyList<DataOperation<Entity>> MapRecord(CreateContact source)
    {
        var result = new Entity("contact")
        {
            Attributes =
            {
                {"firstname", source.FirstName },
                {"lastname", source.LastName },
            }
        };

        return [new DataOperation<Entity>("Create", result)];
    }
}
