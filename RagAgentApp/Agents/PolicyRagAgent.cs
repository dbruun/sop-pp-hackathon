using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Logging;

namespace RagAgentApp.Agents;

/// <summary>
/// Policy RAG Agent
/// 
/// HACKATHON TODO: Implement this agent to retrieve and provide information about
/// company policies, regulations, compliance requirements, and governance.
/// </summary>
public class PolicyRagAgent : IAgentService
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly string _modelDeploymentName;
    private readonly string? _agentId;
    private string? _agentIdResolved;
    private string? _threadId;
    private readonly ILogger<PolicyRagAgent> _logger;

    public string AgentName => "Policy Agent";

    public PolicyRagAgent(PersistentAgentsClient agentsClient, string modelDeploymentName, ILogger<PolicyRagAgent> logger, string? agentId = null)
    {
        _agentsClient = agentsClient;
        _modelDeploymentName = modelDeploymentName;
        _agentId = agentId;
        _logger = logger;
        
        _logger.LogInformation("PolicyRagAgent initialized with model: {ModelName}, AgentId: {AgentId}", 
            modelDeploymentName, agentId ?? "not provided");
    }

    /// <summary>
    /// HACKATHON TODO: Implement this method
    /// 
    /// This method should:
    /// 1. Check if _agentIdResolved is already set (cached)
    /// 2. If _agentId is provided, use it directly
    /// 3. Otherwise, create or find an existing agent:
    ///    - Define a system prompt for Policy expertise
    ///    - Check if an agent with name "Policy Expert Agent" exists
    ///    - If exists, reuse it; if not, create a new one
    /// 4. Cache the agent ID in _agentIdResolved
    /// 5. Return the agent ID
    /// 
    /// Hints:
    /// - Similar to SopRagAgent but with policy-focused prompt
    /// - System prompt should mention using Azure AI Search index
    /// - Use _agentsClient.Administration.GetAgents() and CreateAgent()
    /// </summary>
    private string GetOrResolveAgentId()
    {
        // TODO: Implement agent creation/retrieval logic
        // See HACKATHON.md for detailed instructions
        
        throw new NotImplementedException("HACKATHON TODO: Implement GetOrResolveAgentId() - See HACKATHON.md Task 2");
    }

    /// <summary>
    /// HACKATHON TODO: Implement this method
    /// 
    /// This method should:
    /// 1. Get the agent ID using GetOrResolveAgentId()
    /// 2. Create or reuse a conversation thread
    /// 3. Add the user's query as a message
    /// 4. Create and run the agent
    /// 5. Poll for completion
    /// 6. Retrieve and return the agent's response
    /// 7. Handle errors appropriately
    /// 
    /// Hints:
    /// - Pattern is identical to SopRagAgent
    /// - Reuse thread management approach
    /// - Look for non-User role messages in response
    /// </summary>
    public async Task<string> ProcessQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing Policy query: {Query}", query.Length > 100 ? query.Substring(0, 100) + "..." : query);
            
            // TODO: Implement query processing logic
            // See HACKATHON.md for detailed instructions
            
            throw new NotImplementedException("HACKATHON TODO: Implement ProcessQueryAsync() - See HACKATHON.md Task 2");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Policy query: {Message}", ex.Message);
            return $"Error processing Policy query: {ex.Message}";
        }
    }
}
