using FPTEnglishRAG.Application.DTOs;

namespace FPTEnglishRAG.Application.Abstractions;

/// <summary>
/// Generates grounded chat answers from retrieved source chunks.
/// </summary>
public interface IGeminiService
{
    Task<ChatAnswerResult> GenerateAnswerAsync(
        ChatAnswerRequest request,
        CancellationToken cancellationToken);
}
