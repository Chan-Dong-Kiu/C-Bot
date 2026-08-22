namespace FPTEnglishRAG.Infrastructure.AI;

/// <summary>
/// Minimal response data returned from Gemini content generation.
/// </summary>
/// <param name="Text">The generated response text.</param>
/// <param name="FinishReason">The model finish reason.</param>
public sealed record GeminiGenerateResponse(string Text, string FinishReason);
