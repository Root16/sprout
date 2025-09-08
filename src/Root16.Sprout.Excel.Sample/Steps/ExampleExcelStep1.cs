using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Root16.Sprout.BatchProcessing;
using Root16.Sprout.DataSources;
using Root16.Sprout.DataSources.Dataverse;
using Root16.Sprout.Excel.Factories;
using Root16.Sprout.Excel.Sample.Models;

namespace Root16.Sprout.Excel.Sample.Steps;

public class ExampleExcelStep1 : BatchIntegrationStep<TestClass1, Entity>
{

    public ExampleExcelStep1(BatchProcessor batcher, IExcelDataSourceFactory excelDataSourceFactory, DataverseDataSource dataverseDataSource, EntityOperationReducer reducer)
	{
		this.batcher = batcher;
		this.dataverseDataSource = dataverseDataSource;
		this.reducer = reducer;
		this._excelDataSource = excelDataSourceFactory.GetExcelDataSourceByName<TestClass1>("EXCEL1");
		KeySelector = entity => entity.GetAttributeValue<string>("name");
	}
	private readonly BatchProcessor batcher;
	private readonly IExcelDataSourceFactory excelDataSourceFactory;
	private readonly ExcelDataSource<TestClass1> _excelDataSource;
	private readonly DataverseDataSource dataverseDataSource;
	private readonly EntityOperationReducer reducer;
	protected List<Entity> PotentialMatches = [];

	public override IDataSource<Entity> OutputDataSource => dataverseDataSource;

	public override IPagedQuery<TestClass1> GetInputQuery()
	{
		return _excelDataSource.CreatePagedQuery();
	}

	public override IReadOnlyList<DataOperation<Entity>> MapRecord(TestClass1 source)
	{
		var entity = new Entity("account");
		entity["name"] = source.AccountName;
		entity["address1_line1"] = source.Address1;
		entity["cref1_myfloat"] = (decimal)source.MyCoolFloat; //D365 hates floats
		entity["exchangerate"] = source.DecimalHere;
		entity["numberofemployees"] = source.SimpleWholeNumber;

		return [new DataOperation<Entity>("Create", entity)];
	}
	public override async Task<IReadOnlyList<TestClass1>> OnBeforeMapAsync(IReadOnlyList<TestClass1> batch)
	{
		var names = batch.Select(b => $"<value>{b.AccountName}</value>");
		if (batch.Count() > 0)
		{
			var fetchXml = $@"
                <fetch>
                    <entity name='account'>
                    <attribute name='name'/>
                    <attribute name='address1_line1'/>
                    <filter>
                        <condition attribute='name' operator='in'>
                                {string.Join("", names)}
                        </condition>
                    </filter>
                    </entity>
                </fetch>";

			var potentialMatches = await dataverseDataSource.CrmServiceClient.RetrieveMultipleWithRetryAsync(new FetchExpression(fetchXml));
			PotentialMatches = [.. potentialMatches.Entities];
		}
		else
		{
			PotentialMatches.Clear();
		}
		return batch;
	}

	public override IReadOnlyList<DataOperation<Entity>> OnBeforeDelivery(IReadOnlyList<DataOperation<Entity>> batch)
	{
		reducer.SetPotentialMatches(PotentialMatches);
		return reducer.ReduceOperations(batch, KeySelector!);
	}


	public override Task RunAsync(string stepName)
	{
		BatchSize = 200;
		return batcher.ProcessBatchesAsync(this, stepName);
	}
}
