namespace FPTEnglishRAG.Infrastructure.AI;

/// <summary>
/// Low-level Gemini REST API client boundary.
/// </summary>
public interface IGeminiClient
{
    /// <summary>
    /// Generates text content from a Gemini prompt request.
    /// </summary>
    /// <param name="request">The generation request.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The Gemini generation response.</returns>
    Task<GeminiGenerateResponse> GenerateContentAsync(
        GeminiGenerateRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Embeds text content using a Gemini embedding request.
    /// </summary>
    /// <param name="request">The embedding request.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The Gemini embedding response.</returns>
    Task<GeminiEmbedResponse> EmbedContentAsync(
        GeminiEmbedRequest request,
        CancellationToken cancellationToken);
}
