# Agent Setup Guide

This application supports two ways to work with Azure AI Agents:

## Option 1: Use Pre-Created Agent IDs (Recommended)

If you've already created agents in Azure AI Foundry, you can reference them directly:

1. **Go to Azure AI Foundry**: https://ai.azure.com
2. **Navigate to your project**
3. **Go to the "Agents" section** in the left navigation
4. **Find or create your agents**:
   - Create an "SOP Expert Agent" 
   - Create a "Policy Expert Agent"
5. **Copy the Agent IDs** (format: `asst_xxxxxxxxxxxx`)
6. **Update `appsettings.Development.json`**:
   ```json
   {
     "AzureAI": {
       "SopAgentId": "asst_xxxxxxxxxxxxxxxxxxxx",
       "PolicyAgentId": "asst_yyyyyyyyyyyyyyyyyyyy"
     }
   }
   ```

### Benefits:
- ✅ Full control over agent configuration in Azure AI Foundry portal
- ✅ Can add file search, code interpreter, or other tools via portal
- ✅ Can attach knowledge bases and data sources
- ✅ Simpler application startup (no agent creation logic)
- ✅ Consistent agent behavior across deployments

## Option 2: Auto-Create Agents (Fallback)

If you don't provide agent IDs, the application will automatically:
1. Check if agents with matching names exist in your Azure AI Foundry project
2. Reuse them if found, or create new ones with default prompts

### Default Agent Configuration:

**SOP Expert Agent:**
```
You are a Standard Operating Procedures (SOP) expert assistant. 
Your role is to help users understand and find information about standard operating procedures, 
work instructions, and process documentation. Provide clear, structured responses based on 
standard operating procedures knowledge. If you don't have specific information, acknowledge 
that and provide general guidance on SOPs.
```

**Policy Expert Agent:**
```
You are a Policy expert assistant. Your role is to help users 
understand company policies, regulations, compliance requirements, and governance frameworks. 
Provide clear, authoritative responses based on policy knowledge. When discussing policies, 
cite relevant sections and explain implications. If you don't have specific policy information, 
acknowledge that and provide general policy guidance.
```

### When to use auto-creation:
- ⚠️ Quick testing/development
- ⚠️ You don't need custom agent configurations
- ⚠️ You're okay with basic agents without additional tools or knowledge bases

## Required Configuration

You **must** provide the project endpoint and model name:

```json
{
  "AzureAI": {
    "ProjectEndpoint": "https://<your-foundry>.services.ai.azure.com/api/projects/<YourProject>",
    "ModelDeploymentName": "gpt-4"
  }
}
```

**Authentication**: The application uses `DefaultAzureCredential` by default (Azure CLI, Managed Identity, etc.). You can optionally add `"ApiKey"` for testing, but it's not recommended for production.

### Finding Your Configuration Values:

1. **ProjectEndpoint**: 
   - Go to Azure AI Foundry → Your Project → Overview
   - Look for "Project connection string" or "Endpoint"
   - Format: `https://<foundry-name>.services.ai.azure.com/api/projects/<project-name>`

2. **ModelDeploymentName**: 
   - In Azure AI Foundry → Your Project → Deployments
   - Find your deployed model name (e.g., `gpt-4`, `gpt-4o`, `gpt-35-turbo`)

## Environment Variables (Optional)

For container deployments, you can use environment variables instead:

```bash
AZURE_AI_PROJECT_ENDPOINT=https://your-foundry.services.ai.azure.com/api/projects/YourProject
AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4
AZURE_AI_SOP_AGENT_ID=asst_xxxxxxxxxxxxxxxxxxxx
AZURE_AI_POLICY_AGENT_ID=asst_yyyyyyyyyyyyyyyyyyyy
```

**Note**: `AZURE_AI_API_KEY` is optional. If not provided, the app uses `DefaultAzureCredential` for authentication.

## Troubleshooting

### Authentication Issues
- Ensure you're logged in with `az login` if using Entra ID authentication
- If using API key, verify it's correct and not expired

### "Agent not found"
- Make sure the agent ID you provided exists in your Azure AI Foundry project
- Verify you're using the correct project endpoint
- Check that the agent hasn't been deleted

### "Unauthorized" or "403 Forbidden"
- Run `az login` and verify you're signed in to the correct Azure account
- Ensure your account has the "Azure AI Developer" role on the project
- If using API key, verify it's correct and not expired
- Check that your Azure AI Foundry project is active

### Agents are being created on every restart
- This happens if agent IDs are not provided
- Solution: Use Option 1 and provide pre-created agent IDs
- This will eliminate duplicate agent creation
