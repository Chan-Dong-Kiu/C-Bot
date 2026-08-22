using FPTEnglishRAG.Application.DTOs;

namespace FPTEnglishRAG.Application.Abstractions.RAG;

public interface IVectorStore
{
    Task UpsertAsync(VectorRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        VectorSearchRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
}
