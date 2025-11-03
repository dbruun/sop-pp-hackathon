# System Architecture

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        User Browser                          │
└────────────────────────────┬────────────────────────────────┘
                             │ HTTPS
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                    Blazor Server App                         │
│  ┌──────────────────────────────────────────────────────┐  │
│  │                    UI Layer                           │  │
│  │  ┌────────────┐  ┌──────────────┐  ┌──────────────┐ │  │
│  │  │ Home.razor │  │  Chat.razor  │  │ NavMenu.razor│ │  │
│  │  └────────────┘  └──────────────┘  └──────────────┘ │  │
│  └────────────────────────┬─────────────────────────────┘  │
│                           │                                 │
│  ┌────────────────────────▼─────────────────────────────┐  │
│  │                 Service Layer                         │  │
│  │  ┌───────────────────────────────────────────────┐   │  │
│  │  │      OrchestratorService                      │   │  │
│  │  │  - Routes queries to agents                   │   │  │
│  │  │  - Executes in parallel                       │   │  │
│  │  │  - Aggregates responses                       │   │  │
│  │  └──────────────────┬────────────────────────────┘   │  │
│  └─────────────────────┼──────────────────────────────┬─┘  │
│                        │                              │     │
│         ┌──────────────┴────────────┐                │     │
│         ▼                           ▼                 │     │
│  ┌─────────────┐            ┌─────────────┐          │     │
│  │  SOP Agent  │            │Policy Agent │          │     │
│  │ (IAgentSvc) │            │ (IAgentSvc) │          │     │
│  └──────┬──────┘            └──────┬──────┘          │     │
│         │                          │                  │     │
│         └────────────┬─────────────┘                  │     │
│                      ▼                                │     │
│      ┌──────────────────────────────┐                │     │
│      │  Azure.AI.Agents.Persistent  │◄───────────────┘     │
│      │  PersistentAgentsClient      │                      │
│      │  - Agent Lifecycle Mgmt      │                      │
│      │  - Thread Management         │                      │
│      │  - Run Orchestration         │                      │
│      └──────────────┬───────────────┘                      │
└─────────────────────┼──────────────────────────────────────┘
                       │
                       ▼
          ┌────────────────────────┐
          │  Azure AI Foundry      │
          │  - Agent Service       │
          │  - Azure OpenAI        │
          │  - GPT-4 / GPT-3.5     │
          └────────────────────────┘
```

## Component Breakdown

### UI Layer (Blazor Components)

#### Home.razor
- **Purpose**: Landing page with feature overview
- **Functionality**: 
  - Displays system description
  - Shows agent capabilities
  - Provides navigation to chat interface

#### Chat.razor
- **Purpose**: Main chat interface
- **Features**:
  - User input textbox with Enter key support
  - Two response panels (SOP and Policy)
  - Message history with timestamps
  - Loading indicators
  - Responsive layout
- **State Management**:
  - `userInput`: Current input text
  - `isProcessing`: Loading state
  - `sopMessages`: SOP agent conversation history
  - `policyMessages`: Policy agent conversation history

#### NavMenu.razor
- **Purpose**: Navigation sidebar
- **Links**: Home, Chat

### Service Layer

#### OrchestratorService
- **Responsibility**: Route and coordinate agent requests
- **Key Method**: `RouteQueryToAgentsAsync`
  - Takes user query
  - Sends to all registered agents in parallel
  - Returns dictionary of agent responses
- **Benefits**:
  - Parallel execution for performance
  - Extensible to add more agents
  - Centralized routing logic

### Agent Layer

#### IAgentService (Interface)
```csharp
public interface IAgentService
{
    string AgentName { get; }
    Task<string> ProcessQueryAsync(string query, CancellationToken cancellationToken);
}
```

#### SopRagAgent
- **Specialty**: Standard Operating Procedures
- **System Prompt**: Focused on SOPs, work instructions, processes
- **Response Style**: Clear, structured, procedural
- **Agent Management**: 
  - Checks for existing "SOP Expert Agent" in Azure AI Foundry
  - Reuses existing agent or creates new one
  - Manages conversation threads per query

#### PolicyRagAgent
- **Specialty**: Policies and Compliance
- **System Prompt**: Focused on policies, regulations, governance
- **Response Style**: Authoritative, citation-ready
- **Agent Management**: 
  - Checks for existing "Policy Expert Agent" in Azure AI Foundry
  - Reuses existing agent or creates new one
  - Manages conversation threads per query

### Infrastructure Layer

#### Azure.AI.Agents.Persistent SDK (v1.1.0)
- **Purpose**: Azure AI Agent Service client with persistent agent management
- **Key Component**: `PersistentAgentsClient`
- **Services**:
  - Agent creation and management in Azure AI Foundry
  - Agent listing and reuse across restarts
  - Thread-based conversations with persistent state
  - Run orchestration and polling
  - Message management
  - Function calling and tool integration
- **Configuration**: Project endpoint + authentication
- **Authentication**: DefaultAzureCredential (Entra ID) - recommended, API key fallback

#### Azure AI Foundry
- **Agent Service**: Manages agent lifecycle in the cloud
- **Models Supported**: GPT-4, GPT-3.5-Turbo, GPT-4o, GPT-4o-mini
- **Features**:
  - Persistent agent storage with unique IDs
  - Thread-based conversations with full history
  - Built-in RAG capabilities via Azure AI Search
  - Function calling support for tool integration
  - File search and code interpreter tools
  - Multi-turn conversation management
- **Authentication**: Entra ID (DefaultAzureCredential), API key fallback

## Data Flow

### User Query Flow

```
1. User types question in Chat.razor
   ↓
2. Click "Send" or press Enter
   ↓
3. Chat.razor calls OrchestratorService.RouteQueryToAgentsAsync()
   ↓
4. OrchestratorService sends query to all IAgentService implementations in parallel
   ↓
5a. SopRagAgent.ProcessQueryAsync()        5b. PolicyRagAgent.ProcessQueryAsync()
    - Gets or creates agent                    - Gets or creates agent
      (checks Azure AI Foundry first)            (checks Azure AI Foundry first)
    - Creates conversation thread              - Creates conversation thread
    - Adds user message to thread              - Adds user message to thread
    - Creates run with agent                   - Creates run with agent
    - Polls for run completion                 - Polls for run completion
    ↓                                          ↓
6. Azure AI Agent Service processes requests in Azure AI Foundry
   ↓
7. Azure OpenAI (via Agent Service) generates responses
   ↓
8. Agents poll and retrieve completed responses from threads
   ↓
9. OrchestratorService aggregates responses
   ↓
10. Chat.razor updates UI with both responses
    ↓
11. User sees SOP response (left) and Policy response (right)
```

## Configuration Flow

```
Startup
  ↓
Program.cs reads configuration
  ↓
  ├─ appsettings.json (base settings)
  ├─ appsettings.Development.json (local override)
  └─ Environment Variables (container/production)
  ↓
AzureAISettings populated
  ↓
PersistentAgentsClient configured with DefaultAzureCredential
  ↓
Agent services registered as singletons
  ↓
OrchestratorService configured with function calling
  ↓
Services registered in DI container
  ↓
Application ready
```

## Deployment Architecture

### Local Development
```
Developer Machine
  ├─ .NET SDK
  ├─ appsettings.Development.json
  └─ dotnet run
     ↓
  http://localhost:5000
```

### Docker Local
```
Developer Machine
  ├─ Docker Engine
  ├─ .env file
  └─ docker-compose up
     ↓
  http://localhost:8080
```

### Azure Container Apps
```
Azure Cloud
  ├─ Container Registry (ACR)
  │   └─ ragagentapp:latest
  ├─ Container Apps Environment
  │   └─ ragagentapp
  │       ├─ Container from ACR
  │       ├─ Environment Variables
  │       └─ Managed Identity
  ├─ Azure OpenAI Service
  └─ Application Insights (optional)
     ↓
  https://ragagentapp.{region}.azurecontainerapps.io
```

## Security Architecture

### Authentication Flow (Managed Identity)
```
Container App
  ↓ (Managed Identity enabled)
Azure AD
  ↓ (Token issued)
Container App
  ↓ (Token used)
Azure OpenAI
  ↓ (Access granted)
  Response
```

### Secret Management
```
Local:
  .env file → Environment Variables

Azure:
  Key Vault → Container App Secrets → Environment Variables
  
  OR
  
  Managed Identity → Azure OpenAI (keyless)
```

## Scalability Architecture

### Horizontal Scaling
```
Load Balancer
  ↓
  ├─ Container Instance 1 ──┐
  ├─ Container Instance 2 ──┼─→ Azure OpenAI
  └─ Container Instance N ──┘
```

### Auto-Scaling Triggers
- HTTP request count
- CPU utilization
- Memory utilization
- Custom metrics

### Scale to Zero
```
No Traffic → Scale to 0 instances → Cost = $0
Traffic arrives → Scale to min replicas → Process requests
High traffic → Scale to max replicas → Handle load
Traffic decreases → Scale down → Optimize cost
```

## Error Handling Architecture

```
User Action
  ↓
try {
  Process Request
} catch (Exception ex) {
  ↓
  Log Error
  ↓
  Return User-Friendly Message
  ↓
  Display in UI
}
```

### Error Types Handled
1. **Network Errors**: Azure OpenAI unavailable
2. **Authentication Errors**: Invalid API key
3. **Rate Limiting**: Too many requests
4. **Timeout**: Request takes too long
5. **Invalid Configuration**: Missing settings

## Monitoring Architecture

```
Application
  ↓ (Telemetry)
Application Insights
  ↓ (Metrics, Logs, Traces)
Azure Monitor
  ↓ (Alerts, Dashboards)
Operations Team
```

### Metrics Tracked
- Request count
- Response time
- Error rate
- Token usage
- Active users
- Container resource utilization

## Extension Points

### Adding New Agents
```
1. Create new class implementing IAgentService
2. Define unique system prompt
3. Register in Program.cs:
   builder.Services.AddScoped<IAgentService, NewAgent>();
4. Update UI to display new agent responses
```

### Adding RAG Capabilities
```
1. Add Azure AI Search client
2. Create vector embeddings
3. Implement semantic search
4. Augment agent prompts with retrieved documents
```

### Adding Authentication
```
1. Add Microsoft.Identity.Web
2. Configure Azure AD
3. Add [Authorize] attributes
4. Update UI for login/logout
```

## Performance Considerations

### Parallel Execution
- Both agents process simultaneously
- Total time = max(agent1_time, agent2_time)
- Not agent1_time + agent2_time

### Caching Opportunities
- Semantic Kernel response caching
- Common query caching
- Configuration caching

### Optimization Strategies
- Async/await throughout
- Minimal state in Blazor components
- Efficient JSON serialization
- Connection pooling
- HTTP client reuse

## Technology Choices Rationale

### Why .NET 9.0?
- Latest features and performance
- Native JSON support
- HTTP/3 support
- Minimal APIs
- Top-tier Azure integration

### Why Blazor Server?
- Real-time updates via SignalR
- Full-stack C# development
- Strong typing
- No separate API layer needed
- Excellent tooling

### Why Azure AI Agent Service with PersistentAgentsClient?
- Official Microsoft agentic framework with persistent state
- Native Azure AI Foundry integration for agent storage
- True agent lifecycle management (create, list, update, delete)
- Thread-based conversation management with full history
- Built-in RAG support via Azure AI Search integration
- Function calling for tool integration and orchestration
- Agent reuse across application restarts (no duplicate creation)
- Scalable cloud-based execution with enterprise support
- Secure authentication via Entra ID (DefaultAzureCredential)

### Why Container Apps?
- Serverless containers
- Auto-scaling to zero
- Built-in load balancing
- Managed identity support
- Cost-effective

## Future Architecture Enhancements

### Multi-Tenant Support
```
User Request
  ↓
Tenant Identification
  ↓
Tenant-Specific Configuration
  ↓
Tenant-Specific Agents
  ↓
Tenant-Specific Data
```

### Event-Driven Architecture
```
User Query
  ↓
Message Queue (Azure Service Bus)
  ↓
Agent Processors
  ↓
Response Queue
  ↓
UI Updates
```

### Microservices Architecture
```
API Gateway
  ↓
  ├─ SOP Agent Service
  ├─ Policy Agent Service
  ├─ Orchestrator Service
  └─ UI Service
```

This architecture provides a solid foundation for a production-ready, scalable, and maintainable RAG agent system.
