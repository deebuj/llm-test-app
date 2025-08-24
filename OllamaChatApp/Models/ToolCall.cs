namespace OllamaChatApp.Models;

public class ToolCall
{
    public string ToolName { get; set; } = "";
    public string Query { get; set; } = "";
    public object? Result { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}
