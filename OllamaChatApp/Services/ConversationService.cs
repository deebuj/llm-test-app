using OllamaChatApp.Models;
using System.Collections.Concurrent;

namespace OllamaChatApp.Services;

public class ConversationService
{
    private readonly ConcurrentDictionary<string, ConversationSession> _sessions = new();
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(ILogger<ConversationService> logger)
    {
        _logger = logger;
    }

    public string CreateSession(string? model = null)
    {
        var sessionId = Guid.NewGuid().ToString();
        var session = new ConversationSession
        {
            SessionId = sessionId,
            Model = model ?? "",
            Messages = new List<ChatMessage>()
        };

        _sessions[sessionId] = session;
        _logger.LogInformation("Created new conversation session: {SessionId}", sessionId);
        
        return sessionId;
    }

    public ConversationSession? GetSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    public void AddMessage(string sessionId, string role, string content)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Messages.Add(new ChatMessage
            {
                Role = role,
                Content = content
            });
            session.LastUpdatedAt = DateTime.UtcNow;
            _logger.LogDebug("Added {Role} message to session {SessionId}", role, sessionId);
        }
    }

    public List<ChatMessage> GetMessages(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return session.Messages;
        }
        return new List<ChatMessage>();
    }

    public void ClearSession(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out _))
        {
            _logger.LogInformation("Cleared conversation session: {SessionId}", sessionId);
        }
    }

    public void CleanupOldSessions(TimeSpan maxAge)
    {
        var cutoffTime = DateTime.UtcNow - maxAge;
        var sessionsToRemove = _sessions
            .Where(kvp => kvp.Value.LastUpdatedAt < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var sessionId in sessionsToRemove)
        {
            _sessions.TryRemove(sessionId, out _);
            _logger.LogDebug("Removed expired session: {SessionId}", sessionId);
        }

        if (sessionsToRemove.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired sessions", sessionsToRemove.Count);
        }
    }
}
