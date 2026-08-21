namespace FPTEnglishRAG.Application.DTOs;

public record ChunkResultDto(
    int Ordinal,
    int PageStart,
    int PageEnd,
    string? Section,
    string Content,
    string ContentHash,
    int TokenCount);
