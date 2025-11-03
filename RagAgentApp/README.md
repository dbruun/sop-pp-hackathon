# RAG Agent System

A dual-agent AI system powered by Azure AI Agent Service and Azure AI Foundry for Standard Operating Procedures (SOP) and Policy information retrieval.

## Features

- **Dual Agent System**: Two specialized RAG agents working simultaneously
  - **SOP Agent**: Expert in Standard Operating Procedures, work instructions, and process documentation
  - **Policy Agent**: Expert in company policies, regulations, and compliance requirements
  
- **Orchestrated Communication**: User queries are automatically routed to both agents in parallel

- **Modern Web UI**: Built with Blazor Server for interactive, real-time communication

- **Container-Ready**: Fully dockerized for easy deployment to Azure Container Apps or any container platform

- **Agent Reuse**: Automatically detects and reuses existing agents in Azure AI Foundry

## Architecture

The application uses a clean architecture with:
- **Blazor Server** for the interactive UI
- **Azure AI Agent Service** as the agentic framework
- **Azure AI Foundry** for agent lifecycle management
- **Azure OpenAI** for LLM capabilities
- **Orchestrator Service** for managing agent communication
- **Specialized Agents** with distinct system prompts and capabilities

## Prerequisites

- .NET 9.0 SDK
- Azure CLI (for Entra ID authentication): `az login`
- Docker (optional, for containerized deployment)
- Azure AI Foundry project with:
  - A deployed GPT model (e.g., gpt-4, gpt-35-turbo, gpt-4o)
  - Project endpoint
  - Agent service enabled
  - Azure AI Developer role assigned to your identity

## Configuration

### Authentication

This application uses **Entra ID authentication** by default via `DefaultAzureCredential`:

1. **Local Development**: Use Azure CLI - just run `az login`
2. **Azure Deployment**: Enable Managed Identity on your Azure resource
3. **CI/CD**: Use Service Principal with environment variables

📖 **See [AUTHENTICATION.md](AUTHENTICATION.md) for detailed authentication setup**

### Local Development with Entra ID (Recommended)

1. Clone the repository
2. Login to Azure CLI:
   ```bash
   az login
   ```
3. Configure `appsettings.Development.json`:
   ```json
   {
     "AzureAI": {
       "ProjectEndpoint": "https://your-foundry.services.ai.azure.com/api/projects/YourProject",
       "ModelDeploymentName": "gpt-4",
       "SopAgentId": "",
       "PolicyAgentId": ""
     }
   }
   ```
4. Run the application - it will automatically use your Azure CLI credentials!

### Alternative: API Key for Testing

If you need to use an API key for testing (not recommended for production):

```json
{
  "AzureAI": {
    "ProjectEndpoint": "https://your-foundry.services.ai.azure.com/api/projects/YourProject",
    "ApiKey": "your-api-key",
    "ModelDeploymentName": "gpt-4"
  }
}
```

⚠️ **Never commit API keys to source control!**

### Environment Variables

For container deployments and CI/CD:

**Entra ID (Recommended):**
```bash
AZURE_AI_PROJECT_ENDPOINT=https://your-foundry.services.ai.azure.com/api/projects/YourProject
AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4
# Optional: Pre-created agent IDs
AZURE_AI_SOP_AGENT_ID=asst_xxx
AZURE_AI_POLICY_AGENT_ID=asst_yyy
# Optional: Service principal credentials (for CI/CD)
AZURE_CLIENT_ID=your-client-id
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_SECRET=your-client-secret
```

**API Key (For testing only - not recommended for production):**
```bash
AZURE_AI_PROJECT_ENDPOINT=https://your-foundry.services.ai.azure.com/api/projects/YourProject
AZURE_AI_API_KEY=your-api-key
AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4
```

## Running Locally

### Option 1: Using .NET CLI

```bash
cd RagAgentApp
dotnet run
```

Then navigate to `http://localhost:5000` (or the port shown in the console).

### Option 2: Using Docker

```bash
cd RagAgentApp

# Create .env file from example
cp .env.example .env
# Edit .env with your Azure credentials

# Build and run
docker-compose up --build
```

Navigate to `http://localhost:8080`.

### Option 3: Using Docker without docker-compose

```bash
cd RagAgentApp

# Build the image
docker build -t ragagentapp .

# Run the container with Entra ID (recommended)
# Mount Azure CLI credentials from host
docker run -p 8080:8080 \
  -v ~/.azure:/root/.azure:ro \
  -e AZURE_AI_PROJECT_ENDPOINT="your-endpoint" \
  -e AZURE_AI_MODEL_DEPLOYMENT_NAME="gpt-4" \
  ragagentapp

# OR run with API key (testing only)
docker run -p 8080:8080 \
  -e AZURE_AI_PROJECT_ENDPOINT="your-endpoint" \
  -e AZURE_AI_API_KEY="your-key" \
  -e AZURE_AI_MODEL_DEPLOYMENT_NAME="gpt-4" \
  ragagentapp
```

## Deploying to Azure Container Apps

📖 **See [DEPLOYMENT.md](DEPLOYMENT.md) for comprehensive deployment guide**

### Quick Deployment with Managed Identity (Recommended)

```bash
# Set variables
RESOURCE_GROUP="rg-ragagent"
ACR_NAME="acrragagent"
LOCATION="eastus"
APP_NAME="ragagentapp"

# Create ACR
az acr create --resource-group $RESOURCE_GROUP \
  --name $ACR_NAME --sku Basic --location $LOCATION

# Build and push image
az acr build --registry $ACR_NAME \
  --image ragagentapp:latest --file Dockerfile .

# Create Container App Environment
az containerapp env create \
  --name env-ragagent \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION

# Deploy with Managed Identity (No API key needed!)
az containerapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --environment env-ragagent \
  --image $ACR_NAME.azurecr.io/ragagentapp:latest \
  --registry-server $ACR_NAME.azurecr.io \
  --target-port 8080 \
  --ingress external \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    AZURE_AI_PROJECT_ENDPOINT="your-endpoint" \
    AZURE_AI_MODEL_DEPLOYMENT_NAME="gpt-4" \
  --cpu 1.0 --memory 2.0Gi

# Enable Managed Identity
az containerapp identity assign \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --system-assigned

# Grant access to Azure AI Foundry (replace with your project resource ID)
PRINCIPAL_ID=$(az containerapp identity show \
  --name $APP_NAME --resource-group $RESOURCE_GROUP \
  --query principalId --output tsv)

az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Azure AI Developer" \
  --scope "/subscriptions/<sub-id>/resourceGroups/<ai-rg>/providers/Microsoft.MachineLearningServices/workspaces/<ai-project>"
```

## Usage

1. Navigate to the application URL
2. Click on "Start Chatting" or go to the "/chat" page
3. Type your question in the input box
4. Press Enter or click "Send"
5. Watch as both agents respond simultaneously:
   - **SOP Agent** (left panel): Provides SOP-related guidance
   - **Policy Agent** (right panel): Provides policy-related guidance

## Development

### Project Structure

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

### Adding New Agents

1. Create a new agent class implementing `IAgentService`
2. Inject `AgentsClient` and model deployment name in constructor
3. Define a unique system prompt for the agent's specialty
4. Implement agent reuse logic (check for existing agents in Azure AI Foundry)
5. Register the agent in `Program.cs` as a scoped service
6. Update the UI to display the new agent's responses

### Customizing Agents

Edit the system prompts in the agent classes to customize behavior:
- `SopRagAgent.cs` - Modify SOP expertise and system prompt
- `PolicyRagAgent.cs` - Modify policy expertise and system prompt

Each agent automatically:
- Checks for existing agents in Azure AI Foundry by name
- Reuses existing agents if found
- Creates new agents only when needed
- Manages conversation threads for each query

## Troubleshooting

### "Cannot find Azure AI endpoint"
- Ensure environment variables are set correctly
- Check that `appsettings.json` or `appsettings.Development.json` contains valid configuration

### "Model not found" errors
- Verify your model deployment name matches what's configured in Azure
- Ensure the model is deployed and accessible

### Container won't start
- Check logs: `docker logs <container-id>`
- Verify environment variables are passed correctly
- Ensure port 8080 is not already in use

## License

MIT License

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
