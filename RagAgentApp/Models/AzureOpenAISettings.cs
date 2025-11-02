namespace RagAgentApp.Models;

public class AzureAISettings
{
    public string ProjectEndpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ModelDeploymentName { get; set; } = string.Empty;
}
