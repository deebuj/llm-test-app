using Microsoft.AspNetCore.Mvc;
using OllamaChatApp.Models;
using OllamaChatApp.Services;

namespace OllamaChatApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly OllamaService _ollamaService;
    private readonly ConversationService _conversationService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(OllamaService ollamaService, ConversationService conversationService, ILogger<ChatController> logger)
    {
        _ollamaService = ollamaService;
        _conversationService = conversationService;
        _logger = logger;
    }

    /// <summary>
    /// Send a chat message to Ollama and get a response with conversation context
    /// </summary>
    /// <param name="request">The chat request containing the message and optional session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat response from Ollama with session information</returns>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> PostChat(
        [FromBody] ChatRequest request, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            _logger.LogWarning("Empty message received");
            return BadRequest(new { error = "Message cannot be empty" });
        }

        _logger.LogInformation("Processing chat request for SessionId: {SessionId}", request.SessionId);

        var response = await _ollamaService.GenerateResponseAsync(
            request.Message, 
            request.Model, 
            request.SessionId, 
            cancellationToken);

        if (!response.Success)
        {
            _logger.LogError("Chat request failed");
            return StatusCode(500, new { error = "Failed to generate response" });
        }

        _logger.LogInformation("Chat request successful for SessionId: {SessionId}", response.SessionId);
        return Ok(response);
    }

    /// <summary>
    /// Create a new conversation session
    /// </summary>
    /// <param name="model">Optional model to use for the session</param>
    /// <returns>New session ID</returns>
    [HttpPost("session")]
    public ActionResult<object> CreateSession([FromQuery] string? model = null)
    {
        var sessionId = _conversationService.CreateSession(model);
        _logger.LogInformation("Created new session: {SessionId}", sessionId);
        
        return Ok(new { sessionId });
    }

    /// <summary>
    /// Get conversation history for a session
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <returns>List of messages in the conversation</returns>
    [HttpGet("session/{sessionId}/history")]
    public ActionResult<object> GetSessionHistory(string sessionId)
    {
        var session = _conversationService.GetSession(sessionId);
        if (session == null)
        {
            return NotFound(new { error = "Session not found" });
        }

        return Ok(new 
        { 
            sessionId = session.SessionId,
            model = session.Model,
            createdAt = session.CreatedAt,
            lastUpdatedAt = session.LastUpdatedAt,
            messageCount = session.Messages.Count,
            messages = session.Messages
        });
    }

    /// <summary>
    /// Clear conversation history for a session
    /// </summary>
    /// <param name="sessionId">The session ID to clear</param>
    /// <returns>Success confirmation</returns>
    [HttpDelete("session/{sessionId}")]
    public ActionResult ClearSession(string sessionId)
    {
        _conversationService.ClearSession(sessionId);
        _logger.LogInformation("Cleared session: {SessionId}", sessionId);
        
        return Ok(new { message = "Session cleared successfully" });
    }
}
