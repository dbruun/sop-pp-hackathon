using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Logging;

namespace RagAgentApp.Agents;

public class SopRagAgent : IAgentService
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly string _modelDeploymentName;
    private readonly string? _agentId;
    private string? _agentIdResolved;
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

    private string GetOrResolveAgentId()
    {
        if (_agentIdResolved == null)
        {
            // If agent ID is provided, use it directly
            if (!string.IsNullOrEmpty(_agentId))
            {
                _agentIdResolved = _agentId;
            }
            else
            {
                // Create or reuse agent by name
                var systemPrompt = @"You are a Standard Operating Procedures (SOP) expert assistant. 
Your role is to help users understand and find information about standard operating procedures, 
work instructions, and process documentation. Provide clear, structured responses based on 
standard operating procedures knowledge. If you don't have specific information, acknowledge 
that and provide general guidance on SOPs.";

                const string agentName = "SOP Expert Agent";

                _logger.LogInformation("Searching for existing agent with name: {AgentName}", agentName);
                
                // Check if an agent with this name already exists in Azure AI Foundry
                var existingAgents = _agentsClient.Administration.GetAgents();
                var existingAgent = existingAgents.FirstOrDefault(a => a.Name == agentName);

                if (existingAgent != null)
                {
                    // Reuse the existing agent
                    _agentIdResolved = existingAgent.Id;
                    _logger.LogInformation("Found existing agent: {AgentId} with name: {AgentName}", _agentIdResolved, agentName);
                }
                else
                {
                    _logger.LogInformation("Creating new agent with name: {AgentName}, model: {ModelName}", agentName, _modelDeploymentName);
                    
                    // Create a new agent if none exists
                    var newAgent = _agentsClient.Administration.CreateAgent(
                        model: _modelDeploymentName,
                        name: agentName,
                        instructions: systemPrompt
                    );
                    _agentIdResolved = newAgent.Value.Id;
                    _logger.LogInformation("Successfully created new agent: {AgentId}", _agentIdResolved);
                }
            }
        }
        else
        {
            _logger.LogDebug("Using cached agent ID: {AgentId}", _agentIdResolved);
        }
        return _agentIdResolved;
    }

    public async Task<string> ProcessQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing SOP query: {Query}", query.Length > 100 ? query.Substring(0, 100) + "..." : query);
            
            // Get or resolve the agent ID
            var agentId = GetOrResolveAgentId();

            _logger.LogDebug("Creating new thread for conversation");
            // Create a new thread for this conversation
            var threadResponse = _agentsClient.Threads.CreateThread();
            var thread = threadResponse.Value;
            _logger.LogInformation("Created thread: {ThreadId}", thread.Id);

            _logger.LogDebug("Adding user message to thread: {ThreadId}", thread.Id);
            // Add the user message to the thread
            _agentsClient.Messages.CreateMessage(
                thread.Id,
                MessageRole.User,
                query
            );

            _logger.LogInformation("Starting agent run for agent: {AgentId} on thread: {ThreadId}", agentId, thread.Id);
            // Create and run the agent
            var runResponse = _agentsClient.Runs.CreateRun(
                thread.Id,
                agentId
            );
            var run = runResponse.Value;
            _logger.LogInformation("Run created: {RunId} with status: {Status}", run.Id, run.Status);

            // Poll for completion
            var pollCount = 0;
            do
            {
                await Task.Delay(1000, cancellationToken);
                var runStatusResponse = _agentsClient.Runs.GetRun(thread.Id, run.Id);
                run = runStatusResponse.Value;
                pollCount++;
                
                if (pollCount % 5 == 0) // Log every 5 seconds
                {
                    _logger.LogDebug("Run {RunId} status: {Status} (polled {Count} times)", run.Id, run.Status, pollCount);
                }
            } while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress);

            _logger.LogInformation("Run {RunId} completed with status: {Status}", run.Id, run.Status);

            if (run.Status == RunStatus.Failed)
            {
                _logger.LogError("Run failed with error: {Error}", run.LastError?.Message ?? "Unknown error");
                return $"The agent run failed: {run.LastError?.Message ?? "Unknown error"}";
            }

            _logger.LogDebug("Retrieving messages from thread: {ThreadId}", thread.Id);
            // Get the messages
            var messages = _agentsClient.Messages.GetMessages(thread.Id);
            var messageCount = messages.Count();
            _logger.LogDebug("Retrieved {MessageCount} messages from thread", messageCount);

            // Get the last assistant message (assistant messages are those NOT from User)
            var lastMessage = messages.FirstOrDefault(m => m.Role != MessageRole.User);

            if (lastMessage?.ContentItems?.FirstOrDefault() is MessageTextContent textContent)
            {
                _logger.LogInformation("Successfully generated response (length: {Length} characters)", textContent.Text.Length);
                return textContent.Text;
            }

            _logger.LogWarning("No assistant message found in thread: {ThreadId}", thread.Id);
            return "I apologize, but I couldn't generate a response at this time.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SOP query: {Message}", ex.Message);
            return $"Error processing SOP query: {ex.Message}";
        }
    }
}
