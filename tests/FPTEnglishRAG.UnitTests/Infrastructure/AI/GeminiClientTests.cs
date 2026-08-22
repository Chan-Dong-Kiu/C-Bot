// File: tests/FPTEnglishRAG.UnitTests/Infrastructure/AI/GeminiClientTests.cs

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FPTEnglishRAG.Infrastructure.AI;
using FPTEnglishRAG.Infrastructure.AI.Exceptions;
using FPTEnglishRAG.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FPTEnglishRAG.UnitTests.Infrastructure.AI;

public class GeminiClientTests
{
    private const string ApiKey = "TEST_SECRET_API_KEY";
    private readonly GeminiOptions _options;
    private readonly FakeHttpMessageHandler _httpHandler;
    private readonly HttpClient _httpClient;
    private readonly GeminiClient _sut;

    public GeminiClientTests()
    {
        _options = new GeminiOptions
        {
            ApiKey = ApiKey,
            Endpoint = "https://api.gemini.com/v1beta",
            ChatModel = "gemini-test-chat",
            EmbeddingModel = "gemini-test-embed",
            MaxRetries = 2
        };

        _httpHandler = new FakeHttpMessageHandler();
        _httpClient = new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri(_options.Endpoint)
        };

        _sut = new GeminiClient(_httpClient, Options.Create(_options), NullLogger<GeminiClient>.Instance);
    }

    private string LoadJsonData(string filename)
    {
        var path = Path.Combine("Infrastructure", "AI", "TestData", filename);
        return File.ReadAllText(path);
    }

    // =========================================================================
    // GenerateContentAsync Tests
    // =========================================================================

    [Fact]
    public async Task GenerateContentAsync_WhenSuccess_DeserializesResponseCorrectly()
    {
        // Arrange
        var jsonResponse = LoadJsonData("GenerateSuccess.json");
        _httpHandler.EnqueueResponse(HttpStatusCode.OK, jsonResponse);
        var request = new GeminiGenerateRequest("model-id", "Hello", 0.2, 100);

        // Act
        var result = await _sut.GenerateContentAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be("This is a mocked Gemini response.");
        result.FinishReason.Should().Be("STOP");
    }

    [Fact]
    public async Task GenerateContentAsync_WhenUnauthorized401_ThrowsGeminiAuthenticationException()
    {
        _httpHandler.EnqueueResponse(HttpStatusCode.Unauthorized, "Invalid API key");
        var request = new GeminiGenerateRequest("model-id", "Hello", 0.2, 100);
        
        Func<Task> act = async () => await _sut.GenerateContentAsync(request, CancellationToken.None);
        
        await act.Should().ThrowAsync<GeminiAuthenticationException>();
    }

    [Fact]
    public async Task GenerateContentAsync_WhenForbidden403_ThrowsGeminiAuthenticationException()
    {
        _httpHandler.EnqueueResponse(HttpStatusCode.Forbidden, "Access Denied");
        var request = new GeminiGenerateRequest("model-id", "Hello", 0.2, 100);
        
        Func<Task> act = async () => await _sut.GenerateContentAsync(request, CancellationToken.None);
        
        await act.Should().ThrowAsync<GeminiAuthenticationException>();
    }

    [Fact]
    public async Task GenerateContentAsync_WhenBadRequest400_ThrowsGeminiInvalidRequestException()
    {
        _httpHandler.EnqueueResponse(HttpStatusCode.BadRequest, "Bad Request");
        var request = new GeminiGenerateRequest("model-id", "Hello", 0.2, 100);
        
        Func<Task> act = async () => await _sut.GenerateContentAsync(request, CancellationToken.None);
        
        await act.Should().ThrowAsync<GeminiInvalidRequestException>();
    }

    [Fact]
    public async Task GenerateContentAsync_WhenTooManyRequests429_ThrowsGeminiRateLimitException()
    {
        _httpHandler.EnqueueResponse(HttpStatusCode.TooManyRequests, "Quota Exceeded");
        var request = new GeminiGenerateRequest("model-id", "Hello", 0.2, 100);
        
        Func<Task> act = async () => await _sut.GenerateContentAsync(request, CancellationToken.None);
        
        await act.Should().ThrowAsync<GeminiRateLimitException>();
    }

    [Fact]
    public async Task GenerateContentAsync_WhenServerError5xx_ThrowsGeminiTransientException()
    {
        _httpHandler.EnqueueResponse(HttpStatusCode.InternalServerError, "Server Error");
        var request = new GeminiGenerateRequest("model-id", "Hello", 0.2, 100);
        
        Func<Task> act = async () => await _sut.GenerateContentAsync(request, CancellationToken.None);
        
        await act.Should().ThrowAsync<GeminiTransientException>();
    }

    [Fact]
    public async Task GenerateContentAsync_WhenUnexpectedStatusCode_ThrowsGeminiUnexpectedException()
    {
        _httpHandler.EnqueueResponse((HttpStatusCode)418, "I'm a teapot");
        var request = new GeminiGenerateRequest("model-id", "Hello", 0.2, 100);
        
        Func<Task> act = async () => await _sut.GenerateContentAsync(request, CancellationToken.None);
        
        await act.Should().ThrowAsync<GeminiUnexpectedException>();
    }

    [Fact]
    public async Task GenerateContentAsync_WhenResponseBodyMalformed_ThrowsGeminiUnexpectedExceptionOrDeserializationError()
    {
        _httpHandler.EnqueueResponse(HttpStatusCode.OK, "invalid json {");
        var request = new GeminiGenerateRequest("model-id", "Hello", 0.2, 100);
        
        Func<Task> act = async () => await _sut.GenerateContentAsync(request, CancellationToken.None);
        
        await act.Should().ThrowAsync<GeminiUnexpectedException>().WithMessage("*Failed to deserialize*");
    }

    [Fact]
    public async Task GenerateContentAsync_DoesNotIncludeApiKeyInThrownExceptionMessage()
    {
        _httpHandler.EnqueueResponse(HttpStatusCode.BadRequest, "Bad Request with API Key Details...");
        var request = new GeminiGenerateRequest("model-id", "Hello", 0.2, 100);
        
        Func<Task> act = async () => await _sut.GenerateContentAsync(request, CancellationToken.None);
        
        var ex = await act.Should().ThrowAsync<GeminiInvalidRequestException>();
        
        // Assert it does not contain the key directly
        ex.Which.Message.Should().NotContain(ApiKey);
    }

    [Fact]
    public async Task GenerateContentAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancel
        var request = new GeminiGenerateRequest("model-id", "Hello", 0.2, 100);
        
        // Ensure FakeHandler supports cancellation correctly
        _httpHandler.EnqueueResponse(HttpStatusCode.OK, "{}"); 
        
        Func<Task> act = async () => await _sut.GenerateContentAsync(request, cts.Token);
        
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // =========================================================================
    // EmbedContentAsync Tests
    // =========================================================================

    [Fact]
    public async Task EmbedContentAsync_WhenSuccess_DeserializesResponseCorrectly()
    {
        var jsonResponse = LoadJsonData("EmbedSuccess.json");
        _httpHandler.EnqueueResponse(HttpStatusCode.OK, jsonResponse);
        var request = new GeminiEmbedRequest("embed-model", "Test text");

        var result = await _sut.EmbedContentAsync(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.Vector.Should().BeEquivalentTo(new float[] { 0.1f, -0.2f, 0.3f, 0.0f });
    }

    [Fact]
    public async Task EmbedContentAsync_WhenUnauthorized401_ThrowsGeminiAuthenticationException()
    {
        _httpHandler.EnqueueResponse(HttpStatusCode.Unauthorized, "Invalid API key");
        var request = new GeminiEmbedRequest("embed-model", "Test text");
        
        Func<Task> act = async () => await _sut.EmbedContentAsync(request, CancellationToken.None);
        
        await act.Should().ThrowAsync<GeminiAuthenticationException>();
    }

    [Fact]
    public async Task EmbedContentAsync_WhenTooManyRequests429_ThrowsGeminiRateLimitException()
    {
        _httpHandler.EnqueueResponse(HttpStatusCode.TooManyRequests, "Quota Exceeded");
        var request = new GeminiEmbedRequest("embed-model", "Test text");
        
        Func<Task> act = async () => await _sut.EmbedContentAsync(request, CancellationToken.None);
        
        await act.Should().ThrowAsync<GeminiRateLimitException>();
    }

    [Fact]
    public async Task EmbedContentAsync_WhenServerError5xx_ThrowsGeminiTransientException()
    {
        _httpHandler.EnqueueResponse(HttpStatusCode.InternalServerError, "Server Error");
        var request = new GeminiEmbedRequest("embed-model", "Test text");
        
        Func<Task> act = async () => await _sut.EmbedContentAsync(request, CancellationToken.None);
        
        await act.Should().ThrowAsync<GeminiTransientException>();
    }

    [Fact]
    public async Task EmbedContentAsync_DoesNotIncludeApiKeyInThrownExceptionMessage()
    {
        _httpHandler.EnqueueResponse(HttpStatusCode.BadRequest, "Bad Request details");
        var request = new GeminiEmbedRequest("embed-model", "Test text");
        
        Func<Task> act = async () => await _sut.EmbedContentAsync(request, CancellationToken.None);
        
        var ex = await act.Should().ThrowAsync<GeminiInvalidRequestException>();
        ex.Which.Message.Should().NotContain(ApiKey);
    }
}
