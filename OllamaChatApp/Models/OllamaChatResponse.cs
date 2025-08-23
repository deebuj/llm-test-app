namespace OllamaChatApp.Models;

public record OllamaChatResponse(
    string Model,
    DateTime Created_at,
    OllamaChatMessage Message,
    bool Done,
    string? Done_reason = null,
    long? Total_duration = null,
    long? Load_duration = null,
    int? Prompt_eval_count = null,
    long? Prompt_eval_duration = null,
    int? Eval_count = null,
    long? Eval_duration = null
);

public record OllamaChatMessage(
    string Role,
    string Content
);
