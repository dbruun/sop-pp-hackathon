# SOP-PP-Hackathon

RAG Agent System for Standard Operating Procedures and Policy Management

## Overview

This repository contains a .NET Blazor web application that implements a dual-agent RAG (Retrieval-Augmented Generation) system using Azure AI Agent Service and Azure AI Foundry. The application features two specialized AI agents that simultaneously answer user queries:

- **SOP Agent**: Expert in Standard Operating Procedures, work instructions, and process documentation
- **Policy Agent**: Expert in company policies, regulations, and compliance requirements

## Features

- 🤖 **Dual Agent Architecture**: Questions are routed to both agents simultaneously via an orchestrator
- 💬 **Interactive Chat Interface**: Real-time responses in separate panels for each agent
- 🐳 **Container-Ready**: Fully dockerized for easy deployment
- ☁️ **Azure Container Apps Support**: Deploy to Azure with managed identity support
- 🔒 **Secure Configuration**: Supports both API keys and Azure managed identities

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
- **Azure AI Agent Service**: Agentic AI framework
- **Azure AI Foundry**: Agent lifecycle management
- **Azure OpenAI**: LLM capabilities
- **Docker**: Containerization
- **Azure Container Apps**: Cloud deployment platform

## Getting Started

1. **Prerequisites**:
   - .NET 9.0 SDK
   - Azure AI Foundry project with deployed model
   - Docker (optional, for containerization)

2. **Configuration**:
   ```bash
   cd RagAgentApp
   cp .env.example .env
   # Edit .env with your Azure OpenAI credentials
   ```

3. **Run Locally**:
   ```bash
   dotnet run
   ```
   Or with Docker:
   ```bash
   docker-compose up
   ```

4. **Deploy to Azure**:
   Follow the [Deployment Guide](RagAgentApp/DEPLOYMENT.md)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

MIT License