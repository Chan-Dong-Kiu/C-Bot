namespace FPTEnglishRAG.Domain.ValueObjects;

public record Citation(
    string Label,
    string DocumentName,
    int? Page,
    string Snippet,
    Guid DocumentId,
    Guid ChunkId);
