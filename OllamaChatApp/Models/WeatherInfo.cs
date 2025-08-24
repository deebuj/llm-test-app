namespace OllamaChatApp.Models;

public class WeatherInfo
{
    public string Location { get; set; } = "";
    public double Temperature { get; set; }
    public string Description { get; set; } = "";
    public double FeelsLike { get; set; }
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
