using FPTEnglishRAG.Application.Abstractions.Documents;
using FPTEnglishRAG.Application.DTOs;
using UglyToad.PdfPig;

namespace FPTEnglishRAG.Infrastructure.Documents.Extraction;

public class PdfPigTextExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string mimeType) =>
        string.Equals(mimeType, "application/pdf", StringComparison.OrdinalIgnoreCase);

    public Task<IReadOnlyList<ExtractedPageDto>> ExtractPagesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var pages = new List<ExtractedPageDto>();

        using var document = PdfDocument.Open(filePath);
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var text = page.Text ?? string.Empty;
                pages.Add(new ExtractedPageDto(page.Number, text));
            }
            catch
            {
                // Nếu một trang cụ thể bị lỗi font nội bộ, ghi nhận trang rỗng để không làm hỏng cả tài liệu
                pages.Add(new ExtractedPageDto(page.Number, string.Empty));
            }
        }

        return Task.FromResult<IReadOnlyList<ExtractedPageDto>>(pages);
    }
}
