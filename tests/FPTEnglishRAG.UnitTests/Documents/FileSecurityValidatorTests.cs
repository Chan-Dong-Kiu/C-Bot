using System.IO;
using FPTEnglishRAG.Application.Abstractions.Persistence;
using FPTEnglishRAG.Domain.Entities;
using FPTEnglishRAG.Domain.Enums;
using FPTEnglishRAG.Infrastructure.Documents.Validation;
using FluentAssertions;
using Moq;

namespace FPTEnglishRAG.UnitTests.Documents;

public class FileSecurityValidatorTests : IDisposable
{
    private readonly Mock<IDocumentRepository> _mockRepo;
    private readonly FileSecurityValidator _validator;
    private readonly string _tempDirectory;

    public FileSecurityValidatorTests()
    {
        _mockRepo = new Mock<IDocumentRepository>();
        _validator = new FileSecurityValidator(_mockRepo.Object);
        _tempDirectory = Path.Combine(Path.GetTempPath(), "FileValidatorTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public async Task ValidateAsync_NonExistentFile_ReturnsFileNotFound()
    {
        var result = await _validator.ValidateAsync("C:\\non_existent_file.pdf");
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(DocumentErrorCode.FileNotFound);
    }

    [Fact]
    public async Task ValidateAsync_EmptyFile_ReturnsCorruptedFile()
    {
        var filePath = Path.Combine(_tempDirectory, "empty.pdf");
        await File.WriteAllBytesAsync(filePath, []);

        var result = await _validator.ValidateAsync(filePath);
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(DocumentErrorCode.CorruptedFile);
    }

    [Fact]
    public async Task ValidateAsync_UnsupportedExtension_ReturnsUnsupportedFormat()
    {
        var filePath = Path.Combine(_tempDirectory, "file.docx");
        await File.WriteAllTextAsync(filePath, "test content");

        var result = await _validator.ValidateAsync(filePath);
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(DocumentErrorCode.UnsupportedFormat);
    }

    [Fact]
    public async Task ValidateAsync_InvalidPdfMagicBytes_ReturnsCorruptedFile()
    {
        var filePath = Path.Combine(_tempDirectory, "fake.pdf");
        await File.WriteAllBytesAsync(filePath, [0x00, 0x01, 0x02, 0x03, 0x04]);

        var result = await _validator.ValidateAsync(filePath);
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(DocumentErrorCode.CorruptedFile);
    }

    [Fact]
    public async Task ValidateAsync_ValidTxtFile_ReturnsSuccess()
    {
        var filePath = Path.Combine(_tempDirectory, "valid.txt");
        await File.WriteAllTextAsync(filePath, "Hello English Assessment preparation");

        var result = await _validator.ValidateAsync(filePath);
        result.IsValid.Should().BeTrue();
        result.ErrorCode.Should().Be(DocumentErrorCode.None);
        result.DetectedMimeType.Should().Be("text/plain");
        result.Sha256Hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateAsync_ValidPdfFile_ReturnsSuccess()
    {
        var filePath = Path.Combine(_tempDirectory, "valid.pdf");
        // Magic bytes %PDF-
        await File.WriteAllBytesAsync(filePath, [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37]);

        var result = await _validator.ValidateAsync(filePath);
        result.IsValid.Should().BeTrue();
        result.ErrorCode.Should().Be(DocumentErrorCode.None);
        result.DetectedMimeType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task ValidateAsync_DuplicateFileHash_ReturnsDuplicateFile()
    {
        var filePath = Path.Combine(_tempDirectory, "duplicate.txt");
        await File.WriteAllTextAsync(filePath, "Duplicate Content");

        _mockRepo.Setup(r => r.GetBySha256Async(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Document { DisplayName = "ExistingDoc.txt", Status = DocumentStatus.Ready });

        var result = await _validator.ValidateAsync(filePath);
        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(DocumentErrorCode.DuplicateFile);
    }
}
