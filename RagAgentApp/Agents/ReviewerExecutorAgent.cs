using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Logging;

namespace RagAgentApp.Agents;

/// <summary>
/// ReviewerExecutorAgent: Combined agent that validates grounding AND formats output.
/// Reviews the draft response for accuracy and grounding, then formats it for display.
/// This combines the responsibilities of Reviewer and Executor agents for efficiency.
/// </summary>
public class ReviewerExecutorAgent : IAgentService
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly string _modelDeploymentName;
    private readonly ILogger<ReviewerExecutorAgent> _logger;
    private string? _agentIdResolved;

    public string AgentName => "Reviewer & Executor Agent";

    public ReviewerExecutorAgent(PersistentAgentsClient agentsClient, string modelDeploymentName, ILogger<ReviewerExecutorAgent> logger)
    {
        _agentsClient = agentsClient;
        _modelDeploymentName = modelDeploymentName;
        _logger = logger;
        
        _logger.LogInformation("ReviewerExecutorAgent initialized with model: {ModelName}", modelDeploymentName);
    }

    private string GetOrResolveAgentId()
    {
        if (_agentIdResolved != null)
        {
            _logger.LogDebug("Using cached agent ID: {AgentId}", _agentIdResolved);
            return _agentIdResolved;
        }

        var systemPrompt = @"You are a Reviewer & Executor Agent with dual responsibilities:

PHASE 1 - REVIEW (Validate Grounding):
1. Verify the draft response is factually grounded in the search results
2. Check for hallucinations or unsupported claims
3. Ensure all citations are accurate and traceable
4. Identify any gaps or inconsistencies
5. Validate the response fully addresses the user's query

PHASE 2 - EXECUTE (Format Output):
If the review passes:
1. Format the validated response in clean, professional markdown
2. Ensure proper structure with headers, lists, and emphasis
3. Make citations clear and readable
4. Add helpful formatting for readability
5. Polish the language while maintaining accuracy

If the review fails:
1. Note the issues found
2. Suggest corrections
3. Provide a revised version that addresses the problems

Respond with:
{
  ""review_passed"": true|false,
  ""issues_found"": [""list of any problems""],
  ""confidence_score"": 0.0-1.0,
  ""final_formatted_response"": ""The polished, formatted response ready for display"",
  ""review_notes"": ""Brief notes on the review process""
}

Be thorough but concise. Your goal is to ensure accuracy and deliver a well-formatted final response.";

        _logger.LogInformation("Searching for existing Reviewer & Executor agent");

        var existingAgents = _agentsClient.Administration.GetAgents();
        var existingAgent = existingAgents.FirstOrDefault(a => a.Name == AgentName);

        if (existingAgent != null)
        {
            _agentIdResolved = existingAgent.Id;
            _logger.LogInformation("Found existing Reviewer & Executor agent: {AgentId}", _agentIdResolved);
        }
        else
        {
            _logger.LogInformation("Creating new Reviewer & Executor agent with name: {AgentName}, model: {ModelName}", AgentName, _modelDeploymentName);
            
            var newAgent = _agentsClient.Administration.CreateAgent(
                model: _modelDeploymentName,
                name: AgentName,
                instructions: systemPrompt
            );
            _agentIdResolved = newAgent.Value.Id;
            _logger.LogInformation("Successfully created new Reviewer & Executor agent: {AgentId}", _agentIdResolved);
        }

        return _agentIdResolved;
    }

    public async Task<string> ProcessQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("ReviewerExecutorAgent validating and formatting response: {Query}", 
                query.Length > 100 ? query.Substring(0, 100) + "..." : query);
            
            var agentId = GetOrResolveAgentId();

            // Create a new thread for this review and formatting task
            _logger.LogDebug("Creating new thread for review and formatting");
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
                var response = textContent.Text;
                _logger.LogInformation("Successfully completed review and formatting (length: {Length} characters)", response.Length);
                return response;
            }

            _logger.LogWarning("No valid response content found in messages");
            return "No response from Reviewer & Executor agent";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing review and formatting: {Message}", ex.Message);
            return $"Error in review and formatting: {ex.Message}";
        }
    }
}
