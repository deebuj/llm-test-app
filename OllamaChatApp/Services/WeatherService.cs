using OllamaChatApp.Models;
using System.Text.Json;

namespace OllamaChatApp.Services;

public class WeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;
    private readonly string _baseUrl = "https://wttr.in";

    public WeatherService(HttpClient httpClient, IConfiguration configuration, ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Set a proper User-Agent header as required by wttr.in
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "OllamaChatApp/1.0");
    }

    public async Task<WeatherInfo?> GetWeatherAsync(string location, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching weather for location: {Location}", location);

            // wttr.in provides JSON format when we add ?format=j1
            var url = $"{_baseUrl}/{Uri.EscapeDataString(location)}?format=j1";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Weather API returned error: {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var weatherData = JsonSerializer.Deserialize<JsonElement>(json);

            // Parse wttr.in JSON response
            var currentCondition = weatherData.GetProperty("current_condition")[0];
            var nearestArea = weatherData.GetProperty("nearest_area")[0];

            var weather = new WeatherInfo
            {
                Location = $"{nearestArea.GetProperty("areaName")[0].GetProperty("value").GetString()}, {nearestArea.GetProperty("country")[0].GetProperty("value").GetString()}",
                Temperature = double.Parse(currentCondition.GetProperty("temp_C").GetString() ?? "0"),
                Description = currentCondition.GetProperty("weatherDesc")[0].GetProperty("value").GetString() ?? "",
                FeelsLike = double.Parse(currentCondition.GetProperty("FeelsLikeC").GetString() ?? "0"),
                Humidity = int.Parse(currentCondition.GetProperty("humidity").GetString() ?? "0"),
                WindSpeed = double.Parse(currentCondition.GetProperty("windspeedKmph").GetString() ?? "0") / 3.6 // Convert km/h to m/s
            };

            _logger.LogInformation("Successfully fetched weather for {Location}", weather.Location);
            return weather;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather for location: {Location}", location);
            return null;
        }
    }
}
