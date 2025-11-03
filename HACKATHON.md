# Hackathon Guide: Building a Dual-Agent RAG System

Welcome to the SOP-PP Hackathon! This guide will help you implement a complete dual-agent RAG (Retrieval-Augmented Generation) system using Azure AI Agent Service and Azure AI Foundry.

## 🎯 Overview

This repository contains a **stubbed-out** version of a .NET Blazor web application that will eventually feature:
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
2. Creating AI agents with RAG capabilities
3. Implementing agent communication patterns
4. Working with Azure AI Search
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

**Goal**: Get the agents calling Azure OpenAI directly (no RAG yet)

**Files to modify**:
- `RagAgentApp/Agents/SopRagAgent.cs`
- `RagAgentApp/Agents/PolicyRagAgent.cs`
- `RagAgentApp/Program.cs`

**Steps**:
1. Set up an Azure AI Foundry project
2. Deploy a GPT model (e.g., gpt-4, gpt-35-turbo)
3. Get your project endpoint and credentials
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
6. Implement `ProcessQueryAsync` in both agent classes to:
   - Create a thread
   - Add the user message
   - Create and run the agent
   - Poll for completion
   - Return the response

**Hints**:
- Review the TODO comments in each file
- Use `PersistentAgentsClient` from the Azure.AI.Agents.Persistent package
- Look at the commented-out code for guidance

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

**Resources**:
- Azure AI Search documentation
- File search tool examples in Azure AI docs

### Level 3: Advanced Orchestration (Advanced)

**Goal**: Implement intelligent orchestration using function calling

**Files to modify**:
- `RagAgentApp/Services/OrchestratorService.cs`

**Steps**:
1. Create an orchestrator agent with function calling capabilities
2. Define function tools for `query_sop_agent` and `query_policy_agent`
3. Implement function calling flow:
   - Detect when agent requires action
   - Execute the requested function
   - Submit tool outputs back to the agent
   - Continue until completion
4. Let the orchestrator intelligently decide when to call each agent

**Benefits**:
- More flexible routing
- Agent can decide which specialist to consult
- Can handle complex multi-turn conversations
- Better context management

## 🛠️ Key Azure AI Foundry Concepts

### Agents
- Stateful AI entities with instructions and tools
- Persist across conversations
- Can be enhanced with file search, code interpreter, and function calling

### Threads
- Conversation sessions
- Store message history
- Enable contextual conversations

### Runs
- Execution of an agent on a thread
- Asynchronous by nature
- Can require actions (e.g., function calling)

### Tools
- File Search: RAG over documents
- Code Interpreter: Execute Python code
- Function Calling: Execute custom functions

## 📖 Implementation Guide

### Step-by-Step: Basic Agent Implementation

#### 1. Set Up Azure AI Foundry

1. Go to [ai.azure.com](https://ai.azure.com)
2. Create a new project or use existing
3. Deploy a model:
   - Navigate to "Deployments"
   - Click "Deploy model"
   - Choose GPT-4 or GPT-3.5-turbo
   - Note the deployment name

#### 2. Get Connection Information

From your project overview page:
- Copy the **Project Endpoint** (format: `https://xxx.services.ai.azure.com/api/projects/xxx`)
- Note your **Model Deployment Name**

#### 3. Configure Authentication

**Option A: Azure CLI (Recommended for local dev)**
```bash
az login
```

**Option B: API Key**
- Go to project settings → Keys and Endpoint
- Copy an API key (don't commit this!)

#### 4. Update Configuration

Create `RagAgentApp/appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AzureAI": {
    "ProjectEndpoint": "https://your-foundry.services.ai.azure.com/api/projects/YourProject",
    "ModelDeploymentName": "gpt-4"
  }
}
```

#### 5. Implement SopRagAgent

Replace the stubbed `ProcessQueryAsync` method:

```csharp
public async Task<string> ProcessQueryAsync(string query, CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("Processing SOP query: {Query}", query);
        
        // Get or create agent
        var agentId = GetOrResolveAgentId();

        // Create thread
        var threadResponse = await _agentsClient.Threads.CreateThreadAsync(cancellationToken);
        var threadId = threadResponse.Value.Id;

        // Add user message
        await _agentsClient.Messages.CreateMessageAsync(
            threadId,
            MessageRole.User,
            query,
            cancellationToken: cancellationToken
        );

        // Create and run agent
        var runResponse = await _agentsClient.Runs.CreateRunAsync(
            threadId,
            agentId,
            cancellationToken: cancellationToken
        );
        var run = runResponse.Value;

        // Poll until complete
        while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
        {
            await Task.Delay(1000, cancellationToken);
            var statusResponse = await _agentsClient.Runs.GetRunAsync(threadId, run.Id, cancellationToken);
            run = statusResponse.Value;
        }

        if (run.Status == RunStatus.Failed)
        {
            return $"Agent run failed: {run.LastError?.Message}";
        }

        // Get messages
        var messages = await _agentsClient.Messages.GetMessagesAsync(threadId, cancellationToken);
        var lastMessage = messages.Value.Data
            .Where(m => m.Role == MessageRole.Assistant)
            .FirstOrDefault();

        if (lastMessage?.Content?.FirstOrDefault() is MessageTextContent textContent)
        {
            return textContent.Text.Value;
        }

        return "No response generated";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing query");
        return $"Error: {ex.Message}";
    }
}
```

#### 6. Do the Same for PolicyRagAgent

Follow the same pattern for the Policy agent.

#### 7. Update Program.cs

Uncomment the Azure AI configuration sections and update service registrations.

#### 8. Test Your Implementation

```bash
cd RagAgentApp
dotnet run
```

Navigate to `/chat` and ask questions!

## 🐛 Troubleshooting

### "Cannot find Azure AI endpoint"
- Check `appsettings.Development.json` configuration
- Verify environment variables if using containers

### "Unauthorized" or "403 Forbidden"
- Ensure you're logged in with `az login`
- Check API key if using key-based auth
- Verify your account has access to the project

### "Model not found"
- Verify model deployment name matches configuration
- Check that model is deployed in your project

### Agents create duplicates on every run
- Implement agent name checking in `GetOrResolveAgentId`
- Cache agent IDs
- Or provide explicit agent IDs in configuration

## 📦 Testing

### Manual Testing Checklist

- [ ] Application builds without errors
- [ ] Application runs and loads the UI
- [ ] Can navigate to /chat page
- [ ] Can type and send a message
- [ ] Both agent panels show loading indicators
- [ ] Both agents return responses
- [ ] Responses are relevant to the query
- [ ] Multiple queries work in succession
- [ ] Error handling works (test with invalid config)

### Sample Queries to Test

**For Both Agents:**
```
What are the safety procedures for equipment operation?
What is the policy on remote work?
How do I request vacation time?
What are the security requirements for data handling?
```

## 🎨 Optional Enhancements

Once you have the basics working, consider:

1. **Streaming Responses**: Show agent responses as they're generated
2. **Conversation History**: Show previous Q&A pairs
3. **Agent Status Indicators**: Show what each agent is doing
4. **Error Recovery**: Implement retry logic
5. **Caching**: Cache frequent queries
6. **Metrics**: Add telemetry and monitoring
7. **Custom Tools**: Add function calling for external APIs

## 📚 Additional Resources

- [Azure AI Foundry Documentation](https://learn.microsoft.com/azure/ai-studio/)
- [Azure AI Agent Service SDK](https://learn.microsoft.com/dotnet/api/azure.ai.agents)
- [Blazor Documentation](https://learn.microsoft.com/aspnet/core/blazor/)
- [Azure AI Search](https://learn.microsoft.com/azure/search/)

## 🤝 Getting Help

During the hackathon:
1. Check the inline TODO comments in the code
2. Review this guide's troubleshooting section
3. Ask mentors for help
4. Check Azure AI documentation
5. Use the sample code patterns provided

## 🏆 Success Criteria

**Minimum Viable Product (MVP)**:
- ✅ Application runs without errors
- ✅ Both agents respond to queries
- ✅ UI displays responses correctly

**Good Implementation**:
- ✅ All MVP criteria
- ✅ Agents use proper Azure AI Agent Service patterns
- ✅ Error handling is robust
- ✅ Code follows best practices

**Excellent Implementation**:
- ✅ All Good criteria
- ✅ RAG with file search implemented
- ✅ Advanced orchestration with function calling
- ✅ Enhanced UI/UX features
- ✅ Proper logging and monitoring

## 📝 Submission

When you're done:
1. Ensure all code compiles and runs
2. Test with multiple queries
3. Document any special setup steps
4. Prepare a demo showing both agents working

Good luck and have fun! 🚀
