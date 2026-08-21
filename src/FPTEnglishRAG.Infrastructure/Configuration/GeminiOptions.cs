using System.ComponentModel.DataAnnotations;

namespace FPTEnglishRAG.Infrastructure.Configuration;

/// <summary>
/// Strongly typed options for the Gemini REST API client.
/// Bound from the <c>Gemini</c> section of application configuration.
/// </summary>
/// <remarks>
/// <para>
/// The API key is never stored in this class. It is resolved at runtime from user-secrets or
/// the <c>GEMINI_API_KEY</c> environment variable. Any other key source is rejected at startup.
/// </para>
/// <para>
/// When <see cref="GenerationModel"/> or <see cref="EmbeddingModel"/> changes, review whether
/// the existing vector index is compatible. Changing the embedding model requires re-indexing
/// all documents; mixing vectors from different models corrupts retrieval.
/// </para>
/// </remarks>
public sealed class GeminiOptions
{
    /// <summary>The configuration section name used for binding.</summary>
    public const string SectionName = "Gemini";

    /// <summary>
    /// Gets or sets the name of the environment variable that holds the Gemini API key.
    /// This is the fallback when user-secrets are not present (e.g. CI or production).
    /// </summary>
    public string ApiKeyEnvironmentVariable { get; set; } = "GEMINI_API_KEY";

    /// <summary>
    /// Gets or sets the Gemini REST API base endpoint URL.
    /// </summary>
    [Required]
    [Url]
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

    /// <summary>
    /// Gets or sets the Gemini generation model name used for answer generation.
    /// </summary>
    /// <example><c>gemini-2.0-flash</c></example>
    [Required]
    public string GenerationModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Gemini embedding model name used for both document and query embeddings.
    /// </summary>
    /// <remarks>
    /// This value is stored alongside every embedding vector in the index. Changing it without
    /// re-indexing will corrupt retrieval results.
    /// </remarks>
    /// <example><c>gemini-embedding-001</c></example>
    [Required]
    public string EmbeddingModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP timeout in seconds for generation requests.
    /// </summary>
    [Range(5, 300)]
    public int GenerationTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the HTTP timeout in seconds for embedding requests.
    /// </summary>
    [Range(5, 120)]
    public int EmbeddingTimeoutSeconds { get; set; } = 20;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for transient failures
    /// (HTTP 429, suitable 5xx, and request timeout).
    /// </summary>
    /// <remarks>
    /// Authentication (401/403) and validation (400) errors are never retried.
    /// Retry delay uses exponential backoff with jitter and respects the <c>Retry-After</c> header.
    /// </remarks>
    [Range(0, 10)]
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the generation temperature (0.0–1.0).
    /// Lower values produce more deterministic, grounded responses.
    /// </summary>
    [Range(0.0, 1.0)]
    public double Temperature { get; set; } = 0.2;

    /// <summary>
    /// Gets or sets the maximum number of output tokens for generation responses.
    /// </summary>
    [Range(64, 8192)]
    public int MaxOutputTokens { get; set; } = 2048;

    /// <summary>
    /// Gets or sets a value indicating whether fake implementations of
    /// <c>IGeminiService</c> and <c>IEmbeddingService</c> should be registered
    /// instead of the real HTTP-backed ones.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set to <see langword="true"/> in <c>appsettings.Development.json</c> or via the
    /// <c>Gemini__UseFake=true</c> environment variable to develop or test without a live
    /// Gemini API key.
    /// </para>
    /// <para>
    /// This flag must never be <see langword="true"/> in a production configuration file.
    /// The default is <see langword="false"/> so production is safe even if the setting is absent.
    /// </para>
    /// </remarks>
    public bool UseFake { get; set; } = false;
}
