using OllamaChatApp.Models;
using System.Text.Json;

namespace OllamaChatApp.Services;

public class OllamaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaService> _logger;
    private readonly string _baseUrl;
    private readonly string _defaultModel;

    public OllamaService(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _defaultModel = configuration["Ollama:DefaultModel"] ?? "llama3.2:latest";
        
        var timeoutSeconds = configuration.GetValue<int>("Ollama:TimeoutSeconds", 30);
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    public async Task<ChatResponse> GenerateResponseAsync(string message, string? model = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var modelToUse = model ?? _defaultModel;
            _logger.LogInformation("Generating response for message with model: {Model}", modelToUse);

            var ollamaRequest = new
            {
                model = modelToUse,
                prompt = message,
                stream = false
            };

            var json = JsonSerializer.Serialize(ollamaRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Ollama API returned error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return new ChatResponse
                {
                    Response = "",
                    Model = modelToUse,
                    Success = false
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (ollamaResponse == null)
            {
                _logger.LogError("Failed to deserialize Ollama response");
                return new ChatResponse
                {
                    Response = "",
                    Model = modelToUse,
                    Success = false
                };
            }

            _logger.LogInformation("Successfully generated response with model: {Model}", ollamaResponse.Model);

            return new ChatResponse
            {
                Response = ollamaResponse.Response ?? "No response",
                Model = ollamaResponse.Model ?? modelToUse,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response");
            return new ChatResponse
            {
                Response = "",
                Model = model ?? _defaultModel,
                Success = false
            };
        }
    }
}
