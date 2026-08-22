using System.IO;
using FPTEnglishRAG.Infrastructure.Documents.Extraction;
using FluentAssertions;
using UglyToad.PdfPig.Writer;

namespace FPTEnglishRAG.IntegrationTests.Documents;

public class PdfPigTextExtractorIntegrationTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly PdfPigTextExtractor _extractor;

    public PdfPigTextExtractorIntegrationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "PdfPigTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _extractor = new PdfPigTextExtractor();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public async Task ExtractPagesAsync_ValidPdfWithText_ExtractsPagesCorrectly()
    {
        var pdfPath = Path.Combine(_tempDirectory, "sample_test.pdf");

        // Tạo PDF test mẫu
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
        var font = builder.AddStandard14Font(UglyToad.PdfPig.Fonts.Standard14Fonts.Standard14Font.Helvetica);
        page.AddText("English Entry Assessment - Grammar Unit 1", 12, new UglyToad.PdfPig.Core.PdfPoint(50, 700), font);
        page.AddText("The Present Continuous is used for actions happening now.", 12, new UglyToad.PdfPig.Core.PdfPoint(50, 680), font);

        var bytes = builder.Build();
        await File.WriteAllBytesAsync(pdfPath, bytes);

        // Act
        var pages = await _extractor.ExtractPagesAsync(pdfPath);

        // Assert
        pages.Should().HaveCount(1);
        pages[0].PageNumber.Should().Be(1);
        pages[0].RawText.Should().Contain("English Entry Assessment");
        pages[0].RawText.Should().Contain("Present Continuous");
    }
}
