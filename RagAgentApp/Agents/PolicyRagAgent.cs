using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace RagAgentApp.Agents;

public class PolicyRagAgent : IAgentService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletionService;

    public string AgentName => "Policy Agent";

    public PolicyRagAgent(Kernel kernel)
    {
        _kernel = kernel;
        _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
    }

    public async Task<string> ProcessQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a system prompt for the Policy agent
            var systemPrompt = @"You are a Policy expert assistant. Your role is to help users 
understand company policies, regulations, compliance requirements, and governance frameworks. 
Provide clear, authoritative responses based on policy knowledge. When discussing policies, 
cite relevant sections and explain implications. If you don't have specific policy information, 
acknowledge that and provide general policy guidance.";

            // Create chat history
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddUserMessage(query);

            // Get response from the chat completion service
            var response = await _chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                cancellationToken: cancellationToken);

            return response.Content ?? "I apologize, but I couldn't generate a response at this time.";
        }
        catch (Exception ex)
        {
            return $"Error processing Policy query: {ex.Message}";
        }
    }
}
