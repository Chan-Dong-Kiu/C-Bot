using FPTEnglishRAG.Application.Abstractions.Documents;
using FPTEnglishRAG.Application.Abstractions.Persistence;
using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Domain.Entities;
using FPTEnglishRAG.Domain.Enums;
using FPTEnglishRAG.Wpf.ViewModels;
using FluentAssertions;
using Moq;

namespace FPTEnglishRAG.UnitTests.ViewModels;

public class DocumentsViewModelTests
{
    private readonly Mock<IDocumentIngestionService> _mockIngestionService;
    private readonly Mock<IDocumentRepository> _mockRepository;
    private readonly DocumentsViewModel _viewModel;

    public DocumentsViewModelTests()
    {
        _mockIngestionService = new Mock<IDocumentIngestionService>();
        _mockRepository = new Mock<IDocumentRepository>();
        _viewModel = new DocumentsViewModel(_mockIngestionService.Object, _mockRepository.Object)
        {
            ConfirmDeleteDialog = (t, m) => true // Auto confirm
        };
    }

    [Fact]
    public async Task LoadDocumentsAsync_LoadsAndPopulatesList()
    {
        var docs = new List<Document>
        {
            new() { Id = Guid.NewGuid(), DisplayName = "Doc1.pdf", Status = DocumentStatus.Ready, PageCount = 5, ChunkCount = 10 },
            new() { Id = Guid.NewGuid(), DisplayName = "Doc2.txt", Status = DocumentStatus.Pending, PageCount = 1, ChunkCount = 0 }
        };

        _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(docs);

        await _viewModel.LoadDocumentsCommand.ExecuteAsync(null);

        _viewModel.Documents.Should().HaveCount(2);
        _viewModel.FilteredDocuments.Should().HaveCount(2);
        _viewModel.Documents[0].DisplayName.Should().Be("Doc1.pdf");
    }

    [Fact]
    public async Task SearchText_FiltersDocumentList()
    {
        var docs = new List<Document>
        {
            new() { Id = Guid.NewGuid(), DisplayName = "Grammar Guide.pdf", Status = DocumentStatus.Ready },
            new() { Id = Guid.NewGuid(), DisplayName = "Vocabulary List.txt", Status = DocumentStatus.Ready }
        };

        _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(docs);
        await _viewModel.LoadDocumentsCommand.ExecuteAsync(null);

        _viewModel.SearchText = "Grammar";

        _viewModel.FilteredDocuments.Should().HaveCount(1);
        _viewModel.FilteredDocuments[0].DisplayName.Should().Be("Grammar Guide.pdf");
    }

    [Fact]
    public async Task DeleteDocumentAsync_RemovesItemFromListAndCallsService()
    {
        var doc = new Document { Id = Guid.NewGuid(), DisplayName = "ToDelete.pdf", Status = DocumentStatus.Ready };
        _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Document> { doc });
        await _viewModel.LoadDocumentsCommand.ExecuteAsync(null);

        var itemToDelete = _viewModel.Documents.First();
        await _viewModel.DeleteDocumentCommand.ExecuteAsync(itemToDelete);

        _viewModel.Documents.Should().BeEmpty();
        _mockIngestionService.Verify(s => s.DeleteDocumentAsync(doc.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetryDocumentAsync_UpdatesItemState()
    {
        var docId = Guid.NewGuid();
        var item = new DocumentItemViewModel
        {
            Id = docId,
            DisplayName = "FailedDoc.pdf",
            Status = DocumentStatus.Failed,
            ErrorMessage = "Lỗi cũ"
        };
        _viewModel.Documents.Add(item);

        var readyDoc = new Document
        {
            Id = docId,
            DisplayName = "FailedDoc.pdf",
            Status = DocumentStatus.Ready,
            PageCount = 3,
            ChunkCount = 6
        };

        _mockIngestionService.Setup(s => s.RetryIngestionAsync(docId, It.IsAny<IProgress<IngestionProgressReport>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(readyDoc);

        await _viewModel.RetryDocumentCommand.ExecuteAsync(item);

        item.Status.Should().Be(DocumentStatus.Ready);
        item.PageCount.Should().Be(3);
        item.ChunkCount.Should().Be(6);
    }
}
