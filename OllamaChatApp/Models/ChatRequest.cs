namespace OllamaChatApp.Models;

public record ChatRequest(string Message, string? SessionId = null, string? Model = null);
