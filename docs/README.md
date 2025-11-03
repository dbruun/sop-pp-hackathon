# Documentation Index

Welcome to the SOP-PP-Hackathon documentation! This guide will help you find the right documentation for your needs.

## 📖 Quick Navigation

### Getting Started
Start here if you're new to the project:
- **[Getting Started Guide](GETTING_STARTED.md)** - Quick setup and your first run
- **[Main README](../README.md)** - Project overview and quick links

### Guides
Step-by-step tutorials and how-tos:
- **[Hackathon Guide](guides/HACKATHON_GUIDE.md)** - Implementation guide for hackathon participants
- **[Authentication Guide](guides/AUTHENTICATION.md)** - Setting up Azure authentication

### Deployment
Production deployment guides:
- **[Azure Deployment](deployment/AZURE_DEPLOYMENT.md)** - Deploy to Azure Container Apps

### Application Documentation
Technical documentation for the RagAgentApp:
- **[RagAgentApp README](../RagAgentApp/README.md)** - Application overview
- **[Architecture](../RagAgentApp/ARCHITECTURE.md)** - System architecture and design
- **[Agent Setup](../RagAgentApp/AGENT_SETUP.md)** - Pre-created agent configuration
- **[Migration Notes](../RagAgentApp/MIGRATION.md)** - Azure AI Agent Service migration

## 🎯 Documentation by Role

### For New Users
1. [Getting Started Guide](GETTING_STARTED.md)
2. [Main README](../README.md)
3. [Authentication Guide](guides/AUTHENTICATION.md)

### For Hackathon Participants
1. [Hackathon Guide](guides/HACKATHON_GUIDE.md)
2. [Getting Started Guide](GETTING_STARTED.md)
3. [RagAgentApp README](../RagAgentApp/README.md)

### For Developers
1. [RagAgentApp README](../RagAgentApp/README.md)
2. [Architecture](../RagAgentApp/ARCHITECTURE.md)
3. [Agent Setup](../RagAgentApp/AGENT_SETUP.md)

### For DevOps/Deployment
1. [Azure Deployment](deployment/AZURE_DEPLOYMENT.md)
2. [Authentication Guide](guides/AUTHENTICATION.md)
3. [Getting Started Guide](GETTING_STARTED.md)

## 🔍 Documentation Structure

```
docs/
├── README.md                           # This file - documentation index
├── GETTING_STARTED.md                  # Quick start guide
├── guides/                             # How-to guides and tutorials
│   ├── HACKATHON_GUIDE.md             # Hackathon implementation guide
│   └── AUTHENTICATION.md              # Azure authentication setup
└── deployment/                         # Deployment guides
    └── AZURE_DEPLOYMENT.md            # Azure Container Apps deployment

RagAgentApp/
├── README.md                          # Application overview
├── ARCHITECTURE.md                    # System architecture
├── AGENT_SETUP.md                     # Agent configuration
└── MIGRATION.md                       # Migration notes
```

## 📝 Documentation Guidelines

### What Goes Where

- **docs/GETTING_STARTED.md**: Quick setup for all users, minimal prerequisites
- **docs/guides/**: Step-by-step tutorials and how-to guides
- **docs/deployment/**: Production deployment instructions
- **RagAgentApp/**: Application-specific technical documentation

### Finding What You Need

**I want to...**
- ...run the app quickly → [Getting Started](GETTING_STARTED.md)
- ...implement agents for the hackathon → [Hackathon Guide](guides/HACKATHON_GUIDE.md)
- ...deploy to Azure → [Azure Deployment](deployment/AZURE_DEPLOYMENT.md)
- ...understand authentication → [Authentication Guide](guides/AUTHENTICATION.md)
- ...understand the architecture → [Architecture](../RagAgentApp/ARCHITECTURE.md)
- ...configure pre-created agents → [Agent Setup](../RagAgentApp/AGENT_SETUP.md)

## 🤝 Contributing to Documentation

When adding new documentation:
1. Place it in the appropriate directory (guides, deployment, etc.)
2. Update this index file
3. Add cross-references to related documents
4. Keep documentation focused and concise

## 📚 External Resources

- [Azure AI Foundry Documentation](https://learn.microsoft.com/azure/ai-studio/)
- [Azure AI Agent Service](https://learn.microsoft.com/azure/ai-services/agents/)
- [.NET 9.0 Documentation](https://learn.microsoft.com/dotnet/)
- [Blazor Documentation](https://learn.microsoft.com/aspnet/core/blazor/)

## Need Help?

- 📖 Check the relevant documentation above
- 🐛 [Open an issue](https://github.com/dbruun/sop-pp-hackathon/issues) for bugs
- 💬 [Start a discussion](https://github.com/dbruun/sop-pp-hackathon/discussions) for questions
