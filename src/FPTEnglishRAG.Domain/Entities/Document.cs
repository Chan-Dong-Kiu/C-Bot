using FPTEnglishRAG.Domain.Enums;

namespace FPTEnglishRAG.Domain.Entities;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int PageCount { get; set; }
    public int ChunkCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();

    public void MarkExtracting()
    {
        Status = DocumentStatus.Extracting;
        ErrorCode = null;
        ErrorMessage = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkChunking(int pageCount)
    {
        Status = DocumentStatus.Chunking;
        PageCount = pageCount;
        ErrorCode = null;
        ErrorMessage = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkEmbedding(int chunkCount)
    {
        Status = DocumentStatus.Embedding;
        ChunkCount = chunkCount;
        ErrorCode = null;
        ErrorMessage = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkReady()
    {
        Status = DocumentStatus.Ready;
        ErrorCode = null;
        ErrorMessage = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(DocumentErrorCode errorCode, string errorMessage)
    {
        Status = DocumentStatus.Failed;
        ErrorCode = errorCode.ToString();
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }
}
