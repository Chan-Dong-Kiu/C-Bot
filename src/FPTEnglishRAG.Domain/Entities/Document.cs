using FPTEnglishRAG.Domain.Enums;

namespace FPTEnglishRAG.Domain.Entities;

public sealed class Document
{
    public Document(
        Guid id,
        string displayName,
        string storedPath,
        string mimeType,
        string sha256,
        DocumentStatus status,
        int pageCount,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        IReadOnlyCollection<DocumentChunk>? chunks = null,
        string? errorCode = null)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(storedPath))
        {
            throw new ArgumentException("Stored path is required.", nameof(storedPath));
        }

        if (string.IsNullOrWhiteSpace(mimeType))
        {
            throw new ArgumentException("MIME type is required.", nameof(mimeType));
        }

        if (string.IsNullOrWhiteSpace(sha256))
        {
            throw new ArgumentException("SHA-256 hash is required.", nameof(sha256));
        }

        if (pageCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageCount));
        }

        Id = id;
        DisplayName = displayName;
        StoredPath = storedPath;
        MimeType = mimeType;
        Sha256 = sha256;
        Status = status;
        ErrorCode = errorCode;
        PageCount = pageCount;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Chunks = chunks ?? Array.Empty<DocumentChunk>();
    }

    public Guid Id { get; }

    public string DisplayName { get; }

    public string StoredPath { get; }

    public string MimeType { get; }

    public string Sha256 { get; }

    public DocumentStatus Status { get; }

    public string? ErrorCode { get; }

    public int PageCount { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public IReadOnlyCollection<DocumentChunk> Chunks { get; }
}
