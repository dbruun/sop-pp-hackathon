using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Logging;

namespace RagAgentApp.Agents;

/// <summary>
/// SOP (Standard Operating Procedures) RAG Agent
/// 
/// HACKATHON TODO: Implement this agent to retrieve and provide information about
/// Standard Operating Procedures, work instructions, and process documentation.
/// </summary>
public class SopRagAgent : IAgentService
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly string _modelDeploymentName;
    private readonly string? _agentId;
    private string? _agentIdResolved;
    private string? _threadId;
    private readonly ILogger<SopRagAgent> _logger;

    public string AgentName => "SOP Agent";

    public SopRagAgent(PersistentAgentsClient agentsClient, string modelDeploymentName, ILogger<SopRagAgent> logger, string? agentId = null)
    {
        _agentsClient = agentsClient;
        _modelDeploymentName = modelDeploymentName;
        _agentId = agentId;
        _logger = logger;
        
        _logger.LogInformation("SopRagAgent initialized with model: {ModelName}, AgentId: {AgentId}", 
            modelDeploymentName, agentId ?? "not provided");
    }

    /// <summary>
    /// HACKATHON TODO: Implement this method
    /// 
    /// This method should:
    /// 1. Check if _agentIdResolved is already set (cached)
    /// 2. If _agentId is provided, use it directly
    /// 3. Otherwise, create or find an existing agent:
    ///    - Define a system prompt for SOP expertise
    ///    - Check if an agent with name "SOP Expert Agent" exists
    ///    - If exists, reuse it; if not, create a new one
    /// 4. Cache the agent ID in _agentIdResolved
    /// 5. Return the agent ID
    /// 
    /// Hints:
    /// - Use _agentsClient.Administration.GetAgents() to list agents
    /// - Use _agentsClient.Administration.CreateAgent() to create new agents
    /// - System prompt should mention using Azure AI Search index
    /// </summary>
    private string GetOrResolveAgentId()
    {
        // TODO: Implement agent creation/retrieval logic
        // See HACKATHON.md for detailed instructions
        
        throw new NotImplementedException("HACKATHON TODO: Implement GetOrResolveAgentId() - See HACKATHON.md Task 1");
    }

    /// <summary>
    /// HACKATHON TODO: Implement this method
    /// 
    /// This method should:
    /// 1. Get the agent ID using GetOrResolveAgentId()
    /// 2. Create or reuse a conversation thread:
    ///    - If _threadId is null, create new thread
    ///    - Otherwise, reuse existing thread
    /// 3. Add the user's query as a message to the thread
    /// 4. Create and run the agent on the thread
    /// 5. Poll for completion (wait until RunStatus is Completed or Failed)
    /// 6. Retrieve messages and return the agent's response
    /// 7. Handle errors appropriately
    /// 
    /// Hints:
    /// - Use _agentsClient.Threads.CreateThread() for new threads
    /// - Use _agentsClient.Messages.CreateMessage() to add messages
    /// - Use _agentsClient.Runs.CreateRun() to start the agent
    /// - Use _agentsClient.Runs.GetRun() to check status
    /// - Poll with Task.Delay(1000) between checks
    /// - Get messages with _agentsClient.Messages.GetMessages()
    /// - Look for MessageTextContent in the response
    /// </summary>
    public async Task<string> ProcessQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing SOP query: {Query}", query.Length > 100 ? query.Substring(0, 100) + "..." : query);
            
            // TODO: Implement query processing logic
            // See HACKATHON.md for detailed instructions
            
            throw new NotImplementedException("HACKATHON TODO: Implement ProcessQueryAsync() - See HACKATHON.md Task 1");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SOP query: {Message}", ex.Message);
            return $"Error processing SOP query: {ex.Message}";
        }
    }
}
