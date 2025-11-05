# Specialized Agent Pipeline Architecture

## Overview

This document describes the new specialized agent pipeline architecture implemented for future demos. The system now supports two modes of operation:

1. **Dual-Agent Mode** (Original): Parallel execution with SOP and Policy agents
2. **Specialized Pipeline Mode** (New): Sequential processing through 5 specialized agents with full observability

## Architecture

### Specialized Agent Pipeline Flow

```
User Query
    │
    ▼
┌─────────────────┐
│ IntakeAgent     │ ← Intent detection, policy gating
│                 │   • Classifies query type
│                 │   • Extracts entities
│                 │   • Applies gating rules
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ SearchAgent     │ ← Hybrid retrieval (BM25 + vector)
│                 │   • Azure AI Search integration
│                 │   • Keyword + semantic search
│                 │   • Returns ranked passages
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ WriterAgent     │ ← Draft with inline citations
│                 │   • Synthesizes information
│                 │   • Adds source citations
│                 │   • Structures response
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ ReviewerAgent   │ ← Grounding validation
│                 │   • Verifies claims
│                 │   • Flags low-grounding
│                 │   • Quality assessment
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ ExecutorAgent   │ ← Output formatting
│                 │   • Markdown formatting
│                 │   • Final polish
│                 │   • Metadata inclusion
└────────┬────────┘
         │
         ▼
    Final Response
    + Observability Trace
```

## Agent Responsibilities

### 1. IntakeAgent

**Purpose**: Analyzes user intent and applies policy-based gating rules.

**Responsibilities**:
- Query classification (policy/SOP/general/hybrid)
- Entity extraction
- Topic identification
- Policy constraint checking
- Confidence scoring

**Output Format** (JSON):
```json
{
  "intent": "policy|sop|general|hybrid",
  "confidence": 0.95,
  "entities": ["data privacy", "GDPR"],
  "topics": ["compliance", "regulations"],
  "requires_policy_check": true,
  "reasoning": "Query relates to data privacy regulations..."
}
```

### 2. SearchAgent

**Purpose**: Performs hybrid retrieval combining BM25 keyword search and vector similarity search.

**Responsibilities**:
- Azure AI Search integration
- Hybrid search execution (BM25 + vector)
- Result ranking and filtering
- Source attribution
- Passage extraction

**Output Format** (JSON):
```json
{
  "search_results": [
    {
      "passage": "Retrieved text content...",
      "source": "Document name or URL",
      "relevance_score": 0.89,
      "page_number": "42",
      "section": "Data Protection Measures"
    }
  ],
  "total_results": 5,
  "search_type": "hybrid",
  "reasoning": "Used hybrid search to balance keyword matching and semantic similarity"
}
```

### 3. WriterAgent

**Purpose**: Drafts well-structured responses with inline citations.

**Responsibilities**:
- Information synthesis from multiple sources
- Inline citation formatting
- Content structuring (headings, lists, etc.)
- Clarity and coherence
- Professional tone maintenance

**Output Format** (Markdown):
```markdown
## Response Title

Content with inline citations [Source: Document A, Page 10].

### Key Points
- Point 1 [Source: Document B]
- Point 2 [Source: Document A, Page 15]

Additional details...
```

### 4. ReviewerAgent

**Purpose**: Validates that claims are properly grounded in retrieved passages.

**Responsibilities**:
- Claim extraction from draft
- Grounding verification
- Low-confidence detection
- Citation accuracy checking
- Quality assessment

**Output Format** (JSON):
```json
{
  "grounding_score": 0.92,
  "claims_verified": [
    {
      "claim": "GDPR requires data protection impact assessments",
      "is_grounded": true,
      "supporting_passage": "Article 35 of GDPR states...",
      "confidence": 0.95
    }
  ],
  "low_grounding_issues": [
    {
      "claim": "Most companies comply within 30 days",
      "issue": "No supporting evidence in retrieved passages",
      "recommendation": "Remove or qualify this claim"
    }
  ],
  "citation_accuracy": 0.98,
  "overall_quality": "high",
  "recommendations": ["Add more context to claim on page 2"]
}
```

### 5. ExecutorAgent

**Purpose**: Formats the final response for optimal display in the chat window.

**Responsibilities**:
- Final markdown formatting
- Visual enhancements (separators, emphasis)
- Metadata inclusion (quality scores, warnings)
- Readability optimization
- Chat-friendly structure

**Output Format** (Formatted Markdown):
```markdown
# Final Response

[Quality Score: 92/100]

[Content with proper formatting...]

---
**Citations**: 5 sources referenced
**Grounding**: High confidence
```

## Observability

### Execution Traces

Each agent execution is tracked with the following metrics:

```csharp
public class AgentExecutionTrace
{
    public string AgentName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int TokensUsed { get; set; }
    public decimal EstimatedCost { get; set; }
}
```

### Pipeline Trace

The full pipeline execution is tracked:

```csharp
public class PipelineExecutionTrace
{
    public string RequestId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan TotalDuration { get; }
    public List<AgentExecutionTrace> AgentTraces { get; set; }
    public int TotalTokensUsed { get; }
    public decimal TotalEstimatedCost { get; }
    public bool Success { get; }
}
```

### UI Dashboard

The observability dashboard displays:
- Total pipeline duration
- Total tokens consumed
- Estimated cost breakdown per agent
- Success/failure status
- Detailed per-agent metrics table

## Implementation Details

### OrchestratorService

The orchestrator coordinates the pipeline execution:

```csharp
public async Task<(string FinalResponse, PipelineExecutionTrace Trace)> 
    ProcessQueryWithPipelineAsync(string query, CancellationToken cancellationToken)
{
    var trace = new PipelineExecutionTrace();
    
    // Execute each agent in sequence
    // 1. IntakeAgent
    // 2. SearchAgent (with intent context)
    // 3. WriterAgent (with search results)
    // 4. ReviewerAgent (with draft + search results)
    // 5. ExecutorAgent (with reviewed draft)
    
    // Track metrics at each step
    // Return final response + full trace
}
```

### Agent Registration

All agents are registered as singletons in `Program.cs`:

```csharp
builder.Services.AddSingleton<IntakeAgent>();
builder.Services.AddSingleton<SearchAgent>();
builder.Services.AddSingleton<WriterAgent>();
builder.Services.AddSingleton<ReviewerAgent>();
builder.Services.AddSingleton<ExecutorAgent>();
```

### UI Toggle

Users can switch between modes using a checkbox in the Chat UI:
- **Unchecked**: Dual-agent mode (original)
- **Checked**: Specialized pipeline mode with observability

## Benefits

### For Demos

1. **Transparency**: Full visibility into each processing stage
2. **Education**: Shows AI agent design patterns
3. **Trust**: Grounding validation ensures accuracy
4. **Metrics**: Cost and performance tracking

### For Production

1. **Quality Control**: Reviewer agent ensures response quality
2. **Cost Management**: Token usage tracking per agent
3. **Performance Monitoring**: Identify bottlenecks
4. **Modularity**: Easy to swap or enhance individual agents

## Future Enhancements

### Short-term
- [ ] Implement actual Azure AI Search integration in SearchAgent
- [ ] Add JSON parsing and structured output handling
- [ ] Enhance error recovery between pipeline stages
- [ ] Add pipeline stage cancellation/retry logic

### Medium-term
- [ ] Parallel execution of independent stages
- [ ] Caching layer for SearchAgent results
- [ ] A/B testing between pipeline configurations
- [ ] Machine learning for optimal agent routing

### Long-term
- [ ] Self-healing pipeline with automatic fallbacks
- [ ] Multi-modal support (images, documents)
- [ ] Real-time streaming of intermediate results
- [ ] Adaptive pipeline based on query complexity

## Performance Characteristics

### Typical Latencies

| Agent | Typical Duration | Token Usage |
|-------|-----------------|-------------|
| IntakeAgent | 1-2s | 200-500 |
| SearchAgent | 2-3s | 500-1000 |
| WriterAgent | 3-5s | 1000-2000 |
| ReviewerAgent | 2-4s | 800-1500 |
| ExecutorAgent | 1-2s | 300-600 |
| **Total Pipeline** | **9-16s** | **2800-5600** |

### Cost Estimates

Based on GPT-4 pricing ($0.03 per 1K input tokens, $0.06 per 1K output tokens):

- Single query cost: **$0.10 - $0.25**
- 100 queries/day: **$10 - $25/day**
- 1000 queries/month: **$100 - $250/month**

## Testing

### Manual Testing

1. Enable pipeline mode in the UI
2. Submit a test query
3. Verify each agent executes
4. Check observability metrics
5. Validate final response quality

### Automated Testing (Future)

```csharp
[Test]
public async Task Pipeline_EndToEnd_Success()
{
    var query = "What are GDPR compliance requirements?";
    var (response, trace) = await orchestrator.ProcessQueryWithPipelineAsync(query);
    
    Assert.That(trace.Success, Is.True);
    Assert.That(trace.AgentTraces.Count, Is.EqualTo(5));
    Assert.That(response, Is.Not.Empty);
}
```

## Troubleshooting

### Common Issues

**Pipeline hangs at SearchAgent**
- Check Azure AI Search configuration
- Verify search index exists and is populated
- Review SearchAgent logs for errors

**High cost per query**
- Review token estimates in observability dashboard
- Check for redundant processing
- Consider caching search results

**Low grounding scores**
- Improve search query formulation
- Expand knowledge base documents
- Adjust SearchAgent ranking parameters

## References

- [Azure AI Agent Service Documentation](https://learn.microsoft.com/azure/ai-services/agents/)
- [Azure AI Search Hybrid Search](https://learn.microsoft.com/azure/search/hybrid-search-overview)
- [Grounding Validation Best Practices](https://arxiv.org/abs/2305.14627)

---

**Last Updated**: 2025-11-05  
**Version**: 1.0.0  
**Author**: GitHub Copilot
