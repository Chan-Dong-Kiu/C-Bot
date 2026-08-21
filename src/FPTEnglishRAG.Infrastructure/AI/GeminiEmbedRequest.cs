namespace FPTEnglishRAG.Infrastructure.AI;

/// <summary>
/// Minimal request data for Gemini embeddings.
/// </summary>
/// <param name="Model">The Gemini embedding model name.</param>
/// <param name="Content">The text content to embed.</param>
public sealed record GeminiEmbedRequest(string Model, string Content);
