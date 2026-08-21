// File: src/FPTEnglishRAG.Infrastructure/AI/GeminiClient.cs

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FPTEnglishRAG.Infrastructure.AI.Exceptions;
using FPTEnglishRAG.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FPTEnglishRAG.Infrastructure.AI;

/// <summary>
/// HTTP client boundary for the Gemini REST API.
/// Responsible for request serialization, HTTP transmission, response deserialization,
/// and mapping HTTP errors to specific domain exceptions.
/// </summary>
internal sealed class GeminiClient : IGeminiClient
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiClient> _logger;
    private readonly string _apiKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeminiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client injected via IHttpClientFactory.</param>
    /// <param name="options">The validated Gemini configuration options.</param>
    /// <param name="logger">The structured logger.</param>
    public GeminiClient(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        
        // At this point, the API key is guaranteed to be present because GeminiOptionsValidator ensures it.
        _apiKey = _options.ApiKey;
    }

    /// <inheritdoc/>
    public async Task<GeminiGenerateResponse> GenerateContentAsync(
        GeminiGenerateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var endpointPath = $"models/{Uri.EscapeDataString(request.Model)}:generateContent";
        var requestUri = BuildUri(endpointPath);

        var httpBody = new GenerateHttpRequest(
            Contents: [new ContentItem([new Part(request.Prompt)])],
            GenerationConfig: new GenerationConfig(request.Temperature, request.MaxOutputTokens));

        _logger.LogInformation(
            "Sending GenerateContent request. Model={Model}, PromptLength={PromptLength}",
            request.Model, request.Prompt.Length);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(requestUri, httpBody, s_jsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new GeminiTransientException("Network error occurred while calling Gemini API.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GeminiTransientException("Gemini API request timed out.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, "GenerateContent", cancellationToken);
        }

        var parsed = await DeserializeAsync<GenerateHttpResponse>(response, endpointPath, cancellationToken);

        var text = parsed?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
        var finishReason = parsed?.Candidates?.FirstOrDefault()?.FinishReason ?? "UNKNOWN";

        _logger.LogInformation(
            "GenerateContent completed successfully. Model={Model}, FinishReason={FinishReason}, TextLength={TextLength}",
            request.Model, finishReason, text.Length);

        return new GeminiGenerateResponse(text, finishReason);
    }

    /// <inheritdoc/>
    public async Task<GeminiEmbedResponse> EmbedContentAsync(
        GeminiEmbedRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var endpointPath = $"models/{Uri.EscapeDataString(request.Model)}:embedContent";
        var requestUri = BuildUri(endpointPath);

        var httpBody = new EmbedHttpRequest(
            Model: $"models/{request.Model}",
            Content: new ContentItem([new Part(request.Content)]));

        _logger.LogInformation(
            "Sending EmbedContent request. Model={Model}, ContentLength={ContentLength}",
            request.Model, request.Content.Length);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(requestUri, httpBody, s_jsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new GeminiTransientException("Network error occurred while calling Gemini API for embedding.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GeminiTransientException("Gemini API embedding request timed out.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, "EmbedContent", cancellationToken);
        }

        var parsed = await DeserializeAsync<EmbedHttpResponse>(response, endpointPath, cancellationToken);
        var vector = parsed?.Embedding?.Values ?? [];

        _logger.LogInformation(
            "EmbedContent completed successfully. Model={Model}, Dimensions={Dimensions}",
            request.Model, vector.Length);

        return new GeminiEmbedResponse(vector);
    }

    /// <summary>
    /// Builds the full URI with the API key as a query parameter.
    /// Never log the output of this method.
    /// </summary>
    private string BuildUri(string endpointPath)
    {
        var baseEndpoint = _options.Endpoint.TrimEnd('/');
        return $"{baseEndpoint}/{endpointPath}?key={_apiKey}";
    }

    private static async Task<T?> DeserializeAsync<T>(
        HttpResponseMessage response,
        string endpointPath,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(s_jsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new GeminiUnexpectedException($"Failed to deserialize Gemini response for path '{endpointPath}': {ex.Message}");
        }
    }

    private async Task HandleErrorResponseAsync(
        HttpResponseMessage response,
        string operationName,
        CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;
        string bodyExcerpt;

        try
        {
            var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
            bodyExcerpt = rawBody.Length > 200 ? rawBody[..200] + "..." : rawBody;
        }
        catch
        {
            bodyExcerpt = "(could not read response body)";
        }

        _logger.LogError(
            "Gemini API {Operation} failed with status {StatusCode}. Body: {BodyExcerpt}",
            operationName, statusCode, bodyExcerpt);

        var errorMessage = $"Gemini {operationName} failed with HTTP {statusCode}: {bodyExcerpt}";

        throw statusCode switch
        {
            400 => new GeminiInvalidRequestException(errorMessage),
            401 or 403 => new GeminiAuthenticationException(errorMessage),
            429 => new GeminiRateLimitException(errorMessage),
            >= 500 => new GeminiTransientException(errorMessage),
            _ => new GeminiUnexpectedException(errorMessage)
        };
    }

    // --- Private DTOs for JSON Serialization ---
    private sealed record GenerateHttpRequest(
        [property: JsonPropertyName("contents")] IReadOnlyList<ContentItem> Contents,
        [property: JsonPropertyName("generationConfig")] GenerationConfig GenerationConfig);

    private sealed record EmbedHttpRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("content")] ContentItem Content);

    private sealed record ContentItem(
        [property: JsonPropertyName("parts")] IReadOnlyList<Part> Parts);

    private sealed record Part(
        [property: JsonPropertyName("text")] string Text);

    private sealed record GenerationConfig(
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("maxOutputTokens")] int MaxOutputTokens);

    private sealed record GenerateHttpResponse(
        [property: JsonPropertyName("candidates")] IReadOnlyList<Candidate>? Candidates);

    private sealed record Candidate(
        [property: JsonPropertyName("content")] ContentItem? Content,
        [property: JsonPropertyName("finishReason")] string? FinishReason);

    private sealed record EmbedHttpResponse(
        [property: JsonPropertyName("embedding")] EmbeddingValues? Embedding);

    private sealed record EmbeddingValues(
        [property: JsonPropertyName("values")] float[]? Values);
}
