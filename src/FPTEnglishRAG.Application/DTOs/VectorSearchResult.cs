namespace FPTEnglishRAG.Application.DTOs;

public sealed record VectorSearchResult(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentName,
    int Ordinal,
    int PageStart,
    int PageEnd,
    string? Section,
    string Content,
    double Score);
