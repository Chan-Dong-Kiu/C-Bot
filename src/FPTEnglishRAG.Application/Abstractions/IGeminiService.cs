using FPTEnglishRAG.Application.DTOs;

namespace FPTEnglishRAG.Application.Abstractions;

/// <summary>
/// Generates grounded chat answers from retrieved source chunks.
/// </summary>
public interface IGeminiService
{
    /// <summary>
    /// Generates an answer for a chat request using retrieved context and bounded conversation history.
    /// </summary>
    /// <param name="request">The chat answer request.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The generated answer result.</returns>
    Task<ChatAnswerResult> GenerateAnswerAsync(
        ChatAnswerRequest request,
        CancellationToken cancellationToken);
}
