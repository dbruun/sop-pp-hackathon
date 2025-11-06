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
    private readonly IntakeAgent _intakeAgent;
    private readonly SearchAgent _searchAgent;
    private readonly WriterAgent _writerAgent;
    private readonly ReviewerAgent _reviewerAgent;
    private readonly ExecutorAgent _executorAgent;
    private readonly ILogger<OrchestratorService> _logger;

    public OrchestratorService(
        PersistentAgentsClient agentsClient,
        string modelDeploymentName,
        IntakeAgent intakeAgent,
        SearchAgent searchAgent,
        WriterAgent writerAgent,
        ReviewerAgent reviewerAgent,
        ExecutorAgent executorAgent,
        ILogger<OrchestratorService> logger)
    {
        _agentsClient = agentsClient;
        _modelDeploymentName = modelDeploymentName;
        _intakeAgent = intakeAgent;
        _searchAgent = searchAgent;
        _writerAgent = writerAgent;
        _reviewerAgent = reviewerAgent;
        _executorAgent = executorAgent;
        _logger = logger;
        
        _logger.LogInformation("OrchestratorService initialized with specialized agent pipeline: {Pipeline}", 
            $"{intakeAgent.AgentName} -> {searchAgent.AgentName} -> {writerAgent.AgentName} -> {reviewerAgent.AgentName} -> {executorAgent.AgentName}");
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
