# Migration to Azure AI Agent Service

This document describes the migration from Microsoft Semantic Kernel to the Azure AI Agent Service framework.

## What Changed

### Package Updates
- **Removed**: `Microsoft.SemanticKernel` and `Microsoft.SemanticKernel.Agents.AzureAI`
- **Added**: 
  - `Azure.AI.Projects` (v1.0.0)
  - `Azure.AI.Agents.Persistent` (v1.1.0)
  - `Azure.Identity` (v1.17.0)

### Architecture Changes

#### Before (Semantic Kernel)
- Used `IChatCompletionService` for basic chat completion
- Simple system prompts + user messages
- No real agent orchestration or state management
- API key authentication only

#### After (Azure AI Agent Service)
- Uses `PersistentAgentsClient` from Azure.AI.Agents.Persistent
- Proper agent creation with persistent state in Azure AI Foundry
- Thread-based conversations with state management
- Built-in support for agent tools and RAG capabilities (Azure AI Search)
- Agent lifecycle management (create, list, run, poll for completion)
- **Entra ID authentication** via `DefaultAzureCredential` (preferred)
- Agent reuse across application restarts

### Code Changes

**Agent Implementation**:
- Agents now use `PersistentAgentsClient` instead of Semantic Kernel's `IChatCompletionService`
- Each agent maintains its own persistent state via `Agent` and `AgentThread` objects stored in Azure AI Foundry
- Agents can be listed, reused, and updated across application restarts
- Conversations use thread-based messaging with full history
- Responses are polled asynchronously until completion
- Support for Azure AI Search integration for RAG capabilities

**Dependency Injection**:
- `PersistentAgentsClient` is registered as a singleton
- Uses `DefaultAzureCredential` for Entra ID authentication (Azure CLI, Managed Identity, etc.)
- API key authentication available as fallback for testing
- Model deployment name is injected into agents
- Agent IDs can be pre-configured to reuse existing agents

## Configuration

### Recommended: Entra ID Authentication (Keyless)

```json
{
  "AzureAI": {
    "ProjectEndpoint": "https://your-foundry.services.ai.azure.com/api/projects/YourProject",
    "ModelDeploymentName": "gpt-4",
    "SopAgentId": "",
    "PolicyAgentId": ""
  }
}
```

**Authentication**: Just run `az login` - the app uses `DefaultAzureCredential` automatically!

Or via environment variables:
```bash
AZURE_AI_PROJECT_ENDPOINT=https://your-foundry.services.ai.azure.com/api/projects/YourProject
AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4
# No API key needed!
```

### Alternative: API Key (Testing Only)

```json
{
  "AzureAI": {
    "ProjectEndpoint": "https://your-foundry.services.ai.azure.com/api/projects/YourProject",
    "ApiKey": "your-api-key",
    "ModelDeploymentName": "gpt-4"
  }
}
```

Or via environment variables:
```bash
AZURE_AI_PROJECT_ENDPOINT=https://your-foundry.services.ai.azure.com/api/projects/YourProject
AZURE_AI_API_KEY=your-api-key
AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4
```

**Note:** Connection strings are no longer supported. Use endpoint + Entra ID authentication.

## Benefits of Azure AI Agent Service

1. **True Agent Framework**: Proper agent orchestration with persistent state management
2. **Built-in RAG**: Native support for Azure AI Search integration with file search tools
3. **Agent Tools**: Easy integration of function calling, file search, and code interpreter
4. **Scalability**: Azure-managed agent lifecycle and execution in the cloud
5. **Observability**: Better tracing and monitoring capabilities
6. **Multi-turn Conversations**: Thread-based conversations with persistent context
7. **Agent Persistence**: Agents stored in Azure AI Foundry can be reused across restarts
8. **Secure Authentication**: Entra ID (DefaultAzureCredential) eliminates key management
9. **Orchestrator Agent**: Function calling enables sophisticated agent coordination

## Future Enhancements

Now that the application uses Azure AI Agent Service with PersistentAgentsClient, you can easily add:

- **File Search Tool**: Enable agents to search through uploaded documents via Azure AI Search
- **Function Calling**: Add custom tools/functions for agents to call (partially implemented in OrchestratorService)
- **Code Interpreter**: Enable agents to write and execute code
- **Azure AI Search Integration**: Connect to your knowledge bases for true RAG capabilities
- **Agent-to-Agent Communication**: Already implemented - orchestrator uses function calling to route to specialized agents
- **Multi-Modal Agents**: Add vision capabilities with GPT-4o or GPT-4 Vision
- **Streaming Responses**: Implement real-time token streaming for better UX

## Running the Application

```bash
# Login to Azure (for Entra ID authentication)
az login

# Navigate to app directory
cd RagAgentApp

# Run the application
dotnet run
```

Navigate to `http://localhost:5000/chat` to interact with the agents.

## Key Changes Summary

| Feature | Before (Semantic Kernel) | After (Azure AI Agent Service) |
|---------|-------------------------|--------------------------------|
| Package | Microsoft.SemanticKernel | Azure.AI.Agents.Persistent v1.1.0 |
| Client | IChatCompletionService | PersistentAgentsClient |
| Authentication | API Key only | DefaultAzureCredential (Entra ID) + API Key fallback |
| State | Stateless | Persistent agents in Azure AI Foundry |
| Conversations | Single messages | Thread-based with history |
| RAG Support | Manual implementation | Built-in Azure AI Search integration |
| Agent Reuse | Not supported | Full agent lifecycle management |
