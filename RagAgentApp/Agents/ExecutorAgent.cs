using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Logging;

namespace RagAgentApp.Agents;

/// <summary>
/// ExecutorAgent: Handles final output rendering and formatting for the chat window.
/// Formats the validated response for display to the user.
/// </summary>
public class ExecutorAgent : IAgentService
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly string _modelDeploymentName;
    private readonly ILogger<ExecutorAgent> _logger;
    private string? _agentIdResolved;

    public string AgentName => "Executor Agent";

    public ExecutorAgent(PersistentAgentsClient agentsClient, string modelDeploymentName, ILogger<ExecutorAgent> logger)
    {
        _agentsClient = agentsClient;
        _modelDeploymentName = modelDeploymentName;
        _logger = logger;
        
        _logger.LogInformation("ExecutorAgent initialized with model: {ModelName}", modelDeploymentName);
    }

    private string GetOrResolveAgentId()
    {
        if (_agentIdResolved != null)
        {
            _logger.LogDebug("Using cached agent ID: {AgentId}", _agentIdResolved);
            return _agentIdResolved;
        }

        var systemPrompt = @"You are an Executor Agent responsible for final output formatting and presentation.
Your role is to:
1. Take the reviewed and validated response
2. Format it appropriately for chat window display
3. Ensure proper markdown rendering
4. Add any necessary formatting enhancements (headings, lists, code blocks)
5. Include metadata like quality scores and citations if relevant

Guidelines:
- Use clear, readable markdown formatting
- Ensure citations are properly formatted and easy to identify
- Add visual separators where appropriate
- Include quality indicators if provided by the reviewer
- Optimize for readability in a chat interface

Your output should be well-formatted markdown ready for immediate display.";

        const string agentName = "Executor Agent";

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
            _logger.LogInformation("ExecutorAgent formatting output for display: {Query}", 
                query.Length > 100 ? query.Substring(0, 100) + "..." : query);
            
            var agentId = GetOrResolveAgentId();

            // Create a new thread for each execution (stateless)
            _logger.LogDebug("Creating new thread for execution");
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
                _logger.LogInformation("Successfully formatted output (length: {Length} characters)", textContent.Text.Length);
                return textContent.Text;
            }

            _logger.LogWarning("No assistant message found in thread: {ThreadId}", threadId);
            return "I apologize, but I couldn't format the output at this time.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting output: {Message}", ex.Message);
            return $"Error formatting output: {ex.Message}";
        }
    }
}
