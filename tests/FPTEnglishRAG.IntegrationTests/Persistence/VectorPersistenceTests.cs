using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Domain.Entities;
using FPTEnglishRAG.Domain.Enums;
using FPTEnglishRAG.Infrastructure.Persistence;
using FPTEnglishRAG.Infrastructure.Persistence.Repositories;
using FPTEnglishRAG.Infrastructure.VectorStore;
using Microsoft.EntityFrameworkCore;

namespace FPTEnglishRAG.IntegrationTests.Persistence;

public sealed class VectorPersistenceTests
{
    [Fact]
    public async Task Migration_CreatesExpectedTables()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        await using RagDbContext context = database.CreateDbContext();

        string[] tables = await context.Database
            .SqlQueryRaw<string>(
                "SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name IN ('Documents', 'Chunks', 'Embeddings')")
            .ToArrayAsync();

        Assert.Equal(3, tables.Length);
        Assert.Contains("Documents", tables);
        Assert.Contains("Chunks", tables);
        Assert.Contains("Embeddings", tables);
    }

    [Fact]
    public async Task Search_ReturnsReadyMatchingVectors_InDescendingScoreOrder()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        var repository = new DocumentRepository(database);
        var vectorStore = new SqliteVectorStore(database);
        Document document = CreateDocument(DocumentStatus.Ready, chunkCount: 3);
        await repository.AddAsync(document);

        DocumentChunk[] chunks = document.Chunks.ToArray();
        await vectorStore.UpsertAsync(CreateVector(chunks[0].Id, [1f, 0f]));
        await vectorStore.UpsertAsync(CreateVector(chunks[1].Id, [0.8f, 0.2f]));
        await vectorStore.UpsertAsync(CreateVector(chunks[2].Id, [-1f, 0f]));

        IReadOnlyList<VectorSearchResult> results = await vectorStore.SearchAsync(
            new VectorSearchRequest(new float[] { 1f, 0f }, "fake-embedding", "v1", TopK: 2, Threshold: 0.5));

        Assert.Equal(2, results.Count);
        Assert.Equal(chunks[0].Id, results[0].ChunkId);
        Assert.Equal(chunks[1].Id, results[1].ChunkId);
        Assert.True(results[0].Score >= results[1].Score);
    }

    [Fact]
    public async Task Search_ExcludesDocumentsThatAreNotReady()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        var repository = new DocumentRepository(database);
        var vectorStore = new SqliteVectorStore(database);
        Document document = CreateDocument(DocumentStatus.Embedding, chunkCount: 1);
        await repository.AddAsync(document);
        await vectorStore.UpsertAsync(CreateVector(document.Chunks.Single().Id, [1f, 0f]));

        IReadOnlyList<VectorSearchResult> results = await vectorStore.SearchAsync(
            new VectorSearchRequest(new float[] { 1f, 0f }, "fake-embedding", "v1", TopK: 5, Threshold: -1));

        Assert.Empty(results);
    }

    [Fact]
    public async Task DeleteDocument_CascadesToChunksAndEmbeddings()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        var repository = new DocumentRepository(database);
        var vectorStore = new SqliteVectorStore(database);
        Document document = CreateDocument(DocumentStatus.Ready, chunkCount: 1);
        await repository.AddAsync(document);
        await vectorStore.UpsertAsync(CreateVector(document.Chunks.Single().Id, [1f, 0f]));

        await repository.DeleteAsync(document.Id);

        await using RagDbContext context = database.CreateDbContext();
        Assert.Equal(0, await context.Documents.CountAsync());
        Assert.Equal(0, await context.Chunks.CountAsync());
        Assert.Equal(0, await context.Embeddings.CountAsync());
    }

    [Fact]
    public async Task Repository_ListsChecksHashAndUpdatesStatus()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        var repository = new DocumentRepository(database);
        Document document = CreateDocument(DocumentStatus.Pending, chunkCount: 1);
        await repository.AddAsync(document);

        Assert.True(await repository.ExistsByHashAsync(document.Sha256));
        Assert.Single(await repository.GetAllAsync());

        await repository.UpdateStatusAsync(document.Id, DocumentStatus.Failed, "EMBEDDING_FAILED");
        Document? updated = await repository.GetByIdAsync(document.Id);

        Assert.NotNull(updated);
        Assert.Equal(DocumentStatus.Failed, updated.Status);
        Assert.Equal("EMBEDDING_FAILED", updated.ErrorCode);
    }

    [Fact]
    public async Task Upsert_ReplacesExistingVectorForChunk()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        var repository = new DocumentRepository(database);
        var vectorStore = new SqliteVectorStore(database);
        Document document = CreateDocument(DocumentStatus.Ready, chunkCount: 1);
        Guid chunkId = document.Chunks.Single().Id;
        await repository.AddAsync(document);
        await vectorStore.UpsertAsync(CreateVector(chunkId, [1f, 0f]));
        await vectorStore.UpsertAsync(CreateVector(chunkId, [0f, 1f]));

        IReadOnlyList<VectorSearchResult> oldDirection = await vectorStore.SearchAsync(
            new VectorSearchRequest(new float[] { 1f, 0f }, "fake-embedding", "v1", 5, 0.9));
        IReadOnlyList<VectorSearchResult> newDirection = await vectorStore.SearchAsync(
            new VectorSearchRequest(new float[] { 0f, 1f }, "fake-embedding", "v1", 5, 0.9));

        Assert.Empty(oldDirection);
        Assert.Single(newDirection);
        await using RagDbContext context = database.CreateDbContext();
        Assert.Equal(1, await context.Embeddings.CountAsync());
    }

    [Fact]
    public async Task Search_FiltersDifferentModelAndIndexVersion()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        var repository = new DocumentRepository(database);
        var vectorStore = new SqliteVectorStore(database);
        Document document = CreateDocument(DocumentStatus.Ready, chunkCount: 2);
        DocumentChunk[] chunks = document.Chunks.ToArray();
        await repository.AddAsync(document);
        await vectorStore.UpsertAsync(CreateVector(chunks[0].Id, [1f, 0f]));
        await vectorStore.UpsertAsync(
            new VectorRecord(chunks[1].Id, "other-model", 2, new float[] { 1f, 0f }, "v2"));

        IReadOnlyList<VectorSearchResult> results = await vectorStore.SearchAsync(
            new VectorSearchRequest(new float[] { 1f, 0f }, "fake-embedding", "v1", 5, -1));

        Assert.Single(results);
        Assert.Equal(chunks[0].Id, results[0].ChunkId);
    }

    [Fact]
    public async Task DeleteByDocumentId_RemovesOnlyVectorsAndKeepsDocumentChunks()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();
        var repository = new DocumentRepository(database);
        var vectorStore = new SqliteVectorStore(database);
        Document document = CreateDocument(DocumentStatus.Ready, chunkCount: 1);
        await repository.AddAsync(document);
        await vectorStore.UpsertAsync(CreateVector(document.Chunks.Single().Id, [1f, 0f]));

        await vectorStore.DeleteByDocumentIdAsync(document.Id);

        await using RagDbContext context = database.CreateDbContext();
        Assert.Equal(1, await context.Documents.CountAsync());
        Assert.Equal(1, await context.Chunks.CountAsync());
        Assert.Equal(0, await context.Embeddings.CountAsync());
    }

    private static VectorRecord CreateVector(Guid chunkId, float[] vector)
    {
        return new VectorRecord(chunkId, "fake-embedding", vector.Length, vector, "v1");
    }

    private static Document CreateDocument(DocumentStatus status, int chunkCount)
    {
        Guid documentId = Guid.NewGuid();
        DocumentChunk[] chunks = Enumerable.Range(0, chunkCount)
            .Select(index => new DocumentChunk(
                Guid.NewGuid(),
                index,
                pageStart: 1,
                pageEnd: 1,
                section: "Grammar",
                content: $"English grammar chunk {index}",
                contentHash: $"hash-{documentId:N}-{index}",
                tokenCount: 10))
            .ToArray();

        return new Document(
            documentId,
            "Grammar guide",
            "documents/grammar.pdf",
            "application/pdf",
            documentId.ToString("N").PadRight(64, '0'),
            status,
            pageCount: 1,
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow,
            chunks);
    }
}
