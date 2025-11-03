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

## Quick Start

Navigate to the application directory:

```bash
cd RagAgentApp
```

See the [RagAgentApp/README.md](RagAgentApp/README.md) for detailed setup and usage instructions.

## Documentation

- [Application README](RagAgentApp/README.md) - Setup, configuration, and local development
- [Deployment Guide](RagAgentApp/DEPLOYMENT.md) - Azure Container Apps deployment instructions

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
   │ Azure OpenAI │
   └──────────────┘
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

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

MIT License