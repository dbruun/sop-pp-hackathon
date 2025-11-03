using Microsoft.Extensions.Logging;
using RagAgentApp.Agents;

namespace RagAgentApp.Services;

/// <summary>
/// HACKATHON TODO: This is a stubbed implementation of the Orchestrator Service.
/// Your task is to implement agent orchestration that routes queries to both agents.
/// 
/// Implementation options:
/// 1. SIMPLE: Just call both agents directly and return their responses
/// 2. ADVANCED: Use an orchestrator agent with function calling to route queries
/// 
/// The simple approach is recommended for the hackathon. The advanced approach uses
/// Azure AI Agent Service with function tools to create an intelligent orchestrator.
/// </summary>
public class OrchestratorService
{
    private readonly SopRagAgent _sopAgent;
    private readonly PolicyRagAgent _policyAgent;
    private readonly ILogger<OrchestratorService> _logger;

    public OrchestratorService(
        SopRagAgent sopAgent, 
        PolicyRagAgent policyAgent, 
        ILogger<OrchestratorService> logger)
    {
        _sopAgent = sopAgent;
        _policyAgent = policyAgent;
        _logger = logger;
        
        _logger.LogInformation("OrchestratorService initialized (STUBBED VERSION): {SopAgent}, {PolicyAgent}", 
            sopAgent.AgentName, policyAgent.AgentName);
    }

    /// <summary>
    /// HACKATHON TODO: Implement orchestration logic to route queries to both agents.
    /// 
    /// Current behavior: Returns placeholder responses.
    /// 
    /// SIMPLE Implementation (Recommended for hackathon):
    /// 1. Call both _sopAgent.ProcessQueryAsync() and _policyAgent.ProcessQueryAsync()
    /// 2. Use Task.WhenAll() to run them in parallel
    /// 3. Return both responses in the dictionary
    /// 
    /// ADVANCED Implementation (Optional - uses Azure AI Agent with function calling):
    /// 1. Create an orchestrator agent in Azure AI Foundry
    /// 2. Define function tools for query_sop_agent and query_policy_agent
    /// 3. Let the orchestrator agent decide when to call each tool
    /// 4. Handle function calling and tool outputs
    /// 5. Return the responses from each agent
    /// 
    /// For the hackathon, the simple approach is perfectly fine!
    /// </summary>
    public async Task<Dictionary<string, string>> RouteQueryToAgentsAsync(
        string query, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Orchestrator processing query (STUB): {Query}", 
                query.Length > 100 ? query.Substring(0, 100) + "..." : query);

            // Simulate processing time
            await Task.Delay(200, cancellationToken);

            // TODO: Replace with actual agent calls
            // Simple approach:
            // var sopTask = _sopAgent.ProcessQueryAsync(query, cancellationToken);
            // var policyTask = _policyAgent.ProcessQueryAsync(query, cancellationToken);
            // await Task.WhenAll(sopTask, policyTask);
            // return new Dictionary<string, string>
            // {
            //     ["SOP Agent"] = sopTask.Result,
            //     ["Policy Agent"] = policyTask.Result
            // };

            // For now, return stub responses
            var sopResponse = await _sopAgent.ProcessQueryAsync(query, cancellationToken);
            var policyResponse = await _policyAgent.ProcessQueryAsync(query, cancellationToken);

            return new Dictionary<string, string>
            {
                ["SOP Agent"] = sopResponse,
                ["Policy Agent"] = policyResponse
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in orchestrator: {Message}", ex.Message);
            return new Dictionary<string, string>
            {
                ["SOP Agent"] = $"Orchestrator error: {ex.Message}",
                ["Policy Agent"] = $"Orchestrator error: {ex.Message}"
            };
        }
    }
}
