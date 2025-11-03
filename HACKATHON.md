# 🎯 RAG Agent Hackathon Challenge

Welcome to the SOP-PP RAG Agent Hackathon! Your mission is to implement a multi-agent AI system that analyzes differences between Standard Operating Procedures (SOP) and Policy responses.

## 📋 Challenge Overview

You'll be building a three-panel chat system where:
1. **SOP Agent** (left panel) - Answers questions about procedures and processes
2. **Policy Agent** (right panel) - Answers questions about policies and compliance
3. **Delta Analysis** (bottom panel) - Compares and explains differences between the two responses

## 🎓 What You'll Learn

- Working with Azure AI Agent Service
- Implementing RAG (Retrieval-Augmented Generation) patterns
- Multi-agent orchestration with function calling
- Real-time web interfaces with Blazor
- Agent-to-agent communication patterns

## 🏗️ Project Structure

```
RagAgentApp/
├── Agents/
│   ├── IAgentService.cs           # Interface (complete)
│   ├── SopRagAgent.cs             # TODO: Implement agent logic
│   └── PolicyRagAgent.cs          # TODO: Implement agent logic
├── Services/
│   └── OrchestratorService.cs     # TODO: Implement orchestration
├── Components/Pages/
│   └── Chat.razor                 # UI (complete - no changes needed)
└── Program.cs                     # Configuration (complete)
```

## 🎯 Your Tasks

### Task 0: Register Services with Dependency Injection (20 mins)

**File**: `Program.cs`

**Goal**: Register all Azure AI services as singletons so they can be injected into your application.

**What is Dependency Injection (DI)?**
- Design pattern that provides objects (services) to classes that need them
- Services are registered in `Program.cs` and injected via constructors
- Blazor and ASP.NET Core use DI extensively

**What is Singleton Lifetime?**
- Service instance is created once and shared across the entire application
- Perfect for AI agents because:
  - Conversation threads must persist across HTTP requests
  - Agent creation is expensive
  - State (agent IDs, thread IDs) must be maintained

**Services to Register** (in order):
1. **PersistentAgentsClient** - Main client for Azure AI Agent Service
   - Use `DefaultAzureCredential()` for keyless authentication
   - Validate `ProjectEndpoint` is configured
   
2. **AzureAISettings** - Configuration singleton
   - Just register the `azureAISettings` variable
   
3. **SopRagAgent** - SOP agent service
   - Resolve: `PersistentAgentsClient`, `AzureAISettings`, `ILogger<SopRagAgent>`
   - Pass to constructor with `ModelDeploymentName` and optional `SopAgentId`
   
4. **PolicyRagAgent** - Policy agent service
   - Same pattern as SopRagAgent
   
5. **OrchestratorService** - Orchestrator service
   - Resolve: All of the above plus `SopRagAgent` and `PolicyRagAgent`

**Registration Pattern**:
```csharp
builder.Services.AddSingleton<ServiceType>(sp =>
{
    // Resolve dependencies using sp.GetRequiredService<T>()
    var dependency = sp.GetRequiredService<DependencyType>();
    
    // Create and return the service
    return new ServiceType(dependency);
});
```

**Hints**:
- Read the TODO comments in `Program.cs` carefully - they guide you step by step
- Order matters! Register dependencies before services that need them
- Check each agent's constructor to see what parameters it needs
- Use `sp.GetRequiredService<T>()` to resolve dependencies from the DI container
- For PersistentAgentsClient, you'll need to create it with an endpoint and credential
- For simple registrations, you can pass an existing instance directly

**Success Criteria**:
- All 5 services are registered
- Build succeeds without errors
- App starts without DI exceptions
- You understand why we use singleton lifetime

**Testing**:
```bash
dotnet build  # Should succeed
dotnet run    # Should start without DI errors
```

---

### Task 1: Implement SopRagAgent (30 mins)

**File**: `Agents/SopRagAgent.cs`

**Goal**: Create an agent that retrieves SOP information from Azure AI Search.

**Key Methods to Implement**:
1. `GetOrResolveAgentId()` - Find or create the SOP agent in Azure AI Foundry
2. `ProcessQueryAsync()` - Process user queries and return responses

**Hints**:
- Use `_agentsClient.Administration.CreateAgent()` to create agents
- Set appropriate system prompts for SOP expertise
- Use `_agentsClient.Threads.CreateThread()` for conversations
- Poll run status until `RunStatus.Completed`

**Success Criteria**:
- Agent creates/reuses correctly
- Returns meaningful SOP-related responses
- Handles errors gracefully

---

### Task 2: Implement PolicyRagAgent (30 mins)

**File**: `Agents/PolicyRagAgent.cs`

**Goal**: Create an agent that retrieves Policy information from Azure AI Search.

**Key Methods to Implement**:
1. `GetOrResolveAgentId()` - Find or create the Policy agent
2. `ProcessQueryAsync()` - Process user queries and return responses

**Hints**:
- Similar to SopRagAgent but with policy-focused prompts
- Reuse thread management patterns
- Remember to handle agent caching

**Success Criteria**:
- Agent creates/reuses correctly
- Returns policy-focused responses
- Different from SOP responses

---

### Task 3: Implement OrchestratorService - Agent Routing (45 mins)

**File**: `Services/OrchestratorService.cs`

**Goal**: Create an orchestrator that routes queries to both agents simultaneously using function calling.

**Method to Implement**: `RouteQueryToAgentsAsync()`

**What it should do**:
1. Create an orchestrator agent with function calling tools
2. Define two function tools: `query_sop_agent` and `query_policy_agent`
3. When orchestrator calls functions, route to respective agents
4. Return both responses in a dictionary

**Hints**:
- Use `FunctionToolDefinition` to define tools
- Handle `RunStatus.RequiresAction` status
- Extract function call arguments using `JsonDocument.Parse()`
- Call `_sopAgent.ProcessQueryAsync()` and `_policyAgent.ProcessQueryAsync()`
- Submit tool outputs back to the orchestrator

**Success Criteria**:
- Both agents called for every query
- Responses returned in dictionary with keys "SOP Agent" and "Policy Agent"
- Function calling works correctly

---

### Task 4: Implement Delta Analysis (45 mins)

**File**: `Services/OrchestratorService.cs`

**Goal**: Create a delta analysis agent that compares SOP and Policy responses.

**Methods to Implement**:
1. `GetOrResolveDeltaAnalysisAgentId()` - Create analysis agent WITHOUT function calling tools
2. `AnalyzeDeltaAsync()` - Compare the two responses and identify differences

**What the analysis should include**:
- Key similarities between responses
- Key differences in a comparison table format
- Any contradictions or conflicts
- Unique insights from each agent
- Relevance assessment

**Hints**:
- Create agent with empty tools list: `tools: new List<ToolDefinition>()`
- Use structured markdown prompt with tables
- Format: `| Aspect | SOP Agent | Policy Agent |`
- This agent should NOT have function calling capabilities

**Success Criteria**:
- Delta analysis agent creates successfully WITHOUT tools
- Generates structured comparison with tables
- Highlights key differences clearly
- Returns formatted markdown text

---

## 🚀 Getting Started

### Prerequisites

1. **Azure Setup**:
   ```bash
   az login
   ```

2. **Configuration**:
   - Copy `.env.example` to `.env`
   - Fill in your Azure AI Foundry endpoint
   - Add your deployed model name (e.g., "gpt-4")

3. **Run the App**:
   ```bash
   cd RagAgentApp
   dotnet run
   ```

4. **Open Browser**:
   Navigate to `http://localhost:5074/chat`

### Development Workflow

1. **Implement Stubs**: Start with `SopRagAgent.cs`
2. **Test Incrementally**: Run after each implementation
3. **Check Logs**: Watch console for helpful debugging info
4. **Iterate**: Fix issues and improve responses

---

## 📚 Key Concepts

### Azure AI Agent Service

Agents are persistent entities in Azure AI Foundry that:
- Have their own identity and instructions
- Can use tools (like search indexes)
- Maintain conversation threads
- Are reused across app restarts

### Function Calling Pattern

```csharp
// 1. Define function tool
var toolDef = new FunctionToolDefinition(
    name: "query_sop_agent",
    description: "Query the SOP agent",
    parameters: BinaryData.FromObjectAsJson(...)
);

// 2. Create agent with tools
var agent = _agentsClient.Administration.CreateAgent(
    model: _modelDeploymentName,
    tools: new List<ToolDefinition> { toolDef }
);

// 3. Handle requires_action status
if (run.Status == RunStatus.RequiresAction) {
    // Extract function calls
    // Execute them
    // Submit results back
}
```

### Thread Management

```csharp
// Create once
var thread = _agentsClient.Threads.CreateThread();
_threadId = thread.Value.Id;

// Reuse for conversation
_agentsClient.Messages.CreateMessage(_threadId, MessageRole.User, query);
```

---

## 🔍 Testing Your Implementation

### Test Queries

1. **SOP-focused**: 
   - "How do I create a new work instruction?"
   - "What are the steps in our approval process?"

2. **Policy-focused**:
   - "What is our data retention policy?"
   - "Who approves expense reports over $5000?"

3. **Both agents**:
   - "Tell me about our childcare documentation"
   - "What's the difference between SOPs and policies?"

### Expected Behavior

✅ Both agents respond to every query  
✅ Responses are different but relevant  
✅ Delta analysis appears in bottom panel  
✅ Delta shows comparison table with differences  
✅ No errors in console logs  

---

## 🐛 Common Issues & Solutions

### Issue: "Agent not found"
**Solution**: Check that agent name matches exactly, or let it create a new one.

### Issue: "Run status: requires_action but nothing happens"
**Solution**: Make sure you're handling `SubmitToolOutputsAction` correctly.

### Issue: "Delta analysis shows requires_action"
**Solution**: Delta agent should have NO tools. Pass empty list: `tools: new List<ToolDefinition>()`

### Issue: "Tables look ugly"
**Solution**: Use proper markdown format: `| Header | Header |` with separator line `|--------|--------|`

---

## 💡 Bonus Challenges

If you finish early, try these enhancements:

1. **Add Citations**: Include source references in responses
2. **Implement Streaming**: Show responses as they're generated
3. **Add Memory**: Make agents remember previous conversations
4. **Custom Analysis**: Add specific comparison criteria
5. **Export Feature**: Allow downloading delta analysis

---

## 📖 Resources

- [Azure AI Agent Service Docs](https://learn.microsoft.com/azure/ai-studio/how-to/develop/agents)
- [Function Calling Guide](https://learn.microsoft.com/azure/ai-services/openai/how-to/function-calling)
- [Blazor Documentation](https://learn.microsoft.com/aspnet/core/blazor/)

---

## 🏆 Success Criteria

Your implementation is complete when:

- [ ] SOP Agent returns procedure-focused responses
- [ ] Policy Agent returns policy-focused responses  
- [ ] Orchestrator calls both agents for every query
- [ ] Delta analysis appears in the bottom panel
- [ ] Delta shows structured comparison with tables
- [ ] Application runs without errors
- [ ] All three panels populate correctly

---

## 🎉 Submission

When you're done:

1. Test with multiple queries
2. Take screenshots of the three-panel interface
3. Document any interesting findings or challenges
4. Share your solution with your team!

---

## 🤝 Getting Help

- Check the logs - they're very detailed!
- Review existing code patterns in other files
- Ask your teammates or mentors
- Remember: The UI is complete, focus on backend logic!

---

**Good luck and happy coding!** 🚀
