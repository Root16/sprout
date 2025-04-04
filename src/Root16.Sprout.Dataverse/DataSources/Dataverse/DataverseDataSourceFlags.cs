namespace Root16.Sprout.DataSources.Dataverse;

public static class DataverseDataSourceFlags
{
    /// <summary>
    /// Bypass only synchronous logic. This optional parameter is supported, but not recommended. Use BypassBusinessLogicExecution with the CustomSync value to get the same result.
    /// </summary>
    public static readonly string BypassCustomPluginExecution = nameof(BypassCustomPluginExecution);
    /// <summary>
    /// Bypass Power Automate Flows.
    /// </summary>
    public static readonly string SuppressCallbackRegistrationExpanderJob = nameof(SuppressCallbackRegistrationExpanderJob);
    /// <summary>
    /// Flag for BypassBusinessLogicExecution. Bypass both synchronous and asynchronous custom logic, excluding Power Automate Flows.
    /// </summary>
    public static readonly string BypassBusinessLogicExecution = nameof(BypassBusinessLogicExecution);
    /// <summary>
    /// Flag for BypassBusinessLogicExecution. Bypass only asynchronous custom logic, excluding Power Automate Flows.
    /// </summary>
    public static readonly string BypassBusinessLogicExecutionAsync = nameof(BypassBusinessLogicExecutionAsync);
    /// <summary>
    /// Flag for BypassBusinessLogicExecution. Bypass only synchronous custom logic.
    /// </summary>
    public static readonly string BypassBusinessLogicExecutionSync = nameof(BypassBusinessLogicExecutionSync);
}