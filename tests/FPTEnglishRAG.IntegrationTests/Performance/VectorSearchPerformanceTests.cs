using System.Diagnostics;
using FPTEnglishRAG.Application.DTOs;
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
            var document = new DocumentEntity
            {
                Id = documentId,
                DisplayName = "Retrieval benchmark",
                StoredPath = "benchmark.txt",
                MimeType = "text/plain",
                Sha256 = new string('a', 64),
                Status = DocumentStatus.Ready,
                PageCount = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            for (int index = 0; index < VectorCount; index++)
            {
                Guid chunkId = Guid.NewGuid();
                document.Chunks.Add(new ChunkEntity
                {
                    Id = chunkId,
                    DocumentId = documentId,
                    Ordinal = index,
                    PageStart = 1,
                    PageEnd = 1,
                    Content = $"Benchmark chunk {index}",
                    ContentHash = $"benchmark-{index}",
                    TokenCount = 3,
                    Embedding = new EmbeddingEntity
                    {
                        ChunkId = chunkId,
                        Model = "benchmark-model",
                        Dimensions = Dimensions,
                        Vector = vectorBytes,
                        IndexVersion = "v1",
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                });
            }

            context.Documents.Add(document);
            await context.SaveChangesAsync();
        }

        var vectorStore = new SqliteVectorStore(database);
        var stopwatch = Stopwatch.StartNew();
        IReadOnlyList<VectorSearchResult> results = await vectorStore.SearchAsync(
            new VectorSearchRequest(vector, "benchmark-model", "v1", TopK: 5, Threshold: 0.7));
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
