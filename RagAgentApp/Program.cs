using RagAgentApp.Agents;
using RagAgentApp.Components;
using RagAgentApp.Models;
using RagAgentApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// HACKATHON NOTE: Azure AI configuration is commented out since we're using stubbed implementations
// Uncomment and configure these when you're ready to implement real Azure AI Agent Service
// 
// var azureAISettings = builder.Configuration.GetSection("AzureAI").Get<AzureAISettings>() 
//     ?? new AzureAISettings();
// 
// azureAISettings.ProjectEndpoint = builder.Configuration["AZURE_AI_PROJECT_ENDPOINT"] ?? azureAISettings.ProjectEndpoint;
// azureAISettings.ModelDeploymentName = builder.Configuration["AZURE_AI_MODEL_DEPLOYMENT_NAME"] ?? azureAISettings.ModelDeploymentName;
// azureAISettings.SopAgentId = builder.Configuration["AZURE_AI_SOP_AGENT_ID"] ?? azureAISettings.SopAgentId;
// azureAISettings.PolicyAgentId = builder.Configuration["AZURE_AI_POLICY_AGENT_ID"] ?? azureAISettings.PolicyAgentId;

// HACKATHON TODO: When implementing Azure AI, uncomment these registrations:
//
// using Azure;
// using Azure.AI.Projects;
// using Azure.AI.Agents.Persistent;
// using Azure.Identity;
//
// builder.Services.AddSingleton<PersistentAgentsClient>(sp =>
// {
//     if (string.IsNullOrEmpty(azureAISettings.ProjectEndpoint))
//     {
//         throw new InvalidOperationException(
//             "Azure AI configuration is missing. Please provide AZURE_AI_PROJECT_ENDPOINT.");
//     }
//     
//     return new PersistentAgentsClient(azureAISettings.ProjectEndpoint, new DefaultAzureCredential());
// });
//
// builder.Services.AddSingleton(azureAISettings);

// Register agents as singleton services with stubbed implementations
builder.Services.AddSingleton<SopRagAgent>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<SopRagAgent>>();
    return new SopRagAgent(logger);
    
    // HACKATHON TODO: When implementing Azure AI, replace with:
    // var agentsClient = sp.GetRequiredService<PersistentAgentsClient>();
    // var settings = sp.GetRequiredService<AzureAISettings>();
    // return new SopRagAgent(agentsClient, settings.ModelDeploymentName, logger, settings.SopAgentId);
});

builder.Services.AddSingleton<PolicyRagAgent>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<PolicyRagAgent>>();
    return new PolicyRagAgent(logger);
    
    // HACKATHON TODO: When implementing Azure AI, replace with:
    // var agentsClient = sp.GetRequiredService<PersistentAgentsClient>();
    // var settings = sp.GetRequiredService<AzureAISettings>();
    // return new PolicyRagAgent(agentsClient, settings.ModelDeploymentName, logger, settings.PolicyAgentId);
});

// Register orchestrator service as singleton
builder.Services.AddSingleton<OrchestratorService>(sp =>
{
    var sopAgent = sp.GetRequiredService<SopRagAgent>();
    var policyAgent = sp.GetRequiredService<PolicyRagAgent>();
    var logger = sp.GetRequiredService<ILogger<OrchestratorService>>();
    return new OrchestratorService(sopAgent, policyAgent, logger);
    
    // HACKATHON TODO: For advanced orchestration with Azure AI Agent function calling, use:
    // var agentsClient = sp.GetRequiredService<PersistentAgentsClient>();
    // var settings = sp.GetRequiredService<AzureAISettings>();
    // return new OrchestratorService(agentsClient, settings.ModelDeploymentName, sopAgent, policyAgent, logger);
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
