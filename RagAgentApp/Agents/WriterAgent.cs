using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Logging;

namespace RagAgentApp.Agents;

/// <summary>
/// WriterAgent: Drafts responses with inline citations based on retrieved passages.
/// Generates well-structured content with proper source attribution.
/// </summary>
public class WriterAgent : IAgentService
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly string _modelDeploymentName;
    private readonly ILogger<WriterAgent> _logger;
    private string? _agentIdResolved;

    public string AgentName => "Writer Agent";

    public WriterAgent(PersistentAgentsClient agentsClient, string modelDeploymentName, ILogger<WriterAgent> logger)
    {
        _agentsClient = agentsClient;
        _modelDeploymentName = modelDeploymentName;
        _logger = logger;
        
        _logger.LogInformation("WriterAgent initialized with model: {ModelName}", modelDeploymentName);
    }

    private string GetOrResolveAgentId()
    {
        if (_agentIdResolved != null)
        {
            _logger.LogDebug("Using cached agent ID: {AgentId}", _agentIdResolved);
            return _agentIdResolved;
        }

        var systemPrompt = @"You are a Writer Agent responsible for drafting responses with inline citations.
Your role is to:
1. Read the retrieved passages provided by the Search Agent
2. Synthesize information from multiple sources
3. Draft a well-structured, coherent response
4. Include inline citations in the format [Source: Document Name, Page X]
5. Ensure all claims are supported by the provided passages

Guidelines:
- Write in a clear, professional tone
- Use proper formatting with headings and bullet points where appropriate
- Always cite sources for factual claims
- If information is insufficient, acknowledge gaps
- Maintain accuracy and avoid speculation

Your response should be in markdown format with inline citations.";

        const string agentName = "Writer Agent";

        _logger.LogInformation("Searching for existing agent with name: {AgentName}", agentName);

        var existingAgentsResponse = _agentsClient.Administration.GetAgents();
        var existingAgent = existingAgentsResponse.FirstOrDefault(a => a.Name == agentName);

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
            _logger.LogInformation("WriterAgent drafting response with citations for query: {Query}", 
                query.Length > 100 ? query.Substring(0, 100) + "..." : query);
            
            var agentId = GetOrResolveAgentId();

            // Create a new thread for each writing task (stateless)
            _logger.LogDebug("Creating new thread for writing");
            var threadResponse = _agentsClient.Threads.CreateThread(cancellationToken: cancellationToken);
            var threadId = threadResponse.Value.Id;
            _logger.LogInformation("Created thread: {ThreadId}", threadId);

            _logger.LogDebug("Adding user message to thread: {ThreadId}", threadId);
            _agentsClient.Messages.CreateMessage(
                threadId,
                MessageRole.User,
                query,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Starting agent run for agent: {AgentId} on thread: {ThreadId}", agentId, threadId);
            var runResponse = _agentsClient.Runs.CreateRun(
                threadId,
                agentId,
                cancellationToken: cancellationToken
            );

            var run = runResponse.Value;
            _logger.LogInformation("Run created: {RunId} with status: {Status}", run.Id, run.Status);

            var pollCount = 0;
            do
            {
                await Task.Delay(1000, cancellationToken);
                var runStatusResponse = _agentsClient.Runs.GetRun(threadId, run.Id, cancellationToken);
                run = runStatusResponse.Value;
                pollCount++;
                
                if (pollCount % 5 == 0)
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

            _logger.LogDebug("Retrieving messages from thread: {ThreadId}", threadId);
            var messagesResponse = _agentsClient.Messages.GetMessages(
                threadId,
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
                _logger.LogInformation("Successfully drafted response with citations (length: {Length} characters)", textContent.Text.Length);
                return textContent.Text;
            }

            _logger.LogWarning("No assistant message found in thread: {ThreadId}", threadId);
            return "I apologize, but I couldn't draft a response at this time.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error drafting response: {Message}", ex.Message);
            return $"Error drafting response: {ex.Message}";
        }
    }
}
