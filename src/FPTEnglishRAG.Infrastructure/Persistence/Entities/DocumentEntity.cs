using FPTEnglishRAG.Domain.Enums;

namespace FPTEnglishRAG.Infrastructure.Persistence.Entities;

public sealed class DocumentEntity
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; }
    public string? ErrorCode { get; set; }
    public int PageCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<ChunkEntity> Chunks { get; set; } = new List<ChunkEntity>();
}
