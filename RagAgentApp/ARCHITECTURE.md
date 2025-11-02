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
│         ┌────────────────────────┐                   │     │
│         │  Semantic Kernel       │◄──────────────────┘     │
│         │  - Chat Completion     │                         │
│         │  - Prompt Management   │                         │
│         └────────────┬───────────┘                         │
└──────────────────────┼─────────────────────────────────────┘
                       │
                       ▼
          ┌────────────────────────┐
          │   Azure OpenAI API     │
          │   - GPT-4 / GPT-3.5    │
          │   - Chat Completions   │
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

#### PolicyRagAgent
- **Specialty**: Policies and Compliance
- **System Prompt**: Focused on policies, regulations, governance
- **Response Style**: Authoritative, citation-ready

### Infrastructure Layer

#### Semantic Kernel
- **Purpose**: AI orchestration framework
- **Services**:
  - `IChatCompletionService`: Chat with LLMs
  - `Kernel`: Dependency injection container
- **Configuration**: Injected with Azure OpenAI settings

#### Azure OpenAI
- **Models Supported**: GPT-4, GPT-3.5-Turbo, GPT-4o
- **API**: Chat Completions endpoint
- **Authentication**: API key or Managed Identity

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
    - Creates ChatHistory                      - Creates ChatHistory
    - Adds system prompt                       - Adds system prompt
    - Adds user message                        - Adds user message
    - Calls Semantic Kernel                    - Calls Semantic Kernel
    ↓                                          ↓
6. Semantic Kernel forwards to Azure OpenAI
   ↓
7. Azure OpenAI generates responses
   ↓
8. Responses return to agents
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
  ├─ appsettings.json (development)
  ├─ appsettings.Development.json (override)
  └─ Environment Variables (production)
  ↓
AzureAISettings populated
  ↓
Semantic Kernel configured
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

### Why Semantic Kernel?
- Official Microsoft AI framework
- Abstraction over multiple AI services
- Plugin architecture
- Memory management
- Active development

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
