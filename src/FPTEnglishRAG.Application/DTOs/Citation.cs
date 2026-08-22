namespace FPTEnglishRAG.Application.DTOs;

/// <summary>
/// Citation mapped from a source label to a retrieved chunk.
/// </summary>
/// <param name="Label">The source label, such as S1.</param>
/// <param name="ChunkId">The cited chunk identifier.</param>
public sealed record Citation(string Label, string ChunkId);
