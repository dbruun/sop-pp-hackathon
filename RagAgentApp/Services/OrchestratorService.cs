using RagAgentApp.Agents;

namespace RagAgentApp.Services;

public class OrchestratorService
{
    private readonly IEnumerable<IAgentService> _agents;

    public OrchestratorService(IEnumerable<IAgentService> agents)
    {
        _agents = agents;
    }

    public async Task<Dictionary<string, string>> RouteQueryToAgentsAsync(
        string query, 
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, string>();

        // Execute queries to all agents in parallel
        var tasks = _agents.Select(async agent =>
        {
            var response = await agent.ProcessQueryAsync(query, cancellationToken);
            return new { Agent = agent.AgentName, Response = response };
        });

        var responses = await Task.WhenAll(tasks);

        foreach (var response in responses)
        {
            results[response.Agent] = response.Response;
        }

        return results;
    }
}
