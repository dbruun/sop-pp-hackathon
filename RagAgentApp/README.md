# RAG Agent System

A dual-agent AI system powered by Azure AI Agent Service and Azure AI Foundry for Standard Operating Procedures (SOP) and Policy information retrieval.

## 🚀 Quick Links

- **Getting Started**: [../docs/GETTING_STARTED.md](../docs/GETTING_STARTED.md)
- **Hackathon Guide**: [../docs/guides/HACKATHON_GUIDE.md](../docs/guides/HACKATHON_GUIDE.md)
- **Authentication**: [../docs/guides/AUTHENTICATION.md](../docs/guides/AUTHENTICATION.md)
- **Azure Deployment**: [../docs/deployment/AZURE_DEPLOYMENT.md](../docs/deployment/AZURE_DEPLOYMENT.md)

## Features

- **Dual Agent System**: Two specialized RAG agents working simultaneously
  - **SOP Agent**: Expert in Standard Operating Procedures, work instructions, and process documentation
  - **Policy Agent**: Expert in company policies, regulations, and compliance requirements
  
- **Orchestrated Communication**: User queries are automatically routed to both agents in parallel
- **Modern Web UI**: Built with Blazor Server for interactive, real-time communication
- **Container-Ready**: Fully dockerized for easy deployment to Azure Container Apps
- **Agent Reuse**: Automatically detects and reuses existing agents in Azure AI Foundry
- **Secure Authentication**: Supports both Entra ID (keyless) and API key authentication

## Prerequisites

- .NET 9.0 SDK
- Azure AI Foundry project with:
  - A deployed GPT model (e.g., gpt-4, gpt-35-turbo, gpt-4o)
  - Project endpoint
- Azure CLI (for authentication)
- Docker (optional, for containerized deployment)

## Configuration

### Quick Configuration

1. Login to Azure:
   ```bash
   az login
   ```

2. Configure `appsettings.Development.json`:
   ```json
   {
     "AzureAI": {
       "ProjectEndpoint": "https://your-foundry.services.ai.azure.com/api/projects/YourProject",
       "ModelDeploymentName": "gpt-4"
     }
   }
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

📖 **For detailed authentication options, see [../docs/guides/AUTHENTICATION.md](../docs/guides/AUTHENTICATION.md)**

## Running Locally

### Using .NET CLI

```bash
dotnet run
```

Navigate to `http://localhost:5000`

### Using Docker

```bash
# Create .env file from example
cp .env.example .env
# Edit .env with your Azure credentials

# Build and run
docker-compose up --build
```

Navigate to `http://localhost:8080`

📖 **For Azure deployment, see [../docs/deployment/AZURE_DEPLOYMENT.md](../docs/deployment/AZURE_DEPLOYMENT.md)**

## Usage

1. Navigate to the application URL
2. Click on "Start Chatting" or go to the "/chat" page
3. Type your question in the input box
4. Press Enter or click "Send"
5. Watch as both agents respond simultaneously:
   - **SOP Agent** (left panel): Provides SOP-related guidance
   - **Policy Agent** (right panel): Provides policy-related guidance

## Project Structure

```
RagAgentApp/
├── Agents/              # Agent implementations
│   ├── IAgentService.cs
│   ├── SopRagAgent.cs   # Uses Azure AI Agent Service
│   └── PolicyRagAgent.cs # Uses Azure AI Agent Service
├── Components/          # Blazor components
│   ├── Layout/
│   └── Pages/
│       ├── Home.razor
│       └── Chat.razor
├── Models/              # Data models
│   ├── AzureAISettings.cs
│   └── ChatMessage.cs
├── Services/            # Business logic
│   └── OrchestratorService.cs
├── Program.cs           # Application startup
├── Dockerfile          # Container definition
└── docker-compose.yml  # Local development compose file
```

## Troubleshooting

### "Cannot find Azure AI endpoint"
- Check that `appsettings.Development.json` contains valid configuration
- Verify endpoint URL format

### "Model not found" errors
- Verify your model deployment name matches your Azure configuration
- Ensure the model is deployed and accessible

### "Unauthorized" errors
- Run `az login` to authenticate
- Verify your account has access to the Azure AI Foundry project

📖 **For more help, see [../docs/GETTING_STARTED.md](../docs/GETTING_STARTED.md)**

## Additional Documentation

- [Architecture Overview](ARCHITECTURE.md) - System architecture and design patterns
- [Agent Setup Guide](AGENT_SETUP.md) - Configuring pre-created agents
- [Migration Notes](MIGRATION.md) - Notes on Azure AI Agent Service migration

## License

MIT License

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
