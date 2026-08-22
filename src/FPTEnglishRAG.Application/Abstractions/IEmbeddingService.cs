namespace FPTEnglishRAG.Application.Abstractions;

/// <summary>
/// Creates vector embeddings for user queries.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Embeds a normalized user query for retrieval.
    /// </summary>
    /// <param name="query">The query text to embed.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The query embedding vector.</returns>
    Task<float[]> EmbedQueryAsync(string query, CancellationToken cancellationToken);
}
