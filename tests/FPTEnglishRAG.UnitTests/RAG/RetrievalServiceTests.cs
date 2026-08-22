using FPTEnglishRAG.Application.Abstractions.RAG;
using FPTEnglishRAG.Application.Configuration;
using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Application.Services.RAG;

namespace FPTEnglishRAG.UnitTests.RAG;

public sealed class RetrievalServiceTests
{
    [Fact]
    public async Task RetrieveAsync_RemovesDuplicateContentAndLimitsChunksPerDocument()
    {
        Guid firstDocument = Guid.NewGuid();
        Guid secondDocument = Guid.NewGuid();
        var store = new StubVectorStore(
        [
            Result(firstDocument, 0, "  Present   perfect usage ", 0.99),
            Result(firstDocument, 1, "present perfect usage", 0.98),
            Result(firstDocument, 2, "Past simple usage", 0.97),
            Result(firstDocument, 3, "Future simple usage", 0.96),
            Result(secondDocument, 0, "Vocabulary practice", 0.95)
        ]);
        var service = new RetrievalService(store, new RetrievalOptions
        {
            TopK = 3,
            Threshold = 0.7,
            MaxChunksPerDocument = 2,
            CandidateMultiplier = 3
        });

        IReadOnlyList<VectorSearchResult> results = await service.RetrieveAsync(
            new RetrievalQuery(new float[] { 1f, 0f }, "fake", "v1"));

        Assert.Equal(3, results.Count);
        Assert.Equal(2, results.Count(result => result.DocumentId == firstDocument));
        Assert.Single(results, result => result.DocumentId == secondDocument);
    }

    [Fact]
    public void Constructor_RejectsInvalidOptions()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new RetrievalService(new StubVectorStore([]), new RetrievalOptions { TopK = 0 }));
    }

    private static VectorSearchResult Result(
        Guid documentId,
        int ordinal,
        string content,
        double score)
    {
        return new VectorSearchResult(
            Guid.NewGuid(),
            documentId,
            "Document",
            ordinal,
            1,
            1,
            null,
            content,
            score);
    }

    private sealed class StubVectorStore(IReadOnlyList<VectorSearchResult> results) : IVectorStore
    {
        public Task UpsertAsync(VectorRecord record, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
            VectorSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(results);
        }

        public Task DeleteByDocumentIdAsync(
            Guid documentId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
