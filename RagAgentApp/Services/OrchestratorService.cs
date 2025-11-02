using Microsoft.Extensions.Logging;
using RagAgentApp.Agents;

namespace RagAgentApp.Services;

public class OrchestratorService
{
    private readonly IEnumerable<IAgentService> _agents;
    private readonly ILogger<OrchestratorService> _logger;

    public OrchestratorService(IEnumerable<IAgentService> agents, ILogger<OrchestratorService> logger)
    {
        _agents = agents;
        _logger = logger;
        
        var agentNames = string.Join(", ", _agents.Select(a => a.AgentName));
        _logger.LogInformation("OrchestratorService initialized with agents: {AgentNames}", agentNames);
    }

    public async Task<Dictionary<string, string>> RouteQueryToAgentsAsync(
        string query, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Routing query to {AgentCount} agents: {Query}", 
            _agents.Count(), 
            query.Length > 100 ? query.Substring(0, 100) + "..." : query);

        var results = new Dictionary<string, string>();

        // Execute queries to all agents in parallel
        var startTime = DateTime.UtcNow;
        var tasks = _agents.Select(async agent =>
        {
            try
            {
                _logger.LogDebug("Starting query for agent: {AgentName}", agent.AgentName);
                var agentStartTime = DateTime.UtcNow;
                
                var response = await agent.ProcessQueryAsync(query, cancellationToken);
                
                var duration = DateTime.UtcNow - agentStartTime;
                _logger.LogInformation("Agent {AgentName} completed in {Duration}ms", 
                    agent.AgentName, duration.TotalMilliseconds);
                
                return new { Agent = agent.AgentName, Response = response };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentName} failed with error: {Message}", 
                    agent.AgentName, ex.Message);
                return new { Agent = agent.AgentName, Response = $"Error: {ex.Message}" };
            }
        });

        var responses = await Task.WhenAll(tasks);

        foreach (var response in responses)
        {
            results[response.Agent] = response.Response;
        }

        var totalDuration = DateTime.UtcNow - startTime;
        _logger.LogInformation("All agents completed in {Duration}ms. Successful responses: {SuccessCount}/{TotalCount}", 
            totalDuration.TotalMilliseconds,
            responses.Count(r => !r.Response.StartsWith("Error:")),
            responses.Length);

        return results;
    }
}
