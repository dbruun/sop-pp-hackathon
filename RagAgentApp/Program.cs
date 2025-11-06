using Azure;
using Azure.AI.Projects;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using RagAgentApp.Agents;
using RagAgentApp.Components;
using RagAgentApp.Models;
using RagAgentApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure Azure AI settings
var azureAISettings = builder.Configuration.GetSection("AzureAI").Get<AzureAISettings>() 
    ?? new AzureAISettings();

// Get values from environment variables if not in config (for container deployment)
azureAISettings.ProjectEndpoint = builder.Configuration["AZURE_AI_PROJECT_ENDPOINT"] ?? azureAISettings.ProjectEndpoint;
azureAISettings.ModelDeploymentName = builder.Configuration["AZURE_AI_MODEL_DEPLOYMENT_NAME"] ?? azureAISettings.ModelDeploymentName;
azureAISettings.ConnectionString = builder.Configuration["AZURE_AI_CONNECTION_STRING"] ?? azureAISettings.ConnectionString;
azureAISettings.SopAgentId = builder.Configuration["AZURE_AI_SOP_AGENT_ID"] ?? azureAISettings.SopAgentId;
azureAISettings.PolicyAgentId = builder.Configuration["AZURE_AI_POLICY_AGENT_ID"] ?? azureAISettings.PolicyAgentId;
azureAISettings.SearchAgentId = builder.Configuration["AZURE_AI_SEARCH_AGENT_ID"] ?? azureAISettings.SearchAgentId;
azureAISettings.ApiKey = builder.Configuration["AZURE_AI_API_KEY"] ?? azureAISettings.ApiKey;

// Log the configuration being used
Console.WriteLine($"===== AZURE AI CONFIGURATION =====");
Console.WriteLine($"Model Deployment Name: {azureAISettings.ModelDeploymentName}");
Console.WriteLine($"Project Endpoint: {azureAISettings.ProjectEndpoint}");
Console.WriteLine($"==================================");

// Register PersistentAgentsClient (v1.1.0 API with Azure.AI.Agents.Persistent)
builder.Services.AddSingleton<PersistentAgentsClient>(sp =>
{
    if (string.IsNullOrEmpty(azureAISettings.ProjectEndpoint))
    {
        throw new InvalidOperationException(
            "Azure AI configuration is missing. Please provide AZURE_AI_PROJECT_ENDPOINT.");
    }
    
    // Create PersistentAgentsClient with endpoint + DefaultAzureCredential
    // Entra ID authentication via DefaultAzureCredential (Azure CLI, Managed Identity, etc.)
    return new PersistentAgentsClient(azureAISettings.ProjectEndpoint, new DefaultAzureCredential());
});

// Register Azure AI settings as singleton
builder.Services.AddSingleton(azureAISettings);

// Register agents as singleton services to maintain thread continuity across requests
builder.Services.AddSingleton<SopRagAgent>(sp =>
{
    var agentsClient = sp.GetRequiredService<PersistentAgentsClient>();
    var settings = sp.GetRequiredService<AzureAISettings>();
    var logger = sp.GetRequiredService<ILogger<SopRagAgent>>();
    return new SopRagAgent(agentsClient, settings.ModelDeploymentName, logger, settings.SopAgentId);
});

builder.Services.AddSingleton<PolicyRagAgent>(sp =>
{
    var agentsClient = sp.GetRequiredService<PersistentAgentsClient>();
    var settings = sp.GetRequiredService<AzureAISettings>();
    var logger = sp.GetRequiredService<ILogger<PolicyRagAgent>>();
    return new PolicyRagAgent(agentsClient, settings.ModelDeploymentName, logger, settings.PolicyAgentId);
});

// Register new specialized agents for the pipeline
builder.Services.AddSingleton<IntakeAgent>(sp =>
{
    var agentsClient = sp.GetRequiredService<PersistentAgentsClient>();
    var settings = sp.GetRequiredService<AzureAISettings>();
    var logger = sp.GetRequiredService<ILogger<IntakeAgent>>();
    return new IntakeAgent(agentsClient, settings.ModelDeploymentName, logger);
});

builder.Services.AddSingleton<SearchAgent>(sp =>
{
    var agentsClient = sp.GetRequiredService<PersistentAgentsClient>();
    var settings = sp.GetRequiredService<AzureAISettings>();
    var logger = sp.GetRequiredService<ILogger<SearchAgent>>();
    return new SearchAgent(
        agentsClient, 
        settings.ModelDeploymentName, 
        logger,
        settings.SearchAgentId);
});

builder.Services.AddSingleton<WriterAgent>(sp =>
{
    var agentsClient = sp.GetRequiredService<PersistentAgentsClient>();
    var settings = sp.GetRequiredService<AzureAISettings>();
    var logger = sp.GetRequiredService<ILogger<WriterAgent>>();
    return new WriterAgent(agentsClient, settings.ModelDeploymentName, logger);
});

builder.Services.AddSingleton<ReviewerExecutorAgent>(sp =>
{
    var agentsClient = sp.GetRequiredService<PersistentAgentsClient>();
    var settings = sp.GetRequiredService<AzureAISettings>();
    var logger = sp.GetRequiredService<ILogger<ReviewerExecutorAgent>>();
    return new ReviewerExecutorAgent(agentsClient, settings.ModelDeploymentName, logger);
});

// Register orchestrator service as singleton (it's now an agent itself with function calling)
builder.Services.AddSingleton<OrchestratorService>(sp =>
{
    var agentsClient = sp.GetRequiredService<PersistentAgentsClient>();
    var settings = sp.GetRequiredService<AzureAISettings>();
    var sopAgent = sp.GetRequiredService<SopRagAgent>();
    var policyAgent = sp.GetRequiredService<PolicyRagAgent>();
    var intakeAgent = sp.GetRequiredService<IntakeAgent>();
    var searchAgent = sp.GetRequiredService<SearchAgent>();
    var writerAgent = sp.GetRequiredService<WriterAgent>();
    var reviewerExecutorAgent = sp.GetRequiredService<ReviewerExecutorAgent>();
    var logger = sp.GetRequiredService<ILogger<OrchestratorService>>();
    return new OrchestratorService(
        agentsClient, 
        settings.ModelDeploymentName, 
        sopAgent, 
        policyAgent, 
        intakeAgent, 
        searchAgent, 
        writerAgent, 
        reviewerExecutorAgent, 
        logger);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
