using Microsoft.SemanticKernel;
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
azureAISettings.ApiKey = builder.Configuration["AZURE_AI_API_KEY"] ?? azureAISettings.ApiKey;
azureAISettings.ModelDeploymentName = builder.Configuration["AZURE_AI_MODEL_DEPLOYMENT_NAME"] ?? azureAISettings.ModelDeploymentName;

// Register Semantic Kernel with Azure OpenAI
builder.Services.AddSingleton(sp =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    
    // Only add Azure OpenAI if credentials are provided
    if (!string.IsNullOrEmpty(azureAISettings.ProjectEndpoint) && 
        !string.IsNullOrEmpty(azureAISettings.ApiKey))
    {
        kernelBuilder.AddAzureOpenAIChatCompletion(
            deploymentName: azureAISettings.ModelDeploymentName,
            endpoint: azureAISettings.ProjectEndpoint,
            apiKey: azureAISettings.ApiKey);
    }
    
    return kernelBuilder.Build();
});

// Register agents as scoped services
builder.Services.AddScoped<IAgentService, SopRagAgent>();
builder.Services.AddScoped<IAgentService, PolicyRagAgent>();

// Register orchestrator service
builder.Services.AddScoped<OrchestratorService>();

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
