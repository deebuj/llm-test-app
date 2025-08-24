namespace OllamaChatApp.Models;

public class ChatResponse
{
    public string Response { get; set; } = "";
    public string Model { get; set; } = "";
    public bool Success { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? SessionId { get; set; }
    public int MessageCount { get; set; } = 0;
    public List<ToolCall> ToolCalls { get; set; } = new();
}
