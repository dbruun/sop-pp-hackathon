namespace RagAgentApp.Agents;

public interface IAgentService
{
    string AgentName { get; }
    Task<string> ProcessQueryAsync(string query, CancellationToken cancellationToken = default);
}
