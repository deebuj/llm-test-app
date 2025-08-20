namespace OllamaChatApp.Models;

public record OllamaResponse(
    string Model,
    DateTime Created_at,
    string Response,
    bool Done,
    string? Done_reason = null,
    int[]? Context = null,
    long? Total_duration = null,
    long? Load_duration = null,
    int? Prompt_eval_count = null,
    long? Prompt_eval_duration = null,
    int? Eval_count = null,
    long? Eval_duration = null
);
