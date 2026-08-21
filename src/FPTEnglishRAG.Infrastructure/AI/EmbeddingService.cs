// File: src/FPTEnglishRAG.Infrastructure/AI/EmbeddingService.cs

using System;
using System.Threading;
using System.Threading.Tasks;
using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace FPTEnglishRAG.Infrastructure.AI;

/// <summary>
/// Infrastructure implementation of <see cref="IEmbeddingService"/>.
/// Connects the application layer with the Gemini HTTP client to generate vectors.
/// </summary>
/// <remarks>
/// This interface is shared with the Retrieval module (Person 3) to embed user questions 
/// at query time. It is NOT intended to embed large batches of document chunks during 
/// the import process (which belongs to the Document Processing module).
/// </remarks>
internal sealed class EmbeddingService : IEmbeddingService
{
    private readonly IGeminiClient _geminiClient;
    private readonly GeminiOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingService"/> class.
    /// </summary>
    /// <param name="geminiClient">The underlying Gemini HTTP client.</param>
    /// <param name="options">Configuration options containing the embedding model name.</param>
    public EmbeddingService(IGeminiClient geminiClient, IOptions<GeminiOptions> options)
    {
        _geminiClient = geminiClient;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task<float[]> EmbedQueryAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query text cannot be null or whitespace.", nameof(query));
        }

        var request = new GeminiEmbedRequest(
            Model: _options.EmbeddingModel,
            Content: query);

        var response = await _geminiClient.EmbedContentAsync(request, cancellationToken);
        
        return response.Vector;
    }
}
