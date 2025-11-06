using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Logging;

namespace RagAgentApp.Agents;

/// <summary>
/// SearchAgent: Performs Azure AI Search hybrid retrieval using BM25 + vector search.
/// Retrieves relevant documents and passages from the knowledge base.
/// </summary>
public class SearchAgent : IAgentService
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly string _modelDeploymentName;
    private readonly string? _agentId;
    private readonly ILogger<SearchAgent> _logger;
    private string? _agentIdResolved;

    public string AgentName => "Search Agent";

    public SearchAgent(
        PersistentAgentsClient agentsClient, 
        string modelDeploymentName, 
        ILogger<SearchAgent> logger,
        string? agentId = null)
    {
        _agentsClient = agentsClient;
        _modelDeploymentName = modelDeploymentName;
        _agentId = agentId;
        _logger = logger;
        
        _logger.LogInformation("SearchAgent initialized with model: {ModelName}, AgentId: {AgentId}", 
            modelDeploymentName, agentId ?? "not provided");
    }

    private string GetOrResolveAgentId()
    {
        if (_agentIdResolved != null)
        {
            _logger.LogDebug("Using cached agent ID: {AgentId}", _agentIdResolved);
            return _agentIdResolved;
        }

        // Priority 1: If agent ID is provided, verify it exists and use it
        if (!string.IsNullOrEmpty(_agentId))
        {
            try
            {
                _logger.LogInformation("Attempting to connect to existing agent with ID: {AgentId}", _agentId);
                var agentResponse = _agentsClient.Administration.GetAgent(_agentId);
                
                if (agentResponse != null && agentResponse.Value != null)
                {
                    _agentIdResolved = _agentId;
                    _logger.LogInformation("✅ Successfully connected to existing agent: {AgentId} (Name: {AgentName})", 
                        _agentIdResolved, agentResponse.Value.Name ?? "Unknown");
                    return _agentIdResolved;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️  Could not find agent with provided ID: {AgentId}. Will search by name or create new agent.", _agentId);
            }
        }

        var systemPrompt = @"You are a Search Agent responsible for retrieving relevant information from the knowledge base.
Your role is to:
1. Use your file search tool to query the Azure AI Search index
2. Perform hybrid retrieval (combining BM25 keyword search and vector similarity search)
3. Always use your azure ai search index to find relevant documents and passages
4. Return the most relevant passages with their source information
5. Provide search results in a structured format

Respond with a JSON object containing:
{
  ""search_results"": [
    {
      ""passage"": ""The retrieved text passage"",
      ""source"": ""Document name or URL"",
      ""relevance_score"": 0.0-1.0,
      ""page_number"": ""Page number if available"",
      ""section"": ""Section title if available""
    }
  ],
  ""total_results"": 5,
  ""search_type"": ""hybrid|keyword|vector"",
  ""reasoning"": ""Brief explanation of search strategy used""
}";

        const string agentName = "Search Agent";

        // Priority 2: Search for existing agent by name
        _logger.LogInformation("Searching for existing agent with name: {AgentName}", agentName);

        var existingAgentsResponse = _agentsClient.Administration.GetAgents();
        var existingAgent = existingAgentsResponse.FirstOrDefault(a => a.Name == agentName);

        if (existingAgent != null)
        {
            _agentIdResolved = existingAgent.Id;
            _logger.LogInformation("Found existing agent by name: {AgentId} ({AgentName})", _agentIdResolved, agentName);
        }
        else
        {
            // Priority 3: Create new agent
            _logger.LogInformation("No existing agent found. Creating new agent: {AgentName}, model: {ModelName}", agentName, _modelDeploymentName);
            var newAgent = _agentsClient.Administration.CreateAgent(
                model: _modelDeploymentName,
                name: agentName,
                instructions: systemPrompt
            );
            _agentIdResolved = newAgent.Value.Id;
            _logger.LogInformation("Created new agent: {AgentId}", _agentIdResolved);
        }

        return _agentIdResolved;
    }

    public async Task<string> ProcessQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("SearchAgent performing hybrid retrieval for query: {Query}", 
                query.Length > 100 ? query.Substring(0, 100) + "..." : query);
            
            var agentId = GetOrResolveAgentId();

            // Create a new thread for each search (stateless)
            _logger.LogDebug("Creating new thread for search");
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
                _logger.LogInformation("Successfully retrieved search results (length: {Length} characters)", textContent.Text.Length);
                return textContent.Text;
            }

            _logger.LogWarning("No assistant message found in thread: {ThreadId}", threadId);
            return "I apologize, but I couldn't retrieve search results at this time.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing search: {Message}", ex.Message);
            return $"Error performing search: {ex.Message}";
        }
    }
}
