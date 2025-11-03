using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Logging;
using RagAgentApp.Agents;
using System.Text.Json;

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
    private readonly PersistentAgentsClient _agentsClient;
    private readonly string _modelDeploymentName;
    private readonly SopRagAgent _sopAgent;
    private readonly PolicyRagAgent _policyAgent;
    private readonly ILogger<OrchestratorService> _logger;
    private string? _orchestratorAgentId;
    private string? _threadId;

    public OrchestratorService(
        PersistentAgentsClient agentsClient,
        string modelDeploymentName,
        SopRagAgent sopAgent, 
        PolicyRagAgent policyAgent, 
        ILogger<OrchestratorService> logger)
    {
        _agentsClient = agentsClient;
        _modelDeploymentName = modelDeploymentName;
        _sopAgent = sopAgent;
        _policyAgent = policyAgent;
        _logger = logger;
        
        _logger.LogInformation("OrchestratorService initialized (HACKATHON STUB): {SopAgent}, {PolicyAgent}", 
            sopAgent.AgentName, policyAgent.AgentName);
    }

    /// <summary>
    /// HACKATHON TODO (ADVANCED): Implement this method for advanced orchestration.
    /// This is only needed if you want to use an orchestrator agent with function calling.
    /// 
    /// For the simple approach, you don't need this method - just call both agents directly in RouteQueryToAgentsAsync.
    /// 
    /// Steps for advanced implementation:
    /// 1. Check if _orchestratorAgentId is cached, return it if so
    /// 2. Search for existing "Orchestrator Agent" by name
    /// 3. If found, cache and return its ID
    /// 4. If not found, create a new orchestrator agent with:
    ///    - FunctionToolDefinition for query_sop_agent
    ///    - FunctionToolDefinition for query_policy_agent
    /// 5. Cache and return the new agent ID
    /// </summary>
    private string GetOrResolveOrchestratorAgentId()
    {
        // TODO: Implement orchestrator agent creation (only for advanced approach)
        _logger.LogWarning("GetOrResolveOrchestratorAgentId not implemented - using simple orchestration");
        return "stub-orchestrator-id";
    }

    /// <summary>
    /// HACKATHON TODO: Implement orchestration logic to route queries to both agents.
    /// 
    /// Current behavior: Calls both stubbed agents and returns their placeholder responses.
    /// 
    /// SIMPLE Implementation (Recommended for hackathon - Level 1):
    /// The current implementation already does this! Once you implement the agents themselves,
    /// this will automatically work. It calls both agents in parallel and returns both responses.
    /// 
    /// ADVANCED Implementation (Optional - Level 3 - uses Azure AI Agent with function calling):
    /// 1. Call GetOrResolveOrchestratorAgentId() to get/create orchestrator agent
    /// 2. Create or reuse thread (_threadId)
    /// 3. Add user message to thread
    /// 4. Create run with orchestrator agent
    /// 5. Poll for completion, handling RequiresAction status for function calls
    /// 6. When RequiresAction detected, process function calls (query_sop_agent, query_policy_agent)
    /// 7. Submit tool outputs back to the run
    /// 8. Continue polling until complete
    /// 9. Return individual agent responses
    /// 
    /// For hackathon Level 1 & 2, the current simple approach works great!
    /// </summary>
    public async Task<Dictionary<string, string>> RouteQueryToAgentsAsync(
        string query, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Orchestrator processing query: {Query}", 
                query.Length > 100 ? query.Substring(0, 100) + "..." : query);

            // SIMPLE APPROACH (currently implemented - works once you implement the agents):
            // Call both agents in parallel and return their responses
            var sopResponse = await _sopAgent.ProcessQueryAsync(query, cancellationToken);
            var policyResponse = await _policyAgent.ProcessQueryAsync(query, cancellationToken);

            return new Dictionary<string, string>
            {
                ["SOP Agent"] = sopResponse,
                ["Policy Agent"] = policyResponse
            };

            // TODO (ADVANCED - Level 3): For function calling approach, uncomment and implement:
            // 1. Get orchestrator agent: var agentId = GetOrResolveOrchestratorAgentId();
            // 2. Create/reuse thread: if (string.IsNullOrEmpty(_threadId)) { ... }
            // 3. Add message: _agentsClient.Messages.CreateMessage(_threadId, MessageRole.User, query, ...);
            // 4. Create run: var runResponse = _agentsClient.Runs.CreateRun(_threadId, agentId, ...);
            // 5. Poll and handle function calls in a loop checking run.Status and run.RequiredAction
            // 6. Process SubmitToolOutputsAction and call appropriate agents
            // 7. Submit tool outputs back
            // 8. Return agent responses dictionary
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
