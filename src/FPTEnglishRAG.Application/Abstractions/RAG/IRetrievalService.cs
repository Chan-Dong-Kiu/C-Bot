using FPTEnglishRAG.Application.DTOs;

namespace FPTEnglishRAG.Application.Abstractions.RAG;

public interface IRetrievalService
{
    Task<IReadOnlyList<VectorSearchResult>> RetrieveAsync(
        RetrievalQuery query,
        CancellationToken cancellationToken = default);
}
