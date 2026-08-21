namespace FPTEnglishRAG.Application.DTOs;

/// <summary>
/// A source chunk retrieved for a user question.
/// </summary>
/// <param name="ChunkId">The stable chunk identifier.</param>
/// <param name="Content">The retrieved chunk text.</param>
/// <param name="SourceDocumentName">The display name of the source document.</param>
/// <param name="PageOrPosition">The page number or source position for citation display.</param>
/// <param name="SimilarityScore">The retrieval similarity score.</param>
public sealed record RetrievedChunk(
    string ChunkId,
    string Content,
    string SourceDocumentName,
    string PageOrPosition,
    double SimilarityScore);
