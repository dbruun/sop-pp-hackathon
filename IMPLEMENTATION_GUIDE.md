# Implementation Guide - What's Stubbed and What to Implement

This guide helps hackathon participants understand exactly what needs to be implemented.

## 🎯 What's Already Done (Don't Change)

### Structure & Dependencies
- ✅ All NuGet packages installed (Azure.AI.Agents.Persistent, Azure.AI.Projects, Azure.Identity)
- ✅ All `using` statements in place
- ✅ Complete Blazor UI components
- ✅ Service registration in Program.cs
- ✅ Dependency injection configured
- ✅ All class-level variables declared
- ✅ All constructor parameters defined
- ✅ Error handling structure

### What Works Out of the Box
- ✅ Application builds successfully
- ✅ UI loads and displays correctly
- ✅ Chat interface accepts input
- ✅ Orchestration calls both agents
- ✅ Loading states and error handling

## 🔧 What Needs Implementation

### File 1: `RagAgentApp/Agents/SopRagAgent.cs`

#### Method 1: `GetOrResolveAgentId()` (Lines ~35-42)
**Current State**: Returns "stub-agent-id"  
**What to Implement**:
```csharp
private string GetOrResolveAgentId()
{
    // Step 1: Check cache
    if (_agentIdResolved != null)
    {
        return _agentIdResolved;
    }

    // Step 2: Use provided agent ID if available
    if (!string.IsNullOrEmpty(_agentId))
    {
        _agentIdResolved = _agentId;
        return _agentIdResolved;
    }

    // Step 3: Search for existing agent
    const string agentName = "SOP Expert Agent";
    var existingAgents = _agentsClient.Administration.GetAgents();
    var existingAgent = existingAgents.FirstOrDefault(a => a.Name == agentName);

    if (existingAgent != null)
    {
        _agentIdResolved = existingAgent.Id;
        _logger.LogInformation("Found existing agent: {AgentId}", _agentIdResolved);
        return _agentIdResolved;
    }

    // Step 4: Create new agent
    var systemPrompt = @"You are a Standard Operating Procedures (SOP) expert assistant...";
    var newAgent = _agentsClient.Administration.CreateAgent(
        model: _modelDeploymentName,
        name: agentName,
        instructions: systemPrompt
    );
    
    _agentIdResolved = newAgent.Value.Id;
    _logger.LogInformation("Created new agent: {AgentId}", _agentIdResolved);
    return _agentIdResolved;
}
```

#### Method 2: `ProcessQueryAsync()` (Lines ~70-100)
**Current State**: Returns stub text after 500ms delay  
**What to Implement**:
```csharp
public async Task<string> ProcessQueryAsync(string query, CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("Processing SOP query: {Query}", query);
        
        // Step 1: Get agent ID
        var agentId = GetOrResolveAgentId();

        // Step 2: Create or reuse thread
        if (string.IsNullOrEmpty(_threadId))
        {
            var threadResponse = _agentsClient.Threads.CreateThread();
            _threadId = threadResponse.Value.Id;
            _logger.LogInformation("Created thread: {ThreadId}", _threadId);
        }

        // Step 3: Add user message
        _agentsClient.Messages.CreateMessage(_threadId, MessageRole.User, query);

        // Step 4: Create and run agent
        var runResponse = _agentsClient.Runs.CreateRun(_threadId, agentId);
        var run = runResponse.Value;
        _logger.LogInformation("Run created: {RunId}", run.Id);

        // Step 5: Poll for completion
        while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
        {
            await Task.Delay(1000, cancellationToken);
            var statusResponse = _agentsClient.Runs.GetRun(_threadId, run.Id);
            run = statusResponse.Value;
        }

        // Step 6: Check for failure
        if (run.Status == RunStatus.Failed)
        {
            _logger.LogError("Run failed: {Error}", run.LastError?.Message);
            return $"Agent run failed: {run.LastError?.Message ?? "Unknown error"}";
        }

        // Step 7: Get messages
        var messages = _agentsClient.Messages.GetMessages(_threadId);
        var lastMessage = messages.FirstOrDefault(m => m.Role != MessageRole.User);

        if (lastMessage?.ContentItems?.FirstOrDefault() is MessageTextContent textContent)
        {
            _logger.LogInformation("Response generated successfully");
            return textContent.Text;
        }

        return "No response generated";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing SOP query");
        return $"Error: {ex.Message}";
    }
}
```

### File 2: `RagAgentApp/Agents/PolicyRagAgent.cs`

#### Method 1: `GetOrResolveAgentId()` (Lines ~35-42)
**Implementation**: Same pattern as SopRagAgent, but use:
- Agent name: `"Policy Expert Agent"`
- Different system prompt focused on policies

#### Method 2: `ProcessQueryAsync()` (Lines ~70-100)
**Implementation**: Exactly the same as SopRagAgent

### File 3: `RagAgentApp/Services/OrchestratorService.cs`

#### Current Implementation: WORKS AS-IS! ✅
The simple orchestration is already implemented in `RouteQueryToAgentsAsync()`. It calls both agents in parallel and returns responses. **No changes needed for Level 1 & 2**.

#### Optional Advanced Implementation: `GetOrResolveOrchestratorAgentId()` (Level 3)
Only implement this if you want intelligent orchestration with function calling.

## 📝 Configuration Required

### File: `RagAgentApp/appsettings.Development.json`
Create this file with:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AzureAI": {
    "ProjectEndpoint": "https://YOUR-FOUNDRY.services.ai.azure.com/api/projects/YOUR-PROJECT",
    "ModelDeploymentName": "gpt-4"
  }
}
```

### Authentication
Run: `az login` (uses Azure CLI credentials automatically)

## 🧪 Testing Your Implementation

### Step 1: Verify Configuration
```bash
az account show  # Verify you're logged in
```

### Step 2: Build
```bash
cd RagAgentApp
dotnet build
```

### Step 3: Run
```bash
dotnet run
```

### Step 4: Test
1. Navigate to `http://localhost:5074/chat`
2. Type a question
3. Click Send
4. Verify both agents respond (not with stub text)

### Sample Test Queries
- "What are the safety procedures for equipment operation?"
- "What is the policy on remote work?"
- "How do I submit a vacation request?"

## 🎓 Implementation Order

### Recommended Sequence:
1. ✅ Configure appsettings.Development.json
2. ✅ Run `az login`
3. ✅ Implement `SopRagAgent.GetOrResolveAgentId()`
4. ✅ Implement `SopRagAgent.ProcessQueryAsync()`
5. ✅ Test SOP agent works
6. ✅ Implement `PolicyRagAgent.GetOrResolveAgentId()`
7. ✅ Implement `PolicyRagAgent.ProcessQueryAsync()`
8. ✅ Test Policy agent works
9. ✅ Test both agents together
10. ✅ (Optional) Add RAG features
11. ✅ (Optional) Advanced orchestration

## 📊 Success Checklist

### Level 1: Basic Implementation
- [ ] Application builds without errors
- [ ] Application runs and loads UI
- [ ] Can send a query
- [ ] SOP Agent returns real AI response (not stub)
- [ ] Policy Agent returns real AI response (not stub)
- [ ] Both responses appear simultaneously

### Level 2: RAG Implementation (Optional)
- [ ] Azure AI Search resource created
- [ ] Documents uploaded to vector stores
- [ ] File search tool added to agents
- [ ] Agents cite sources from documents

### Level 3: Advanced Orchestration (Optional)
- [ ] Orchestrator agent created
- [ ] Function tools defined
- [ ] Function calling working
- [ ] Tool outputs submitted correctly

## 🔍 Debugging Tips

### Issue: "Cannot find Azure AI endpoint"
**Solution**: Check appsettings.Development.json has correct ProjectEndpoint

### Issue: "Unauthorized" or "403 Forbidden"
**Solution**: Run `az login` and verify correct subscription

### Issue: "Model not found"
**Solution**: Verify ModelDeploymentName matches your deployment

### Issue: Duplicate agents created
**Solution**: Check GetOrResolveAgentId() searches by name first

### Issue: No response from agent
**Solution**: Check polling logic waits for completion

### Issue: Build errors
**Solution**: Ensure all using statements are present at top of files

## 📚 Key Classes and Methods Reference

### PersistentAgentsClient
- `Administration.GetAgents()` - List existing agents
- `Administration.CreateAgent()` - Create new agent
- `Threads.CreateThread()` - Create conversation thread
- `Messages.CreateMessage()` - Add message to thread
- `Messages.GetMessages()` - Retrieve messages
- `Runs.CreateRun()` - Execute agent on thread
- `Runs.GetRun()` - Check run status

### RunStatus Enum
- `Queued` - Waiting to start
- `InProgress` - Currently running
- `Completed` - Finished successfully
- `Failed` - Errored
- `RequiresAction` - Needs function calling (Level 3)

### MessageRole Enum
- `User` - Message from user
- `Assistant` - Message from AI

## 🎯 Final Notes

**What Makes This a Good Learning Project**:
- Real Azure AI integration
- Production-ready patterns
- Clean architecture
- Proper error handling
- Comprehensive logging
- Multi-agent orchestration

**What You'll Learn**:
- Azure AI Foundry setup
- AI Agent Service patterns
- Async/await in C#
- Dependency injection
- Thread and run management
- Error handling best practices

Good luck with your implementation! 🚀
