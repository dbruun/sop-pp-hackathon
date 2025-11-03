using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Logging;
using RagAgentApp.Agents;
using System.Text.Json;

namespace RagAgentApp.Services;

public class OrchestratorService
{
    private readonly PersistentAgentsClient _agentsClient;
    private readonly string _modelDeploymentName;
    private readonly SopRagAgent _sopAgent;
    private readonly PolicyRagAgent _policyAgent;
    private readonly ILogger<OrchestratorService> _logger;
    private string? _orchestratorAgentId;
    private string? _threadId;

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
        
        _logger.LogInformation("OrchestratorService initialized as agent with tools: {SopAgent}, {PolicyAgent}", 
            sopAgent.AgentName, policyAgent.AgentName);
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
}
