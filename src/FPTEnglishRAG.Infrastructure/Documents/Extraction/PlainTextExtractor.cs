using System.Text;
using FPTEnglishRAG.Application.Abstractions.Documents;
using FPTEnglishRAG.Application.DTOs;

namespace FPTEnglishRAG.Infrastructure.Documents.Extraction;

public class PlainTextExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string mimeType) =>
        string.Equals(mimeType, "text/plain", StringComparison.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<ExtractedPageDto>> ExtractPagesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // Tự động nhận diện UTF-8 hoặc ANSI
        using var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var content = await reader.ReadToEndAsync(cancellationToken);

        return new List<ExtractedPageDto>
        {
            new(1, content)
        };
    }
}
