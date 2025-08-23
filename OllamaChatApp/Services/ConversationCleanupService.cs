using OllamaChatApp.Services;

namespace OllamaChatApp.Services;

public class ConversationCleanupService : BackgroundService
{
    private readonly ConversationService _conversationService;
    private readonly ILogger<ConversationCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval;
    private readonly TimeSpan _maxSessionAge;

    public ConversationCleanupService(ConversationService conversationService, ILogger<ConversationCleanupService> logger, IConfiguration configuration)
    {
        _conversationService = conversationService;
        _logger = logger;
        _cleanupInterval = TimeSpan.FromMinutes(configuration.GetValue<int>("Conversation:CleanupIntervalMinutes", 30));
        _maxSessionAge = TimeSpan.FromHours(configuration.GetValue<int>("Conversation:MaxSessionAgeHours", 24));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ConversationCleanupService started. Cleanup interval: {Interval}, Max session age: {MaxAge}", _cleanupInterval, _maxSessionAge);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _conversationService.CleanupOldSessions(_maxSessionAge);
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // This is expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during conversation cleanup");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes on error
            }
        }

        _logger.LogInformation("ConversationCleanupService stopped");
    }
}
