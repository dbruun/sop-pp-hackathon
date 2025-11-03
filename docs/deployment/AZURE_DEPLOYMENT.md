# Azure Container Apps Deployment Guide

This guide provides step-by-step instructions for deploying the RAG Agent System to Azure Container Apps.

## Prerequisites

1. **Azure Subscription**: An active Azure subscription
2. **Azure CLI**: Installed and configured ([Install Guide](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli))
3. **Azure AI Foundry Project**: With a deployed model (gpt-4, gpt-35-turbo, or gpt-4o)
4. **Docker**: Installed for local testing (optional)

## Step-by-Step Deployment

### 1. Login to Azure

```bash
az login
az account set --subscription "YOUR_SUBSCRIPTION_NAME_OR_ID"
```

### 2. Set Environment Variables

```bash
# Resource names (customize these)
RESOURCE_GROUP="rg-ragagent"
LOCATION="eastus"
ACR_NAME="acrragagent$(openssl rand -hex 3)"  # Must be globally unique
CONTAINERAPPS_ENV="env-ragagent"
APP_NAME="ragagentapp"

# Azure AI Foundry configuration
AZURE_AI_ENDPOINT="https://your-foundry.services.ai.azure.com/api/projects/YourProject"
MODEL_DEPLOYMENT="gpt-4"  # or gpt-35-turbo, gpt-4o, etc.
```

**Note**: For production, use Managed Identity instead of API keys.

### 3. Create Resource Group

```bash
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION
```

### 4. Create Azure Container Registry

```bash
az acr create \
  --resource-group $RESOURCE_GROUP \
  --name $ACR_NAME \
  --sku Basic \
  --location $LOCATION \
  --admin-enabled true
```

### 5. Build and Push Docker Image

#### Option A: Build in Azure (Recommended)

```bash
# Navigate to the RagAgentApp directory
cd RagAgentApp

# Build and push directly to ACR
az acr build \
  --registry $ACR_NAME \
  --image ragagentapp:latest \
  --file Dockerfile .
```

#### Option B: Build Locally and Push

```bash
# Login to ACR
az acr login --name $ACR_NAME

# Build the image
docker build -t ragagentapp:latest .

# Tag the image
docker tag ragagentapp:latest $ACR_NAME.azurecr.io/ragagentapp:latest

# Push to ACR
docker push $ACR_NAME.azurecr.io/ragagentapp:latest
```

### 6. Create Container Apps Environment

```bash
az containerapp env create \
  --name $CONTAINERAPPS_ENV \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION
```

### 7. Get ACR Credentials

```bash
ACR_USERNAME=$(az acr credential show \
  --name $ACR_NAME \
  --query username \
  --output tsv)

ACR_PASSWORD=$(az acr credential show \
  --name $ACR_NAME \
  --query passwords[0].value \
  --output tsv)
```

### 8. Deploy Container App with Managed Identity (Recommended)

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
  --cpu 1.0 \
  --memory 2.0Gi \
  --min-replicas 1 \
  --max-replicas 3 \
  --system-assigned
```

### 9. Configure Managed Identity Access

```bash
# Get the managed identity principal ID
PRINCIPAL_ID=$(az containerapp identity show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId \
  --output tsv)

# Grant access to Azure AI Foundry
# Replace with your actual Azure AI project resource ID
AI_PROJECT_RESOURCE_ID="/subscriptions/YOUR_SUBSCRIPTION_ID/resourceGroups/YOUR_AI_RG/providers/Microsoft.MachineLearningServices/workspaces/YOUR_AI_PROJECT"

az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Azure AI Developer" \
  --scope $AI_PROJECT_RESOURCE_ID
```

### 10. Get Application URL

```bash
APP_URL=$(az containerapp show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query properties.configuration.ingress.fqdn \
  --output tsv)

echo "Application is deployed at: https://$APP_URL"
```

## Configuration Updates

### Update Environment Variables

```bash
az containerapp update \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --set-env-vars \
    AZURE_AI_MODEL_DEPLOYMENT_NAME="new-model-name"
```

### Update Container Image

After making code changes:

```bash
# Build and push new image
az acr build \
  --registry $ACR_NAME \
  --image ragagentapp:latest \
  --file Dockerfile .

# Update the container app
az containerapp update \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --image $ACR_NAME.azurecr.io/ragagentapp:latest
```

## Scaling Configuration

### Manual Scaling

```bash
az containerapp update \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --min-replicas 2 \
  --max-replicas 10
```

### Auto-Scaling Rules

```bash
az containerapp update \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --scale-rule-name http-rule \
  --scale-rule-type http \
  --scale-rule-http-concurrency 50
```

## Monitoring and Logs

### View Logs

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

### Enable Application Insights (Optional)

```bash
# Create Application Insights
APPINSIGHTS_NAME="ai-ragagent"

az monitor app-insights component create \
  --app $APPINSIGHTS_NAME \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP

# Get instrumentation key
INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app $APPINSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query instrumentationKey \
  --output tsv)

# Update container app
az containerapp update \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --set-env-vars \
    APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$INSTRUMENTATION_KEY"
```

## Troubleshooting

### Container won't start

```bash
# Check container logs
az containerapp logs show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --tail 50

# Check revision status
az containerapp revision list \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --output table
```

### Can't access the application

```bash
# Verify ingress is enabled
az containerapp ingress show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP

# Check if app is running
az containerapp show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query properties.runningStatus
```

### Azure AI Foundry connection issues

- Verify endpoint URL format: `https://your-foundry.services.ai.azure.com/api/projects/YourProject`
- If using Managed Identity, ensure proper role assignments are in place
- Ensure model deployment name matches your Azure AI Foundry deployment
- Verify agent service is enabled in your Azure AI project
- Check that `DefaultAzureCredential` can authenticate (view container logs for details)

## Cost Optimization

### Scale to Zero

```bash
az containerapp update \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --min-replicas 0
```

## Cleanup

To remove all resources:

```bash
az group delete \
  --name $RESOURCE_GROUP \
  --yes \
  --no-wait
```

## Additional Resources

- [Azure Container Apps Documentation](https://docs.microsoft.com/en-us/azure/container-apps/)
- [Azure AI Foundry Documentation](https://learn.microsoft.com/en-us/azure/ai-studio/)
- [Azure AI Agent Service Documentation](https://learn.microsoft.com/en-us/azure/ai-services/agents/)
