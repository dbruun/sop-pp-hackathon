using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Logging;
using RagAgentApp.Agents;
using System.Text.Json;

namespace RagAgentApp.Services;

/// <summary>
/// Orchestrator Service for Multi-Agent Coordination
/// 
/// HACKATHON TODO: Implement orchestration logic to route queries to both
/// SOP and Policy agents, then provide delta analysis of their responses.
/// </summary>
public class OrchestratorService
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly string _modelDeploymentName;
    private readonly SopRagAgent _sopAgent;
    private readonly PolicyRagAgent _policyAgent;
    private readonly ILogger<OrchestratorService> _logger;
    private string? _orchestratorAgentId;
    private string? _deltaAnalysisAgentId;
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
        
        _logger.LogInformation("OrchestratorService initialized as agent with tools: {SopAgent}, {PolicyAgent}", 
            sopAgent.AgentName, policyAgent.AgentName);
    }

    /// <summary>
    /// HACKATHON TODO: Implement this method
    /// 
    /// This method should:
    /// 1. Check if _orchestratorAgentId is already cached
    /// 2. Define a system prompt for orchestration
    /// 3. Create two FunctionToolDefinition objects:
    ///    - query_sop_agent: for calling SOP agent
    ///    - query_policy_agent: for calling Policy agent
    /// 4. Check if "Orchestrator Agent" exists, reuse or create new
    /// 5. Pass the function tools when creating the agent
    /// 6. Cache and return the agent ID
    /// 
    /// Hints:
    /// - Use FunctionToolDefinition for function calling tools
    /// - Parameters should be BinaryData.FromObjectAsJson()
    /// - System prompt should instruct to ALWAYS call BOTH tools
    /// </summary>
    private string GetOrResolveOrchestratorAgentId()
    {
        // TODO: Implement orchestrator agent creation with function calling
        // See HACKATHON.md for detailed instructions
        
        throw new NotImplementedException("HACKATHON TODO: Implement GetOrResolveOrchestratorAgentId() - See HACKATHON.md Task 3");
    }

    /// <summary>
    /// HACKATHON TODO: Implement this method
    /// 
    /// This method should:
    /// 1. Check if _deltaAnalysisAgentId is already cached
    /// 2. Define a system prompt for delta analysis
    /// 3. Check if "Delta Analysis Agent (No Tools)" exists
    /// 4. If it exists, DELETE it (to ensure no tools attached)
    /// 5. Create new agent with:
    ///    - name: "Delta Analysis Agent (No Tools)"
    ///    - instructions: analysis-focused prompt
    ///    - tools: new List<ToolDefinition>() -- EMPTY LIST!
    /// 6. Cache and return the agent ID
    /// 
    /// CRITICAL: This agent must NOT have any function calling tools!
    /// Pass an explicit empty list for tools parameter.
    /// </summary>
    private string GetOrResolveDeltaAnalysisAgentId()
    {
        // TODO: Implement delta analysis agent creation WITHOUT tools
        // See HACKATHON.md for detailed instructions
        
        throw new NotImplementedException("HACKATHON TODO: Implement GetOrResolveDeltaAnalysisAgentId() - See HACKATHON.md Task 4");
    }

    /// <summary>
    /// HACKATHON TODO: Implement this method
    /// 
    /// This method should:
    /// 1. Get orchestrator agent ID
    /// 2. Create/reuse a thread for the conversation
    /// 3. Add user message to the thread
    /// 4. Create and run the orchestrator agent
    /// 5. Poll for completion, handling RunStatus.RequiresAction:
    ///    - When status is RequiresAction, extract function calls
    ///    - For each function call:
    ///      * Parse the function name and arguments
    ///      * If "query_sop_agent", call _sopAgent.ProcessQueryAsync()
    ///      * If "query_policy_agent", call _policyAgent.ProcessQueryAsync()
    ///      * Store responses in agentResponses dictionary
    ///    - Create ToolOutput objects with results
    ///    - Submit tool outputs back to the run
    /// 6. Return dictionary with keys "SOP Agent" and "Policy Agent"
    /// 
    /// Hints:
    /// - Check for run.RequiredAction is SubmitToolOutputsAction
    /// - Use JsonDocument.Parse() to extract arguments
    /// - Use new ToolOutput(callId, result) to create outputs
    /// - Submit with _agentsClient.Runs.SubmitToolOutputsToRun()
    /// </summary>
    public async Task<Dictionary<string, string>> RouteQueryToAgentsAsync(
        string query, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Orchestrator processing query: {Query}", 
                query.Length > 100 ? query.Substring(0, 100) + "..." : query);

            var startTime = DateTime.UtcNow;
            
            // TODO: Implement orchestration logic with function calling
            // See HACKATHON.md for detailed instructions
            
            throw new NotImplementedException("HACKATHON TODO: Implement RouteQueryToAgentsAsync() - See HACKATHON.md Task 3");
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

    /// <summary>
    /// HACKATHON TODO: Implement this method
    /// 
    /// This method should:
    /// 1. Get delta analysis agent ID (the one WITHOUT tools)
    /// 2. Create a NEW thread for delta analysis
    /// 3. Create a detailed prompt that includes:
    ///    - Original question
    ///    - SOP Agent response
    ///    - Policy Agent response
    ///    - Instructions to create markdown tables comparing them
    /// 4. Add the prompt as a message
    /// 5. Create and run the agent
    /// 6. Poll for completion (should NOT hit requires_action)
    /// 7. Extract and return the analysis text
    /// 
    /// Hints:
    /// - Use structured markdown format with headers (##)
    /// - Request comparison tables: | Aspect | SOP Agent | Policy Agent |
    /// - Look for MessageRole.Agent in messages
    /// - Join text content items with newlines
    /// </summary>
    public async Task<string> AnalyzeDeltaAsync(
        string query,
        string sopResponse, 
        string policyResponse, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Analyzing delta between SOP and Policy agent responses");

            // TODO: Implement delta analysis logic
            // See HACKATHON.md for detailed instructions
            
            throw new NotImplementedException("HACKATHON TODO: Implement AnalyzeDeltaAsync() - See HACKATHON.md Task 4");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing delta: {Message}", ex.Message);
            return $"Error analyzing delta: {ex.Message}";
        }
    }
}
