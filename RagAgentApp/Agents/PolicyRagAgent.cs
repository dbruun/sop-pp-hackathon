using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Logging;

namespace RagAgentApp.Agents;

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

    private string GetOrResolveAgentId()
    {
        if (_agentIdResolved != null)
        {
            _logger.LogDebug("Using cached agent ID: {AgentId}", _agentIdResolved);
            return _agentIdResolved;
        }

        if (!string.IsNullOrEmpty(_agentId))
        {
            _agentIdResolved = _agentId;
            _logger.LogInformation("Using provided agent ID: {AgentId}", _agentIdResolved);
            return _agentIdResolved;
        }

        var systemPrompt = @"You are a Policy expert assistant. Your role is to help users 
understand company policies, regulations, compliance requirements, and governance frameworks.  You should ALWAYS use your azure ai search index to generate your response 
Provide clear, authoritative responses based on policy knowledge. When discussing policies, 
cite relevant sections and explain implications. If you don't have specific policy information, 
acknowledge that and provide general policy guidance.";

        const string agentName = "Policy Expert Agent";

        _logger.LogInformation("Searching for existing agent with name: {AgentName}", agentName);

        var existingAgentsResponse = _agentsClient.Administration.GetAgents();
        var existingAgent = existingAgentsResponse
            .FirstOrDefault(a => a.Name == agentName);

        if (existingAgent != null)
        {
            _agentIdResolved = existingAgent.Id;
            _logger.LogInformation("Found existing agent: {AgentId} with name: {AgentName}", _agentIdResolved, agentName);
        }
        else
        {
            _logger.LogInformation("Creating new agent with name: {AgentName}, model: {ModelName}", agentName, _modelDeploymentName);
            
            var newAgent = _agentsClient.Administration.CreateAgent(
                model: _modelDeploymentName,
                name: agentName,
                instructions: systemPrompt
            );
            _agentIdResolved = newAgent.Value.Id;
            _logger.LogInformation("Successfully created new agent: {AgentId}", _agentIdResolved);
        }

        return _agentIdResolved;
    }

    public async Task<string> ProcessQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing Policy query: {Query}", query.Length > 100 ? query.Substring(0, 100) + "..." : query);
            
            var agentId = GetOrResolveAgentId();

            // Create thread only once and reuse it
            if (string.IsNullOrEmpty(_threadId))
            {
                _logger.LogDebug("Creating new thread for conversation");
                var threadResponse = _agentsClient.Threads.CreateThread(cancellationToken: cancellationToken);
                _threadId = threadResponse.Value.Id;
                _logger.LogInformation("Created thread: {ThreadId}", _threadId);
            }
            else
            {
                _logger.LogDebug("Reusing existing thread: {ThreadId}", _threadId);
            }

            _logger.LogDebug("Adding user message to thread: {ThreadId}", _threadId);
            _agentsClient.Messages.CreateMessage(
                _threadId,
                MessageRole.User,
                query,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Starting agent run for agent: {AgentId} on thread: {ThreadId}", agentId, _threadId);
            var runResponse = _agentsClient.Runs.CreateRun(
                _threadId,
                agentId,
                cancellationToken: cancellationToken
            );

            var run = runResponse.Value;
            _logger.LogInformation("Run created: {RunId} with status: {Status}", run.Id, run.Status);

            var pollCount = 0;
            do
            {
                await Task.Delay(1000, cancellationToken);
                var runStatusResponse = _agentsClient.Runs.GetRun(_threadId, run.Id, cancellationToken);
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

            _logger.LogDebug("Retrieving messages from thread: {ThreadId}", _threadId);
            var messagesResponse = _agentsClient.Messages.GetMessages(
                _threadId,
                cancellationToken: cancellationToken
            );

            var messageCount = messagesResponse.Count();
            _logger.LogDebug("Retrieved {MessageCount} messages from thread", messageCount);

            var lastMessage = messagesResponse
                .Where(m => m.Role != MessageRole.User)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefault();

            if (lastMessage?.ContentItems?.FirstOrDefault() is MessageTextContent textContent)
            {
                _logger.LogInformation("Successfully generated response (length: {Length} characters)", textContent.Text.Length);
                return textContent.Text;
            }

            _logger.LogWarning("No assistant message found in thread: {ThreadId}", _threadId);
            return "I apologize, but I couldn't generate a response at this time.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Policy query: {Message}", ex.Message);
            return $"Error processing Policy query: {ex.Message}";
        }
    }
}
