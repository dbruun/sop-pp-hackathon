using Microsoft.Extensions.Logging;
using RagAgentApp.Agents;

namespace RagAgentApp.Services;

public class OrchestratorService
{
    private readonly SopRagAgent _sopAgent;
    private readonly PolicyRagAgent _policyAgent;
    private readonly ILogger<OrchestratorService> _logger;

    public OrchestratorService(
        SopRagAgent sopAgent, 
        PolicyRagAgent policyAgent, 
        ILogger<OrchestratorService> logger)
    {
        _sopAgent = sopAgent;
        _policyAgent = policyAgent;
        _logger = logger;
        
        _logger.LogInformation("OrchestratorService initialized with parallel agent execution: {SopAgent}, {PolicyAgent}", 
            sopAgent.AgentName, policyAgent.AgentName);
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
}
