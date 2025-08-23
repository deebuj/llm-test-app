namespace OllamaChatApp.Models;

public class ConversationSession
{
    public string SessionId { get; set; } = "";
    public List<ChatMessage> Messages { get; set; } = new();
    public string Model { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
