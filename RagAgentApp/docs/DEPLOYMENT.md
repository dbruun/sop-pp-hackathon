# Azure Container Apps Deployment Guide

This guide provides step-by-step instructions for deploying the RAG Agent System to Azure Container Apps.

## Prerequisites

Before you begin, ensure you have:

1. **Azure Subscription**: An active Azure subscription
2. **Azure CLI**: Installed and configured ([Install Guide](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli))
3. **Azure OpenAI Resource**: Deployed with a model (gpt-4, gpt-35-turbo, or gpt-4o)
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

# Azure AI Foundry configuration (Recommended: Use Managed Identity - no keys!)
AZURE_AI_ENDPOINT="https://your-foundry.services.ai.azure.com/api/projects/YourProject"
MODEL_DEPLOYMENT="gpt-4"  # or gpt-35-turbo, gpt-4o, etc.

# Optional: Pre-created agent IDs (recommended)
SOP_AGENT_ID="asst_xxx"
POLICY_AGENT_ID="asst_yyy"

# Optional: For testing with API key (not recommended for production)
# AZURE_AI_KEY="your-api-key-here"
```

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
cd /path/to/RagAgentApp

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

### 8. Deploy Container App

**Recommended: Using Managed Identity (Keyless Authentication)**

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
  --max-replicas 3
```

**Note:** No API key needed! Continue to step "Using Managed Identity" below to grant access.

**Alternative: Using API Key (For Testing Only)**

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
  --secrets \
    azure-ai-api-key="$AZURE_AI_KEY" \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    AZURE_AI_PROJECT_ENDPOINT="$AZURE_AI_ENDPOINT" \
    AZURE_AI_API_KEY=secretref:azure-ai-api-key \
    AZURE_AI_MODEL_DEPLOYMENT_NAME="$MODEL_DEPLOYMENT" \
  --cpu 1.0 \
  --memory 2.0Gi \
  --min-replicas 1 \
  --max-replicas 3
```

### 9. Get Application URL

```bash
APP_URL=$(az containerapp show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query properties.configuration.ingress.fqdn \
  --output tsv)

echo "Application is deployed at: https://$APP_URL"
```

### 10. Test the Application

Open your browser and navigate to the URL from step 9. You should see the RAG Agent System home page.

## Configuration Updates

### Update Environment Variables

If you need to update configuration after deployment:

```bash
az containerapp update \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --set-env-vars \
    AZURE_AI_MODEL_DEPLOYMENT_NAME="new-model-name"
```

### Update Secrets

```bash
az containerapp secret set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --secrets \
    azure-ai-api-key="new-api-key"
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

Add HTTP scaling rule:

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

### View Metrics

```bash
az containerapp show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query properties.latestRevisionFqdn
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

# Update container app with Application Insights
az containerapp update \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --set-env-vars \
    APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$INSTRUMENTATION_KEY"
```

## Using Managed Identity (Recommended - Keyless)

**This is the recommended approach for production deployments.**

### 1. Enable Managed Identity

```bash
az containerapp identity assign \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --system-assigned
```

### 2. Get the Identity Principal ID

```bash
PRINCIPAL_ID=$(az containerapp identity show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId \
  --output tsv)

echo "Managed Identity Principal ID: $PRINCIPAL_ID"
```

### 3. Grant Access to Azure AI Foundry

```bash
# Get your Azure AI Foundry project resource ID
# Replace with your actual values
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

### 4. Verify Configuration

```bash
# Ensure no API key is set (for keyless authentication)
az containerapp show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query properties.configuration.secrets

# Should not show azure-ai-api-key
```

The application will automatically use `DefaultAzureCredential` which picks up the Managed Identity.

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

- Verify endpoint URL is correct (format: `https://your-foundry.services.ai.azure.com/api/projects/YourProject`)
- If using Managed Identity: Ensure "Azure AI Developer" role is assigned
- If using API key: Check API key is valid and not expired
- Ensure model deployment name matches your Azure AI Foundry deployment
- Verify agent service is enabled in your Azure AI project
- Verify network connectivity from Container Apps to Azure AI Foundry
- Check Container App logs for authentication errors

## Cost Optimization

### Use Consumption Plan

Container Apps automatically scales to zero when not in use:

```bash
az containerapp update \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --min-replicas 0
```

### Monitor Costs

```bash
# View resource costs
az consumption usage list \
  --start-date $(date -d "30 days ago" +%Y-%m-%d) \
  --end-date $(date +%Y-%m-%d) \
  --output table
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
- [Azure CLI Reference](https://docs.microsoft.com/en-us/cli/azure/)

## Support

For issues or questions:
- Check the [README.md](README.md) for general usage
- Review Azure Container Apps logs
- Consult Azure OpenAI service health status
