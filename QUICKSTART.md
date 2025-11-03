# Quick Start Guide# Quick Start Guide



Get the RAG Agent System running in **5 minutes**!Get the RAG Agent System up and running in 5 minutes!



## What You'll Build## What You'll Build



A dual-agent chat system where both specialized agents respond simultaneously:A dual-agent chat system where:

- 🤖 **SOP Agent** - Expert in Standard Operating Procedures- 🤖 **SOP Agent** answers questions about Standard Operating Procedures

- 📜 **Policy Agent** - Expert in policies and compliance- 📜 **Policy Agent** answers questions about policies and compliance

- Both agents respond **simultaneously** to every question

## Prerequisites

## Prerequisites

Choose your preferred method:

Choose your preferred method:

### Option A: .NET (Fastest)

- .NET 9.0 SDK ([Download](https://dotnet.microsoft.com/download))### Option A: Run with .NET (Fastest)

- Azure AI Foundry project with deployed model- ✅ .NET 9.0 SDK ([Download](https://dotnet.microsoft.com/download))

- ✅ Azure OpenAI access with API key

### Option B: Docker

- Docker Desktop ([Download](https://www.docker.com/products/docker-desktop))### Option B: Run with Docker

- Azure AI Foundry project with deployed model- ✅ Docker Desktop ([Download](https://www.docker.com/products/docker-desktop))

- ✅ Azure OpenAI access with API key

## Step 1: Azure Setup

## Step 1: Setup Authentication (Recommended: Entra ID)

### Get Your Azure AI Foundry Details

### Option A: Entra ID (Recommended - No keys needed! 🎉)

1. Go to [Azure AI Foundry](https://ai.azure.com)

2. Navigate to your project (or create one)1. Install Azure CLI:

3. Deploy a model: **gpt-4**, **gpt-4o**, or **gpt-35-turbo**   ```bash

4. Copy your **Project Endpoint**:   # Windows

   ```   winget install Microsoft.AzureCLI

   https://your-foundry.services.ai.azure.com/api/projects/YourProject   

   ```   # macOS

5. Note your **Model Deployment Name** (e.g., "gpt-4")   brew install azure-cli

   

### Setup Authentication (Recommended: Entra ID - No Keys!)   # Linux

   curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

**Best Practice:** Use Entra ID authentication (keyless)   ```



```bash2. Login to Azure:

# Install Azure CLI   ```bash

winget install Microsoft.AzureCLI  # Windows   az login

brew install azure-cli              # macOS   ```



# Login to Azure3. You're done! The app will use your Azure credentials automatically via `DefaultAzureCredential`.

az login

### Option B: API Key (For Testing Only - Not Recommended)

# That's it! No API keys needed.

```1. Go to [Azure AI Foundry](https://ai.azure.com)

2. Navigate to your Azure AI project

**Alternative:** Use an API key for quick testing (not recommended for production)3. Go to **Settings** → **Keys and Endpoint**

- Get API key from: Azure AI Foundry → Your Project → Settings → Keys4. Copy your API key



## Step 2: Clone and Configure⚠️ **Note:** API keys should only be used for testing. Production deployments should use Entra ID.



```bash## Step 2: Get Azure AI Foundry Project Details

# Clone repository

git clone https://github.com/dbruun/sop-pp-hackathon.gitYou need an Azure AI Foundry project with a deployed model:

cd sop-pp-hackathon/RagAgentApp

1. Go to [Azure AI Foundry](https://ai.azure.com)

# Create configuration2. Create or navigate to your Azure AI project

cp .env.example .env3. Deploy a model (gpt-4, gpt-35-turbo, or gpt-4o)

```4. Copy these values:

   - **Project Endpoint**: `https://your-foundry.services.ai.azure.com/api/projects/YourProject`

**Edit `.env` file:**   - **Model Name**: Your deployment name (e.g., "gpt-4")

5. Ensure your Azure identity has "Azure AI Developer" role on the project

```bash

# Option 1: Entra ID (Recommended - No API key)## Step 3: Clone and Configure

AZURE_AI_PROJECT_ENDPOINT=https://your-foundry.services.ai.azure.com/api/projects/YourProject

AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4```bash

# Clone the repository

# Option 2: API Key (Testing only)git clone https://github.com/dbruun/sop-pp-hackathon.git

AZURE_AI_PROJECT_ENDPOINT=https://your-foundry.services.ai.azure.com/api/projects/YourProjectcd sop-pp-hackathon/RagAgentApp

AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4

AZURE_AI_API_KEY=your-api-key-here# Create configuration file

```cp .env.example .env



## Step 3: Run the Application# Edit .env with your credentials

nano .env  # or use your favorite editor

### Option A: Using .NET (Fastest)```



```bashYour `.env` should look like:

cd RagAgentApp

dotnet run**Option 1 - Entra ID (Recommended):**

``````bash

AZURE_AI_PROJECT_ENDPOINT=https://your-foundry.services.ai.azure.com/api/projects/YourProject

Open browser: **http://localhost:5000**AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4

# No API key needed! Just run 'az login' before starting the app

### Option B: Using Docker```



```bash**Option 2 - API Key (Testing Only):**

cd RagAgentApp```bash

docker-compose upAZURE_AI_PROJECT_ENDPOINT=https://your-foundry.services.ai.azure.com/api/projects/YourProject

```AZURE_AI_API_KEY=your-api-key-here

AZURE_AI_MODEL_DEPLOYMENT_NAME=gpt-4

Open browser: **http://localhost:8080**```



## Step 4: Test the Agents## Step 4: Run the Application



1. Navigate to the **Chat** page### Option A: Using .NET CLI (Recommended for Development)

2. Type a question: *"What are the key components of a good SOP?"*

3. Press **Enter**```bash

4. Watch both agents respond simultaneously!# Navigate to the app directory

cd RagAgentApp

### Sample Questions

# Run the application

**SOP Agent Expertise:**dotnet run

- "What should be included in a standard operating procedure?"```

- "How do I create an effective work instruction?"

- "What's the difference between a procedure and a process?"Open your browser to: **http://localhost:5000**



**Policy Agent Expertise:**### Option B: Using Docker

- "What are common elements in a data privacy policy?"

- "How should we handle policy violations?"```bash

- "What's the purpose of a compliance framework?"# Navigate to the app directory

cd RagAgentApp

**Both Agents:**

- "How do SOPs and policies differ?"# Start with docker-compose

- "What documentation is required for regulatory compliance?"docker-compose up

```

## Troubleshooting

Open your browser to: **http://localhost:8080**

### "DefaultAzureCredential failed to retrieve a token"

```bash## Step 5: Test the Agents

# Solution: Login to Azure

az login1. Click on **"Start Chatting"** or go to `/chat`

az account show  # Verify correct subscription2. Type a question like: **"What are the key components of a good SOP?"**

```3. Press **Enter** or click **Send**

4. Watch both agents respond:

### "Cannot connect to Azure AI Foundry"   - **Left panel**: SOP Agent's response

- ✅ Check your endpoint URL is correct   - **Right panel**: Policy Agent's response

- ✅ Verify model deployment name matches

- ✅ Ensure agent service is enabled in your project### Try These Sample Questions

- ✅ Check you have "Azure AI Developer" role

**For SOP Agent:**

### "Model not found"- "What should be included in a standard operating procedure?"

- ✅ Verify model deployment name exactly matches- "How do I create an effective work instruction?"

- ✅ Check model is deployed and running- "What's the difference between a procedure and a process?"



### Port Already in Use**For Policy Agent:**

```bash- "What are common elements in a data privacy policy?"

# .NET: Edit Properties/launchSettings.json- "How should we handle policy violations?"

# Docker: Edit docker-compose.yml port mapping- "What's the purpose of a compliance framework?"

```

**General Questions (Both respond):**

## What's Next?- "How do SOPs and policies differ?"

- "What documentation is required for regulatory compliance?"

### Deploy to Azure- "Explain the approval process for new procedures"

See [docs/GUIDE.md](RagAgentApp/docs/GUIDE.md#azure-deployment) for complete deployment guide.

## Troubleshooting

### Customize Agents

- Edit prompts in `Agents/SopRagAgent.cs` and `Agents/PolicyRagAgent.cs`### "DefaultAzureCredential failed to retrieve a token"

- Add RAG capabilities with Azure AI Search- ✅ Run `az login` to authenticate

- Customize UI in `Components/Pages/Chat.razor`- ✅ Verify you're logged into the correct subscription: `az account show`

- ✅ Ensure your identity has "Azure AI Developer" role on the project

### Learn More

- 📖 [Complete Guide](RagAgentApp/docs/GUIDE.md) - Full documentation### "Cannot connect to Azure AI Foundry"

- 🏗️ [Technical Details](RagAgentApp/docs/TECHNICAL.md) - Architecture and implementation- ✅ Check your endpoint URL is correct

- 🐛 [Report Issues](https://github.com/dbruun/sop-pp-hackathon/issues)- ✅ Verify API key is correct (if using API key)

- ✅ Ensure model is deployed in Azure AI Foundry

## Architecture Overview- ✅ Verify agent service is enabled in your project



```### "Model not found"

User Question- ✅ Check deployment name matches exactly

     ↓- ✅ Verify model is deployed and available

 Orchestrator (routes to both agents)

     ↓### Port already in use

  ┌──┴──┐- ✅ .NET: Change port in `Properties/launchSettings.json`

  ↓     ↓- ✅ Docker: Change port in `docker-compose.yml`

SOP   Policy

Agent Agent### Application won't start

  ↓     ↓```bash

  └──┬──┘# Check logs

     ↓dotnet run --verbose

Azure OpenAI

     ↓# Or with Docker

Two Responses (parallel!)docker-compose logs

``````



## Performance Notes## What's Next?



- **First Query**: May take 5-10 seconds (agent initialization)### Learn More

- **Subsequent Queries**: 2-5 seconds (threads reused)- 📖 [Full Documentation](RagAgentApp/README.md)

- **Parallel Processing**: Both agents run simultaneously- 🚀 [Deploy to Azure](RagAgentApp/DEPLOYMENT.md)

- **Token Usage**: Each query uses tokens for both agents- 🏗️ [Architecture Guide](RagAgentApp/ARCHITECTURE.md)

- 📋 [Implementation Details](RagAgentApp/SUMMARY.md)

## Security Reminders

### Customize

- 🔒 Never commit `.env` file (already in `.gitignore`)1. **Modify Agent Prompts**: Edit `Agents/SopRagAgent.cs` and `Agents/PolicyRagAgent.cs`

- 🔒 Use Managed Identity for production (not API keys)2. **Add More Agents**: Implement `IAgentService` interface

- 🔒 Enable HTTPS for production deployments3. **Change UI**: Edit `Components/Pages/Chat.razor`

- 🔒 Rotate API keys regularly if you must use them4. **Add RAG**: Integrate Azure AI Search or Cosmos DB



---### Deploy to Production

Follow the [Deployment Guide](RagAgentApp/DEPLOYMENT.md) to deploy to Azure Container Apps.

**Success!** 🎉 You now have a working dual-agent RAG system!

## Architecture at a Glance

For advanced features, check out the [Complete Guide](RagAgentApp/docs/GUIDE.md).

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
