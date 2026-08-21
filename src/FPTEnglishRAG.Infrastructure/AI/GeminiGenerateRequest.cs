namespace FPTEnglishRAG.Infrastructure.AI;

/// <summary>
/// Minimal request data for Gemini content generation.
/// </summary>
/// <param name="Model">The Gemini generation model name.</param>
/// <param name="Prompt">The prompt content sent to Gemini.</param>
/// <param name="Temperature">The generation temperature.</param>
/// <param name="MaxOutputTokens">The maximum number of output tokens.</param>
public sealed record GeminiGenerateRequest(
    string Model,
    string Prompt,
    double Temperature,
    int MaxOutputTokens);
