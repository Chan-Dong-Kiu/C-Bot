// File: src/FPTEnglishRAG.Application/Testing/FakeGeminiService.cs

using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Application.DTOs;

namespace FPTEnglishRAG.Application.Testing;

/// <summary>
/// Fake implementation of <see cref="IGeminiService"/> for development and unit testing.
/// </summary>
/// <remarks>
/// <para>
/// This class must NOT be used in production. It exists solely to allow:
/// <list type="bullet">
///   <item>M1 (Chat UI) to develop and test the full conversation flow without a live Gemini API key.</item>
///   <item>Unit and integration tests to run offline and deterministically.</item>
/// </list>
/// </para>
/// <para>
/// Grounding logic mirrors the real pipeline threshold check so the Chat UI can exercise
/// both the grounded-answer path and the not-grounded fallback path during development.
/// </para>
/// <para>
/// No HTTP calls, no delays, and no exceptions are thrown for valid inputs.
/// </para>
/// </remarks>
public sealed class FakeGeminiService : IGeminiService
{
    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Returns <see cref="ChatAnswerResult.NotGrounded()"/> when either:
    /// <list type="bullet">
    ///   <item><paramref name="request"/><c>.RetrievedChunks</c> is empty, or</item>
    ///   <item>no chunk has <c>SimilarityScore &gt;= request.RelevanceThreshold</c>.</item>
    /// </list>
    /// </para>
    /// <para>
    /// Otherwise returns a grounded result. The <c>Answer</c> is a fixed marker string so tests
    /// can assert on it without parsing natural language. <c>Citations</c> are auto-generated
    /// in retrieval-rank order: the first chunk above threshold receives label <c>S1</c>,
    /// the second <c>S2</c>, and so on.
    /// </para>
    /// </remarks>
    public Task<ChatAnswerResult> GenerateAnswerAsync(
        ChatAnswerRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Filter to chunks that meet the relevance threshold.
        var relevantChunks = request.RetrievedChunks
            .Where(c => c.SimilarityScore >= request.RelevanceThreshold)
            .ToList();

        if (relevantChunks.Count == 0)
            return Task.FromResult(ChatAnswerResult.NotGrounded());

        // Build citations in rank order: S1, S2, ... matching the filtered list.
        var citations = relevantChunks
            .Select((chunk, index) => new Citation($"S{index + 1}", chunk.ChunkId))
            .ToList();

        var result = new ChatAnswerResult(
            Answer: $"[FAKE] Trả lời giả cho: {request.Question}",
            Citations: citations,
            IsGrounded: true);

        return Task.FromResult(result);
    }
}
