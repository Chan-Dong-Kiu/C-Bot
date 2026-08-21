// File: src/FPTEnglishRAG.Infrastructure/Configuration/GeminiOptions.cs

namespace FPTEnglishRAG.Infrastructure.Configuration;

/// <summary>
/// Strongly typed options for the Gemini REST API client.
/// Bound from the <c>Gemini</c> section of application configuration.
/// </summary>
public sealed class GeminiOptions
{
    /// <summary>The configuration section name used for binding.</summary>
    public const string SectionName = "Gemini";

    /// <summary>
    /// Gets or sets the Gemini API key.
    /// This should NEVER be hard-coded or committed in appsettings.json.
    /// It must be supplied via user-secrets or the GEMINI_API_KEY environment variable.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Gemini REST API base endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

    /// <summary>
    /// Gets or sets the Gemini chat model name used for generation.
    /// </summary>
    public string ChatModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Gemini embedding model name used for document and query embeddings.
    /// </summary>
    public string EmbeddingModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP timeout in seconds for content generation requests.
    /// Generation typically takes much longer than embedding.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the HTTP timeout in seconds specifically for embedding requests.
    /// Embedding is usually fast, so this can be shorter.
    /// </summary>
    public int EmbeddingTimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for transient failures.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the generation temperature.
    /// </summary>
    public double Temperature { get; set; } = 0.2;

    /// <summary>
    /// Gets or sets the maximum number of output tokens.
    /// </summary>
    public int MaxOutputTokens { get; set; } = 2048;

    /// <summary>
    /// Enables the fake offline implementation instead of real HTTP calls.
    /// </summary>
    public bool UseFake { get; set; } = false;
}
