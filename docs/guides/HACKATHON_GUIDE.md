# Hackathon Guide: Building a Dual-Agent RAG System

Welcome to the SOP-PP Hackathon! This guide will help you implement a complete dual-agent RAG system using Azure AI Agent Service.

## 🎯 Overview

This repository contains a **stubbed-out** version of a .NET Blazor web application that will feature:
- Two specialized AI agents (SOP Agent and Policy Agent)
- An orchestrator that routes queries to both agents
- A responsive UI that displays responses from both agents simultaneously

**Your mission**: Implement the agent logic to make this system work!

## 📋 What's Already Done

✅ Complete Blazor UI with chat interface  
✅ Agent interfaces and service structure  
✅ Orchestration framework  
✅ Docker configuration  
✅ Project structure and best practices  

## 🎓 What You'll Learn

1. Setting up Azure AI Foundry projects
2. Creating AI agents with the Azure AI Agent Service
3. Implementing agent communication patterns
4. Working with Azure AI Search (optional)
5. Building orchestrated multi-agent systems
6. .NET dependency injection patterns
7. Async/await patterns in C#

## 🚀 Quick Start

### Prerequisites

- .NET 9.0 SDK
- Azure subscription
- Visual Studio Code or Visual Studio 2022
- Docker (optional, for containerization)

### 1. Run the Stubbed Version

```bash
cd RagAgentApp
dotnet run
```

Navigate to `http://localhost:5000` and try the chat interface. You'll see placeholder responses from both agents.

## 📚 Implementation Levels

We've designed three implementation levels for different skill levels:

### Level 1: Basic Agent Implementation (Beginner-Friendly)

**Goal**: Get the agents calling Azure OpenAI via Azure AI Agent Service (no RAG yet)

**Files to modify**:
- `RagAgentApp/Agents/SopRagAgent.cs`
- `RagAgentApp/Agents/PolicyRagAgent.cs`
- `RagAgentApp/Program.cs`

**Steps**:
1. Set up an Azure AI Foundry project
2. Deploy a GPT model (e.g., gpt-4, gpt-35-turbo)
3. Get your project endpoint
4. Update `appsettings.Development.json`:
   ```json
   {
     "AzureAI": {
       "ProjectEndpoint": "https://your-foundry.services.ai.azure.com/api/projects/YourProject",
       "ModelDeploymentName": "gpt-4"
     }
   }
   ```
5. Uncomment the Azure AI setup in `Program.cs`
6. Implement `ProcessQueryAsync` in both agent classes

**Key Azure AI Concepts**:

#### PersistentAgentsClient
Main client for interacting with Azure AI Agent Service:
```csharp
var client = new PersistentAgentsClient(endpoint, new DefaultAzureCredential());
```

#### Thread
A conversation session:
```csharp
var threadResponse = client.Threads.CreateThread();
var threadId = threadResponse.Value.Id;
```

#### Agent
An AI assistant with instructions and tools:
```csharp
var agent = client.Administration.CreateAgent(
    model: "gpt-4",
    name: "My Agent",
    instructions: "You are a helpful assistant..."
);
```

#### Run
Execution of an agent on a thread:
```csharp
var runResponse = client.Runs.CreateRun(threadId, agentId);
var run = runResponse.Value;

// Poll until complete
while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
{
    await Task.Delay(1000);
    run = client.Runs.GetRun(threadId, run.Id).Value;
}
```

### Level 2: Add RAG with File Search (Intermediate)

**Goal**: Connect agents to knowledge bases using Azure AI Search

**Additional steps**:
1. Create Azure AI Search resources
2. Upload sample SOP and Policy documents
3. Create vector stores in Azure AI Foundry
4. Attach file search tools to your agents
5. Update agent creation to include the search tool:
   ```csharp
   var fileSearchTool = new FileSearchToolDefinition();
   var agent = _agentsClient.Administration.CreateAgent(
       model: _modelDeploymentName,
       name: "SOP Expert Agent",
       instructions: systemPrompt,
       tools: new List<ToolDefinition> { fileSearchTool }
   );
   ```

### Level 3: Advanced Orchestration (Advanced)

**Goal**: Implement intelligent orchestration using function calling

**Files to modify**:
- `RagAgentApp/Services/OrchestratorService.cs`

**Steps**:
1. Create an orchestrator agent with function calling capabilities
2. Define function tools for `query_sop_agent` and `query_policy_agent`
3. Implement function calling flow
4. Let the orchestrator intelligently decide when to call each agent

## 📖 Quick Reference

### Files to Modify (in order)

1. **Configuration**: `appsettings.Development.json`
2. **SOP Agent**: `RagAgentApp/Agents/SopRagAgent.cs` - Look for `// TODO` comments
3. **Policy Agent**: `RagAgentApp/Agents/PolicyRagAgent.cs` - Same pattern as SOP Agent
4. **Program Setup**: `RagAgentApp/Program.cs` - Uncomment Azure AI configuration

### Basic Agent Implementation Pattern

```csharp
public async Task<string> ProcessQueryAsync(string query, CancellationToken ct = default)
{
    // 1. Get or create agent
    var agentId = GetOrResolveAgentId();
    
    // 2. Create thread
    var threadResponse = _agentsClient.Threads.CreateThread();
    var threadId = threadResponse.Value.Id;
    
    // 3. Add user message
    _agentsClient.Messages.CreateMessage(threadId, MessageRole.User, query);
    
    // 4. Create run
    var runResponse = _agentsClient.Runs.CreateRun(threadId, agentId);
    var run = runResponse.Value;
    
    // 5. Poll until complete
    while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
    {
        await Task.Delay(1000, ct);
        run = _agentsClient.Runs.GetRun(threadId, run.Id).Value;
    }
    
    // 6. Get response
    var messages = _agentsClient.Messages.GetMessages(threadId);
    var lastMessage = messages
        .Where(m => m.Role == MessageRole.Assistant)
        .FirstOrDefault();
    
    if (lastMessage?.ContentItems?.FirstOrDefault() is MessageTextContent textContent)
    {
        return textContent.Text;
    }
    
    return "No response generated";
}
```

## 🐛 Troubleshooting

| Error | Fix |
|-------|-----|
| "Cannot find Azure AI endpoint" | Check appsettings.Development.json |
| "Unauthorized" | Run `az login` |
| "Model not found" | Verify model deployment name |
| Duplicate agents | Use agent name checking |

## 💡 Pro Tips

1. **Start Simple**: Get basic agent working before adding RAG
2. **Reuse Agents**: Check for existing agents before creating new ones
3. **Cache Thread IDs**: Reuse threads for conversation continuity
4. **Log Everything**: Use `_logger` to see what's happening
5. **Test Incrementally**: Test after each small change
6. **Handle Errors**: Add try-catch blocks around Azure calls

## 🏁 Success Metrics

**Minimum**: Both agents respond with real AI (not stubs)  
**Good**: RAG working with document search  
**Excellent**: Intelligent orchestration with function calling

## 📚 Resources

- **Azure AI Foundry**: https://ai.azure.com
- **Azure AI Docs**: https://learn.microsoft.com/azure/ai-studio/
- **SDK Reference**: https://learn.microsoft.com/dotnet/api/azure.ai.agents

Good luck! 🚀
