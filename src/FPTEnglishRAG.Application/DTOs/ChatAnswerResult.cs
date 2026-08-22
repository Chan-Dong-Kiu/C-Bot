namespace FPTEnglishRAG.Application.DTOs;

/// <summary>
/// Result returned after attempting to generate a grounded answer.
/// </summary>
/// <param name="Answer">The generated answer text.</param>
/// <param name="Citations">The citations attached to the answer.</param>
/// <param name="IsGrounded">Whether the answer is grounded in retrieved source chunks.</param>
public sealed record ChatAnswerResult(
    string Answer,
    IReadOnlyList<Citation> Citations,
    bool IsGrounded)
{
    /// <summary>
    /// Creates a result for cases where retrieved materials are insufficient.
    /// </summary>
    /// <returns>An ungrounded answer result with no citations.</returns>
    public static ChatAnswerResult NotGrounded() =>
        new("The provided materials do not contain enough information to answer this question.", Array.Empty<Citation>(), false);
}
