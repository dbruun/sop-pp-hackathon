# SOP-PP-Hackathon

RAG Agent System for Standard Operating Procedures and Policy Management

## Overview

This repository contains a .NET Blazor web application that implements a dual-agent RAG (Retrieval-Augmented Generation) system using Azure AI Agent Service and Azure AI Foundry. The application features two specialized AI agents that simultaneously answer user queries:

- **SOP Agent**: Expert in Standard Operating Procedures, work instructions, and process documentation
- **Policy Agent**: Expert in company policies, regulations, and compliance requirements

## Features

- 🤖 **Dual Agent Architecture**: Questions are routed to both agents via orchestrator with function calling
- 💬 **Interactive Chat Interface**: Real-time responses in separate panels for each agent
- 🔐 **Entra ID Authentication**: Keyless authentication via DefaultAzureCredential (recommended)
- 🐳 **Container-Ready**: Fully dockerized for easy deployment
- ☁️ **Azure Container Apps Support**: Deploy to Azure with managed identity support
- 🔄 **Agent Persistence**: Agents stored in Azure AI Foundry and reused across restarts
- 🔍 **RAG Capabilities**: Built-in Azure AI Search integration for knowledge retrieval
- 🎯 **Thread Management**: Conversation threads reused for efficiency

## Quick Start

```bash
cd RagAgentApp
```

See the [RagAgentApp/README.md](RagAgentApp/README.md) for detailed setup and usage instructions.

## Documentation

- [Application README](RagAgentApp/README.md) - Setup, configuration, and local development
- [Deployment Guide](RagAgentApp/DEPLOYMENT.md) - Azure Container Apps deployment instructions

## Architecture

```
┌─────────────────────┐
│     User (Browser)  │
│   Blazor Interface  │
└──────────┬──────────┘
           │
           ▼
┌──────────────────────┐
│   Orchestrator       │
│   Service            │
└──────────┬───────────┘
           │
     ┌─────┴─────┐
     │           │
     ▼           ▼
┌─────────┐ ┌──────────┐
│   SOP   │ │  Policy  │
│  Agent  │ │  Agent   │
└────┬────┘ └────┬─────┘
     │           │
     └─────┬─────┘
           ▼
┌──────────────────────┐
│  Azure AI Foundry    │
│  • Agent Service     │
│  • Azure OpenAI      │
│  • Thread Management │
└──────────────────────┘
```

## Technology Stack

- **.NET 9.0**: Latest .NET framework
- **Blazor Server**: Interactive web UI
- **Azure.AI.Agents.Persistent (v1.1.0)**: Persistent agent service with lifecycle management
- **Azure.AI.Projects (v1.0.0)**: Azure AI Foundry integration
- **Azure.Identity (v1.17.0)**: Entra ID authentication via DefaultAzureCredential
- **Azure AI Foundry**: Agent lifecycle management and storage
- **Azure OpenAI**: LLM capabilities (GPT-4, GPT-3.5-Turbo, GPT-4o)
- **Azure AI Search**: RAG capabilities (optional)
- **Docker**: Containerization
- **Azure Container Apps**: Cloud deployment platform

## Getting Started

1. **Prerequisites**:
   - .NET 9.0 SDK
   - Azure CLI (for Entra ID auth): `az login`
   - Azure AI Foundry project with deployed model
   - Azure AI Developer role assigned to your identity
   - Docker (optional, for containerization)

2. **Configuration**:
   ```bash
   cd RagAgentApp
   
   # Login to Azure (for Entra ID authentication)
   az login
   
   # Create config from example
   cp .env.example .env
   # Edit .env with your Azure AI Foundry endpoint and model name
   # No API key needed!
   ```

3. **Run Locally**:
   ```bash
   # With .NET
   dotnet run
   
   # Or with Docker
   docker-compose up
   ```

4. **Deploy to Azure**:
   Follow the [Deployment Guide](RagAgentApp/DEPLOYMENT.md) for Container Apps with Managed Identity

## Project Structure

```
sop-pp-hackathon/
├── README.md                    # This file
├── QUICKSTART.md               # 5-minute quick start
├── RagAgentApp/
│   ├── Agents/                 # Agent implementations
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
