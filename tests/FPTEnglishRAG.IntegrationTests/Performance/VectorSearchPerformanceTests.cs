using System.Diagnostics;
using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Domain.Entities;
using FPTEnglishRAG.Domain.Enums;
using FPTEnglishRAG.Infrastructure.Persistence.Entities;
using FPTEnglishRAG.Infrastructure.VectorStore;

namespace FPTEnglishRAG.IntegrationTests.Performance;

public sealed class VectorSearchPerformanceTests
{
    private const int VectorCount = 10_000;
    private const int Dimensions = 768;

    [Fact]
    [Trait("Category", "Performance")]
    public async Task Search_TenThousandVectors_CompletesWithinTarget()
    {
        await using var database = new Persistence.SqliteTestDatabase();
        await database.InitializeAsync();
        Guid documentId = Guid.NewGuid();
        float[] vector = CreateNormalizedVector();
        byte[] vectorBytes = FloatVectorSerializer.Serialize(vector);

        await using (var context = database.CreateDbContext())
        {
            context.ChangeTracker.AutoDetectChangesEnabled = false;
            var document = new Document
            {
                Id = documentId,
                DisplayName = "Retrieval benchmark",
                StoredPath = "benchmark.txt",
                MimeType = "text/plain",
                Sha256 = new string('a', 64),
                Status = DocumentStatus.Ready,
                PageCount = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var embeddings = new List<EmbeddingEntity>(VectorCount);

            for (int index = 0; index < VectorCount; index++)
            {
                Guid chunkId = Guid.NewGuid();
                document.Chunks.Add(new DocumentChunk
                {
                    Id = chunkId,
                    DocumentId = documentId,
                    Ordinal = index,
                    PageStart = 1,
                    PageEnd = 1,
                    Content = $"Benchmark chunk {index}",
                    ContentHash = $"benchmark-{index}",
                    TokenCount = 3
                });
                embeddings.Add(new EmbeddingEntity
                {
                    ChunkId = chunkId,
                    Model = "benchmark-model",
                    Dimensions = Dimensions,
                    Vector = vectorBytes,
                    IndexVersion = "v1",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            context.Documents.Add(document);
            context.Embeddings.AddRange(embeddings);
            await context.SaveChangesAsync();
        }

        var vectorStore = new SqliteVectorStore(database);
        var request = new VectorSearchRequest(
            vector,
            "benchmark-model",
            "v1",
            TopK: 5,
            Threshold: 0.7);

        // Exclude one-time EF query compilation and SQLite initialization from the retrieval target.
        await vectorStore.SearchAsync(request);

        var stopwatch = Stopwatch.StartNew();
        IReadOnlyList<VectorSearchResult> results = await vectorStore.SearchAsync(request);
        stopwatch.Stop();

        Assert.Equal(5, results.Count);
        Assert.True(
            stopwatch.ElapsedMilliseconds < 500,
            $"Retrieval took {stopwatch.ElapsedMilliseconds} ms; target is below 500 ms.");
    }

    private static float[] CreateNormalizedVector()
    {
        float value = 1f / MathF.Sqrt(Dimensions);
        return Enumerable.Repeat(value, Dimensions).ToArray();
    }
}
