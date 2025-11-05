using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Logging;
using RagAgentApp.Agents;
using RagAgentApp.Models;
using System.Text.Json;

namespace RagAgentApp.Services;

public class OrchestratorService
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly string _modelDeploymentName;
    private readonly SopRagAgent _sopAgent;
    private readonly PolicyRagAgent _policyAgent;
    private readonly IntakeAgent _intakeAgent;
    private readonly SearchAgent _searchAgent;
    private readonly WriterAgent _writerAgent;
    private readonly ReviewerAgent _reviewerAgent;
    private readonly ExecutorAgent _executorAgent;
    private readonly ILogger<OrchestratorService> _logger;
    private string? _orchestratorAgentId;
    private string? _deltaAnalysisAgentId;
    private string? _threadId;

    public OrchestratorService(
        PersistentAgentsClient agentsClient,
        string modelDeploymentName,
        SopRagAgent sopAgent, 
        PolicyRagAgent policyAgent,
        IntakeAgent intakeAgent,
        SearchAgent searchAgent,
        WriterAgent writerAgent,
        ReviewerAgent reviewerAgent,
        ExecutorAgent executorAgent,
        ILogger<OrchestratorService> logger)
    {
        _agentsClient = agentsClient;
        _modelDeploymentName = modelDeploymentName;
        _sopAgent = sopAgent;
        _policyAgent = policyAgent;
        _intakeAgent = intakeAgent;
        _searchAgent = searchAgent;
        _writerAgent = writerAgent;
        _reviewerAgent = reviewerAgent;
        _executorAgent = executorAgent;
        _logger = logger;
        
        _logger.LogInformation("OrchestratorService initialized with specialized agent pipeline: {Pipeline}", 
            $"{intakeAgent.AgentName} -> {searchAgent.AgentName} -> {writerAgent.AgentName} -> {reviewerAgent.AgentName} -> {executorAgent.AgentName}");
    }

    private string GetOrResolveOrchestratorAgentId()
    {
        if (_orchestratorAgentId != null)
        {
            _logger.LogDebug("Using cached orchestrator agent ID: {AgentId}", _orchestratorAgentId);
            return _orchestratorAgentId;
        }

        const string agentName = "Orchestrator Agent";
        var systemPrompt = @"You are an intelligent orchestrator that routes user queries to specialized expert agents.
You have access to two tools:
1. query_sop_agent: Use this to query the SOP (Standard Operating Procedures) expert agent for questions about procedures, work instructions, and process documentation.
2. query_policy_agent: Use this to query the Policy expert agent for questions about company policies, regulations, compliance, and governance.

IMPORTANT: You MUST ALWAYS call BOTH tools for every user question to get comprehensive answers from both perspectives.
Call query_sop_agent AND query_policy_agent for every query, regardless of the topic.
After receiving both responses, provide a brief acknowledgment that both agents have been consulted.";

        _logger.LogInformation("Searching for existing orchestrator agent with name: {AgentName}", agentName);

        var existingAgents = _agentsClient.Administration.GetAgents();
        var existingAgent = existingAgents.FirstOrDefault(a => a.Name == agentName);

        if (existingAgent != null)
        {
            _orchestratorAgentId = existingAgent.Id;
            _logger.LogInformation("Found existing orchestrator agent: {AgentId}", _orchestratorAgentId);
        }
        else
        {
            _logger.LogInformation("Creating new orchestrator agent with function calling tools");
            
            // Define function tools for calling the SOP and Policy agents
            var sopToolDefinition = new FunctionToolDefinition(
                name: "query_sop_agent",
                description: "Query the SOP (Standard Operating Procedures) expert agent. Use this for questions about procedures, work instructions, and process documentation.",
                parameters: BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        query = new
                        {
                            type = "string",
                            description = "The question or query to send to the SOP agent"
                        }
                    },
                    required = new[] { "query" }
                })
            );

            var policyToolDefinition = new FunctionToolDefinition(
                name: "query_policy_agent",
                description: "Query the Policy expert agent. Use this for questions about company policies, regulations, compliance requirements, and governance frameworks.",
                parameters: BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        query = new
                        {
                            type = "string",
                            description = "The question or query to send to the Policy agent"
                        }
                    },
                    required = new[] { "query" }
                })
            );

            var newAgent = _agentsClient.Administration.CreateAgent(
                model: _modelDeploymentName,
                name: agentName,
                instructions: systemPrompt,
                tools: new List<ToolDefinition> { sopToolDefinition, policyToolDefinition }
            );
            
            _orchestratorAgentId = newAgent.Value.Id;
            _logger.LogInformation("Successfully created orchestrator agent: {AgentId}", _orchestratorAgentId);
        }

        return _orchestratorAgentId;
    }

    private string GetOrResolveDeltaAnalysisAgentId()
    {
        if (_deltaAnalysisAgentId != null)
        {
            _logger.LogDebug("Using cached delta analysis agent ID: {AgentId}", _deltaAnalysisAgentId);
            return _deltaAnalysisAgentId;
        }

        const string agentName = "Delta Analysis Agent (No Tools)";
        var systemPrompt = @"You are an expert analyst that compares and contrasts responses from different agents.
Your role is to provide clear, insightful analysis of the similarities and differences between agent responses.
You should identify key points of agreement, areas of divergence, contradictions, and unique insights from each perspective.
Provide structured, easy-to-understand analysis that helps users make informed decisions.
DO NOT call any tools or functions - only provide text-based analysis.";

        _logger.LogInformation("Searching for existing delta analysis agent with name: {AgentName}", agentName);

        var existingAgents = _agentsClient.Administration.GetAgents();
        var existingAgent = existingAgents.FirstOrDefault(a => a.Name == agentName);

        if (existingAgent != null)
        {
            // Delete the existing agent and recreate it to ensure no tools are attached
            _logger.LogInformation("Deleting existing delta analysis agent to recreate without tools: {AgentId}", existingAgent.Id);
            _agentsClient.Administration.DeleteAgent(existingAgent.Id);
        }

        _logger.LogInformation("Creating new delta analysis agent without tools");
        
        // Create agent without any tools - pure text analysis only
        var newAgent = _agentsClient.Administration.CreateAgent(
            model: _modelDeploymentName,
            name: agentName,
            instructions: systemPrompt,
            tools: new List<ToolDefinition>() // Explicitly pass empty tools list
        );
        
        _deltaAnalysisAgentId = newAgent.Value.Id;
        _logger.LogInformation("Successfully created delta analysis agent: {AgentId}", _deltaAnalysisAgentId);

        return _deltaAnalysisAgentId;
    }

    public async Task<Dictionary<string, string>> RouteQueryToAgentsAsync(
        string query, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Orchestrator processing query: {Query}", 
                query.Length > 100 ? query.Substring(0, 100) + "..." : query);

            var startTime = DateTime.UtcNow;
            var agentId = GetOrResolveOrchestratorAgentId();

            // Create thread only once and reuse it
            if (string.IsNullOrEmpty(_threadId))
            {
                _logger.LogDebug("Creating new thread for orchestrator");
                var threadResponse = _agentsClient.Threads.CreateThread(cancellationToken: cancellationToken);
                _threadId = threadResponse.Value.Id;
                _logger.LogInformation("Created orchestrator thread: {ThreadId}", _threadId);
            }
            else
            {
                _logger.LogDebug("Reusing existing orchestrator thread: {ThreadId}", _threadId);
            }

            // Add user message
            _agentsClient.Messages.CreateMessage(_threadId, MessageRole.User, query, cancellationToken: cancellationToken);

            // Create and run the orchestrator agent
            var runResponse = _agentsClient.Runs.CreateRun(_threadId, agentId, cancellationToken: cancellationToken);
            var run = runResponse.Value;
            _logger.LogInformation("Orchestrator run created: {RunId} with status: {Status}", run.Id, run.Status);

            // Dictionary to store individual agent responses
            var agentResponses = new Dictionary<string, string>();

            // Poll for completion with function calling support
            var pollCount = 0;
            while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress || run.Status == RunStatus.RequiresAction)
            {
                await Task.Delay(1000, cancellationToken);
                var runStatusResponse = _agentsClient.Runs.GetRun(_threadId, run.Id, cancellationToken);
                run = runStatusResponse.Value;
                pollCount++;

                if (run.Status == RunStatus.RequiresAction && run.RequiredAction is SubmitToolOutputsAction submitToolOutputsAction)
                {
                    _logger.LogInformation("Orchestrator requires action - processing {ToolCount} tool calls", 
                        submitToolOutputsAction.ToolCalls.Count);

                    // Process all tool calls in parallel
                    var toolOutputs = new List<ToolOutput>();
                    var toolTasks = submitToolOutputsAction.ToolCalls.Select(async toolCall =>
                    {
                        if (toolCall is RequiredFunctionToolCall functionToolCall)
                        {
                            _logger.LogInformation("Processing tool call: {FunctionName} with call ID: {CallId}", 
                                functionToolCall.Name, functionToolCall.Id);

                            try
                            {
                                var arguments = JsonDocument.Parse(functionToolCall.Arguments);
                                var queryArg = arguments.RootElement.GetProperty("query").GetString() ?? query;

                                string result;
                                var toolStartTime = DateTime.UtcNow;

                                if (functionToolCall.Name == "query_sop_agent")
                                {
                                    _logger.LogDebug("Calling SOP Agent with query: {Query}", queryArg);
                                    result = await _sopAgent.ProcessQueryAsync(queryArg, cancellationToken);
                                    var duration = DateTime.UtcNow - toolStartTime;
                                    _logger.LogInformation("SOP Agent completed in {Duration}ms", duration.TotalMilliseconds);
                                    agentResponses["SOP Agent"] = result;
                                }
                                else if (functionToolCall.Name == "query_policy_agent")
                                {
                                    _logger.LogDebug("Calling Policy Agent with query: {Query}", queryArg);
                                    result = await _policyAgent.ProcessQueryAsync(queryArg, cancellationToken);
                                    var duration = DateTime.UtcNow - toolStartTime;
                                    _logger.LogInformation("Policy Agent completed in {Duration}ms", duration.TotalMilliseconds);
                                    agentResponses["Policy Agent"] = result;
                                }
                                else
                                {
                                    result = $"Unknown tool: {functionToolCall.Name}";
                                    _logger.LogWarning("Unknown tool called: {ToolName}", functionToolCall.Name);
                                }

                                return new ToolOutput(functionToolCall.Id, result);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing tool call {FunctionName}: {Message}", 
                                    functionToolCall.Name, ex.Message);
                                return new ToolOutput(functionToolCall.Id, $"Error: {ex.Message}");
                            }
                        }
                        return null;
                    });

                    var completedToolOutputs = await Task.WhenAll(toolTasks);
                    toolOutputs.AddRange(completedToolOutputs.Where(to => to != null)!);

                    // Submit tool outputs back to the run
                    _logger.LogInformation("Submitting {OutputCount} tool outputs to orchestrator", toolOutputs.Count);
                    
                    // Create a request with tool outputs as JSON
                    var toolOutputsData = new { tool_outputs = toolOutputs.Select(to => new { tool_call_id = to.ToolCallId, output = to.Output }) };
                    var content = BinaryData.FromObjectAsJson(toolOutputsData);
                    _agentsClient.Runs.SubmitToolOutputsToRun(
                        _threadId, 
                        run.Id, 
                        Azure.Core.RequestContent.Create(content)
                    );
                    
                    // Get updated run status after submitting tool outputs
                    var updatedRunResponse = _agentsClient.Runs.GetRun(_threadId, run.Id, cancellationToken);
                    run = updatedRunResponse.Value;
                }
                else if (pollCount % 5 == 0)
                {
                    _logger.LogDebug("Orchestrator run {RunId} status: {Status} (polled {Count} times)", 
                        run.Id, run.Status, pollCount);
                }
            }

            _logger.LogInformation("Orchestrator run {RunId} completed with status: {Status}", run.Id, run.Status);

            if (run.Status == RunStatus.Failed)
            {
                _logger.LogError("Orchestrator run failed: {Error}", run.LastError?.Message ?? "Unknown error");
                return new Dictionary<string, string>
                {
                    ["SOP Agent"] = $"Orchestrator failed: {run.LastError?.Message ?? "Unknown error"}",
                    ["Policy Agent"] = $"Orchestrator failed: {run.LastError?.Message ?? "Unknown error"}"
                };
            }

            var totalDuration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Orchestrator completed in {Duration}ms. Collected {ResponseCount} agent responses", 
                totalDuration.TotalMilliseconds, agentResponses.Count);

            // Return the individual agent responses
            return agentResponses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in orchestrator: {Message}", ex.Message);
            return new Dictionary<string, string>
            {
                ["SOP Agent"] = $"Orchestrator error: {ex.Message}",
                ["Policy Agent"] = $"Orchestrator error: {ex.Message}"
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

            // Create and run without function calling (just analysis)
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

    /// <summary>
    /// Processes a query through the new specialized agent pipeline with observability.
    /// Flow: Intake -> Search -> Writer -> Reviewer -> Executor
    /// </summary>
    public async Task<(string FinalResponse, PipelineExecutionTrace Trace)> ProcessQueryWithPipelineAsync(
        string query, 
        CancellationToken cancellationToken = default)
    {
        var trace = new PipelineExecutionTrace
        {
            StartTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting specialized agent pipeline for query: {Query}", 
                query.Length > 100 ? query.Substring(0, 100) + "..." : query);

            // Step 1: Intake Agent - Analyze intent and gate
            var intakeTrace = await ExecuteAgentWithTraceAsync(
                _intakeAgent, 
                query, 
                "Intake", 
                cancellationToken);
            trace.AgentTraces.Add(intakeTrace);
            
            var intakeResult = intakeTrace.Success ? 
                await _intakeAgent.ProcessQueryAsync(query, cancellationToken) : 
                "Intent analysis failed";

            _logger.LogInformation("Intake Agent completed. Intent analysis: {Result}", 
                intakeResult.Length > 200 ? intakeResult.Substring(0, 200) + "..." : intakeResult);

            // Step 2: Search Agent - Hybrid retrieval
            var searchTrace = await ExecuteAgentWithTraceAsync(
                _searchAgent, 
                $"Retrieve relevant information for: {query}", 
                "Search", 
                cancellationToken);
            trace.AgentTraces.Add(searchTrace);
            
            var searchResults = searchTrace.Success ? 
                await _searchAgent.ProcessQueryAsync($"Retrieve relevant information for: {query}", cancellationToken) : 
                "Search failed";

            _logger.LogInformation("Search Agent completed. Retrieved results: {ResultCount} characters", searchResults.Length);

            // Step 3: Writer Agent - Draft with citations
            var writerTrace = await ExecuteAgentWithTraceAsync(
                _writerAgent, 
                $"Draft a response for: {query}\n\nBased on these search results:\n{searchResults}", 
                "Writer", 
                cancellationToken);
            trace.AgentTraces.Add(writerTrace);
            
            var draftResponse = writerTrace.Success ? 
                await _writerAgent.ProcessQueryAsync(
                    $"Draft a response for: {query}\n\nBased on these search results:\n{searchResults}", 
                    cancellationToken) : 
                "Draft failed";

            _logger.LogInformation("Writer Agent completed. Draft length: {Length} characters", draftResponse.Length);

            // Step 4: Reviewer Agent - Validate grounding
            var reviewerTrace = await ExecuteAgentWithTraceAsync(
                _reviewerAgent, 
                $"Review this response for grounding:\n\nQuery: {query}\n\nResponse: {draftResponse}\n\nSearch Results: {searchResults}", 
                "Reviewer", 
                cancellationToken);
            trace.AgentTraces.Add(reviewerTrace);
            
            var reviewResults = reviewerTrace.Success ? 
                await _reviewerAgent.ProcessQueryAsync(
                    $"Review this response for grounding:\n\nQuery: {query}\n\nResponse: {draftResponse}\n\nSearch Results: {searchResults}", 
                    cancellationToken) : 
                "Review failed";

            _logger.LogInformation("Reviewer Agent completed. Review: {Review}", 
                reviewResults.Length > 200 ? reviewResults.Substring(0, 200) + "..." : reviewResults);

            // Step 5: Executor Agent - Format output
            var executorTrace = await ExecuteAgentWithTraceAsync(
                _executorAgent, 
                $"Format this response for display:\n\n{draftResponse}\n\nReview Results: {reviewResults}", 
                "Executor", 
                cancellationToken);
            trace.AgentTraces.Add(executorTrace);
            
            var finalResponse = executorTrace.Success ? 
                await _executorAgent.ProcessQueryAsync(
                    $"Format this response for display:\n\n{draftResponse}\n\nReview Results: {reviewResults}", 
                    cancellationToken) : 
                draftResponse; // Fallback to draft if executor fails

            _logger.LogInformation("Executor Agent completed. Final response length: {Length} characters", finalResponse.Length);

            trace.EndTime = DateTime.UtcNow;
            _logger.LogInformation("Pipeline completed in {Duration}ms with total cost ${Cost}", 
                trace.TotalDuration.TotalMilliseconds, trace.TotalEstimatedCost);

            return (finalResponse, trace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in agent pipeline: {Message}", ex.Message);
            trace.EndTime = DateTime.UtcNow;
            
            return ($"Error in agent pipeline: {ex.Message}", trace);
        }
    }

    private async Task<AgentExecutionTrace> ExecuteAgentWithTraceAsync(
        IAgentService agent, 
        string query, 
        string agentName, 
        CancellationToken cancellationToken)
    {
        var trace = new AgentExecutionTrace
        {
            AgentName = agentName,
            StartTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Executing {AgentName} agent...", agentName);
            var response = await agent.ProcessQueryAsync(query, cancellationToken);
            
            trace.EndTime = DateTime.UtcNow;
            trace.Success = true;
            
            // Estimate tokens and cost (rough estimates based on typical usage)
            // In production, these should come from actual API responses
            trace.TokensUsed = EstimateTokens(query) + EstimateTokens(response ?? "");
            trace.EstimatedCost = CalculateEstimatedCost(trace.TokensUsed);
            
            _logger.LogInformation("{AgentName} completed in {Duration}ms, ~{Tokens} tokens, ~${Cost}", 
                agentName, trace.Duration.TotalMilliseconds, trace.TokensUsed, trace.EstimatedCost);
        }
        catch (Exception ex)
        {
            trace.EndTime = DateTime.UtcNow;
            trace.Success = false;
            trace.ErrorMessage = ex.Message;
            
            _logger.LogError(ex, "{AgentName} failed after {Duration}ms: {Message}", 
                agentName, trace.Duration.TotalMilliseconds, ex.Message);
        }

        return trace;
    }

    // Token estimation constants
    private const int CHARACTERS_PER_TOKEN = 4; // Approximate for English text
    private const decimal INPUT_TOKEN_COST_PER_1K = 0.03m; // GPT-4 pricing
    private const decimal OUTPUT_TOKEN_COST_PER_1K = 0.06m; // GPT-4 pricing

    private int EstimateTokens(string text)
    {
        // Rough estimate: ~4 characters per token for English text
        return text.Length / CHARACTERS_PER_TOKEN;
    }

    private decimal CalculateEstimatedCost(int tokens)
    {
        // Rough estimate based on GPT-4 pricing
        // Assuming roughly equal split between input and output
        var inputTokens = tokens / 2;
        var outputTokens = tokens / 2;
        
        var inputCost = (inputTokens / 1000.0m) * INPUT_TOKEN_COST_PER_1K;
        var outputCost = (outputTokens / 1000.0m) * OUTPUT_TOKEN_COST_PER_1K;
        
        return inputCost + outputCost;
    }
}
