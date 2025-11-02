namespace RagAgentApp.Models;

public class AzureAISettings
{
    public string ProjectEndpoint { get; set; } = string.Empty;
    public string ModelDeploymentName { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string SopAgentId { get; set; } = string.Empty;
    public string PolicyAgentId { get; set; } = string.Empty;
    
    // Optional: API Key for development/testing (Entra ID is preferred for production)
    public string? ApiKey { get; set; }
}
