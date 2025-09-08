namespace Root16.Sprout.DataSources.Dataverse;

public static class DataverseDataSourceFlags
{
    /// <summary>
    /// Bypass only synchronous logic. This optional parameter is supported, but not recommended. Use BypassBusinessLogicExecutionSync to get the same result.
    /// </summary>
    [Obsolete("Use BypassBusinessLogicExecutionSync to get the same result for Dataverse.")]
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
    /// <summary>
    /// Do not pass this flag directly. Instead, pass a comma separated list of plug-in step registrations to bypass only the specified plug-in steps.
    /// </summary>
    internal static readonly string BypassBusinessLogicExecutionStepIds = nameof(BypassBusinessLogicExecutionStepIds);
}