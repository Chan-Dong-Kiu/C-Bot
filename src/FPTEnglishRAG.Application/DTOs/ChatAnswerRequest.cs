using FPTEnglishRAG.Domain.Enums;

namespace FPTEnglishRAG.Application.DTOs;

/// <summary>
/// Request data needed to generate a grounded chat answer.
/// </summary>
/// <param name="Question">The user's current question.</param>
/// <param name="RetrievedChunks">The retrieved source chunks for the current question.</param>
/// <param name="RecentMessages">A bounded snapshot of recent conversation messages.</param>
/// <param name="RelevanceThreshold">The minimum retrieval score required for grounded answering.</param>
/// <param name="GroundingMode">Whether generation must use sources or may use labeled general knowledge.</param>
public sealed record ChatAnswerRequest(
    string Question,
    IReadOnlyList<RetrievedChunk> RetrievedChunks,
    IReadOnlyList<ChatMessageSnapshot> RecentMessages,
    double RelevanceThreshold,
    AnswerGroundingMode GroundingMode = AnswerGroundingMode.Grounded);
