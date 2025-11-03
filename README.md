# RAG Agent System# SOP-PP-Hackathon



A production-ready dual-agent system for Standard Operating Procedures (SOP) and Policy management, powered by Azure AI Agent Service and Azure AI Foundry.RAG Agent System for Standard Operating Procedures and Policy Management



## Overview ##



This .NET Blazor application implements an intelligent RAG (Retrieval-Augmented Generation) system with two specialized AI agents that work simultaneously to answer user queries:This repository contains a .NET Blazor web application that implements a dual-agent RAG (Retrieval-Augmented Generation) system using Azure AI Agent Service and Azure AI Foundry. The application features two specialized AI agents that simultaneously answer user queries:



- **SOP Agent**: Expert in Standard Operating Procedures, work instructions, and process documentation- **SOP Agent**: Expert in Standard Operating Procedures, work instructions, and process documentation

- **Policy Agent**: Expert in company policies, regulations, and compliance requirements- **Policy Agent**: Expert in company policies, regulations, and compliance requirements



Both agents run in parallel, providing comprehensive responses from different perspectives in real-time.## Features



## Key Features- 🤖 **Dual Agent Architecture**: Questions are routed to both agents via orchestrator with function calling

- 💬 **Interactive Chat Interface**: Real-time responses in separate panels for each agent

- 🤖 **Parallel Dual-Agent Architecture**: Both agents respond simultaneously to every query- 🔐 **Entra ID Authentication**: Keyless authentication via DefaultAzureCredential (recommended)

- 💬 **Interactive Blazor UI**: Real-time chat interface with separate response panels- 🐳 **Container-Ready**: Fully dockerized for easy deployment

- 🔐 **Keyless Authentication**: Uses Azure Entra ID (no API keys needed!)- ☁️ **Azure Container Apps Support**: Deploy to Azure with managed identity support

- 🔄 **Persistent Agents**: Agents stored in Azure AI Foundry, reused across restarts- 🔄 **Agent Persistence**: Agents stored in Azure AI Foundry and reused across restarts

- 🔍 **RAG-Ready**: Built-in Azure AI Search integration support- 🔍 **RAG Capabilities**: Built-in Azure AI Search integration for knowledge retrieval

- 🐳 **Container-Ready**: Fully dockerized with docker-compose support

- ☁️ **Azure-Native**: Deploy to Container Apps with Managed Identity## Quick Start

- 🎯 **Thread Management**: Conversation threads reused for efficiency

Navigate to the application directory:

## Quick Start

```bash

**Prerequisites**: .NET 9.0 SDK, Azure AI Foundry project with deployed modelcd RagAgentApp

```

```bash

# 1. Login to AzureSee the [RagAgentApp/README.md](RagAgentApp/README.md) for detailed setup and usage instructions.

az login

## Documentation

# 2. Clone and configure

git clone https://github.com/dbruun/sop-pp-hackathon.git- [Application README](RagAgentApp/README.md) - Setup, configuration, and local development

cd sop-pp-hackathon/RagAgentApp- [Deployment Guide](RagAgentApp/DEPLOYMENT.md) - Azure Container Apps deployment instructions

cp .env.example .env

# Edit .env with your Azure AI Foundry endpoint and model name## Architecture



# 3. Run locally```

dotnet run┌─────────────┐

│   User UI   │

# Open browser to http://localhost:5000└──────┬──────┘

```       │

       ▼

**That's it!** The app uses your Azure credentials automatically via `DefaultAzureCredential`.┌─────────────────┐

│  Orchestrator   │

## Architecture└────────┬────────┘

         │

```    ┌────┴────┐

┌─────────────┐    │         │

│   Browser   │    ▼         ▼

└──────┬──────┘┌────────┐ ┌─────────┐

       ││  SOP   │ │ Policy  │

       ▼│ Agent  │ │  Agent  │

┌─────────────────┐└────┬───┘ └────┬────┘

│  Blazor Server  │     │          │

│                 │     └────┬─────┘

│  Chat Interface │          ▼

└──────┬──────────┘   ┌──────────────┐

       │   │ Azure OpenAI │

       ▼   └──────────────┘

┌─────────────────────┐```

│  Orchestrator       │◄──── Routes queries

│  Service            │## Technology Stack

└──────┬──────────────┘

       │- **.NET 9.0**: Latest .NET framework

    ┌──┴───┐- **Blazor Server**: Interactive web UI

    ▼      ▼- **Azure.AI.Agents.Persistent (v1.1.0)**: Persistent agent service with lifecycle management

┌────────┐ ┌────────┐- **Azure.AI.Projects (v1.0.0)**: Azure AI Foundry integration

│  SOP   │ │ Policy │- **Azure.Identity (v1.17.0)**: Entra ID authentication via DefaultAzureCredential

│ Agent  │ │ Agent  │- **Azure AI Foundry**: Agent lifecycle management and storage

└────┬───┘ └───┬────┘- **Azure OpenAI**: LLM capabilities (GPT-4, GPT-3.5-Turbo, GPT-4o)

     │         │- **Azure AI Search**: RAG capabilities (optional)

     └────┬────┘- **Docker**: Containerization

          ▼- **Azure Container Apps**: Cloud deployment platform

┌──────────────────────┐

│ Azure AI Foundry     │## Getting Started

│ • Agent Service      │

│ • Azure OpenAI       │1. **Prerequisites**:

│ • Thread Management  │   - .NET 9.0 SDK

└──────────────────────┘   - Azure CLI (for Entra ID auth): `az login`

```   - Azure AI Foundry project with deployed model

   - Azure AI Developer role assigned to your identity

## Technology Stack   - Docker (optional, for containerization)



- **.NET 9.0** - Latest framework with native JSON support2. **Configuration**:

- **Blazor Server** - Interactive web UI with SignalR   ```bash

- **Azure.AI.Agents.Persistent v1.1.0** - Agent lifecycle management   cd RagAgentApp

- **Azure.AI.Projects v1.0.0** - Azure AI Foundry integration   

- **Azure.Identity v1.17.0** - Entra ID authentication   # Login to Azure (for Entra ID authentication)

- **Azure AI Foundry** - Agent storage and orchestration   az login

- **Azure OpenAI** - GPT-4, GPT-4o, GPT-3.5-Turbo support   

- **Docker** - Containerization for deployment   # Create config from example

- **Azure Container Apps** - Cloud hosting platform   cp .env.example .env

   # Edit .env with your Azure AI Foundry endpoint and model name

## Documentation   # No API key needed!

   ```

### Essential Guides

- **[QUICKSTART.md](QUICKSTART.md)** - Get running in 5 minutes3. **Run Locally**:

- **[docs/GUIDE.md](RagAgentApp/docs/GUIDE.md)** - Complete setup, configuration, and deployment guide   ```bash

- **[docs/TECHNICAL.md](RagAgentApp/docs/TECHNICAL.md)** - Architecture, implementation details, and migration notes   # With .NET

   dotnet run

### Quick Links   

- [Authentication Setup](RagAgentApp/docs/GUIDE.md#authentication)   # Or with Docker

- [Local Development](RagAgentApp/docs/GUIDE.md#local-development)   docker-compose up

- [Azure Deployment](RagAgentApp/docs/GUIDE.md#azure-deployment)   ```

- [System Architecture](RagAgentApp/docs/TECHNICAL.md#architecture)

- [Troubleshooting](RagAgentApp/docs/GUIDE.md#troubleshooting)4. **Deploy to Azure**:

   Follow the [Deployment Guide](RagAgentApp/DEPLOYMENT.md) for Container Apps with Managed Identity

## Project Structure

## Contributing

```

sop-pp-hackathon/Contributions are welcome! Please feel free to submit a Pull Request.

├── README.md                    # This file

├── QUICKSTART.md               # 5-minute quick start## License

├── RagAgentApp/

│   ├── Agents/                 # Agent implementationsMIT License
│   │   ├── SopRagAgent.cs
│   │   └── PolicyRagAgent.cs
│   ├── Components/             # Blazor UI components
│   │   └── Pages/
│   │       └── Chat.razor      # Main chat interface
│   ├── Services/
│   │   └── OrchestratorService.cs
│   ├── docs/
│   │   ├── GUIDE.md           # Complete setup guide
│   │   └── TECHNICAL.md       # Technical documentation
│   ├── Program.cs              # App startup
│   ├── Dockerfile             # Container definition
│   └── docker-compose.yml     # Local development
└── .gitignore
```

## Security & Best Practices

✅ **Keyless Authentication**: Uses Entra ID Managed Identity (no API keys to manage)  
✅ **Secrets Protection**: `.gitignore` excludes all sensitive configuration files  
✅ **Thread Reuse**: Efficient conversation thread management  
✅ **Agent Persistence**: Agents stored in Azure AI Foundry (no duplicates)  
✅ **Container-Ready**: Production-ready Docker configuration  
✅ **Error Handling**: Comprehensive error handling and logging  

## Getting Started

### Local Development
```bash
# With .NET
cd RagAgentApp
dotnet run

# With Docker
cd RagAgentApp
docker-compose up
```

### Azure Deployment
```bash
# Quick deploy to Azure Container Apps
az containerapp create \
  --name ragagentapp \
  --resource-group rg-ragagent \
  --environment env-ragagent \
  --image youracr.azurecr.io/ragagentapp:latest \
  --target-port 8080 \
  --ingress external \
  --env-vars \
    AZURE_AI_PROJECT_ENDPOINT="https://your-foundry.services.ai.azure.com/api/projects/YourProject" \
    AZURE_AI_MODEL_DEPLOYMENT_NAME="gpt-4"

# Enable Managed Identity (keyless!)
az containerapp identity assign --name ragagentapp --system-assigned
```

See [docs/GUIDE.md](RagAgentApp/docs/GUIDE.md#azure-deployment) for complete deployment instructions.

## What Makes This Special

- **True Parallel Execution**: Both agents process simultaneously (time = max(agent1, agent2), not sum)
- **Persistent Agent Architecture**: Agents stored in Azure AI Foundry, no duplicate creation
- **Thread Reuse Pattern**: Efficient conversation management with cached thread IDs
- **Keyless Security**: Uses Managed Identity and DefaultAzureCredential (no API keys!)
- **Production-Ready**: Container-ready, auto-scaling, comprehensive error handling

## Sample Queries

**For SOP Agent:**
- "What are the key components of a standard operating procedure?"
- "How do I document a new process workflow?"

**For Policy Agent:**
- "What are the main elements of a data privacy policy?"
- "How should we handle compliance violations?"

**General (Both Respond):**
- "What's the difference between SOPs and policies?"
- "How do we maintain regulatory compliance documentation?"

## Requirements

- .NET 9.0 SDK
- Azure AI Foundry project with:
  - Deployed GPT model (gpt-4, gpt-4o, or gpt-35-turbo)
  - Project endpoint
  - Agent service enabled
- Azure CLI (for local development): `az login`
- Docker (optional, for containerized deployment)

## Contributing

Contributions welcome! Please ensure:
- All tests pass
- Documentation is updated
- Security best practices are followed
- No API keys or secrets are committed

## License

MIT License - See LICENSE file for details

## Support

- 📖 [Complete Guide](RagAgentApp/docs/GUIDE.md)
- 🏗️ [Technical Documentation](RagAgentApp/docs/TECHNICAL.md)
- 🚀 [Quick Start](QUICKSTART.md)
- 🐛 [Report Issues](https://github.com/dbruun/sop-pp-hackathon/issues)

---

**Ready to get started?** Check out the [QUICKSTART.md](QUICKSTART.md) guide!
