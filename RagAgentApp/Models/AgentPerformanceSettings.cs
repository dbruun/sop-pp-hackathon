namespace RagAgentApp.Models;

/// <summary>
/// Performance configuration for agent execution
/// </summary>
public class AgentPerformanceSettings
{
    /// <summary>
    /// Temperature for simple agents (Intake, Search, Executor) - lower = faster
    /// </summary>
    public double SimpleAgentTemperature { get; set; } = 0.3;
    
    /// <summary>
    /// Temperature for complex agents (Writer, Reviewer) - balanced
    /// </summary>
    public double ComplexAgentTemperature { get; set; } = 0.7;
    
    /// <summary>
    /// Max tokens for simple agent responses
    /// </summary>
    public int SimpleAgentMaxTokens { get; set; } = 500;
    
    /// <summary>
    /// Max tokens for complex agent responses
    /// </summary>
    public int ComplexAgentMaxTokens { get; set; } = 2000;
    
    /// <summary>
    /// Whether to enable parallel execution where possible
    /// </summary>
    public bool EnableParallelExecution { get; set; } = false;
    
    /// <summary>
    /// Timeout in seconds for each agent
    /// </summary>
    public int AgentTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Whether to use faster model for simple agents (e.g., gpt-4o-mini)
    /// </summary>
    public bool UseFasterModelForSimpleAgents { get; set; } = false;
    
    /// <summary>
    /// Fast model deployment name (e.g., gpt-4o-mini or gpt-35-turbo)
    /// </summary>
    public string? FastModelDeploymentName { get; set; }
}
