namespace FPTEnglishRAG.Infrastructure.AI;

/// <summary>
/// Minimal response data returned from Gemini embeddings.
/// </summary>
/// <param name="Vector">The embedding vector.</param>
public sealed record GeminiEmbedResponse(float[] Vector);
