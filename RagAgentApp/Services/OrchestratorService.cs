using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Logging;
using RagAgentApp.Agents;

namespace RagAgentApp.Services;

public class OrchestratorService
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly string _modelDeploymentName;
    private readonly SopRagAgent _sopAgent;
    private readonly PolicyRagAgent _policyAgent;
    private readonly ILogger<OrchestratorService> _logger;
    private string? _deltaAnalysisAgentId;

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
        
        _logger.LogInformation("OrchestratorService initialized with parallel agent execution: {SopAgent}, {PolicyAgent}", 
            sopAgent.AgentName, policyAgent.AgentName);
    }

    private string GetOrResolveDeltaAnalysisAgentId()
    {
        if (_deltaAnalysisAgentId != null)
        {
            _logger.LogDebug("Using cached delta analysis agent ID: {AgentId}", _deltaAnalysisAgentId);
            return _deltaAnalysisAgentId;
        }

        const string agentName = "Delta Analysis Agent";
        var systemPrompt = @"You are a Delta Analysis expert that compares and contrasts responses from two different expert agents.
Your role is to identify similarities, differences, contradictions, and unique insights between the responses.
Provide structured analysis using markdown formatting with clear sections and tables for easy comparison.
Be objective and highlight both agreements and disagreements between the responses.";

        _logger.LogInformation("Searching for existing delta analysis agent with name: {AgentName}", agentName);

        var existingAgents = _agentsClient.Administration.GetAgents();
        var existingAgent = existingAgents.FirstOrDefault(a => a.Name == agentName);

        if (existingAgent != null)
        {
            _deltaAnalysisAgentId = existingAgent.Id;
            _logger.LogInformation("Found existing delta analysis agent: {AgentId}", _deltaAnalysisAgentId);
        }
        else
        {
            _logger.LogInformation("Creating new delta analysis agent");
            
            var newAgent = _agentsClient.Administration.CreateAgent(
                model: _modelDeploymentName,
                name: agentName,
                instructions: systemPrompt
            );
            
            _deltaAnalysisAgentId = newAgent.Value.Id;
            _logger.LogInformation("Successfully created delta analysis agent: {AgentId}", _deltaAnalysisAgentId);
        }

        return _deltaAnalysisAgentId;
    }

    public async Task<Dictionary<string, string>> RouteQueryToAgentsAsync(
        string query, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Routing query to both agents in parallel: {Query}", 
                query.Length > 100 ? query.Substring(0, 100) + "..." : query);

            var startTime = DateTime.UtcNow;

            // Call both agents directly in TRUE parallel execution
            var sopTask = Task.Run(async () =>
            {
                var agentStartTime = DateTime.UtcNow;
                _logger.LogInformation("Starting SOP Agent query");
                try
                {
                    var result = await _sopAgent.ProcessQueryAsync(query, cancellationToken);
                    var duration = DateTime.UtcNow - agentStartTime;
                    _logger.LogInformation("SOP Agent completed in {Duration}ms with response length: {Length} chars", 
                        duration.TotalMilliseconds, result.Length);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SOP Agent failed: {Message}", ex.Message);
                    return $"Error: {ex.Message}";
                }
            }, cancellationToken);

            var policyTask = Task.Run(async () =>
            {
                var agentStartTime = DateTime.UtcNow;
                _logger.LogInformation("Starting Policy Agent query");
                try
                {
                    var result = await _policyAgent.ProcessQueryAsync(query, cancellationToken);
                    var duration = DateTime.UtcNow - agentStartTime;
                    _logger.LogInformation("Policy Agent completed in {Duration}ms with response length: {Length} chars", 
                        duration.TotalMilliseconds, result.Length);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Policy Agent failed: {Message}", ex.Message);
                    return $"Error: {ex.Message}";
                }
            }, cancellationToken);

            // Wait for both agents to complete in parallel
            await Task.WhenAll(sopTask, policyTask);

            var sopResult = await sopTask;
            var policyResult = await policyTask;

            var totalDuration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Both agents completed in TRUE parallel execution. Total time: {Duration}ms", 
                totalDuration.TotalMilliseconds);

            return new Dictionary<string, string>
            {
                ["SOP Agent"] = sopResult,
                ["Policy Agent"] = policyResult
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing query to agents: {Message}", ex.Message);
            return new Dictionary<string, string>
            {
                ["SOP Agent"] = $"Error: {ex.Message}",
                ["Policy Agent"] = $"Error: {ex.Message}"
            };
        }
    }

    public async Task<string> AnalyzeDeltaAsync(
        string query,
        string sopResponse, 
        string policyResponse, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Analyzing delta between SOP and Policy agent responses");

            var agentId = GetOrResolveDeltaAnalysisAgentId();

            // Create a new thread for delta analysis
            var threadResponse = _agentsClient.Threads.CreateThread(cancellationToken: cancellationToken);
            var threadId = threadResponse.Value.Id;
            _logger.LogInformation("Created delta analysis thread: {ThreadId}", threadId);

            // Create a prompt for delta analysis
            var deltaPrompt = $@"Original Question: {query}

SOP Agent Response:
{sopResponse}

Policy Agent Response:
{policyResponse}

Please analyze the differences between the SOP Agent and Policy Agent responses. Provide your analysis in a well-structured format with the following sections:

## Key Similarities
List the main points where both agents agree or provide similar information.

## Key Differences
Present a comparison table showing the main differences:
| Aspect | SOP Agent | Policy Agent |
|--------|-----------|--------------|
| (Add rows comparing specific aspects)

## Contradictions or Conflicts
Identify any contradictions or conflicts between the responses. If none exist, state that clearly.

## Unique Insights
| Agent | Unique Insights |
|-------|----------------|
| SOP Agent | (List unique points from SOP) |
| Policy Agent | (List unique points from Policy) |

## Relevance Assessment
Which response is more relevant to the original question and why?

Use clear markdown formatting with tables where appropriate to make the comparison easy to understand.";

            // Add user message for delta analysis
            _agentsClient.Messages.CreateMessage(threadId, MessageRole.User, deltaPrompt, cancellationToken: cancellationToken);

            // Create and run the agent
            var runResponse = _agentsClient.Runs.CreateRun(threadId, agentId, cancellationToken: cancellationToken);
            var run = runResponse.Value;
            _logger.LogInformation("Delta analysis run created: {RunId}", run.Id);

            // Poll for completion
            while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
            {
                await Task.Delay(1000, cancellationToken);
                var runStatusResponse = _agentsClient.Runs.GetRun(threadId, run.Id, cancellationToken);
                run = runStatusResponse.Value;
            }

            if (run.Status == RunStatus.Completed)
            {
                var messages = _agentsClient.Messages.GetMessages(threadId, cancellationToken: cancellationToken);
                
                // Get the latest agent message
                var agentMessage = messages
                    .Where(m => m.Role == MessageRole.Agent)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefault();

                if (agentMessage != null)
                {
                    var deltaAnalysis = string.Join("\n", agentMessage.ContentItems
                        .OfType<MessageTextContent>()
                        .Select(c => c.Text));
                    
                    _logger.LogInformation("Delta analysis completed successfully");
                    return deltaAnalysis;
                }
            }

            _logger.LogWarning("Delta analysis run did not complete successfully. Status: {Status}", run.Status);
            return "Unable to complete delta analysis. Please try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing delta: {Message}", ex.Message);
            return $"Error analyzing delta: {ex.Message}";
        }
    }
}
