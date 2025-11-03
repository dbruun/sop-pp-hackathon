# Complete Setup & Deployment Guide

This comprehensive guide covers authentication, configuration, local development, and Azure deployment.

## Table of Contents

1. [Authentication](#authentication)
2. [Agent Configuration](#agent-configuration)
3. [Local Development](#local-development)
4. [Azure Deployment](#azure-deployment)
5. [Configuration Reference](#configuration-reference)
6. [Troubleshooting](#troubleshooting)

---

## Authentication

The application supports two authentication methods for Azure AI Foundry.

### Option 1: Entra ID (Recommended)

**Best Practice:** Use Entra ID for secure, keyless authentication.

#### Local Development with Azure CLI

```bash
# 1. Install Azure CLI
winget install Microsoft.AzureCLI  # Windows
brew install azure-cli              # macOS
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash  # Linux

# 2. Login to Azure
az login

# 3. Set subscription (if you have multiple)
az account set --subscription "Your Subscription Name"

# 4. Run the app
cd RagAgentApp
dotnet run
```

The app automatically uses your Azure CLI credentials via `DefaultAzureCredential`.

#### Azure Deployment with Managed Identity

When deploying to Azure (Container Apps, App Service, AKS):

```bash
# 1. Enable Managed Identity on your resource
az containerapp identity assign \
  --name ragagentapp \
  --resource-group rg-ragagent \
  --system-assigned

# 2. Get the identity's Principal ID
PRINCIPAL_ID=$(az containerapp identity show \
  --name ragagentapp \
  --resource-group rg-ragagent \
  --query principalId --output tsv)

# 3. Grant access to Azure AI Foundry
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Azure AI Developer" \
  --scope "/subscriptions/<sub-id>/resourceGroups/<ai-rg>/providers/Microsoft.MachineLearningServices/workspaces/<ai-project>"
```

No API keys needed! The app uses Managed Identity automatically.

#### CI/CD with Service Principal

For automated deployments:

```bash
# 1. Create Service Principal
az ad sp create-for-rbac \
  --name "RagAgentApp" \
  --role "Azure AI Developer" \
  --scopes /subscriptions/<sub-id>/resourceGroups/<rg>

# 2. Set environment variables in your pipeline
AZURE_CLIENT_ID=<appId>
AZURE_TENANT_ID=<tenant>
AZURE_CLIENT_SECRET=<password>
```

#### How DefaultAzureCredential Works

The application tries authentication methods in this order:
1. Environment Variables (AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_CLIENT_SECRET)
2. Managed Identity (when running in Azure)
3. Visual Studio credentials
4. VS Code credentials
5. Azure CLI credentials (`az login`)
6. Azure PowerShell credentials

It uses the first method that succeeds.

### Option 2: API Key (Testing Only)

**⚠️ Not recommended for production!** Use only for quick testing.

#### Get Your API Key

1. Go to [Azure AI Foundry](https://ai.azure.com)
2. Navigate to your project
3. Settings → **Keys and Endpoint**
4. Copy **Key 1** or **Key 2**

#### Configuration

```json
{
  "AzureAI": {
    "ProjectEndpoint": "https://your-foundry.services.ai.azure.com/api/projects/YourProject",
    "ApiKey": "your-api-key-here",
    "ModelDeploymentName": "gpt-4"
  }
}
```

**Security Warning:** Never commit API keys to source control!

---

## Agent Configuration

The application uses two specialized agents that can be pre-created or auto-generated.

### Option 1: Pre-Created Agents (Recommended)

For full control over agent configuration, create agents in Azure AI Foundry portal.

#### Steps

1. **Go to Azure AI Foundry** (https://ai.azure.com)
2. **Navigate to your project**
3. **Go to "Agents" section**
4. **Create two agents:**
   - Name: "SOP Expert Agent" 
   - Name: "Policy Expert Agent"
5. **Configure each agent:**
   - Add system prompts
   - Enable tools (file search, Azure AI Search, code interpreter)
   - Attach knowledge bases
6. **Copy Agent IDs** (format: `asst_xxxxxxxxxxxx`)
7. **Update configuration:**

```json
{
  "AzureAI": {
    "ProjectEndpoint": "https://your-foundry.services.ai.azure.com/api/projects/YourProject",
    "ModelDeploymentName": "gpt-4",
    "SopAgentId": "asst_xxxxxxxxxxxx",
    "PolicyAgentId": "asst_yyyyyyyyyyyy"
  }
}
```

Or via environment variables:

```bash
AZURE_AI_SOP_AGENT_ID=asst_xxxxxxxxxxxx
AZURE_AI_POLICY_AGENT_ID=asst_yyyyyyyyyyyy
```

#### Benefits

✅ Full control over agent configuration  
✅ Add RAG tools (Azure AI Search integration)  
✅ Attach knowledge bases and documents  
✅ Consistent behavior across deployments  
✅ No agent creation on every startup  

### Option 2: Auto-Created Agents

If no agent IDs are provided, the app automatically:
1. Checks for existing agents by name
2. Reuses them if found
3. Creates new ones if not found

**Default System Prompts:**

**SOP Expert Agent:**
```
You are a Standard Operating Procedures (SOP) expert assistant. 
Your role is to help users understand and find information about 
standard operating procedures, work instructions, and process 
documentation. Provide clear, structured responses based on 
best practices for SOP development and management.
```

**Policy Expert Agent:**
```
You are a Policy expert assistant. Your role is to help users 
understand company policies, regulations, compliance requirements, 
and governance frameworks. Provide clear, authoritative responses 
based on policy knowledge. When discussing policies, cite relevant 
sections and explain implications.
```

### Finding Your Configuration Values

#### Project Endpoint

1. Go to Azure AI Foundry → Your Project
2. Click **Overview**
3. Find "Project connection string" or "Endpoint"
4. Format: `https://<foundry-name>.services.ai.azure.com/api/projects/<project-name>`

#### Model Deployment Name

1. In Azure AI Foundry → Your Project
2. Click **Deployments**
3. Find your deployed model name
4. Common names: `gpt-4`, `gpt-4o`, `gpt-35-turbo`

---

## Local Development

### Prerequisites

- .NET 9.0 SDK ([Download](https://dotnet.microsoft.com/download))
- Azure CLI (for Entra ID auth)
- Docker Desktop (optional, for containerized development)

### Configuration File Setup

#### Option A: appsettings.Development.json (Recommended for .NET)

Create or edit `RagAgentApp/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "RagAgentApp.Agents": "Information",
      "RagAgentApp.Services": "Information"
    }
  },
  "AzureAI": {
    "ProjectEndpoint": "https://your-foundry.services.ai.azure.com/api/projects/YourProject",
    "ModelDeploymentName": "gpt-4",
    "SopAgentId": "",
    "PolicyAgentId": ""
  }
}
```

**Note:** This file is in `.gitignore` - your credentials stay local!

#### Option B: .env File (Recommended for Docker)

Create `RagAgentApp/.env`:

```bash
# Azure AI Configuration
AZURE_AI_PROJECT_ENDPOINT=https://your-foundry.services.ai.azure.com/api/projects/YourProject
AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4

# Optional: Pre-created agent IDs
AZURE_AI_SOP_AGENT_ID=
AZURE_AI_POLICY_AGENT_ID=

# Optional: API Key (use Entra ID instead!)
# AZURE_AI_API_KEY=
```

**Note:** `.env` is in `.gitignore` - never commit it!

### Run with .NET CLI

```bash
# Navigate to app directory
cd RagAgentApp

# Ensure you're logged into Azure
az login

# Run the application
dotnet run

# Open browser
# http://localhost:5000
```

### Run with Docker Compose

```bash
# Navigate to app directory
cd RagAgentApp

# Ensure .env file is configured
# If using Entra ID, you need to mount Azure credentials

# Option 1: With Azure CLI credentials (Entra ID)
docker-compose up

# Option 2: Build and run manually
docker build -t ragagentapp .
docker run -p 8080:8080 \
  -v ~/.azure:/root/.azure:ro \
  --env-file .env \
  ragagentapp
```

### Development Tips

#### Hot Reload

```bash
# Watch mode for automatic recompilation
dotnet watch run
```

#### Debug Logging

Update `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "RagAgentApp.Agents": "Debug",
      "RagAgentApp.Services": "Debug"
    }
  }
}
```

#### Customize Agents

Edit agent prompts and behavior:
- `Agents/SopRagAgent.cs` - SOP agent implementation
- `Agents/PolicyRagAgent.cs` - Policy agent implementation

#### Customize UI

Edit Blazor components:
- `Components/Pages/Chat.razor` - Main chat interface
- `Components/Layout/MainLayout.razor` - App layout
- `wwwroot/app.css` - Custom styles

---

## Azure Deployment

Complete guide to deploying the RAG Agent System to Azure Container Apps.

### Prerequisites

- Azure subscription
- Azure CLI installed
- Azure AI Foundry project with deployed model
- Docker (optional, for local testing)

### Step 1: Setup Variables

```bash
# Resource names (customize these)
RESOURCE_GROUP="rg-ragagent"
LOCATION="eastus"
ACR_NAME="acrragagent$(openssl rand -hex 3)"  # Must be globally unique
CONTAINERAPPS_ENV="env-ragagent"
APP_NAME="ragagentapp"

# Your Azure AI Foundry configuration
AZURE_AI_ENDPOINT="https://your-foundry.services.ai.azure.com/api/projects/YourProject"
MODEL_DEPLOYMENT="gpt-4"

# Optional: Pre-created agent IDs
SOP_AGENT_ID="asst_xxxxxxxxxxxx"
POLICY_AGENT_ID="asst_yyyyyyyyyyyy"
```

### Step 2: Create Azure Resources

```bash
# Login to Azure
az login
az account set --subscription "YOUR_SUBSCRIPTION_NAME"

# Create resource group
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# Create Container Registry
az acr create \
  --resource-group $RESOURCE_GROUP \
  --name $ACR_NAME \
  --sku Basic \
  --location $LOCATION \
  --admin-enabled true
```

### Step 3: Build and Push Container

#### Option A: Build in Azure (Recommended)

```bash
cd RagAgentApp

az acr build \
  --registry $ACR_NAME \
  --image ragagentapp:latest \
  --file Dockerfile .
```

#### Option B: Build Locally

```bash
# Login to ACR
az acr login --name $ACR_NAME

# Build image
docker build -t ragagentapp:latest .

# Tag and push
docker tag ragagentapp:latest $ACR_NAME.azurecr.io/ragagentapp:latest
docker push $ACR_NAME.azurecr.io/ragagentapp:latest
```

### Step 4: Create Container App Environment

```bash
az containerapp env create \
  --name $CONTAINERAPPS_ENV \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION
```

### Step 5: Get ACR Credentials

```bash
ACR_USERNAME=$(az acr credential show \
  --name $ACR_NAME \
  --query username --output tsv)

ACR_PASSWORD=$(az acr credential show \
  --name $ACR_NAME \
  --query passwords[0].value --output tsv)
```

### Step 6: Deploy Container App (with Managed Identity)

```bash
az containerapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --environment $CONTAINERAPPS_ENV \
  --image $ACR_NAME.azurecr.io/ragagentapp:latest \
  --registry-server $ACR_NAME.azurecr.io \
  --registry-username $ACR_USERNAME \
  --registry-password $ACR_PASSWORD \
  --target-port 8080 \
  --ingress external \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    AZURE_AI_PROJECT_ENDPOINT="$AZURE_AI_ENDPOINT" \
    AZURE_AI_MODEL_DEPLOYMENT_NAME="$MODEL_DEPLOYMENT" \
    AZURE_AI_SOP_AGENT_ID="$SOP_AGENT_ID" \
    AZURE_AI_POLICY_AGENT_ID="$POLICY_AGENT_ID" \
  --cpu 1.0 \
  --memory 2.0Gi \
  --min-replicas 1 \
  --max-replicas 5
```

### Step 7: Enable Managed Identity

```bash
# Enable system-assigned managed identity
az containerapp identity assign \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --system-assigned

# Get the identity's Principal ID
PRINCIPAL_ID=$(az containerapp identity show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId --output tsv)

echo "Managed Identity Principal ID: $PRINCIPAL_ID"
```

### Step 8: Grant Azure AI Foundry Access

```bash
# Replace with your actual Azure AI project resource ID
SUBSCRIPTION_ID="your-subscription-id"
AI_RG="your-ai-resource-group"
AI_PROJECT="your-ai-project-name"

AI_PROJECT_RESOURCE_ID="/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$AI_RG/providers/Microsoft.MachineLearningServices/workspaces/$AI_PROJECT"

# Grant "Azure AI Developer" role
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Azure AI Developer" \
  --scope $AI_PROJECT_RESOURCE_ID

echo "✅ Managed Identity configured successfully!"
```

### Step 9: Get Application URL

```bash
APP_URL=$(az containerapp show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query properties.configuration.ingress.fqdn \
  --output tsv)

echo "🚀 Application deployed at: https://$APP_URL"
```

### Step 10: Test Deployment

Open your browser and navigate to: `https://<your-app-url>`

You should see the RAG Agent System home page.

### Configuration Updates

#### Update Environment Variables

```bash
az containerapp update \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --set-env-vars \
    AZURE_AI_MODEL_DEPLOYMENT_NAME="new-model"
```

#### Update Container Image

```bash
# Build and push new image
az acr build \
  --registry $ACR_NAME \
  --image ragagentapp:latest \
  --file Dockerfile .

# Update container app (triggers automatic restart)
az containerapp update \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --image $ACR_NAME.azurecr.io/ragagentapp:latest
```

### Scaling Configuration

#### Manual Scaling

```bash
az containerapp update \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --min-replicas 2 \
  --max-replicas 10
```

#### Auto-Scaling Rules

```bash
# HTTP-based auto-scaling
az containerapp update \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --scale-rule-name http-rule \
  --scale-rule-type http \
  --scale-rule-http-concurrency 50
```

#### Scale to Zero (Cost Optimization)

```bash
az containerapp update \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --min-replicas 0
```

### Monitoring

#### View Logs

```bash
# Stream logs
az containerapp logs show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --follow

# View recent logs
az containerapp logs show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --tail 100
```

#### Enable Application Insights

```bash
# Create Application Insights
APPINSIGHTS_NAME="ai-ragagent"

az monitor app-insights component create \
  --app $APPINSIGHTS_NAME \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP

# Get connection string
INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app $APPINSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query instrumentationKey --output tsv)

# Update container app
az containerapp update \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --set-env-vars \
    APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$INSTRUMENTATION_KEY"
```

### Cleanup

To remove all Azure resources:

```bash
az group delete \
  --name $RESOURCE_GROUP \
  --yes \
  --no-wait
```

---

## Configuration Reference

### appsettings.json Structure

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "RagAgentApp.Agents": "Information",
      "RagAgentApp.Services": "Information"
    }
  },
  "AllowedHosts": "*",
  "AzureAI": {
    "ProjectEndpoint": "https://your-foundry.services.ai.azure.com/api/projects/YourProject",
    "ApiKey": "",
    "ModelDeploymentName": "gpt-4",
    "SopAgentId": "",
    "PolicyAgentId": ""
  }
}
```

### Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `AZURE_AI_PROJECT_ENDPOINT` | Yes | Azure AI Foundry project endpoint |
| `AZURE_AI_MODEL_DEPLOYMENT_NAME` | Yes | Deployed model name (gpt-4, gpt-4o, etc.) |
| `AZURE_AI_API_KEY` | No | API key (use Entra ID instead!) |
| `AZURE_AI_SOP_AGENT_ID` | No | Pre-created SOP agent ID |
| `AZURE_AI_POLICY_AGENT_ID` | No | Pre-created Policy agent ID |
| `AZURE_CLIENT_ID` | No | Service Principal client ID (CI/CD) |
| `AZURE_TENANT_ID` | No | Azure tenant ID (CI/CD) |
| `AZURE_CLIENT_SECRET` | No | Service Principal secret (CI/CD) |

### Docker Environment Variables

When using `docker-compose.yml` or running containers:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - AZURE_AI_PROJECT_ENDPOINT=${AZURE_AI_PROJECT_ENDPOINT}
  - AZURE_AI_MODEL_DEPLOYMENT_NAME=${AZURE_AI_MODEL_DEPLOYMENT_NAME}
  - AZURE_AI_SOP_AGENT_ID=${AZURE_AI_SOP_AGENT_ID}
  - AZURE_AI_POLICY_AGENT_ID=${AZURE_AI_POLICY_AGENT_ID}
```

---

## Troubleshooting

### Authentication Issues

#### "DefaultAzureCredential failed to retrieve a token"

**Solution:**
```bash
# Verify Azure CLI login
az login
az account show

# Ensure correct subscription
az account set --subscription "Your Subscription"

# Clear Azure CLI cache if needed
az account clear
az login
```

#### "Insufficient permissions"

**Solution:**
```bash
# Grant yourself Azure AI Developer role
az role assignment create \
  --role "Azure AI Developer" \
  --assignee $(az ad signed-in-user show --query id -o tsv) \
  --scope /subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.MachineLearningServices/workspaces/<project>
```

### Connection Issues

#### "Cannot connect to Azure AI Foundry"

**Checklist:**
- ✅ Endpoint URL is correct format
- ✅ Model deployment name matches exactly
- ✅ Agent service is enabled in your project
- ✅ Network connectivity (not blocked by firewall)
- ✅ Correct Azure subscription selected

#### "Model not found"

**Solution:**
```bash
# List available deployments in your project
az ml online-deployment list \
  --resource-group <rg> \
  --workspace-name <project>

# Verify model name matches exactly (case-sensitive!)
```

### Agent Issues

#### "Agent not found"

**Solution:**
- Verify agent ID is correct (format: `asst_xxxxxxxxxxxx`)
- Check agent exists in Azure AI Foundry portal
- Ensure agent wasn't deleted
- Try auto-creation by removing agent IDs from config

#### "Agents created on every restart"

**Solution:**
- Provide pre-created agent IDs in configuration
- This prevents duplicate agent creation

### Container Issues

#### Container won't start

**Solution:**
```bash
# Check logs
docker logs <container-id>

# Or for Container Apps
az containerapp logs show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --tail 50

# Check revision status
az containerapp revision list \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP
```

#### Port already in use

**Solution:**
```bash
# .NET: Edit Properties/launchSettings.json
# Docker: Edit docker-compose.yml

# Or find and kill process
netstat -ano | findstr :8080  # Windows
lsof -i :8080                  # macOS/Linux
```

### Performance Issues

#### Slow first query

**Expected:** First query initializes agents (5-10 seconds)

**Optimization:**
- Pre-create agents in Azure AI Foundry
- Provide agent IDs in configuration
- Use min-replicas > 0 to avoid cold starts

#### Slow subsequent queries

**Investigation:**
```bash
# Check logs for timing information
# Look for "completed in XXXms" messages

# Verify:
- Thread reuse is working (check logs)
- Network latency to Azure AI Foundry
- Model performance (try different model)
```

### Cost Issues

#### Unexpected costs

**Check:**
```bash
# View resource costs
az consumption usage list \
  --start-date $(date -d "30 days ago" +%Y-%m-%d) \
  --end-date $(date +%Y-%m-%d)

# Enable scale-to-zero
az containerapp update \
  --name $APP_NAME \
  --min-replicas 0
```

### Additional Support

- 📖 [Technical Documentation](TECHNICAL.md)
- 🐛 [Report Issues](https://github.com/dbruun/sop-pp-hackathon/issues)
- 📚 [Azure AI Foundry Docs](https://learn.microsoft.com/azure/ai-studio/)
- 📚 [Container Apps Docs](https://learn.microsoft.com/azure/container-apps/)

---

**Need more help?** Check the [Technical Documentation](TECHNICAL.md) or open an issue on GitHub!
