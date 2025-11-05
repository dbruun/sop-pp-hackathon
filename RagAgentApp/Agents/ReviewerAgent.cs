using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Logging;

namespace RagAgentApp.Agents;

/// <summary>
/// ReviewerAgent: Validates that each claim in the drafted response is supported by retrieved passages.
/// Flags low-grounding issues and provides quality assessment.
/// </summary>
public class ReviewerAgent : IAgentService
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly string _modelDeploymentName;
    private readonly ILogger<ReviewerAgent> _logger;
    private string? _agentIdResolved;

    public string AgentName => "Reviewer Agent";

    public ReviewerAgent(PersistentAgentsClient agentsClient, string modelDeploymentName, ILogger<ReviewerAgent> logger)
    {
        _agentsClient = agentsClient;
        _modelDeploymentName = modelDeploymentName;
        _logger = logger;
        
        _logger.LogInformation("ReviewerAgent initialized with model: {ModelName}", modelDeploymentName);
    }

    private string GetOrResolveAgentId()
    {
        if (_agentIdResolved != null)
        {
            _logger.LogDebug("Using cached agent ID: {AgentId}", _agentIdResolved);
            return _agentIdResolved;
        }

        var systemPrompt = @"You are a Reviewer Agent responsible for validating response quality and grounding.
Your role is to:
1. Verify that each claim in the drafted response is supported by the retrieved passages
2. Identify claims that lack proper grounding (low-grounding issues)
3. Check the accuracy and relevance of citations
4. Assess the overall quality of the response
5. Flag any potential issues or inconsistencies

Respond with a JSON object containing:
{
  ""grounding_score"": 0.0-1.0,
  ""claims_verified"": [
    {
      ""claim"": ""The specific claim from the response"",
      ""is_grounded"": true|false,
      ""supporting_passage"": ""Passage that supports this claim"",
      ""confidence"": 0.0-1.0
    }
  ],
  ""low_grounding_issues"": [
    {
      ""claim"": ""Claim with low grounding"",
      ""issue"": ""Description of the grounding issue"",
      ""recommendation"": ""How to fix the issue""
    }
  ],
  ""citation_accuracy"": 0.0-1.0,
  ""overall_quality"": ""high|medium|low"",
  ""recommendations"": [""List of recommendations for improvement""]
}";

        const string agentName = "Reviewer Agent";

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
            _logger.LogInformation("ReviewerAgent validating response grounding and quality: {Query}", 
                query.Length > 100 ? query.Substring(0, 100) + "..." : query);
            
            var agentId = GetOrResolveAgentId();

            // Create a new thread for each review (stateless)
            _logger.LogDebug("Creating new thread for review");
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
                _logger.LogInformation("Successfully completed review (length: {Length} characters)", textContent.Text.Length);
                return textContent.Text;
            }

            _logger.LogWarning("No assistant message found in thread: {ThreadId}", threadId);
            return "I apologize, but I couldn't complete the review at this time.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviewing response: {Message}", ex.Message);
            return $"Error reviewing response: {ex.Message}";
        }
    }
}
