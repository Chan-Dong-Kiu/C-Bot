using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Domain.Entities;

namespace FPTEnglishRAG.Application.Abstractions.Documents;

public interface IDocumentIngestionService
{
    Task<Document> IngestDocumentAsync(string sourceFilePath, IProgress<IngestionProgressReport>? progress = null, CancellationToken cancellationToken = default);
    Task<Document> RetryIngestionAsync(Guid documentId, IProgress<IngestionProgressReport>? progress = null, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
}
