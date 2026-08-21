namespace FPTEnglishRAG.Infrastructure.Persistence.Entities;

public sealed class ChunkEntity
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int Ordinal { get; set; }
    public int PageStart { get; set; }
    public int PageEnd { get; set; }
    public string? Section { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public int TokenCount { get; set; }
    public DocumentEntity Document { get; set; } = null!;
    public EmbeddingEntity? Embedding { get; set; }
}
