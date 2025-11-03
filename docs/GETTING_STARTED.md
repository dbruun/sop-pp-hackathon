# Getting Started with SOP-PP RAG Agent System

This guide will help you get the RAG Agent System up and running quickly.

## What You'll Build

A dual-agent chat system where:
- 🤖 **SOP Agent** answers questions about Standard Operating Procedures
- 📜 **Policy Agent** answers questions about policies and compliance
- Both agents respond **simultaneously** to every question

## Prerequisites

Choose your preferred development method:

### Option A: Run with .NET (Recommended)
- ✅ .NET 9.0 SDK ([Download](https://dotnet.microsoft.com/download))
- ✅ Azure AI Foundry project with deployed model

### Option B: Run with Docker
- ✅ Docker Desktop ([Download](https://www.docker.com/products/docker-desktop))
- ✅ Azure AI Foundry project with deployed model

## Step 1: Set Up Azure AI Foundry

You need an Azure AI Foundry project with a deployed model:

1. Go to [Azure AI Foundry](https://ai.azure.com)
2. Create or navigate to your Azure AI project
3. Deploy a model (gpt-4, gpt-35-turbo, or gpt-4o)
4. Copy these values:
   - **Project Endpoint**: `https://your-foundry.services.ai.azure.com/api/projects/YourProject`
   - **Model Name**: Your deployment name (e.g., "gpt-4")

## Step 2: Authentication Setup

### Recommended: Entra ID (Keyless Authentication)

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

3. **That's it!** The app will automatically use your Azure credentials via `DefaultAzureCredential`.

### Alternative: API Key (Testing Only)

⚠️ **Not recommended for production**

1. Go to [Azure AI Foundry](https://ai.azure.com)
2. Navigate to your Azure AI project
3. Go to **Settings** → **Keys and Endpoint**
4. Copy your API key

**Note**: When `AZURE_AI_API_KEY` is not provided, the application automatically uses `DefaultAzureCredential` for authentication.

## Step 3: Clone and Configure

```bash
# Clone the repository
git clone https://github.com/dbruun/sop-pp-hackathon.git
cd sop-pp-hackathon
```

### Configure for .NET Development

Create or edit `RagAgentApp/appsettings.Development.json`:

```json
{
  "AzureAI": {
    "ProjectEndpoint": "https://your-foundry.services.ai.azure.com/api/projects/YourProject",
    "ModelDeploymentName": "gpt-4"
  }
}
```

### Configure for Docker

```bash
cd RagAgentApp
cp .env.example .env
# Edit .env with your credentials
```

For Entra ID (Recommended):
```bash
AZURE_AI_PROJECT_ENDPOINT=https://your-foundry.services.ai.azure.com/api/projects/YourProject
AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4
```

For API Key (Testing only):
```bash
AZURE_AI_PROJECT_ENDPOINT=https://your-foundry.services.ai.azure.com/api/projects/YourProject
AZURE_AI_API_KEY=your-api-key-here
AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4
```

## Step 4: Run the Application

### Option A: Using .NET CLI

```bash
cd RagAgentApp
dotnet run
```

Open your browser to: **http://localhost:5000**

### Option B: Using Docker

```bash
cd RagAgentApp
docker-compose up
```

Open your browser to: **http://localhost:8080**

## Step 5: Test the Agents

1. Click on **"Start Chatting"** or go to `/chat`
2. Type a question like: **"What are the key components of a good SOP?"**
3. Press **Enter** or click **Send**
4. Watch both agents respond:
   - **Left panel**: SOP Agent's response
   - **Right panel**: Policy Agent's response

### Sample Questions

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
- ✅ Check your endpoint URL is correct
- ✅ Verify API key is correct (if using API key)
- ✅ Ensure model is deployed in Azure AI Foundry
- ✅ Run `az login` if using Entra ID authentication

### "Model not found"
- ✅ Check deployment name matches exactly
- ✅ Verify model is deployed and available

### "Unauthorized" or "403 Forbidden"
- ✅ Run `az login` and verify you're signed in
- ✅ Check API key if using key-based auth
- ✅ Verify your account has access to the project

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
- 📖 [Architecture Overview](../RagAgentApp/ARCHITECTURE.md)
- 🚀 [Deploy to Azure](deployment/AZURE_DEPLOYMENT.md)
- 🔧 [Hackathon Implementation Guide](guides/HACKATHON_GUIDE.md)

### Customize
1. **Modify Agent Prompts**: Edit `Agents/SopRagAgent.cs` and `Agents/PolicyRagAgent.cs`
2. **Add More Agents**: Implement `IAgentService` interface
3. **Change UI**: Edit `Components/Pages/Chat.razor`
4. **Add RAG**: Integrate Azure AI Search or Cosmos DB

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

- 📚 Check the [main README](../README.md) for project overview
- 🔧 Review [RagAgentApp README](../RagAgentApp/README.md) for detailed configuration
- 📖 Read the [Architecture docs](../RagAgentApp/ARCHITECTURE.md)
- 🐛 [Open an issue](https://github.com/dbruun/sop-pp-hackathon/issues) if you find a bug

## Success Checklist

- ✅ Application starts without errors
- ✅ Can access the chat page
- ✅ Can type and send messages
- ✅ Both agents respond to questions
- ✅ Responses are relevant and well-formatted

**Congratulations!** 🎉 You now have a working dual-agent RAG system!

## Security Reminders

- 🔒 Never commit `.env` file to git (already in `.gitignore`)
- 🔒 Use managed identity in production instead of API keys
- 🔒 Enable HTTPS for production deployments
- 🔒 Rotate API keys regularly
