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

// ============================================================================
// STEP 1: Configure Azure AI settings (ALREADY DONE - Study this!)
// ============================================================================
// This section loads configuration from appsettings.json and environment
// variables. No changes needed here - this is a reference for you to understand
// how configuration works in ASP.NET Core.
// ============================================================================
var azureAISettings = builder.Configuration.GetSection("AzureAI").Get<AzureAISettings>() 
    ?? new AzureAISettings();

// Get values from environment variables if not in config (for container deployment)
azureAISettings.ProjectEndpoint = builder.Configuration["AZURE_AI_PROJECT_ENDPOINT"] ?? azureAISettings.ProjectEndpoint;
azureAISettings.ModelDeploymentName = builder.Configuration["AZURE_AI_MODEL_DEPLOYMENT_NAME"] ?? azureAISettings.ModelDeploymentName;
azureAISettings.ConnectionString = builder.Configuration["AZURE_AI_CONNECTION_STRING"] ?? azureAISettings.ConnectionString;
azureAISettings.SopAgentId = builder.Configuration["AZURE_AI_SOP_AGENT_ID"] ?? azureAISettings.SopAgentId;
azureAISettings.PolicyAgentId = builder.Configuration["AZURE_AI_POLICY_AGENT_ID"] ?? azureAISettings.PolicyAgentId;
azureAISettings.ApiKey = builder.Configuration["AZURE_AI_API_KEY"] ?? azureAISettings.ApiKey;

/* ============================================================================
 * HACKATHON TODO - TASK 0: Register Services with Dependency Injection
 * ============================================================================
 * 
 * Your mission: Register all Azure AI services as singletons so they can be
 * injected into your Blazor pages. This is a critical step for the app to work!
 * 
 * WHY SINGLETON LIFETIME?
 * -----------------------
 * Singleton services live for the entire application lifetime. This is crucial
 * for AI agents because:
 * - Conversation threads must persist across HTTP requests
 * - Agent instances are expensive to create
 * - State (agent IDs, thread IDs) must be maintained
 * 
 * THE FACTORY PATTERN:
 * --------------------
 * Each registration uses a factory function with this pattern:
 * 
 * builder.Services.AddSingleton<ServiceType>(sp =>
 * {
 *     // 1. Resolve dependencies using sp.GetRequiredService<T>()
 *     var dependency = sp.GetRequiredService<DependencyType>();
 *     
 *     // 2. Create and return the service instance
 *     return new ServiceType(dependency, otherArgs);
 * });
 * 
 * WHERE:
 * - sp = ServiceProvider (the DI container)
 * - sp.GetRequiredService<T>() = Gets a registered service
 * - Factory returns the created instance
 * 
 * ============================================================================
 */

// TODO STEP 1: Register PersistentAgentsClient
// 
// This is the main client for Azure AI Agent Service.
// 
// What to do:
// - Use AddSingleton<PersistentAgentsClient> with a factory function
// - Validate azureAISettings.ProjectEndpoint is not null/empty (throw InvalidOperationException if missing)
// - Create PersistentAgentsClient with TWO parameters:
//   1. The project endpoint (string)
//   2. A credential object for authentication (hint: DefaultAzureCredential)
// 
// Hint: Look at the using statements - what credential class is imported?
// Hint: This service has NO dependencies from DI (doesn't need sp.GetRequiredService)


// TODO STEP 2: Register AzureAISettings
// 
// This makes your configuration object available to other services.
// 
// What to do:
// - Use AddSingleton to register the azureAISettings variable (already created above)
// - This is the simplest registration - just one line!
// 
// Hint: When you have an existing instance, you can register it directly without a factory


// TODO STEP 3: Register SopRagAgent
// 
// The SOP agent answers questions about Standard Operating Procedures.
// 
// What to do:
// - Use AddSingleton<SopRagAgent> with a factory function
// - Resolve THREE dependencies from sp:
//   1. PersistentAgentsClient
//   2. AzureAISettings
//   3. ILogger<SopRagAgent>
// - Create SopRagAgent by passing FOUR constructor parameters (check the constructor!)
// 
// Hint: Check SopRagAgent.cs constructor to see what parameters it needs
// Hint: Some parameters come from resolved services, others from service properties


// TODO STEP 4: Register PolicyRagAgent
// 
// The Policy agent answers questions about company policies.
// 
// What to do:
// - Very similar pattern to SopRagAgent above
// - Use AddSingleton<PolicyRagAgent> with a factory function
// - Resolve the same THREE dependencies
// - Create PolicyRagAgent with FOUR constructor parameters
// 
// Hint: Look at the PolicyRagAgent.cs constructor - what's different from SopRagAgent?
// Hint: Check AzureAISettings properties - there's one specifically for this agent


// TODO STEP 5: Register OrchestratorService
// 
// The orchestrator coordinates between agents and performs delta analysis.
// 
// What to do:
// - Use AddSingleton<OrchestratorService> with a factory function
// - Resolve FIVE dependencies from sp:
//   1. PersistentAgentsClient
//   2. AzureAISettings
//   3. SopRagAgent (yes, you can inject other agents!)
//   4. PolicyRagAgent
//   5. ILogger<OrchestratorService>
// - Create OrchestratorService with FIVE constructor parameters
// 
// Hint: This service depends on the other agents - make sure you registered them first!
// Hint: Check OrchestratorService.cs constructor for the parameter order


/* ============================================================================
 * IMPORTANT NOTES:
 * ----------------
 * - ORDER MATTERS! Register dependencies before services that need them
 * - All constructors are in the agent class files - check them for parameters
 * - If you get a DI error at runtime, check the error message - it tells you what's missing
 * - The app won't start until all 5 services are registered correctly
 * 
 * TESTING YOUR WORK:
 * ------------------
 * 1. dotnet build        - Should succeed with no errors
 * 2. dotnet run          - Should start without DI exceptions
 * 3. Navigate to /chat   - Should load the chat page
 * 
 * If you get stuck, check:
 * - HACKATHON.md Task 0 for more detailed examples
 * - Agent constructors for required parameters
 * - AzureAISettings.cs for available properties
 * 
 * ============================================================================
 */

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
