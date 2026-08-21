using FPTEnglishRAG.Application.Abstractions.Persistence;
using FPTEnglishRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPTEnglishRAG.Infrastructure.Persistence.Repositories;

public class SqliteDocumentRepository : IDocumentRepository
{
    private readonly IDbContextFactory<DocumentDbContext> _contextFactory;

    public SqliteDocumentRepository(IDbContextFactory<DocumentDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Documents
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Documents
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<Document?> GetBySha256Async(string sha256, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Documents
            .FirstOrDefaultAsync(d => d.Sha256 == sha256, cancellationToken);
    }

    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Documents.AddAsync(document, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Documents.Update(document);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var document = await context.Documents.FindAsync([id], cancellationToken);
        if (document != null)
        {
            context.Documents.Remove(document);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SaveChunksAsync(Guid documentId, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        // Xóa các chunk cũ của document nếu có (phục vụ retry)
        var oldChunks = await context.Chunks.Where(c => c.DocumentId == documentId).ToListAsync(cancellationToken);
        if (oldChunks.Count > 0)
        {
            context.Chunks.RemoveRange(oldChunks);
        }

        await context.Chunks.AddRangeAsync(chunks, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentChunk>> GetChunksByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Chunks
            .AsNoTracking()
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.Ordinal)
            .ToListAsync(cancellationToken);
    }
}
