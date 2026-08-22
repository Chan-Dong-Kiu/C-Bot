using FPTEnglishRAG.Application.Abstractions.RAG;
using FPTEnglishRAG.Application.Configuration;
using FPTEnglishRAG.Application.DTOs;

namespace FPTEnglishRAG.Application.Services.RAG;

public sealed class RetrievalService : IRetrievalService
{
    private readonly IVectorStore _vectorStore;
    private readonly RetrievalOptions _options;

    public RetrievalService(IVectorStore vectorStore, RetrievalOptions options)
    {
        _vectorStore = vectorStore;
        _options = options;
        _options.Validate();
    }

    public async Task<IReadOnlyList<VectorSearchResult>> RetrieveAsync(
        RetrievalQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        int candidateCount = checked(_options.TopK * _options.CandidateMultiplier);
        IReadOnlyList<VectorSearchResult> candidates = await _vectorStore.SearchAsync(
            new VectorSearchRequest(
                query.Vector,
                query.Model,
                query.IndexVersion,
                candidateCount,
                _options.Threshold),
            cancellationToken);

        var countsByDocument = new Dictionary<Guid, int>();
        var seenContent = new HashSet<string>(StringComparer.Ordinal);
        var selected = new List<VectorSearchResult>(_options.TopK);

        foreach (VectorSearchResult candidate in candidates)
        {
            string normalizedContent = NormalizeForDeduplication(candidate.Content);
            if (!seenContent.Add(normalizedContent))
            {
                continue;
            }

            countsByDocument.TryGetValue(candidate.DocumentId, out int documentCount);
            if (documentCount >= _options.MaxChunksPerDocument)
            {
                continue;
            }

            countsByDocument[candidate.DocumentId] = documentCount + 1;
            selected.Add(candidate);
            if (selected.Count == _options.TopK)
            {
                break;
            }
        }

        return selected;
    }

    private static string NormalizeForDeduplication(string content)
    {
        return string.Join(' ', content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
    }
}
