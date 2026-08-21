namespace FPTEnglishRAG.Application.Abstractions.Embeddings;

public interface IEmbeddingService
{
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
}
