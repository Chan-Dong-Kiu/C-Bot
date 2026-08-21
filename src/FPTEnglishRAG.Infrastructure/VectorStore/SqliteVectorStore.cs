using FPTEnglishRAG.Application.Abstractions.RAG;
using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Domain.Enums;
using FPTEnglishRAG.Infrastructure.Persistence;
using FPTEnglishRAG.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPTEnglishRAG.Infrastructure.VectorStore;

public sealed class SqliteVectorStore(IDbContextFactory<RagDbContext> contextFactory) : IVectorStore
{
    public async Task UpsertAsync(VectorRecord record, CancellationToken cancellationToken = default)
    {
        ValidateRecord(record);
        await using RagDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);

        bool chunkExists = await context.Chunks
            .AnyAsync(chunk => chunk.Id == record.ChunkId, cancellationToken);
        if (!chunkExists)
        {
            throw new InvalidOperationException($"Chunk not found: {record.ChunkId}");
        }

        EmbeddingEntity? embedding = await context.Embeddings
            .SingleOrDefaultAsync(item => item.ChunkId == record.ChunkId, cancellationToken);

        byte[] vectorBytes = FloatVectorSerializer.Serialize(record.Vector.Span);
        if (embedding is null)
        {
            context.Embeddings.Add(new EmbeddingEntity
            {
                ChunkId = record.ChunkId,
                Model = record.Model,
                Dimensions = record.Dimensions,
                Vector = vectorBytes,
                IndexVersion = record.IndexVersion,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            embedding.Model = record.Model;
            embedding.Dimensions = record.Dimensions;
            embedding.Vector = vectorBytes;
            embedding.IndexVersion = record.IndexVersion;
            embedding.CreatedAt = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        VectorSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        int dimensions = request.QueryVector.Length;

        await using RagDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
        List<StoredVectorCandidate> candidates = await context.Embeddings
            .AsNoTracking()
            .Where(embedding =>
                embedding.Model == request.Model &&
                embedding.Dimensions == dimensions &&
                embedding.IndexVersion == request.IndexVersion &&
                embedding.Chunk.Document.Status == DocumentStatus.Ready)
            .Select(embedding => new StoredVectorCandidate(
                embedding.ChunkId,
                embedding.Chunk.DocumentId,
                embedding.Chunk.Document.DisplayName,
                embedding.Chunk.Ordinal,
                embedding.Chunk.PageStart,
                embedding.Chunk.PageEnd,
                embedding.Chunk.Section,
                embedding.Chunk.Content,
                embedding.Dimensions,
                embedding.Vector))
            .ToListAsync(cancellationToken);

        return candidates
            .Select(embedding =>
            {
                float[] vector = FloatVectorSerializer.Deserialize(
                    embedding.Vector,
                    embedding.Dimensions);
                double score = CosineSimilarity.Calculate(request.QueryVector.Span, vector);

                return new VectorSearchResult(
                    embedding.ChunkId,
                    embedding.DocumentId,
                    embedding.DocumentName,
                    embedding.Ordinal,
                    embedding.PageStart,
                    embedding.PageEnd,
                    embedding.Section,
                    embedding.Content,
                    score);
            })
            .Where(result => result.Score >= request.Threshold)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.DocumentId)
            .ThenBy(result => result.Ordinal)
            .Take(request.TopK)
            .ToArray();
    }

    public async Task DeleteByDocumentIdAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        await using RagDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Embeddings
            .Where(embedding => embedding.Chunk.DocumentId == documentId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static void ValidateRecord(VectorRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (string.IsNullOrWhiteSpace(record.Model))
        {
            throw new ArgumentException("Embedding model is required.", nameof(record));
        }

        if (string.IsNullOrWhiteSpace(record.IndexVersion))
        {
            throw new ArgumentException("Index version is required.", nameof(record));
        }

        if (record.Dimensions <= 0 || record.Vector.Length != record.Dimensions)
        {
            throw new ArgumentException("Vector length must match its dimensions.", nameof(record));
        }
    }

    private static void ValidateRequest(VectorSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.QueryVector.IsEmpty)
        {
            throw new ArgumentException("Query vector cannot be empty.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Model) || string.IsNullOrWhiteSpace(request.IndexVersion))
        {
            throw new ArgumentException("Model and index version are required.", nameof(request));
        }

        if (request.TopK <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "TopK must be greater than zero.");
        }

        if (request.Threshold is < -1 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Threshold must be between -1 and 1.");
        }
    }

    private sealed record StoredVectorCandidate(
        Guid ChunkId,
        Guid DocumentId,
        string DocumentName,
        int Ordinal,
        int PageStart,
        int PageEnd,
        string? Section,
        string Content,
        int Dimensions,
        byte[] Vector);
}
