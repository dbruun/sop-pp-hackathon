# Quick Reference Card for Hackathon Participants

## 🚀 Quick Start

```bash
cd RagAgentApp
dotnet run
# Navigate to http://localhost:5000
```

## 📝 Files to Modify (in order)

### 1. Configuration
**File**: `RagAgentApp/appsettings.Development.json`
```json
{
  "AzureAI": {
    "ProjectEndpoint": "https://YOUR-FOUNDRY.services.ai.azure.com/api/projects/YOUR-PROJECT",
    "ModelDeploymentName": "gpt-4"
  }
}
```

### 2. SOP Agent
**File**: `RagAgentApp/Agents/SopRagAgent.cs`  
**What to do**: Implement `ProcessQueryAsync()` method  
**Search for**: `// TODO: Replace this with actual Azure AI Agent Service call`

### 3. Policy Agent
**File**: `RagAgentApp/Agents/PolicyRagAgent.cs`  
**What to do**: Implement `ProcessQueryAsync()` method (same pattern as SOP Agent)  
**Search for**: `// TODO: Replace this with actual Azure AI Agent Service call`

### 4. Program Setup
**File**: `RagAgentApp/Program.cs`  
**What to do**: Uncomment Azure AI configuration sections  
**Search for**: `// HACKATHON TODO`

## 🔑 Key Azure AI Concepts

### PersistentAgentsClient
Main client for interacting with Azure AI Agent Service.

```csharp
var client = new PersistentAgentsClient(endpoint, new DefaultAzureCredential());
```

### Thread
A conversation session.

```csharp
var threadResponse = await client.Threads.CreateThreadAsync();
var threadId = threadResponse.Value.Id;
```

### Agent
An AI assistant with instructions and tools.

```csharp
var agent = client.Administration.CreateAgent(
    model: "gpt-4",
    name: "My Agent",
    instructions: "You are a helpful assistant..."
);
```

### Run
Execution of an agent on a thread.

```csharp
var runResponse = await client.Runs.CreateRunAsync(threadId, agentId);
var run = runResponse.Value;

// Poll until complete
while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
{
    await Task.Delay(1000);
    run = (await client.Runs.GetRunAsync(threadId, run.Id)).Value;
}
```

### Messages
Get the agent's response.

```csharp
var messages = await client.Messages.GetMessagesAsync(threadId);
var lastMessage = messages.Value.Data
    .Where(m => m.Role == MessageRole.Assistant)
    .FirstOrDefault();
```

## 🎯 Implementation Checklist

### Level 1: Basic Implementation
- [ ] Set up Azure AI Foundry project
- [ ] Deploy GPT model
- [ ] Configure appsettings.Development.json
- [ ] Login with `az login`
- [ ] Uncomment Azure AI setup in Program.cs
- [ ] Implement SopRagAgent.ProcessQueryAsync()
- [ ] Implement PolicyRagAgent.ProcessQueryAsync()
- [ ] Test with sample queries

### Level 2: Add RAG (Optional)
- [ ] Create Azure AI Search resource
- [ ] Upload SOP documents
- [ ] Upload Policy documents
- [ ] Create vector stores in Azure AI Foundry
- [ ] Add file search tool to agents
- [ ] Test RAG responses

### Level 3: Advanced Orchestration (Optional)
- [ ] Create orchestrator agent with function tools
- [ ] Define query_sop_agent function
- [ ] Define query_policy_agent function
- [ ] Implement function calling flow
- [ ] Handle tool outputs
- [ ] Test intelligent routing

## 🔧 Common Code Patterns

### Basic Agent Implementation Pattern

```csharp
public async Task<string> ProcessQueryAsync(string query, CancellationToken ct = default)
{
    // 1. Get or create agent
    var agentId = GetOrResolveAgentId();
    
    // 2. Create thread
    var threadResponse = await _agentsClient.Threads.CreateThreadAsync(ct);
    var threadId = threadResponse.Value.Id;
    
    // 3. Add user message
    await _agentsClient.Messages.CreateMessageAsync(threadId, MessageRole.User, query, ct);
    
    // 4. Create run
    var runResponse = await _agentsClient.Runs.CreateRunAsync(threadId, agentId, ct);
    var run = runResponse.Value;
    
    // 5. Poll until complete
    while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
    {
        await Task.Delay(1000, ct);
        run = (await _agentsClient.Runs.GetRunAsync(threadId, run.Id, ct)).Value;
    }
    
    // 6. Get response
    var messages = await _agentsClient.Messages.GetMessagesAsync(threadId, ct);
    var lastMessage = messages.Value.Data
        .Where(m => m.Role == MessageRole.Assistant)
        .FirstOrDefault();
    
    if (lastMessage?.Content?.FirstOrDefault() is MessageTextContent textContent)
    {
        return textContent.Text.Value;
    }
    
    return "No response generated";
}
```

### Agent Creation with Reuse

```csharp
private string GetOrResolveAgentId()
{
    // Check if agent already exists
    var existingAgents = _agentsClient.Administration.GetAgents();
    var existingAgent = existingAgents.FirstOrDefault(a => a.Name == "My Agent");
    
    if (existingAgent != null)
    {
        return existingAgent.Id;
    }
    
    // Create new agent
    var newAgent = _agentsClient.Administration.CreateAgent(
        model: _modelDeploymentName,
        name: "My Agent",
        instructions: "Your system prompt here..."
    );
    
    return newAgent.Value.Id;
}
```

## 🐛 Troubleshooting Quick Fixes

| Error | Fix |
|-------|-----|
| "Cannot find Azure AI endpoint" | Check appsettings.Development.json |
| "Unauthorized" | Run `az login` |
| "Model not found" | Verify model deployment name |
| Duplicate agents | Use agent name checking |

## 📚 Resources

- **Full Guide**: [HACKATHON.md](HACKATHON.md)
- **Azure AI Docs**: https://learn.microsoft.com/azure/ai-studio/
- **SDK Reference**: https://learn.microsoft.com/dotnet/api/azure.ai.agents

## 💡 Pro Tips

1. **Start Simple**: Get basic agent working before adding RAG
2. **Reuse Agents**: Check for existing agents before creating new ones
3. **Cache Thread IDs**: Reuse threads for conversation continuity
4. **Log Everything**: Use `_logger` to see what's happening
5. **Test Incrementally**: Test after each small change
6. **Handle Errors**: Add try-catch blocks around Azure calls

## 🎓 Learning Path

1. Run the stubbed version → See placeholder responses
2. Set up Azure AI Foundry → Get familiar with the portal
3. Implement basic agent → Get first real AI response
4. Add orchestration → See both agents working
5. (Optional) Add RAG → Connect to document stores
6. (Optional) Advanced features → Function calling, streaming, etc.

## 🏁 Success Metrics

**Minimum**: Both agents respond with real AI (not stubs)  
**Good**: RAG working with document search  
**Excellent**: Intelligent orchestration with function calling

Good luck! 🚀
