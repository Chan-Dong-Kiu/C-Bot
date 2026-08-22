using FPTEnglishRAG.Application.DTOs;

namespace FPTEnglishRAG.Application.Abstractions.Documents;

public interface IDocumentTextExtractor
{
    bool CanHandle(string mimeType);
    Task<IReadOnlyList<ExtractedPageDto>> ExtractPagesAsync(string filePath, CancellationToken cancellationToken = default);
}
