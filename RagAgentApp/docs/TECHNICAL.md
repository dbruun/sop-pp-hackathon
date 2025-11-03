# Technical Documentation

Deep technical reference for the RAG Agent System architecture, implementation details, and migration notes.

## Table of Contents

1. [System Architecture](#system-architecture)
2. [Component Breakdown](#component-breakdown)
3. [Data Flow](#data-flow)
4. [Implementation Details](#implementation-details)
5. [Migration from Semantic Kernel](#migration-from-semantic-kernel)
6. [Technical Decisions](#technical-decisions)
7. [Performance Characteristics](#performance-characteristics)
8. [Security Considerations](#security-considerations)

---

## System Architecture

### High-Level Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      User Interface                         │
│                  (Blazor Server - Chat.razor)               │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                   OrchestratorService                        │
│    • Request routing                                         │
│    • Agent selection (SOP vs Policy)                        │
│    • Response aggregation                                    │
└────────┬───────────────────────────────────┬────────────────┘
         │                                   │
         ▼                                   ▼
┌──────────────────────┐          ┌──────────────────────┐
│   SopRagAgent        │          │   PolicyRagAgent     │
│  • SOP queries       │          │  • Policy queries    │
│  • Work instructions │          │  • Regulations       │
│  • Process docs      │          │  • Compliance        │
└──────────┬───────────┘          └──────────┬───────────┘
           │                                  │
           └──────────────┬───────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│               Azure AI Agent Service                         │
│    • Thread management (persistent conversations)           │
│    • Agent execution                                         │
│    • Tool orchestration (file search, Azure AI Search)      │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                  Azure OpenAI Service                        │
│    • GPT-4 / GPT-4o inference                               │
│    • Token generation                                        │
│    • Response streaming                                      │
└─────────────────────────────────────────────────────────────┘
```

### Component Layers

#### 1. Presentation Layer
- **Technology:** Blazor Server
- **Components:** Razor pages, interactive UI
- **Responsibility:** User interaction, real-time updates via SignalR

#### 2. Application Layer
- **Services:** `OrchestratorService`
- **Responsibility:** Business logic, request routing, orchestration

#### 3. Agent Layer
- **Agents:** `SopRagAgent`, `PolicyRagAgent`
- **Responsibility:** Agent initialization, thread management, query processing

#### 4. Integration Layer
- **SDK:** `Azure.AI.Agents.Persistent v1.1.0`
- **Responsibility:** Azure AI Foundry communication, authentication

#### 5. Infrastructure Layer
- **Platform:** Azure AI Agent Service
- **Models:** Azure OpenAI GPT-4/GPT-4o
- **Tools:** File Search, Azure AI Search, Code Interpreter

---

## Component Breakdown

### OrchestratorService.cs

**Purpose:** Central service that routes queries to appropriate agents.

```csharp
public class OrchestratorService
{
    private readonly IAgentService _sopAgent;
    private readonly IAgentService _policyAgent;
    
    // Query routing logic
    public async Task<string> ProcessQueryAsync(string query, AgentType agentType)
    {
        IAgentService selectedAgent = agentType switch
        {
            AgentType.Sop => _sopAgent,
            AgentType.Policy => _policyAgent,
            _ => throw new ArgumentException("Invalid agent type")
        };
        
        return await selectedAgent.GetResponseAsync(query);
    }
}
```

**Key Responsibilities:**
- Agent selection based on user choice
- Request forwarding
- Response handling
- Error propagation

**Design Pattern:** Service Locator + Strategy Pattern

### SopRagAgent.cs & PolicyRagAgent.cs

**Purpose:** Agent wrappers that manage Azure AI Agent lifecycle.

```csharp
public class SopRagAgent : IAgentService
{
    private AgentsClient _client;
    private Agent _agent;
    private AgentThread? _thread;
    
    public async Task<string> GetResponseAsync(string query)
    {
        // 1. Ensure agent is initialized
        await EnsureAgentInitialized();
        
        // 2. Get or create thread (conversation context)
        _thread ??= await _client.CreateThreadAsync();
        
        // 3. Create message
        await _client.CreateMessageAsync(_thread.Id, MessageRole.User, query);
        
        // 4. Run agent and stream response
        return await StreamAgentResponse(_thread.Id);
    }
}
```

**Key Features:**
- **Lazy Initialization:** Agent created on first use
- **Thread Reuse:** Single thread per agent instance = conversation memory
- **Auto-Recovery:** If agent not found, creates new one
- **Streaming:** Response streaming for better UX

**Thread Management Strategy:**
- **Single Thread:** Each agent maintains one thread throughout app lifetime
- **Conversation Memory:** Azure AI Agent Service stores full conversation history
- **Persistence:** Thread survives restarts if ID is persisted (future enhancement)

### Models/AzureOpenAISettings.cs

**Purpose:** Configuration model with validation.

```csharp
public class AzureOpenAISettings
{
    public string ProjectEndpoint { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string ModelDeploymentName { get; set; } = string.Empty;
    public string? SopAgentId { get; set; }
    public string? PolicyAgentId { get; set; }
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ProjectEndpoint))
            throw new InvalidOperationException("ProjectEndpoint is required");
        if (string.IsNullOrWhiteSpace(ModelDeploymentName))
            throw new InvalidOperationException("ModelDeploymentName is required");
    }
}
```

**Validation:** Performed at startup (Program.cs)

### Program.cs

**Purpose:** Application bootstrap, dependency injection setup.

```csharp
// 1. Load configuration
var settings = builder.Configuration
    .GetSection("AzureAI")
    .Get<AzureOpenAISettings>() ?? new();

settings.Validate();

// 2. Register agents as singletons (maintain state across requests)
builder.Services.AddSingleton<IAgentService, SopRagAgent>(sp => 
    new SopRagAgent(settings));
builder.Services.AddSingleton<IAgentService, PolicyRagAgent>(sp => 
    new PolicyRagAgent(settings));

// 3. Register orchestrator
builder.Services.AddSingleton<OrchestratorService>();
```

**Singleton Rationale:**
- Agents maintain conversation state
- Thread reuse across requests
- Connection pooling to Azure AI Foundry
- Cost optimization (fewer agent initializations)

### Components/Pages/Chat.razor

**Purpose:** Interactive chat UI with real-time updates.

```razor
@code {
    private List<ChatMessage> _messages = new();
    private string _userInput = string.Empty;
    private AgentType _selectedAgent = AgentType.Sop;
    
    private async Task SendMessage()
    {
        // Add user message
        _messages.Add(new ChatMessage 
        { 
            Role = "user", 
            Content = _userInput 
        });
        
        // Get agent response
        string response = await OrchestratorService
            .ProcessQueryAsync(_userInput, _selectedAgent);
        
        // Add assistant message
        _messages.Add(new ChatMessage 
        { 
            Role = "assistant", 
            Content = response 
        });
        
        StateHasChanged();
    }
}
```

**Key Features:**
- Real-time message display
- Agent selection dropdown
- Message history
- Responsive design (Bootstrap)

---

## Data Flow

### Request Flow (Step-by-Step)

```
1. User types query in Chat.razor
   ↓
2. User clicks "Send" or presses Enter
   ↓
3. Chat.razor.SendMessage() invoked
   ↓
4. OrchestratorService.ProcessQueryAsync() called
   │  • AgentType.Sop or AgentType.Policy selected
   │  • Route to appropriate agent
   ↓
5. SopRagAgent.GetResponseAsync() or PolicyRagAgent.GetResponseAsync()
   │  • Check if agent exists
   │  • Create agent if needed
   │  • Get or create thread
   ↓
6. Create message in thread
   │  • await _client.CreateMessageAsync(thread.Id, MessageRole.User, query)
   ↓
7. Create run (agent execution)
   │  • await _client.CreateRunAsync(thread.Id, agent.Id)
   ↓
8. Stream response
   │  • Poll for completion
   │  • await foreach (message in GetMessagesAsync(thread.Id))
   ↓
9. Return response to OrchestratorService
   ↓
10. Return response to Chat.razor
   ↓
11. Chat.razor adds message to UI
    ↓
12. StateHasChanged() triggers re-render
```

### Authentication Flow (DefaultAzureCredential)

```
1. Application starts
   ↓
2. AgentsClient initialization
   │  • new AgentsClient(endpoint, new DefaultAzureCredential())
   ↓
3. DefaultAzureCredential tries (in order):
   │  a. Environment Variables (AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_CLIENT_SECRET)
   │  b. Managed Identity (if running in Azure)
   │  c. Visual Studio credentials
   │  d. VS Code credentials
   │  e. Azure CLI credentials (az login)
   │  f. Azure PowerShell
   ↓
4. First successful method provides token
   ↓
5. Token used for all Azure AI Foundry API calls
   ↓
6. Token automatically refreshed when expired
```

### Thread Conversation Flow

```
Initial Request:
User: "What is the SOP for handling customer complaints?"
   ↓
Agent creates thread: thread_abc123
   ↓
Agent responds: "The SOP involves 3 steps..."

Second Request (SAME SESSION):
User: "Can you elaborate on step 2?"
   ↓
Agent uses SAME thread: thread_abc123
   ↓
Agent has context from previous message
   ↓
Agent responds: "Step 2 involves..."

Third Request (SAME SESSION):
User: "What about step 3?"
   ↓
Agent uses SAME thread: thread_abc123
   ↓
Agent has full conversation history
   ↓
Agent responds with context-aware answer
```

**Note:** Thread survives for app lifetime. If app restarts, new thread created (conversation history lost unless persisted).

---

## Implementation Details

### Agent Initialization

#### Auto-Discovery Pattern

```csharp
private async Task EnsureAgentInitialized()
{
    if (_agent != null) return;
    
    // Try to use pre-created agent if ID provided
    if (!string.IsNullOrEmpty(_settings.SopAgentId))
    {
        try
        {
            _agent = await _client.GetAgentAsync(_settings.SopAgentId);
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not find agent {_settings.SopAgentId}: {ex.Message}");
        }
    }
    
    // Search for existing agent by name
    await foreach (var existingAgent in _client.GetAgentsAsync())
    {
        if (existingAgent.Name == "SOP Expert Agent")
        {
            _agent = existingAgent;
            return;
        }
    }
    
    // Create new agent if not found
    _agent = await CreateAgentAsync();
}
```

**Benefits:**
- Reuses existing agents (no duplicates)
- Supports pre-created agents (recommended)
- Auto-creates if needed (development convenience)

#### Agent Configuration

```csharp
private async Task<Agent> CreateAgentAsync()
{
    return await _client.CreateAgentAsync(
        model: _settings.ModelDeploymentName,
        name: "SOP Expert Agent",
        instructions: "You are a Standard Operating Procedures (SOP) expert...",
        toolResources: new ToolResources
        {
            FileSearch = new FileSearchToolResource()  // Enable RAG
        }
    );
}
```

**Available Tools:**
- **FileSearch:** Vector search over uploaded documents
- **AzureAISearch:** Integration with Azure AI Search service
- **CodeInterpreter:** Python code execution (data analysis)

### Thread Management

#### Single Thread Strategy

```csharp
private AgentThread? _thread;

public async Task<string> GetResponseAsync(string query)
{
    // Create thread once, reuse forever
    _thread ??= await _client.CreateThreadAsync();
    
    // All messages go to same thread
    await _client.CreateMessageAsync(_thread.Id, MessageRole.User, query);
    
    // Agent has full conversation history
    return await StreamAgentResponse(_thread.Id);
}
```

**Why Single Thread?**
- ✅ Conversation memory across requests
- ✅ Context awareness (user can ask follow-up questions)
- ✅ Reduced API calls (no thread recreation)
- ✅ Lower cost (fewer resources)

**Limitation:** 
- If app restarts, conversation history lost
- **Future Enhancement:** Persist thread ID to database for cross-session memory

#### Multi-Thread Strategy (Alternative)

```csharp
// Create new thread per request (stateless)
public async Task<string> GetResponseAsync(string query)
{
    var thread = await _client.CreateThreadAsync();
    await _client.CreateMessageAsync(thread.Id, MessageRole.User, query);
    var response = await StreamAgentResponse(thread.Id);
    
    // Optional: Delete thread to clean up
    await _client.DeleteThreadAsync(thread.Id);
    
    return response;
}
```

**When to Use:**
- No conversation memory needed
- Each query is independent
- Multi-user environment (thread per user)

### Response Streaming

```csharp
private async Task<string> StreamAgentResponse(string threadId)
{
    var run = await _client.CreateRunAsync(threadId, _agent.Id);
    
    // Poll until completion
    while (run.Status == RunStatus.InProgress || run.Status == RunStatus.Queued)
    {
        await Task.Delay(500);
        run = await _client.GetRunAsync(threadId, run.Id);
    }
    
    // Get agent's response (last message)
    var messages = await _client.GetMessagesAsync(threadId);
    var lastMessage = messages.Data
        .Where(m => m.Role == MessageRole.Assistant)
        .OrderByDescending(m => m.CreatedAt)
        .First();
    
    // Extract text content
    return lastMessage.ContentItems
        .OfType<MessageTextContent>()
        .FirstOrDefault()?.Text ?? "No response";
}
```

**Optimization Opportunities:**
- Use streaming API (not yet available in SDK)
- Implement Server-Sent Events for real-time updates
- Add progress indicators during long operations

### Configuration Precedence

```
1. Environment Variables (highest priority)
   ↓
2. appsettings.{Environment}.json
   ↓
3. appsettings.json
   ↓
4. Default values (lowest priority)
```

**Example:**
```bash
# Environment variable overrides all config files
export AZURE_AI_MODEL_DEPLOYMENT_NAME="gpt-4o"

# Will use gpt-4o even if appsettings.json says gpt-4
```

---

## Migration from Semantic Kernel

### Why Migrate?

**Previous:** Semantic Kernel (SK) with Azure OpenAI  
**Current:** Azure.AI.Agents.Persistent with Azure AI Foundry

**Reasons:**
1. **Native Agent Service Support:** Azure AI Foundry provides managed agent lifecycle
2. **Built-in RAG Tools:** File Search, Azure AI Search integration without custom code
3. **Persistent Threads:** Conversation memory managed by service
4. **Simplified Architecture:** No need for custom prompt engineering
5. **Better Scaling:** Managed service handles concurrency
6. **Future-Proof:** Microsoft's strategic direction for AI agents

### Key Differences

| Aspect | Semantic Kernel | Azure AI Agents |
|--------|----------------|----------------|
| **Agent Definition** | Code-based (C# classes) | Service-managed (portal + API) |
| **Conversation Memory** | Manual (custom storage) | Built-in (thread persistence) |
| **RAG** | Custom vector DB integration | Built-in File Search tool |
| **Prompts** | Hardcoded in code | Configurable in portal |
| **Plugins** | Custom C# functions | Standard tools (FileSearch, CodeInterpreter) |
| **Deployment** | App deployment only | App + agent configuration |
| **Scaling** | App scaling | Service scaling |

### Migration Steps

#### 1. Replace SDK

```bash
# OLD
dotnet add package Microsoft.SemanticKernel

# NEW
dotnet add package Azure.AI.Agents.Persistent --version 1.1.0
```

#### 2. Replace Kernel with AgentsClient

**Before (Semantic Kernel):**
```csharp
var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey)
    .Build();

var prompt = "You are an SOP expert...";
var result = await kernel.InvokePromptAsync(prompt + userQuery);
```

**After (Azure AI Agents):**
```csharp
var client = new AgentsClient(new Uri(endpoint), new DefaultAzureCredential());

var agent = await client.CreateAgentAsync(
    model: deploymentName,
    name: "SOP Expert",
    instructions: "You are an SOP expert..."
);

var thread = await client.CreateThreadAsync();
await client.CreateMessageAsync(thread.Id, MessageRole.User, userQuery);
var run = await client.CreateRunAsync(thread.Id, agent.Id);

// Wait for completion and get response
```

#### 3. Replace Custom RAG with File Search

**Before (Semantic Kernel + Custom Vector DB):**
```csharp
// Custom vector search
var embedding = await kernel.GetService<IEmbeddingGenerator>()
    .GenerateEmbeddingAsync(query);

var results = await vectorDb.SearchAsync(embedding, topK: 5);

var context = string.Join("\n", results.Select(r => r.Text));
var prompt = $"Context:\n{context}\n\nQuestion: {query}";

var response = await kernel.InvokePromptAsync(prompt);
```

**After (Azure AI Agents with File Search):**
```csharp
// Upload documents once
var fileStream = File.OpenRead("sop-documents.pdf");
var file = await client.UploadFileAsync(fileStream, "sop-documents.pdf");

// Create agent with File Search enabled
var agent = await client.CreateAgentAsync(
    model: "gpt-4",
    name: "SOP Expert",
    instructions: "You are an SOP expert...",
    toolResources: new ToolResources
    {
        FileSearch = new FileSearchToolResource
        {
            VectorStores = new List<VectorStoreRef>
            {
                new VectorStoreRef { FileId = file.Id }
            }
        }
    }
);

// RAG happens automatically!
// Agent searches documents when needed
```

#### 4. Replace Custom Memory with Threads

**Before (Semantic Kernel + Manual History):**
```csharp
private List<ChatMessage> _conversationHistory = new();

public async Task<string> ChatAsync(string userMessage)
{
    _conversationHistory.Add(new ChatMessage(ChatRole.User, userMessage));
    
    var chatHistory = new ChatHistory();
    foreach (var msg in _conversationHistory)
        chatHistory.Add(msg);
    
    var response = await kernel.GetService<IChatCompletion>()
        .GetChatMessageContentAsync(chatHistory);
    
    _conversationHistory.Add(new ChatMessage(ChatRole.Assistant, response.Content));
    
    return response.Content;
}
```

**After (Azure AI Agents with Threads):**
```csharp
private AgentThread _thread;

public async Task<string> ChatAsync(string userMessage)
{
    // Thread automatically stores all messages
    await client.CreateMessageAsync(_thread.Id, MessageRole.User, userMessage);
    
    var run = await client.CreateRunAsync(_thread.Id, _agent.Id);
    
    // Agent has access to full conversation history
    return await GetRunResponse(run);
}
```

### Migration Checklist

- [x] Replace `Microsoft.SemanticKernel` with `Azure.AI.Agents.Persistent`
- [x] Replace `Kernel` with `AgentsClient`
- [x] Replace prompt engineering with agent instructions
- [x] Replace custom conversation history with threads
- [x] Replace custom RAG with File Search tool
- [x] Update authentication to use `DefaultAzureCredential`
- [x] Create agents in Azure AI Foundry portal (optional but recommended)
- [x] Update deployment scripts (no changes needed for Container Apps)
- [x] Test conversation memory (thread persistence)
- [x] Test RAG functionality (file search)
- [ ] **Optional:** Migrate existing conversation history to threads
- [ ] **Optional:** Implement thread persistence across app restarts

### Performance Comparison

| Metric | Semantic Kernel | Azure AI Agents | Notes |
|--------|----------------|----------------|-------|
| **First Query** | ~2-3s | ~5-10s | Agent initialization overhead |
| **Subsequent Queries** | ~1-2s | ~1-2s | Similar once initialized |
| **Memory Usage** | Lower | Higher | Agent service maintains state |
| **Code Complexity** | Higher | Lower | Less custom code needed |
| **RAG Setup** | Complex | Simple | Built-in vs custom |

---

## Technical Decisions

### Why Azure AI Agent Service?

**Alternatives Considered:**
1. **LangChain (Python):** Mature but Python-based, not .NET native
2. **Semantic Kernel (C#):** Great but requires more custom code
3. **AutoGen (Multi-Agent):** Overkill for our use case
4. **Direct OpenAI API:** No RAG, no managed agents
5. **Azure AI Agent Service:** ✅ Best fit for .NET + Azure + RAG

**Decision Factors:**
- Native .NET SDK
- Managed agent lifecycle
- Built-in RAG tools
- Azure integration
- Microsoft support

### Why Blazor Server?

**Alternatives Considered:**
1. **ASP.NET Core MVC:** Traditional but less interactive
2. **Blazor WebAssembly:** Client-side but larger download
3. **React/Next.js:** Popular but JavaScript ecosystem
4. **Blazor Server:** ✅ Real-time, small payload, C# full-stack

**Decision Factors:**
- Real-time updates via SignalR
- Small initial load (no WASM download)
- Full .NET ecosystem
- Simpler deployment (single app)

### Why Singleton Services?

**Alternatives Considered:**
1. **Transient:** New instance per request (stateless)
2. **Scoped:** Instance per HTTP request (multi-thread issues)
3. **Singleton:** ✅ Single instance (conversation memory)

**Decision Factors:**
- Agents maintain conversation state
- Thread reuse optimization
- Connection pooling
- Cost optimization

**Thread Safety:** Single thread per agent (no concurrency issues)

### Why Single Thread Per Agent?

**Alternatives Considered:**
1. **Thread per request:** Stateless, no memory
2. **Thread per user:** Complex user management
3. **Thread per agent:** ✅ Simple, conversation memory

**Decision Factors:**
- Simpler architecture
- Conversation continuity
- Lower API calls
- Development convenience

**Limitation:** Not multi-user ready (future: thread per user session)

---

## Performance Characteristics

### Latency Metrics

| Operation | Typical Time | Notes |
|-----------|-------------|-------|
| **Agent Initialization** | 3-5s | First time only |
| **Thread Creation** | 500ms-1s | First time only |
| **Message Creation** | 100-200ms | Per query |
| **Run Execution** | 1-3s | LLM inference time |
| **Response Streaming** | 50-100ms/token | GPT-4 speed |
| **Total First Query** | 5-10s | Includes initialization |
| **Total Subsequent Query** | 2-4s | Already initialized |

### Resource Usage

| Resource | Usage | Notes |
|----------|-------|-------|
| **Memory** | 200-300MB | Base app + agents |
| **CPU** | <10% idle | Mostly I/O bound |
| **Network** | 5-20KB/query | Depends on response length |
| **Azure AI Tokens** | ~500-2000 | Depends on conversation |

### Scaling Characteristics

#### Horizontal Scaling (Multiple Instances)

```yaml
# Container Apps auto-scaling
minReplicas: 1
maxReplicas: 10
rules:
  - http:
      metadata:
        concurrentRequests: 50
```

**Considerations:**
- Each instance maintains separate conversation state
- Load balancer may route user to different instances
- **Solution:** Implement thread persistence + session affinity

#### Vertical Scaling (Instance Size)

```yaml
resources:
  cpu: 1.0
  memory: 2Gi
```

**Recommendations:**
- **Development:** 0.5 CPU, 1Gi memory
- **Production:** 1.0 CPU, 2Gi memory
- **High Load:** 2.0 CPU, 4Gi memory

### Optimization Strategies

#### 1. Pre-Create Agents

**Before:** Auto-create on every restart (5-10s delay)  
**After:** Use pre-created agent IDs (instant)

```json
{
  "AzureAI": {
    "SopAgentId": "asst_xxxxxxxxxxxx",
    "PolicyAgentId": "asst_yyyyyyyyyyyy"
  }
}
```

**Savings:** 5-10s per agent per restart

#### 2. Thread Persistence

**Before:** New thread on every restart (lose history)  
**After:** Persist thread ID (maintain history)

```csharp
// Store thread ID
await db.SaveThreadIdAsync(userId, thread.Id);

// Restore on next session
var threadId = await db.GetThreadIdAsync(userId);
_thread = await client.GetThreadAsync(threadId);
```

**Benefits:** Cross-session memory, better UX

#### 3. Response Caching

**Strategy:** Cache common queries

```csharp
private Dictionary<string, string> _responseCache = new();

public async Task<string> GetResponseAsync(string query)
{
    if (_responseCache.TryGetValue(query, out var cached))
        return cached;
    
    var response = await GetAgentResponse(query);
    _responseCache[query] = response;
    
    return response;
}
```

**Caution:** Only cache truly static responses

#### 4. Connection Pooling

Already handled by `AgentsClient` - reuses HTTP connections.

---

## Security Considerations

### Authentication

#### Recommended: Entra ID (Keyless)

```csharp
var credential = new DefaultAzureCredential();
var client = new AgentsClient(new Uri(endpoint), credential);
```

**Benefits:**
- No secrets in code/config
- Automatic token rotation
- Azure RBAC integration
- Audit logging

#### Not Recommended: API Keys

```csharp
var credential = new AzureKeyCredential(apiKey);
var client = new AgentsClient(new Uri(endpoint), credential);
```

**Risks:**
- Key leakage in logs/config
- Manual rotation
- No fine-grained permissions

### Authorization

#### Azure RBAC Roles

| Role | Permissions | Use Case |
|------|------------|----------|
| **Azure AI Developer** | Read/write agents, threads | Production app |
| **Azure AI Contributor** | Full access | Admin/DevOps |
| **Azure AI Reader** | Read-only | Monitoring |

#### Assign Roles

```bash
az role assignment create \
  --assignee <principal-id> \
  --role "Azure AI Developer" \
  --scope <ai-project-resource-id>
```

### Data Protection

#### Conversation Data

**Storage:** Azure AI Agent Service (Microsoft-managed)  
**Encryption:** At-rest and in-transit  
**Retention:** Threads persist until deleted  
**Compliance:** SOC 2, ISO 27001, HIPAA eligible

#### Sensitive Data Handling

**Best Practices:**
- Don't send PII unless required
- Use data masking for logs
- Implement data retention policies
- Delete threads when conversation complete

```csharp
// Delete thread when done
await client.DeleteThreadAsync(thread.Id);
```

### Network Security

#### Recommended Architecture

```
Internet → Azure Front Door → Container Apps (Private VNet)
                                      ↓
                            Azure AI Foundry (Private Endpoint)
```

**Benefits:**
- No public internet exposure
- DDoS protection
- Web Application Firewall (WAF)

#### Minimal Architecture (Current)

```
Internet → Container Apps (Public) → Azure AI Foundry (Public)
```

**Considerations:**
- HTTPS enforced
- Azure network security
- Managed identity authentication

### Compliance

#### Data Residency

Azure AI Foundry respects region selection:
```bash
# Example: Keep data in EU
LOCATION="westeurope"
```

#### Logging

**What's Logged:**
- API requests/responses (no message content by default)
- Authentication attempts
- Errors and exceptions

**What's NOT Logged:**
- Message content (unless explicitly enabled)
- API keys (automatically redacted)

---

## Future Enhancements

### Short-Term (Next Sprint)

1. **Thread Persistence:** Store thread IDs in database for cross-session memory
2. **Multi-User Support:** Thread per user instead of per agent
3. **File Upload UI:** Allow users to upload documents for RAG
4. **Streaming Responses:** Real-time token-by-token display
5. **Error Handling:** Better error messages and retry logic

### Medium-Term (Next Quarter)

1. **Azure AI Search Integration:** Enterprise knowledge base
2. **Role-Based Access:** Different agent access per user role
3. **Conversation Export:** Download chat history
4. **Agent Customization:** UI for creating/modifying agents
5. **Performance Monitoring:** Application Insights integration

### Long-Term (Roadmap)

1. **Multi-Agent Orchestration:** Automatic agent selection based on query
2. **Proactive Agents:** Push notifications for policy updates
3. **Approval Workflows:** Human-in-the-loop for critical decisions
4. **Analytics Dashboard:** Usage metrics, popular queries
5. **Mobile App:** Native iOS/Android apps

---

## References

### Documentation
- [Azure AI Agent Service](https://learn.microsoft.com/azure/ai-services/agents/)
- [Azure.AI.Agents.Persistent SDK](https://www.nuget.org/packages/Azure.AI.Agents.Persistent)
- [Blazor Server](https://learn.microsoft.com/aspnet/core/blazor/)
- [DefaultAzureCredential](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential)
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps/)

### Code Samples
- [Official Azure AI Samples](https://github.com/Azure-Samples/azure-ai-samples)
- [Blazor Chat Sample](https://github.com/dotnet/blazor-samples)

### Related Projects
- [Semantic Kernel](https://github.com/microsoft/semantic-kernel)
- [LangChain](https://github.com/hwchase17/langchain)
- [AutoGen](https://github.com/microsoft/autogen)

---

**Need help?** Check the [Setup Guide](GUIDE.md) or open an issue on GitHub!
