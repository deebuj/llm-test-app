using Microsoft.AspNetCore.Mvc;
using OllamaChatApp.Models;
using OllamaChatApp.Services;

namespace OllamaChatApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly OllamaService _ollamaService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(OllamaService ollamaService, ILogger<ChatController> logger)
    {
        _ollamaService = ollamaService;
        _logger = logger;
    }

    /// <summary>
    /// Send a chat message to Ollama and get a response
    /// </summary>
    /// <param name="request">The chat request containing the message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat response from Ollama</returns>
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

        _logger.LogInformation("Processing chat request");

        var response = await _ollamaService.GenerateResponseAsync(request.Message, cancellationToken: cancellationToken);

        if (!response.Success)
        {
            _logger.LogError("Chat request failed");
            return StatusCode(500, new { error = "Failed to generate response" });
        }

        _logger.LogInformation("Chat request successful");
        return Ok(response);
    }
}
