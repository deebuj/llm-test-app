using OllamaChatApp.Models;
using System.Text.Json;

namespace OllamaChatApp.Services;

public class OllamaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaService> _logger;
    private readonly ConversationService _conversationService;
    private readonly string _baseUrl;
    private readonly string _defaultModel;

    public OllamaService(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaService> logger, ConversationService conversationService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _conversationService = conversationService;
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

            // Add user message to conversation
            _conversationService.AddMessage(sessionId, "user", message);

            // Get conversation history
            var messages = _conversationService.GetMessages(sessionId);

            // Prepare messages for Ollama chat API
            var ollamaMessages = messages.Select(m => new
            {
                role = m.Role,
                content = m.Content
            }).ToArray();

            var ollamaRequest = new
            {
                model = modelToUse,
                messages = ollamaMessages,
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
                    MessageCount = messages.Count
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
                    MessageCount = messages.Count
                };
            }

            // Add assistant response to conversation
            _conversationService.AddMessage(sessionId, "assistant", ollamaResponse.Message.Content);

            _logger.LogInformation("Successfully generated response with model: {Model}", ollamaResponse.Model);

            return new ChatResponse
            {
                Response = ollamaResponse.Message.Content ?? "No response",
                Model = ollamaResponse.Model ?? modelToUse,
                Success = true,
                SessionId = sessionId,
                MessageCount = messages.Count + 1 // +1 for the assistant response just added
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
                MessageCount = 0
            };
        }
    }

    // Keep the original method for backward compatibility
    public async Task<ChatResponse> GenerateResponseAsync(string message, string? model = null, CancellationToken cancellationToken = default)
    {
        return await GenerateResponseAsync(message, model, null, cancellationToken);
    }
}
