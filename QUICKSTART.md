# Quick Start Guide

Get the RAG Agent System up and running in 5 minutes!

## What You'll Build

A dual-agent chat system where:
- 🤖 **SOP Agent** answers questions about Standard Operating Procedures
- 📜 **Policy Agent** answers questions about policies and compliance
- Both agents respond **simultaneously** to every question

## Prerequisites

Choose your preferred method:

### Option A: Run with .NET (Fastest)
- ✅ .NET 9.0 SDK ([Download](https://dotnet.microsoft.com/download))
- ✅ Azure OpenAI access with API key

### Option B: Run with Docker
- ✅ Docker Desktop ([Download](https://www.docker.com/products/docker-desktop))
- ✅ Azure OpenAI access with API key

## Step 1: Authentication Setup

### Recommended: Entra ID (No API keys needed!)

1. **Install Azure CLI** (if not already installed):
   ```powershell
   winget install Microsoft.AzureCLI
   ```

2. **Login to Azure**:
   ```powershell
   az login
   ```

3. **That's it!** The app will automatically use your Azure credentials via `DefaultAzureCredential`.

### Alternative: API Key (For quick testing only)

⚠️ **Not recommended for production**

1. Go to [Azure AI Foundry](https://ai.azure.com)
2. Navigate to your Azure AI project
3. Go to **Settings** → **Keys and Endpoint**
4. Copy your API key (you'll use this in Step 2b)

## Step 1b: Get Azure AI Foundry Project Details

You need an Azure AI Foundry project with a deployed model:

1. Go to [Azure AI Foundry](https://ai.azure.com)
2. Create or navigate to your Azure AI project
3. Deploy a model (gpt-4, gpt-35-turbo, or gpt-4o)
4. Copy these values:
   - **Project Endpoint**: `https://your-foundry.services.ai.azure.com/api/projects/YourProject`
   - **Model Name**: Your deployment name (e.g., "gpt-4")

## Step 2: Clone and Configure

```bash
# Clone the repository
git clone https://github.com/dbruun/sop-pp-hackathon.git
cd sop-pp-hackathon/RagAgentApp

# Create configuration file
cp .env.example .env

# Edit .env with your credentials
nano .env  # or use your favorite editor
```

### Using Entra ID (Recommended):
```bash
AZURE_AI_PROJECT_ENDPOINT=https://your-foundry.services.ai.azure.com/api/projects/YourProject
AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4
```

### Using API Key (Testing only):
```bash
AZURE_AI_PROJECT_ENDPOINT=https://your-foundry.services.ai.azure.com/api/projects/YourProject
AZURE_AI_API_KEY=your-api-key-here
AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4
```

**Note**: When `AZURE_AI_API_KEY` is not provided, the application automatically uses `DefaultAzureCredential` for authentication (Azure CLI, Managed Identity, etc.).

## Step 3: Run the Application

### Option A: Using .NET CLI (Recommended for Development)

```bash
# Navigate to the app directory
cd RagAgentApp

# Run the application
dotnet run
```

Open your browser to: **http://localhost:5000**

### Option B: Using Docker

```bash
# Navigate to the app directory
cd RagAgentApp

# Start with docker-compose
docker-compose up
```

Open your browser to: **http://localhost:8080**

## Step 4: Test the Agents

1. Click on **"Start Chatting"** or go to `/chat`
2. Type a question like: **"What are the key components of a good SOP?"**
3. Press **Enter** or click **Send**
4. Watch both agents respond:
   - **Left panel**: SOP Agent's response
   - **Right panel**: Policy Agent's response

### Try These Sample Questions

**For SOP Agent:**
- "What should be included in a standard operating procedure?"
- "How do I create an effective work instruction?"
- "What's the difference between a procedure and a process?"

**For Policy Agent:**
- "What are common elements in a data privacy policy?"
- "How should we handle policy violations?"
- "What's the purpose of a compliance framework?"

**General Questions (Both respond):**
- "How do SOPs and policies differ?"
- "What documentation is required for regulatory compliance?"
- "Explain the approval process for new procedures"

## Troubleshooting

### "Cannot connect to Azure AI Foundry"
- ✅ Check your connection string or endpoint URL is correct
- ✅ Verify API key is correct (if not using connection string)
- ✅ Ensure model is deployed in Azure AI Foundry
- ✅ Verify agent service is enabled in your project

### "Model not found"
- ✅ Check deployment name matches exactly
- ✅ Verify model is deployed and available

### Port already in use
- ✅ .NET: Change port in `Properties/launchSettings.json`
- ✅ Docker: Change port in `docker-compose.yml`

### Application won't start
```bash
# Check logs
dotnet run --verbose

# Or with Docker
docker-compose logs
```

## What's Next?

### Learn More
- 📖 [Full Documentation](RagAgentApp/README.md)
- 🚀 [Deploy to Azure](RagAgentApp/DEPLOYMENT.md)
- 🏗️ [Architecture Guide](RagAgentApp/ARCHITECTURE.md)
- 📋 [Implementation Details](RagAgentApp/SUMMARY.md)

### Customize
1. **Modify Agent Prompts**: Edit `Agents/SopRagAgent.cs` and `Agents/PolicyRagAgent.cs`
2. **Add More Agents**: Implement `IAgentService` interface
3. **Change UI**: Edit `Components/Pages/Chat.razor`
4. **Add RAG**: Integrate Azure AI Search or Cosmos DB

### Deploy to Production
Follow the [Deployment Guide](RagAgentApp/DEPLOYMENT.md) to deploy to Azure Container Apps.

## Architecture at a Glance

```
User Question
     ↓
Orchestrator (parallel)
     ↓
  ┌──┴──┐
  ↓     ↓
SOP   Policy
Agent  Agent
  ↓     ↓
  └──┬──┘
     ↓
 Azure OpenAI
     ↓
Two Responses
```

## Need Help?

- 📚 Check the [README](RagAgentApp/README.md) for detailed instructions
- 🔧 Review [Troubleshooting section](RagAgentApp/README.md#troubleshooting)
- 📖 Read the [Architecture docs](RagAgentApp/ARCHITECTURE.md)
- 🐛 [Open an issue](https://github.com/dbruun/sop-pp-hackathon/issues) if you find a bug

## Success Checklist

- ✅ Application starts without errors
- ✅ Can access the chat page
- ✅ Can type and send messages
- ✅ Both agents respond to questions
- ✅ Responses are relevant and well-formatted

**Congratulations!** 🎉 You now have a working dual-agent RAG system!

## Performance Tips

- **Response Time**: First query may be slower (cold start)
- **Parallel Processing**: Both agents process simultaneously
- **Token Usage**: Each query uses tokens for both agents
- **Rate Limits**: Be aware of Azure OpenAI rate limits

## Security Reminders

- 🔒 Never commit `.env` file to git (already in `.gitignore`)
- 🔒 Use managed identity in production instead of API keys
- 🔒 Enable HTTPS for production deployments
- 🔒 Rotate API keys regularly

---

Ready to dive deeper? Check out the full [README](RagAgentApp/README.md) for advanced configuration options and deployment to Azure Container Apps!
