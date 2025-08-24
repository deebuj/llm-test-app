using OllamaChatApp.Models;
using System.Text.Json;

namespace OllamaChatApp.Services;

public class OllamaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaService> _logger;
    private readonly ConversationService _conversationService;
    private readonly ToolDetectionService _toolDetectionService;
    private readonly string _baseUrl;
    private readonly string _defaultModel;

    public OllamaService(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaService> logger, ConversationService conversationService, ToolDetectionService toolDetectionService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _conversationService = conversationService;
        _toolDetectionService = toolDetectionService;
        _baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _defaultModel = configuration["Ollama:DefaultModel"] ?? "llama3.2:latest";
        
        var timeoutSeconds = configuration.GetValue<int>("Ollama:TimeoutSeconds", 30);
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    public async Task<ChatResponse> GenerateResponseAsync(string message, string? model = null, string? sessionId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var modelToUse = model ?? _defaultModel;
            _logger.LogInformation("Generating response for message with model: {Model}, SessionId: {SessionId}", modelToUse, sessionId);

            // If no session ID provided, create a new one for a single interaction
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = _conversationService.CreateSession(modelToUse);
            }

            // Detect and execute tool calls
            var toolCalls = await _toolDetectionService.DetectAndExecuteToolsAsync(message, cancellationToken);

            // Add user message to conversation
            _conversationService.AddMessage(sessionId, "user", message);

            // Prepare the message for the LLM
            var llmMessage = message;

            // If we have tool results, add them as context
            if (toolCalls.Any(tc => tc.Success))
            {
                var toolContext = _toolDetectionService.FormatToolResultsForLLM(toolCalls);
                
                // Add tool context as a system message before the user message
                var contextMessage = $"[SYSTEM CONTEXT]\n{toolContext}\n[USER MESSAGE]\n{message}";
                llmMessage = contextMessage;

                // Also add tool call information to conversation history for future reference
                foreach (var toolCall in toolCalls.Where(tc => tc.Success))
                {
                    var toolMessage = $"[Tool: {toolCall.ToolName}] {_toolDetectionService.FormatToolResultsForLLM(new List<ToolCall> { toolCall })}";
                    _conversationService.AddMessage(sessionId, "system", toolMessage);
                }
            }

            // Get conversation history
            var messages = _conversationService.GetMessages(sessionId);

            // For the LLM call, we'll use a modified approach:
            // - Include previous conversation context
            // - Add the current message with tool context if available
            var ollamaMessages = new List<object>();

            // Add previous messages (but filter out system tool messages to avoid clutter)
            var conversationMessages = messages.Where(m => 
                m.Role != "system" || !m.Content.StartsWith("[Tool:")).ToList();

            foreach (var msg in conversationMessages)
            {
                ollamaMessages.Add(new { role = msg.Role, content = msg.Content });
            }

            // Replace the last user message with our enhanced version if we have tool context
            if (toolCalls.Any(tc => tc.Success) && ollamaMessages.Count > 0)
            {
                var lastMessage = ollamaMessages.Last();
                ollamaMessages[ollamaMessages.Count - 1] = new { role = "user", content = llmMessage };
            }

            var ollamaRequest = new
            {
                model = modelToUse,
                messages = ollamaMessages.ToArray(),
                stream = false
            };

            var json = JsonSerializer.Serialize(ollamaRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/chat", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Ollama API returned error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return new ChatResponse
                {
                    Response = "",
                    Model = modelToUse,
                    Success = false,
                    SessionId = sessionId,
                    MessageCount = messages.Count,
                    ToolCalls = toolCalls
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var ollamaResponse = JsonSerializer.Deserialize<OllamaChatResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (ollamaResponse == null || ollamaResponse.Message == null)
            {
                _logger.LogError("Failed to deserialize Ollama response");
                return new ChatResponse
                {
                    Response = "",
                    Model = modelToUse,
                    Success = false,
                    SessionId = sessionId,
                    MessageCount = messages.Count,
                    ToolCalls = toolCalls
                };
            }

            // Add assistant response to conversation
            _conversationService.AddMessage(sessionId, "assistant", ollamaResponse.Message.Content);

            _logger.LogInformation("Successfully generated response with model: {Model}, Tools used: {ToolCount}", ollamaResponse.Model, toolCalls.Count);

            return new ChatResponse
            {
                Response = ollamaResponse.Message.Content ?? "No response",
                Model = ollamaResponse.Model ?? modelToUse,
                Success = true,
                SessionId = sessionId,
                MessageCount = messages.Count + 1, // +1 for the assistant response just added
                ToolCalls = toolCalls
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response");
            return new ChatResponse
            {
                Response = "",
                Model = model ?? _defaultModel,
                Success = false,
                SessionId = sessionId,
                MessageCount = 0,
                ToolCalls = new List<ToolCall>()
            };
        }
    }

    // Keep the original method for backward compatibility
    public async Task<ChatResponse> GenerateResponseAsync(string message, string? model = null, CancellationToken cancellationToken = default)
    {
        return await GenerateResponseAsync(message, model, null, cancellationToken);
    }
}
