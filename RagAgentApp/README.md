# RAG Agent System

A dual-agent AI system powered by Microsoft Semantic Kernel and Azure AI Foundry for Standard Operating Procedures (SOP) and Policy information retrieval.

## Features

- **Dual Agent System**: Two specialized RAG agents working simultaneously
  - **SOP Agent**: Expert in Standard Operating Procedures, work instructions, and process documentation
  - **Policy Agent**: Expert in company policies, regulations, and compliance requirements
  
- **Orchestrated Communication**: User queries are automatically routed to both agents in parallel

- **Modern Web UI**: Built with Blazor Server for interactive, real-time communication

- **Container-Ready**: Fully dockerized for easy deployment to Azure Container Apps or any container platform

## Architecture

The application uses a clean architecture with:
- **Blazor Server** for the interactive UI
- **Microsoft Semantic Kernel** as the agentic framework
- **Azure OpenAI** for LLM capabilities
- **Orchestrator Service** for managing agent communication
- **Specialized Agents** with distinct system prompts and capabilities

## Prerequisites

- .NET 9.0 SDK
- Docker (for containerized deployment)
- Azure OpenAI or Azure AI Foundry account with:
  - A deployed GPT model (e.g., gpt-4, gpt-35-turbo, gpt-4o)
  - API endpoint and key

## Configuration

### Local Development

1. Clone the repository
2. Create an `appsettings.Development.json` file or set environment variables:

```json
{
  "AzureAI": {
    "ProjectEndpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key",
    "ModelDeploymentName": "gpt-4"
  }
}
```

### Environment Variables

The application supports configuration via environment variables (useful for container deployments):

- `AZURE_AI_PROJECT_ENDPOINT`: Your Azure OpenAI endpoint
- `AZURE_AI_API_KEY`: Your Azure OpenAI API key
- `AZURE_AI_MODEL_DEPLOYMENT_NAME`: Your deployed model name (default: gpt-4)

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

# Run the container
docker run -p 8080:8080 \
  -e AZURE_AI_PROJECT_ENDPOINT="your-endpoint" \
  -e AZURE_AI_API_KEY="your-key" \
  -e AZURE_AI_MODEL_DEPLOYMENT_NAME="gpt-4" \
  ragagentapp
```

## Deploying to Azure Container Apps

### Prerequisites
- Azure CLI installed
- Azure subscription
- Resource group created

### Step 1: Create Azure Container Registry (ACR)

```bash
# Set variables
RESOURCE_GROUP="your-resource-group"
ACR_NAME="your-acr-name"
LOCATION="eastus"

# Create ACR
az acr create --resource-group $RESOURCE_GROUP \
  --name $ACR_NAME \
  --sku Basic \
  --location $LOCATION
```

### Step 2: Build and Push Docker Image

```bash
# Log in to ACR
az acr login --name $ACR_NAME

# Build and push image
cd RagAgentApp
az acr build --registry $ACR_NAME \
  --image ragagentapp:latest \
  --file Dockerfile .
```

### Step 3: Create Container App Environment

```bash
CONTAINERAPPS_ENVIRONMENT="your-env-name"

az containerapp env create \
  --name $CONTAINERAPPS_ENVIRONMENT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION
```

### Step 4: Deploy Container App

```bash
APP_NAME="ragagentapp"

az containerapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --environment $CONTAINERAPPS_ENVIRONMENT \
  --image $ACR_NAME.azurecr.io/ragagentapp:latest \
  --registry-server $ACR_NAME.azurecr.io \
  --target-port 8080 \
  --ingress external \
  --secrets \
    azure-ai-api-key="your-api-key" \
  --env-vars \
    AZURE_AI_PROJECT_ENDPOINT="your-endpoint" \
    AZURE_AI_API_KEY=secretref:azure-ai-api-key \
    AZURE_AI_MODEL_DEPLOYMENT_NAME="gpt-4" \
  --cpu 1.0 \
  --memory 2.0Gi
```

### Step 5: Get the App URL

```bash
az containerapp show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query properties.configuration.ingress.fqdn \
  --output tsv
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
│   ├── SopRagAgent.cs
│   └── PolicyRagAgent.cs
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
2. Define a unique system prompt for the agent's specialty
3. Register the agent in `Program.cs`
4. Update the UI to display the new agent's responses

### Customizing Agents

Edit the system prompts in the agent classes to customize behavior:
- `SopRagAgent.cs` - Modify SOP expertise
- `PolicyRagAgent.cs` - Modify policy expertise

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
