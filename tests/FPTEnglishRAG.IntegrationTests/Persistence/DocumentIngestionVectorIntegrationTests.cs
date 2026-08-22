using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Application.Abstractions.Documents;
using FPTEnglishRAG.Application.Configuration;
using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Application.Services;
using FPTEnglishRAG.Domain.Enums;
using FPTEnglishRAG.Infrastructure.Persistence.Repositories;
using FPTEnglishRAG.Infrastructure.VectorStore;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FPTEnglishRAG.IntegrationTests.Persistence;

public sealed class DocumentIngestionVectorIntegrationTests
{
    [Fact]
    public async Task IngestDocument_PersistsEmbeddingsThatRetrievalCanFind()
    {
        await using var database = new SqliteTestDatabase();
        await database.InitializeAsync();

        var repository = new SqliteDocumentRepository(database);
        var vectorStore = new SqliteVectorStore(database);
        var validator = new Mock<IDocumentValidator>();
        var extractor = new Mock<IDocumentTextExtractor>();
        var normalizer = new Mock<ITextNormalizer>();
        var chunker = new Mock<IChunkingService>();
        var storage = new Mock<IFileStorageService>();
        var embeddingService = new Mock<IEmbeddingService>();

        validator
            .Setup(service => service.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileValidationResult(
                true,
                DocumentErrorCode.None,
                null,
                "text/plain",
                new string('a', 64),
                100));
        storage
            .Setup(service => service.StoreFileAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored/test.txt");
        extractor.Setup(service => service.CanHandle("text/plain")).Returns(true);
        extractor
            .Setup(service => service.ExtractPagesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ExtractedPageDto(1, "Present perfect grammar")]);
        normalizer.Setup(service => service.Normalize(It.IsAny<string>())).Returns("Present perfect grammar");
        chunker
            .Setup(service => service.ChunkPages(
                It.IsAny<IReadOnlyList<ExtractedPageDto>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns([new ChunkResultDto(
                0,
                1,
                1,
                "Grammar",
                "Present perfect grammar",
                "chunk-hash",
                3)]);
        embeddingService
            .Setup(service => service.EmbedQueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([1f, 0f]);

        var service = new DocumentIngestionService(
            validator.Object,
            [extractor.Object],
            normalizer.Object,
            chunker.Object,
            storage.Object,
            repository,
            embeddingService.Object,
            vectorStore,
            new VectorIndexOptions
            {
                EmbeddingModel = "test-model",
                IndexVersion = "test-v1"
            });

        var document = await service.IngestDocumentAsync("grammar.txt");

        Assert.Equal(DocumentStatus.Ready, document.Status);
        await using (var context = database.CreateDbContext())
        {
            Assert.Equal(1, await context.Embeddings.CountAsync());
        }

        IReadOnlyList<VectorSearchResult> results = await vectorStore.SearchAsync(
            new VectorSearchRequest(new float[] { 1f, 0f }, "test-model", "test-v1", 5, 0.7));

        VectorSearchResult result = Assert.Single(results);
        Assert.Equal(document.Id, result.DocumentId);
        Assert.Equal("Present perfect grammar", result.Content);
    }
}
