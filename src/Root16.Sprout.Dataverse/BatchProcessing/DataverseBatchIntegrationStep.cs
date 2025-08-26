using Root16.Sprout.DataSources.Dataverse;

namespace Root16.Sprout.BatchProcessing.Dataverse;

public abstract partial class DataverseBatchIntegrationStep<TInput, TOutput> : BatchIntegrationStep<TInput, TOutput> 
    where TOutput : class 
{
    protected DataverseBatchIntegrationStep() : base() { }

    public void BypassCustomBusinessLogic(BusinessLogicType type = BusinessLogicType.Both)
    {
        switch (type)
        {
            case BusinessLogicType.Both: AddDataOperationFlag(DataverseDataSourceFlags.BypassBusinessLogicExecution); break;
            case BusinessLogicType.Synchronous: AddDataOperationFlag(DataverseDataSourceFlags.BypassBusinessLogicExecutionSync); break;
            case BusinessLogicType.Asynchronous: AddDataOperationFlag(DataverseDataSourceFlags.BypassBusinessLogicExecutionAsync); break;
            default: throw new InvalidOperationException($"Unsupported {nameof(BusinessLogicType)}.");
        }
    }
    public void BypassPowerAutomateFlows() => AddDataOperationFlag(DataverseDataSourceFlags.SuppressCallbackRegistrationExpanderJob);
    public void BypassPluginStepIds(params string[] stepIds) => AddDataOperationFlag(string.Join(",", stepIds));
    public void BypassPluginStepIds(params Guid[] stepIds) => AddDataOperationFlag(string.Join(",", stepIds));
}