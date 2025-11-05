namespace RagAgentApp.Models;

/// <summary>
/// Represents a trace of an agent's execution for observability.
/// </summary>
public class AgentExecutionTrace
{
    public string AgentName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int TokensUsed { get; set; }
    public decimal EstimatedCost { get; set; }
}

/// <summary>
/// Represents the full execution pipeline with all agent traces.
/// </summary>
public class PipelineExecutionTrace
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan TotalDuration => EndTime - StartTime;
    public List<AgentExecutionTrace> AgentTraces { get; set; } = new();
    public int TotalTokensUsed => AgentTraces.Sum(t => t.TokensUsed);
    public decimal TotalEstimatedCost => AgentTraces.Sum(t => t.EstimatedCost);
    public bool Success => AgentTraces.All(t => t.Success);
}
