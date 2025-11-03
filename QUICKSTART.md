# Quick Start Guide

Get the RAG Agent System up and running in **5 minutes**!

## What You'll Build

A dual-agent chat system where:
- 🤖 **SOP Agent** answers questions about Standard Operating Procedures
- 📜 **Policy Agent** answers questions about policies and compliance
- Both agents respond **simultaneously** to every question

## Prerequisites

Choose your preferred method:

### Option A: Run with .NET (Fastest)
- ✅ .NET 9.0 SDK ([Download](https://dotnet.microsoft.com/download))
- ✅ Azure CLI (for authentication)
- ✅ Azure AI Foundry project with deployed model

### Option B: Run with Docker
- ✅ Docker Desktop ([Download](https://www.docker.com/products/docker-desktop))
- ✅ Azure CLI (for authentication)
- ✅ Azure AI Foundry project with deployed model

---

## Step 1: Setup Authentication

### Option A: Entra ID (Recommended - No keys needed! 🎉)

**1. Install Azure CLI:**

```bash
# Windows
winget install Microsoft.AzureCLI

# macOS
brew install azure-cli

# Linux
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

**2. Login to Azure:**

```bash
az login
```

**3. Done!** The app will use your Azure credentials automatically via `DefaultAzureCredential`.

### Option B: API Key (For Testing Only - Not Recommended)

1. Go to [Azure AI Foundry](https://ai.azure.com)
2. Navigate to your Azure AI project
3. Go to **Settings** → **Keys and Endpoint**
4. Copy your API key

⚠️ **Note:** API keys should only be used for testing. Production deployments should use Entra ID.

---

## Step 2: Get Azure AI Foundry Project Details

You need an Azure AI Foundry project with a deployed model:

1. Go to [Azure AI Foundry](https://ai.azure.com)
2. Create or navigate to your Azure AI project
3. Deploy a model (gpt-4, gpt-35-turbo, or gpt-4o)
4. Copy these values:
   - **Project Endpoint**: `https://your-foundry.services.ai.azure.com/api/projects/YourProject`
   - **Model Name**: Your deployment name (e.g., "gpt-4")
5. Ensure your Azure identity has "Azure AI Developer" role on the project

---

## Step 3: Clone and Configure

```bash
# Clone the repository
git clone https://github.com/dbruun/sop-pp-hackathon.git
cd sop-pp-hackathon/RagAgentApp

# Create configuration file
cp .env.example .env

# Edit .env with your credentials
nano .env  # or use your favorite editor
```

Your `.env` should look like:

**Option 1 - Entra ID (Recommended):**

```bash
AZURE_AI_PROJECT_ENDPOINT=https://your-foundry.services.ai.azure.com/api/projects/YourProject
AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4
# No API key needed! Just run 'az login' before starting the app
```

**Option 2 - API Key (Testing Only):**

```bash
AZURE_AI_PROJECT_ENDPOINT=https://your-foundry.services.ai.azure.com/api/projects/YourProject
AZURE_AI_API_KEY=your-api-key-here
AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4
```

---

## Step 4: Run the Application

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

---

## Step 5: Test the Agents

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

---

## Troubleshooting

### "DefaultAzureCredential failed to retrieve a token"
- ✅ Run `az login` to authenticate
- ✅ Verify you're logged into the correct subscription: `az account show`
- ✅ Ensure your identity has "Azure AI Developer" role on the project

### "Cannot connect to Azure AI Foundry"
- ✅ Check your endpoint URL is correct
- ✅ Verify API key is correct (if using API key)
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

---

## What's Next?

### Learn More
- 📖 [Complete Guide](RagAgentApp/docs/GUIDE.md) - Full documentation
- 🏗️ [Technical Details](RagAgentApp/docs/TECHNICAL.md) - Architecture and implementation
- 🐛 [Report Issues](https://github.com/dbruun/sop-pp-hackathon/issues)

### Customize
1. **Modify Agent Prompts**: Edit `Agents/SopRagAgent.cs` and `Agents/PolicyRagAgent.cs`
2. **Add More Agents**: Implement `IAgentService` interface
3. **Change UI**: Edit `Components/Pages/Chat.razor`
4. **Add RAG**: Integrate Azure AI Search or file uploads

### Deploy to Production
Follow the [Deployment Guide](RagAgentApp/docs/GUIDE.md#azure-deployment) to deploy to Azure Container Apps.

---

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

---

## Performance Tips

- **First Query**: May take 5-10 seconds (agent initialization)
- **Subsequent Queries**: 2-5 seconds (threads reused)
- **Parallel Processing**: Both agents run simultaneously
- **Token Usage**: Each query uses tokens for both agents

---

## Security Reminders

- 🔒 Never commit `.env` file to git (already in `.gitignore`)
- 🔒 Use Managed Identity in production instead of API keys
- 🔒 Enable HTTPS for production deployments
- 🔒 Rotate API keys regularly if you must use them

---

**Congratulations!** 🎉 You now have a working dual-agent RAG system!

For advanced features, check out the [Complete Guide](RagAgentApp/docs/GUIDE.md).
