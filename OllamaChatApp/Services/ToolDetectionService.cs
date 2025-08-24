using OllamaChatApp.Models;
using System.Text.RegularExpressions;

namespace OllamaChatApp.Services;

public class ToolDetectionService
{
    private readonly WeatherService _weatherService;
    private readonly ILogger<ToolDetectionService> _logger;

    public ToolDetectionService(WeatherService weatherService, ILogger<ToolDetectionService> logger)
    {
        _weatherService = weatherService;
        _logger = logger;
    }

    public async Task<List<ToolCall>> DetectAndExecuteToolsAsync(string message, CancellationToken cancellationToken = default)
    {
        var toolCalls = new List<ToolCall>();

        // Detect weather-related queries
        if (IsWeatherQuery(message))
        {
            var location = ExtractLocation(message);
            if (!string.IsNullOrEmpty(location))
            {
                var toolCall = new ToolCall
                {
                    ToolName = "weather",
                    Query = location
                };

                try
                {
                    var weatherInfo = await _weatherService.GetWeatherAsync(location, cancellationToken);
                    if (weatherInfo != null)
                    {
                        toolCall.Result = weatherInfo;
                        toolCall.Success = true;
                        _logger.LogInformation("Successfully executed weather tool for location: {Location}", location);
                    }
                    else
                    {
                        toolCall.Success = false;
                        toolCall.Error = "Unable to fetch weather data";
                        _logger.LogWarning("Failed to fetch weather data for location: {Location}", location);
                    }
                }
                catch (Exception ex)
                {
                    toolCall.Success = false;
                    toolCall.Error = ex.Message;
                    _logger.LogError(ex, "Error executing weather tool for location: {Location}", location);
                }

                toolCalls.Add(toolCall);
            }
        }

        return toolCalls;
    }

    private bool IsWeatherQuery(string message)
    {
        var weatherKeywords = new[]
        {
            "weather", "temperature", "temp", "forecast", "rain", "sunny", "cloudy",
            "humidity", "wind", "climate", "hot", "cold", "warm", "cool"
        };

        var lowerMessage = message.ToLowerInvariant();
        return weatherKeywords.Any(keyword => lowerMessage.Contains(keyword));
    }

    private string ExtractLocation(string message)
    {
        // Pattern to match "weather in [location]" or "temperature in [location]", etc.
        var patterns = new[]
        {
            @"(?:weather|temperature|temp|forecast|climate)\s+in\s+([^?.!]+)",
            @"(?:weather|temperature|temp|forecast|climate)\s+for\s+([^?.!]+)",
            @"(?:weather|temperature|temp|forecast|climate)\s+at\s+([^?.!]+)",
            @"(?:what's|what\s+is)\s+the\s+(?:weather|temperature|temp)\s+in\s+([^?.!]+)",
            @"(?:how's|how\s+is)\s+the\s+(?:weather|temperature|temp)\s+in\s+([^?.!]+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var location = match.Groups[1].Value.Trim();
                // Clean up common suffixes
                location = Regex.Replace(location, @"\s*[?.!]*$", "");
                return location;
            }
        }

        // Fallback: look for city names after common phrases
        var cityPatterns = new[]
        {
            @"(?:weather|temperature|temp)\s.*?([A-Z][a-z]+(?:\s+[A-Z][a-z]+)*)",
            @"in\s+([A-Z][a-z]+(?:\s+[A-Z][a-z]+)*)"
        };

        foreach (var pattern in cityPatterns)
        {
            var matches = Regex.Matches(message, pattern);
            if (matches.Count > 0)
            {
                var lastMatch = matches[matches.Count - 1];
                if (lastMatch.Groups.Count > 1)
                {
                    return lastMatch.Groups[1].Value.Trim();
                }
            }
        }

        return "";
    }

    public string FormatToolResultsForLLM(List<ToolCall> toolCalls)
    {
        if (!toolCalls.Any()) return "";

        var context = "Here is some current information that may be relevant to your response:\n\n";

        foreach (var toolCall in toolCalls)
        {
            if (toolCall.Success && toolCall.Result != null)
            {
                switch (toolCall.ToolName)
                {
                    case "weather":
                        if (toolCall.Result is WeatherInfo weather)
                        {
                            context += $"Current weather information for {weather.Location}:\n";
                            context += $"- Temperature: {weather.Temperature:F1}°C (feels like {weather.FeelsLike:F1}°C)\n";
                            context += $"- Conditions: {weather.Description}\n";
                            context += $"- Humidity: {weather.Humidity}%\n";
                            context += $"- Wind Speed: {weather.WindSpeed:F1} m/s\n";
                            context += $"- Last updated: {weather.Timestamp:yyyy-MM-dd HH:mm:ss} UTC\n\n";
                        }
                        break;
                }
            }
            else if (!toolCall.Success)
            {
                context += $"Note: Unable to fetch {toolCall.ToolName} information";
                if (!string.IsNullOrEmpty(toolCall.Error))
                {
                    context += $" ({toolCall.Error})";
                }
                context += "\n\n";
            }
        }

        context += "Please use this information to provide a helpful and accurate response to the user's question.\n\n";
        return context;
    }
}
