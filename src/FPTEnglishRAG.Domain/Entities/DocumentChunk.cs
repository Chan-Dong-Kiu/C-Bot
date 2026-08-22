namespace FPTEnglishRAG.Domain.Entities;

public class DocumentChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }
    public int Ordinal { get; set; }
    public int PageStart { get; set; }
    public int PageEnd { get; set; }
    public string? Section { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public int TokenCount { get; set; }
}
