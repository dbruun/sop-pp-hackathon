using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace RagAgentApp.Agents;

public class SopRagAgent : IAgentService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletionService;

    public string AgentName => "SOP Agent";

    public SopRagAgent(Kernel kernel)
    {
        _kernel = kernel;
        _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
    }

    public async Task<string> ProcessQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a system prompt for the SOP agent
            var systemPrompt = @"You are a Standard Operating Procedures (SOP) expert assistant. 
Your role is to help users understand and find information about standard operating procedures, 
work instructions, and process documentation. Provide clear, structured responses based on 
standard operating procedures knowledge. If you don't have specific information, acknowledge 
that and provide general guidance on SOPs.";

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
            return $"Error processing SOP query: {ex.Message}";
        }
    }
}
