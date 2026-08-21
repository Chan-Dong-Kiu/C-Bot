using FPTEnglishRAG.Application.Abstractions.Persistence;
using FPTEnglishRAG.Domain.Entities;
using FPTEnglishRAG.Domain.Enums;
using FPTEnglishRAG.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPTEnglishRAG.Infrastructure.Persistence.Repositories;

public sealed class DocumentRepository(IDbContextFactory<RagDbContext> contextFactory)
    : IDocumentRepository
{
    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        await using RagDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Documents.Add(ToEntity(document));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Document?> GetByIdAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        await using RagDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
        DocumentEntity? entity = await context.Documents
            .AsNoTracking()
            .Include(document => document.Chunks.OrderBy(chunk => chunk.Ordinal))
            .SingleOrDefaultAsync(document => document.Id == documentId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<Document>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using RagDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
        List<DocumentEntity> entities = await context.Documents
            .AsNoTracking()
            .Include(document => document.Chunks.OrderBy(chunk => chunk.Ordinal))
            .OrderByDescending(document => document.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToArray();
    }

    public async Task<bool> ExistsByHashAsync(
        string sha256,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sha256))
        {
            throw new ArgumentException("SHA-256 hash is required.", nameof(sha256));
        }

        await using RagDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Documents
            .AsNoTracking()
            .AnyAsync(document => document.Sha256 == sha256, cancellationToken);
    }

    public async Task UpdateStatusAsync(
        Guid documentId,
        DocumentStatus status,
        string? errorCode = null,
        CancellationToken cancellationToken = default)
    {
        await using RagDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
        DocumentEntity? entity = await context.Documents.FindAsync([documentId], cancellationToken);
        if (entity is null)
        {
            throw new InvalidOperationException($"Document not found: {documentId}");
        }

        entity.Status = status;
        entity.ErrorCode = errorCode;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await using RagDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
        DocumentEntity? entity = await context.Documents.FindAsync([documentId], cancellationToken);

        if (entity is null)
        {
            return;
        }

        context.Documents.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static DocumentEntity ToEntity(Document document)
    {
        return new DocumentEntity
        {
            Id = document.Id,
            DisplayName = document.DisplayName,
            StoredPath = document.StoredPath,
            MimeType = document.MimeType,
            Sha256 = document.Sha256,
            Status = document.Status,
            ErrorCode = document.ErrorCode,
            PageCount = document.PageCount,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            Chunks = document.Chunks.Select(chunk => new ChunkEntity
            {
                Id = chunk.Id,
                DocumentId = document.Id,
                Ordinal = chunk.Ordinal,
                PageStart = chunk.PageStart,
                PageEnd = chunk.PageEnd,
                Section = chunk.Section,
                Content = chunk.Content,
                ContentHash = chunk.ContentHash,
                TokenCount = chunk.TokenCount
            }).ToList()
        };
    }

    private static Document ToDomain(DocumentEntity entity)
    {
        DocumentChunk[] chunks = entity.Chunks
            .OrderBy(chunk => chunk.Ordinal)
            .Select(chunk => new DocumentChunk(
                chunk.Id,
                chunk.Ordinal,
                chunk.PageStart,
                chunk.PageEnd,
                chunk.Section,
                chunk.Content,
                chunk.ContentHash,
                chunk.TokenCount))
            .ToArray();

        return new Document(
            entity.Id,
            entity.DisplayName,
            entity.StoredPath,
            entity.MimeType,
            entity.Sha256,
            entity.Status,
            entity.PageCount,
            entity.CreatedAt,
            entity.UpdatedAt,
            chunks,
            entity.ErrorCode);
    }
}
