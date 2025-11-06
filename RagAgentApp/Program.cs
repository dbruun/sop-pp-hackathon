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
azureAISettings.ApiKey = builder.Configuration["AZURE_AI_API_KEY"] ?? azureAISettings.ApiKey;

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
    return new SearchAgent(agentsClient, settings.ModelDeploymentName, logger);
});

builder.Services.AddSingleton<WriterAgent>(sp =>
{
    var agentsClient = sp.GetRequiredService<PersistentAgentsClient>();
    var settings = sp.GetRequiredService<AzureAISettings>();
    var logger = sp.GetRequiredService<ILogger<WriterAgent>>();
    return new WriterAgent(agentsClient, settings.ModelDeploymentName, logger);
});

builder.Services.AddSingleton<ReviewerAgent>(sp =>
{
    var agentsClient = sp.GetRequiredService<PersistentAgentsClient>();
    var settings = sp.GetRequiredService<AzureAISettings>();
    var logger = sp.GetRequiredService<ILogger<ReviewerAgent>>();
    return new ReviewerAgent(agentsClient, settings.ModelDeploymentName, logger);
});

builder.Services.AddSingleton<ExecutorAgent>(sp =>
{
    var agentsClient = sp.GetRequiredService<PersistentAgentsClient>();
    var settings = sp.GetRequiredService<AzureAISettings>();
    var logger = sp.GetRequiredService<ILogger<ExecutorAgent>>();
    return new ExecutorAgent(agentsClient, settings.ModelDeploymentName, logger);
});

// Register orchestrator service as singleton
builder.Services.AddSingleton<OrchestratorService>(sp =>
{
    var agentsClient = sp.GetRequiredService<PersistentAgentsClient>();
    var settings = sp.GetRequiredService<AzureAISettings>();
    var intakeAgent = sp.GetRequiredService<IntakeAgent>();
    var searchAgent = sp.GetRequiredService<SearchAgent>();
    var writerAgent = sp.GetRequiredService<WriterAgent>();
    var reviewerAgent = sp.GetRequiredService<ReviewerAgent>();
    var executorAgent = sp.GetRequiredService<ExecutorAgent>();
    var logger = sp.GetRequiredService<ILogger<OrchestratorService>>();
    return new OrchestratorService(
        agentsClient, 
        settings.ModelDeploymentName, 
        intakeAgent, 
        searchAgent, 
        writerAgent, 
        reviewerAgent, 
        executorAgent, 
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
