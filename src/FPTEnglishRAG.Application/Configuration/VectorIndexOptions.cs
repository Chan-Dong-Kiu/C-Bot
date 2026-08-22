namespace FPTEnglishRAG.Application.Configuration;

public sealed class VectorIndexOptions
{
    public string EmbeddingModel { get; init; } = string.Empty;
    public string IndexVersion { get; init; } = "v1";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(EmbeddingModel))
        {
            throw new InvalidOperationException("The embedding model is required for vector indexing.");
        }

        if (string.IsNullOrWhiteSpace(IndexVersion))
        {
            throw new InvalidOperationException("The vector index version is required.");
        }
    }
}
