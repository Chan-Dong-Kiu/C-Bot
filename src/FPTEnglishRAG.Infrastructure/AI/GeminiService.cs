// File: src/FPTEnglishRAG.Infrastructure/AI/GeminiService.cs

using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Infrastructure.AI.Exceptions;
using FPTEnglishRAG.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FPTEnglishRAG.Infrastructure.AI;

/// <summary>
/// Infrastructure implementation of <see cref="IGeminiService"/>.
/// Orchestrates the generation of chat answers by combining prompt building, 
/// the Gemini API client, and citation validation.
/// </summary>
internal sealed class GeminiService : IGeminiService
{
    private readonly IGeminiClient _geminiClient;
    private readonly IPromptBuilder _promptBuilder;
    private readonly ICitationValidator _citationValidator;
    private readonly ILogger<GeminiService> _logger;
    private readonly GeminiOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeminiService"/> class.
    /// </summary>
    /// <param name="geminiClient">The HTTP client for Gemini API.</param>
    /// <param name="promptBuilder">The builder to construct the context-aware prompt.</param>
    /// <param name="citationValidator">The validator to map and verify citations.</param>
    /// <param name="options">The configuration options for Gemini.</param>
    /// <param name="logger">The structured logger.</param>
    public GeminiService(
        IGeminiClient geminiClient,
        IPromptBuilder promptBuilder,
        ICitationValidator citationValidator,
        IOptions<GeminiOptions> options,
        ILogger<GeminiService> logger)
    {
        _geminiClient = geminiClient;
        _promptBuilder = promptBuilder;
        _citationValidator = citationValidator;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>Threshold Check:</b>
    /// Before calling the Gemini API, this method checks if the retrieved chunks meet the 
    /// minimum relevance threshold. If the chunks are empty or below the threshold, 
    /// it returns a non-grounded result immediately. This saves API costs, reduces latency, 
    /// and prevents the model from generating hallucinated answers when there is no supporting evidence.
    /// </para>
    /// </remarks>
    public async Task<ChatAnswerResult> GenerateAnswerAsync(
        ChatAnswerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // a. Check grounded condition FIRST before calling Gemini
        bool hasRelevantChunks = request.RetrievedChunks.Count > 0 && 
                                 request.RetrievedChunks.Any(c => c.SimilarityScore >= request.RelevanceThreshold);

        if (!hasRelevantChunks)
        {
            _logger.LogInformation("No relevant chunks found meeting the threshold {Threshold}. Returning NotGrounded.", request.RelevanceThreshold);
            return ChatAnswerResult.NotGrounded();
        }

        // b. Build the prompt using the injected builder
        var prompt = _promptBuilder.Build(request);

        // Prepare the request to Gemini Client
        var generateRequest = new GeminiGenerateRequest(
            Model: _options.ChatModel,
            Prompt: prompt,
            Temperature: _options.Temperature,
            MaxOutputTokens: _options.MaxOutputTokens);

        GeminiGenerateResponse geminiResponse;
        try
        {
            // c. Call Gemini API
            _logger.LogInformation("Calling Gemini API with model {Model} for question length {Length}", _options.ChatModel, request.Question.Length);
            geminiResponse = await _geminiClient.GenerateContentAsync(generateRequest, cancellationToken);
        }
        catch (GeminiApiException ex)
        {
            // d. Catch predefined exceptions, log structured info (no secrets), and rethrow.
            // TODO: If Application layer introduces domain-specific exceptions (e.g., AIPresentationException),
            // we should map these infrastructure exceptions to domain exceptions here to avoid leaking infra concerns.
            // For now, rethrowing as-is.
            _logger.LogError(ex, "Gemini API failed during GenerateAnswerAsync. ErrorType={ErrorType}", ex.GetType().Name);
            
            switch (ex)
            {
                case GeminiAuthenticationException:
                case GeminiRateLimitException:
                case GeminiTransientException:
                case GeminiInvalidRequestException:
                case GeminiUnexpectedException:
                    throw; // Rethrow exact type
                default:
                    throw;
            }
        }

        // e. Validate and map citations based on model output
        var citations = _citationValidator.ValidateAndMap(geminiResponse.Text, request.RetrievedChunks);

        // f. Return grounded result
        return new ChatAnswerResult(
            Answer: geminiResponse.Text,
            Citations: citations,
            IsGrounded: true);
    }
}
