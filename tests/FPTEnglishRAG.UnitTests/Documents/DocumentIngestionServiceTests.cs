using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Application.Abstractions.Documents;
using FPTEnglishRAG.Application.Abstractions.Persistence;
using FPTEnglishRAG.Application.Abstractions.RAG;
using FPTEnglishRAG.Application.Configuration;
using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Application.Services;
using FPTEnglishRAG.Domain.Entities;
using FPTEnglishRAG.Domain.Enums;
using FluentAssertions;
using Moq;

namespace FPTEnglishRAG.UnitTests.Documents;

public class DocumentIngestionServiceTests
{
    private readonly Mock<IDocumentValidator> _mockValidator;
    private readonly Mock<IDocumentTextExtractor> _mockExtractor;
    private readonly Mock<ITextNormalizer> _mockNormalizer;
    private readonly Mock<IChunkingService> _mockChunker;
    private readonly Mock<IFileStorageService> _mockStorage;
    private readonly Mock<IDocumentRepository> _mockRepo;
    private readonly Mock<IEmbeddingService> _mockEmbeddingService;
    private readonly Mock<IVectorStore> _mockVectorStore;
    private readonly DocumentIngestionService _service;

    public DocumentIngestionServiceTests()
    {
        _mockValidator = new Mock<IDocumentValidator>();
        _mockExtractor = new Mock<IDocumentTextExtractor>();
        _mockNormalizer = new Mock<ITextNormalizer>();
        _mockChunker = new Mock<IChunkingService>();
        _mockStorage = new Mock<IFileStorageService>();
        _mockRepo = new Mock<IDocumentRepository>();
        _mockEmbeddingService = new Mock<IEmbeddingService>();
        _mockVectorStore = new Mock<IVectorStore>();

        _mockExtractor.Setup(e => e.CanHandle("application/pdf")).Returns(true);

        _service = new DocumentIngestionService(
            _mockValidator.Object,
            new[] { _mockExtractor.Object },
            _mockNormalizer.Object,
            _mockChunker.Object,
            _mockStorage.Object,
            _mockRepo.Object,
            _mockEmbeddingService.Object,
            _mockVectorStore.Object,
            new VectorIndexOptions
            {
                EmbeddingModel = "test-embedding-model",
                IndexVersion = "test-v1"
            });
    }

    [Fact]
    public async Task IngestDocumentAsync_ValidationFails_ReturnsFailedDocument()
    {
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileValidationResult(false, DocumentErrorCode.FileTooLarge, "File quá lớn", "application/pdf", "hash", 100));

        var doc = await _service.IngestDocumentAsync("sample.pdf");

        doc.Status.Should().Be(DocumentStatus.Failed);
        doc.ErrorCode.Should().Be(DocumentErrorCode.FileTooLarge.ToString());
        doc.ErrorMessage.Should().Be("File quá lớn");
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestDocumentAsync_SuccessfulPipeline_TransitionsToReady()
    {
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileValidationResult(true, DocumentErrorCode.None, null, "application/pdf", "dummy_hash", 1024));

        _mockStorage.Setup(s => s.StoreFileAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("C:\\AppData\\docs\\sample.pdf");

        _mockExtractor.Setup(e => e.ExtractPagesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExtractedPageDto> { new(1, "Raw text from page 1") });

        _mockNormalizer.Setup(n => n.Normalize(It.IsAny<string>()))
            .Returns("Clean normalized text from page 1");

        _mockChunker.Setup(c => c.ChunkPages(It.IsAny<IReadOnlyList<ExtractedPageDto>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new List<ChunkResultDto>
            {
                new(0, 1, 1, null, "Clean normalized text from page 1", "chunk_hash", 10)
            });
        _mockEmbeddingService
            .Setup(service => service.EmbedQueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 1f, 0f });

        var progressReports = new List<IngestionProgressReport>();
        var progress = new Progress<IngestionProgressReport>(p => progressReports.Add(p));

        var doc = await _service.IngestDocumentAsync("sample.pdf", progress);

        doc.Status.Should().Be(DocumentStatus.Ready);
        doc.PageCount.Should().Be(1);
        doc.ChunkCount.Should().Be(1);
        _mockRepo.Verify(r => r.SaveChunksAsync(doc.Id, It.IsAny<IReadOnlyList<DocumentChunk>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockVectorStore.Verify(store => store.UpsertAsync(
            It.Is<VectorRecord>(record =>
                record.Model == "test-embedding-model" &&
                record.IndexVersion == "test-v1" &&
                record.Dimensions == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
