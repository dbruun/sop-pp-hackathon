# Migration to Azure AI Agent Service

This document describes the migration from Microsoft Semantic Kernel to the Azure AI Agent Service framework.

## What Changed

### Package Updates
- **Removed**: `Microsoft.SemanticKernel` and `Microsoft.SemanticKernel.Agents.AzureAI`
- **Added**: `Azure.AI.Projects` (v1.0.0-beta.2)

### Architecture Changes

#### Before (Semantic Kernel)
- Used `IChatCompletionService` for basic chat completion
- Simple system prompts + user messages
- No real agent orchestration or state management

#### After (Azure AI Agent Service)
- Uses `AgentsClient` from Azure.AI.Projects
- Proper agent creation with persistent state
- Thread-based conversations
- Built-in support for agent tools and RAG capabilities
- Agent lifecycle management (create, run, poll for completion)

### Code Changes

**Agent Implementation**:
- Agents now use `AgentsClient` instead of Semantic Kernel's `IChatCompletionService`
- Each agent maintains its own state via `Agent` and `AgentThread` objects
- Conversations use thread-based messaging
- Responses are polled asynchronously until completion

**Dependency Injection**:
- `AgentsClient` is registered as a singleton
- Supports both connection string and endpoint/key authentication
- Model deployment name is injected into agents

## Configuration

### Option 1: Azure AI Foundry Project Connection String (Recommended)

```json
{
  "AzureAI": {
    "ConnectionString": "your-azure-ai-foundry-connection-string",
    "ModelDeploymentName": "gpt-4"
  }
}
```

Or via environment variables:
```bash
AZURE_AI_CONNECTION_STRING=your-azure-ai-foundry-connection-string
AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4
```

### Option 2: Direct Endpoint + API Key

```json
{
  "AzureAI": {
    "ProjectEndpoint": "https://your-project.cognitiveservices.azure.com/",
    "ApiKey": "your-api-key",
    "ModelDeploymentName": "gpt-4"
  }
}
```

Or via environment variables:
```bash
AZURE_AI_PROJECT_ENDPOINT=https://your-project.cognitiveservices.azure.com/
AZURE_AI_API_KEY=your-api-key
AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4
```

## Benefits of Azure AI Agent Service

1. **True Agent Framework**: Proper agent orchestration with state management
2. **Built-in RAG**: Native support for Azure AI Search integration
3. **Agent Tools**: Easy integration of function calling, file search, and code interpreter
4. **Scalability**: Azure-managed agent lifecycle and execution
5. **Observability**: Better tracing and monitoring capabilities
6. **Multi-turn Conversations**: Thread-based conversations with persistent context

## Future Enhancements

Now that the application uses Azure AI Agent Service, you can easily add:

- **File Search Tool**: Enable agents to search through uploaded documents
- **Function Calling**: Add custom tools/functions for agents to call
- **Code Interpreter**: Enable agents to write and execute code
- **Azure AI Search Integration**: Connect to your knowledge bases for RAG
- **Agent-to-Agent Communication**: Orchestrate multiple agents working together

## Running the Application

```bash
cd RagAgentApp
dotnet run
```

Navigate to `http://localhost:5000/chat` to interact with the agents.
