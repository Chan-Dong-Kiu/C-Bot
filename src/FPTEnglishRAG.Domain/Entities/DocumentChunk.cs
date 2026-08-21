namespace FPTEnglishRAG.Domain.Entities;

public sealed class DocumentChunk
{
    public DocumentChunk(
        Guid id,
        int ordinal,
        int pageStart,
        int pageEnd,
        string? section,
        string content,
        string contentHash,
        int tokenCount)
    {
        if (ordinal < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal));
        }

        if (pageStart < 1 || pageEnd < pageStart)
        {
            throw new ArgumentOutOfRangeException(nameof(pageStart));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Chunk content is required.", nameof(content));
        }

        if (string.IsNullOrWhiteSpace(contentHash))
        {
            throw new ArgumentException("Content hash is required.", nameof(contentHash));
        }

        if (tokenCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tokenCount));
        }

        Id = id;
        Ordinal = ordinal;
        PageStart = pageStart;
        PageEnd = pageEnd;
        Section = section;
        Content = content;
        ContentHash = contentHash;
        TokenCount = tokenCount;
    }

    public Guid Id { get; }

    public int Ordinal { get; }

    public int PageStart { get; }

    public int PageEnd { get; }

    public string? Section { get; }

    public string Content { get; }

    public string ContentHash { get; }

    public int TokenCount { get; }
}
