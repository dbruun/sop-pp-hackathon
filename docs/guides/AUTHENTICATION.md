# Azure AI Authentication Guide

This application supports two authentication methods for connecting to Azure AI Foundry.

## 🔐 Option 1: Entra ID (Recommended for Production)

Entra ID provides secure, credential-free authentication using managed identities or service principals.

### Local Development with Azure CLI

The easiest way to use Entra ID authentication locally:

1. **Install Azure CLI** (if not already installed):
   ```bash
   # Windows
   winget install Microsoft.AzureCLI
   
   # macOS
   brew install azure-cli
   
   # Linux
   curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
   ```

2. **Login to Azure**:
   ```bash
   az login
   ```

3. **Set your default subscription** (if you have multiple):
   ```bash
   az account set --subscription "Your Subscription Name"
   ```

4. **Run the application** - it will automatically use your Azure CLI credentials!
   ```bash
   dotnet run
   ```

### Azure Deployment with Managed Identity

When deploying to Azure (App Service, Container Apps, AKS, etc.):

1. **Enable Managed Identity** on your Azure resource:
   ```bash
   # For App Service
   az webapp identity assign --name <app-name> --resource-group <rg-name>
   
   # For Container Apps
   az containerapp identity assign --name <app-name> --resource-group <rg-name> --system-assigned
   ```

2. **Grant the Managed Identity access to Azure AI Foundry**:
   - Go to Azure AI Foundry → Your Project → Access Control (IAM)
   - Add role assignment: **Azure AI Developer** or **Cognitive Services User**
   - Select your managed identity

3. **Deploy without any API keys** - the app will automatically use Managed Identity!

### Service Principal Authentication

For CI/CD pipelines or non-Azure environments:

1. **Create a Service Principal**:
   ```bash
   az ad sp create-for-rbac --name "RagAgentApp" --role "Cognitive Services User" --scopes /subscriptions/<subscription-id>/resourceGroups/<rg-name>
   ```

2. **Set environment variables**:
   ```bash
   export AZURE_CLIENT_ID="<appId from previous command>"
   export AZURE_TENANT_ID="<tenant from previous command>"
   export AZURE_CLIENT_SECRET="<password from previous command>"
   ```

3. **Run the application** - it will use the service principal.

### Configuration for Entra ID

In `appsettings.json` or `appsettings.Development.json`:

```json
{
  "AzureAI": {
    "ProjectEndpoint": "https://your-foundry.services.ai.azure.com/api/projects/YourProject",
    "ModelDeploymentName": "gpt-4"
  }
}
```

**Note**: No `ApiKey` field needed! The application will automatically use `DefaultAzureCredential` for authentication.

### How DefaultAzureCredential Works

The application uses `DefaultAzureCredential` which tries these authentication methods in order:

1. **Environment Variables** (AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_CLIENT_SECRET)
2. **Managed Identity** (when running in Azure)
3. **Visual Studio** credentials
4. **VS Code** credentials  
5. **Azure CLI** credentials
6. **Azure PowerShell** credentials

It automatically picks the first one that works!

---

## 🔑 Option 2: API Key (Development/Testing Only)

For quick local testing, you can use an API key:

### Configuration

In `appsettings.Development.json`:

```json
{
  "AzureAI": {
    "ProjectEndpoint": "https://your-foundry.services.ai.azure.com/api/projects/YourProject",
    "ApiKey": "your-api-key-here",
    "ModelDeploymentName": "gpt-4"
  }
}
```

### Finding Your API Key

1. Go to **Azure AI Foundry** → Your Project
2. Navigate to **Settings** → **Keys and Endpoint**
3. Copy **Key 1** or **Key 2**

⚠️ **Warning**: API keys should never be committed to source control!

---

## 🚀 Quick Start

### Recommended: Use Azure CLI

```bash
# 1. Login to Azure
az login

# 2. Set subscription (if needed)
az account set --subscription "Your Subscription"

# 3. Run the app
cd RagAgentApp
dotnet run
```

That's it! No API keys needed.

---

## 🔒 Security Best Practices

### ✅ DO:
- Use Entra ID authentication in production
- Enable Managed Identity for Azure deployments
- Use Azure CLI for local development
- Store API keys in Azure Key Vault (if you must use them)
- Rotate API keys regularly
- Use different credentials for dev/staging/prod

### ❌ DON'T:
- Commit API keys to source control
- Share API keys via email or chat
- Use API keys in production environments
- Hard-code credentials in source code
- Use production credentials for local testing

---

## 🐛 Troubleshooting

### "DefaultAzureCredential failed to retrieve a token"

**Solution**: Make sure you're logged in:
```bash
az login
az account show  # Verify you're logged into the correct subscription
```

### "Insufficient permissions"

**Solution**: Grant your identity the **Azure AI Developer** role:
```bash
# Get your current user's object ID
USER_ID=$(az ad signed-in-user show --query id -o tsv)

# Grant access to Azure AI Foundry project
az role assignment create --role "Azure AI Developer" --assignee $USER_ID --scope /subscriptions/<subscription-id>/resourceGroups/<rg-name>/providers/Microsoft.MachineLearningServices/workspaces/<project-name>
```

### Azure CLI credentials not working

**Solution**: Clear and re-authenticate:
```bash
az logout
az login
az account set --subscription "Your Subscription"
```

---

## 📚 Additional Resources

- [Azure AI Foundry Authentication](https://learn.microsoft.com/azure/ai-studio/how-to/authentication)
- [DefaultAzureCredential](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential)
- [Managed Identity](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview)
- [Azure CLI Authentication](https://learn.microsoft.com/cli/azure/authenticate-azure-cli)
