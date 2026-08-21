namespace FPTEnglishRAG.Application.DTOs;

/// <summary>
/// A single labeled source block built from a retrieved chunk for use in prompts and citation display.
/// </summary>
/// <param name="Label">
/// The deterministic source label used in the prompt and expected in model output, e.g. <c>S1</c>.
/// Labels are 1-based integers assigned in retrieval-rank order.
/// </param>
/// <param name="ChunkId">The stable identifier of the originating chunk.</param>
/// <param name="DocumentName">The display name of the source document.</param>
/// <param name="PageOrPosition">The page number or position string for citation display.</param>
/// <param name="Content">The full chunk content included in the context window.</param>
public sealed record SourceBlock(
    string Label,
    string ChunkId,
    string DocumentName,
    string PageOrPosition,
    string Content);
