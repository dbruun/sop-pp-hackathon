# SOP-PP-Hackathon

RAG Agent System for Standard Operating Procedures and Policy Management

## 🎯 Overview

This repository contains a .NET Blazor web application that implements a dual-agent RAG (Retrieval-Augmented Generation) system using Azure AI Agent Service and Azure AI Foundry. The application features two specialized AI agents that simultaneously answer user queries:

- **SOP Agent**: Expert in Standard Operating Procedures, work instructions, and process documentation
- **Policy Agent**: Expert in company policies, regulations, and compliance requirements

## 🚀 Quick Start

Get started in 5 minutes:

```bash
# Clone the repository
git clone https://github.com/dbruun/sop-pp-hackathon.git
cd sop-pp-hackathon

# Login to Azure (for authentication)
az login

# Configure and run
cd RagAgentApp
# Edit appsettings.Development.json with your Azure AI Foundry endpoint
dotnet run
```

Open your browser to: **http://localhost:5000**

📖 **See the [Getting Started Guide](docs/GETTING_STARTED.md) for detailed setup instructions**

## 🎓 For Hackathon Participants

**This version has stubbed agent implementations!** Perfect for learning how to build AI agent systems.

👉 **Start here**: [Hackathon Guide](docs/guides/HACKATHON_GUIDE.md) - Complete implementation guide

## 📚 Documentation

### Getting Started
- [Getting Started Guide](docs/GETTING_STARTED.md) - Quick setup and configuration
- [Authentication Setup](docs/guides/AUTHENTICATION.md) - Azure authentication methods

### Implementation
- [Hackathon Guide](docs/guides/HACKATHON_GUIDE.md) - Step-by-step implementation for hackathon
- [Architecture Overview](RagAgentApp/ARCHITECTURE.md) - System architecture and design

### Deployment
- [Azure Deployment](docs/deployment/AZURE_DEPLOYMENT.md) - Deploy to Azure Container Apps
- [RagAgentApp README](RagAgentApp/README.md) - Application-specific configuration

## Features

- 🤖 **Dual Agent Architecture**: Questions are routed to both agents simultaneously via an orchestrator
- 💬 **Interactive Chat Interface**: Real-time responses in separate panels for each agent
- 🐳 **Container-Ready**: Fully dockerized for easy deployment
- ☁️ **Azure Container Apps Support**: Deploy to Azure with managed identity support
- 🔒 **Secure Authentication**: Supports Entra ID (keyless) and API key authentication

## Architecture

```
┌─────────────┐
│   User UI   │
└──────┬──────┘
       │
       ▼
┌─────────────────┐
│  Orchestrator   │
└────────┬────────┘
         │
    ┌────┴────┐
    │         │
    ▼         ▼
┌────────┐ ┌─────────┐
│  SOP   │ │ Policy  │
│ Agent  │ │  Agent  │
└────┬───┘ └────┬────┘
     │          │
     └────┬─────┘
          ▼
   ┌──────────────┐
   │ Azure AI     │
   │ Agent Service│
   └──────────────┘
```

## Technology Stack

- **.NET 9.0**: Latest .NET framework
- **Blazor Server**: Interactive web UI
- **Azure AI Agent Service**: Agentic AI framework
- **Azure AI Foundry**: Agent lifecycle management
- **Azure OpenAI**: LLM capabilities
- **Docker**: Containerization
- **Azure Container Apps**: Cloud deployment platform

## Prerequisites

- .NET 9.0 SDK ([Download](https://dotnet.microsoft.com/download))
- Azure AI Foundry project with deployed model
- Docker (optional, for containerization)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

MIT License