# Branch Instructions for Hackathon Version

## Important Note

**This PR should NOT be merged into the master/main branch.**

Instead, these changes should be merged into a new branch called `hackathon` which will serve as the starting point for hackathon participants.

## How to Create the Hackathon Branch

### Option 1: From GitHub UI (Recommended)

1. Go to the repository on GitHub
2. Click on the branch dropdown (usually shows "main" or "master")
3. Type "hackathon" in the text field
4. Click "Create branch: hackathon from 'main'"
5. Change the base branch of this PR to `hackathon`
6. Merge this PR into the `hackathon` branch

### Option 2: From Command Line

```bash
# Fetch the latest changes
git fetch origin

# Create the hackathon branch from main
git checkout main
git pull origin main
git checkout -b hackathon

# Push the new branch
git push origin hackathon

# Now merge this PR into hackathon through GitHub UI
```

## Why a Separate Branch?

The `hackathon` branch contains:
- ✅ Stubbed agent implementations for learning
- ✅ Educational TODO comments throughout
- ✅ Simplified code without Azure dependencies initially
- ✅ Complete HACKATHON.md guide

The `main` branch should keep:
- ✅ Fully implemented agents with Azure AI
- ✅ Production-ready code
- ✅ Complete Azure integration
- ✅ Real RAG functionality

## For Hackathon Participants

Participants should:
1. Fork or clone the repository
2. Check out the `hackathon` branch
3. Follow the HACKATHON.md guide
4. Implement the agents step by step
5. Learn about Azure AI Foundry and agents

## After the Hackathon

The `hackathon` branch can be:
- Kept as a learning resource
- Used for future workshops
- Referenced in tutorials
- Used as a template for new projects

The `main` branch continues with production code and can receive updates independently.
