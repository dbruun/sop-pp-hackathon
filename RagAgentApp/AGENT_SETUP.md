# Agent Setup Guide

This application supports two ways to work with Azure AI Agents:

## Option 1: Use Pre-Created Agent IDs (Recommended)

If you've already created agents in Azure AI Foundry, you can reference them directly:

1. **Go to Azure AI Foundry**: https://ai.azure.com
2. **Navigate to your project**: DerekAzureFoundry
3. **Go to the "Agents" section** in the left navigation
4. **Find or create your agents**:
   - Create an "SOP Expert Agent" 
   - Create a "Policy Expert Agent"
5. **Copy the Agent IDs** (format: `asst_xxxxxxxxxxxx`)
6. **Update `appsettings.Development.json`**:
   ```json
   {
     "AzureAI": {
       "SopAgentId": "asst_0EmJcRf2foX9fE0QKRMrcaXp",
       "PolicyAgentId": "asst_1FnKdsGh3gpY0gF1PLSndbYq"
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

Regardless of which option you choose, you **must** provide:

```json
{
  "AzureAI": {
    "ProjectEndpoint": "https://<your-foundry>.services.ai.azure.com/api/projects/<YourProject>",
    "ApiKey": "<your-api-key>",
    "ModelDeploymentName": "gpt-4"
  }
}
```

### Finding Your Configuration Values:

1. **ProjectEndpoint**: 
   - Go to Azure AI Foundry → Your Project → Overview
   - Look for "Project connection string" or "Endpoint"
   - Format: `https://<foundry-name>.services.ai.azure.com/api/projects/<project-name>`

2. **ApiKey**: 
   - In Azure AI Foundry → Your Project → Settings → Keys and Endpoint
   - Copy one of the API keys

3. **ModelDeploymentName**: 
   - In Azure AI Foundry → Your Project → Deployments
   - Find your deployed model name (e.g., `gpt-4`, `gpt-4o`, `gpt-35-turbo`)

## Environment Variables (Optional)

For container deployments, you can use environment variables instead:

```bash
AZURE_AI_PROJECT_ENDPOINT=https://derekazurefoundry.services.ai.azure.com/api/projects/DerekAzureFoundry
AZURE_AI_API_KEY=<your-api-key>
AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4
AZURE_AI_SOP_AGENT_ID=asst_0EmJcRf2foX9fE0QKRMrcaXp
AZURE_AI_POLICY_AGENT_ID=asst_1FnKdsGh3gpY0gF1PLSndbYq
```

## Troubleshooting

### "Invalid connection string format"
- ✅ **Fixed!** This error has been resolved. The app now uses endpoint + API key directly.

### "Agent not found"
- Make sure the agent ID you provided exists in your Azure AI Foundry project
- Verify you're using the correct project endpoint
- Check that the agent hasn't been deleted

### "Unauthorized" or "403 Forbidden"
- Verify your API key is correct and not expired
- Ensure your API key has permission to access the project
- Check that your Azure AI Foundry project is active

### Agents are being created on every restart
- This happens if agent IDs are not provided
- Solution: Use Option 1 and provide pre-created agent IDs
- This will eliminate duplicate agent creation
