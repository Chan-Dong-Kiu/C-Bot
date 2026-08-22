// File: tests/FPTEnglishRAG.UnitTests/Infrastructure/AI/GeminiHttpPolicyTests.cs

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FPTEnglishRAG.Infrastructure.AI;
using FPTEnglishRAG.Infrastructure.AI.Exceptions;
using FPTEnglishRAG.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FPTEnglishRAG.UnitTests.Infrastructure.AI;

public class GeminiHttpPolicyTests
{
    private readonly ServiceCollection _services;
    private readonly GeminiOptions _options;
    private readonly FakeHttpMessageHandler _fakeHandler;

    public GeminiHttpPolicyTests()
    {
        _services = new ServiceCollection();
        
        _options = new GeminiOptions
        {
            ApiKey = "POLICY_TEST_KEY",
            Endpoint = "https://api.gemini.com/v1beta",
            MaxRetries = 2,
            ChatModel = "test"
        };
        _services.AddSingleton(Microsoft.Extensions.Options.Options.Create(_options));
        _services.AddLogging();
        
        _fakeHandler = new FakeHttpMessageHandler();
    }

    private IGeminiClient CreateClientWithPolicy()
    {
        _services.AddHttpClient<IGeminiClient, GeminiClient>(client =>
        {
            client.BaseAddress = new Uri(_options.Endpoint);
        })
        .AddPolicyHandler((sp, request) => GeminiHttpPolicy.GetRetryPolicy(sp))
        .ConfigurePrimaryHttpMessageHandler(() => _fakeHandler);
        
        var provider = _services.BuildServiceProvider();
        return provider.GetRequiredService<IGeminiClient>();
    }

    [Fact]
    public async Task RetryPolicy_WhenReceives429ThenThenSuccess_RetriesAndEventuallySucceeds()
    {
        // Arrange
        _fakeHandler.EnqueueResponse(HttpStatusCode.TooManyRequests, "Quota Exceeded", TimeSpan.FromMilliseconds(10));
        _fakeHandler.EnqueueResponse(HttpStatusCode.TooManyRequests, "Quota Exceeded", TimeSpan.FromMilliseconds(10));
        _fakeHandler.EnqueueResponse(HttpStatusCode.OK, "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Success\"}]},\"finishReason\":\"STOP\"}]}");
        
        var client = CreateClientWithPolicy();
        var request = new GeminiGenerateRequest("model", "Hello", 0.2, 100);

        // Act
        var result = await client.GenerateContentAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be("Success");
        _fakeHandler.Requests.Should().HaveCount(3);
    }

    [Fact]
    public async Task RetryPolicy_WhenReceives401_DoesNotRetry()
    {
        // Arrange
        _fakeHandler.EnqueueResponse(HttpStatusCode.Unauthorized, "Unauthorized");
        var client = CreateClientWithPolicy();
        var request = new GeminiGenerateRequest("model", "Hello", 0.2, 100);

        // Act
        Func<Task> act = async () => await client.GenerateContentAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<GeminiAuthenticationException>();
        _fakeHandler.Requests.Should().HaveCount(1, "401 should fail fast and not be retried");
    }

    [Fact]
    public async Task RetryPolicy_ExceedsMaxRetries_ThrowsAfterConfiguredAttempts()
    {
        // Arrange
        _fakeHandler.EnqueueResponse(HttpStatusCode.InternalServerError, "Error 1");
        _fakeHandler.EnqueueResponse(HttpStatusCode.InternalServerError, "Error 2");
        _fakeHandler.EnqueueResponse(HttpStatusCode.InternalServerError, "Error 3");
        _fakeHandler.EnqueueResponse(HttpStatusCode.InternalServerError, "Error 4");

        var client = CreateClientWithPolicy();
        var request = new GeminiGenerateRequest("model", "Hello", 0.2, 100);

        // Act
        Func<Task> act = async () => await client.GenerateContentAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<GeminiTransientException>();
        
        // 1 initial request + MaxRetries (2) = 3 attempts total
        _fakeHandler.Requests.Should().HaveCount(_options.MaxRetries + 1);
    }

    [Fact]
    public async Task RetryPolicy_RespectsRetryAfterHeader_WhenPresent()
    {
        // Arrange
        var retryAfterMs = 150;
        _fakeHandler.EnqueueResponse(HttpStatusCode.TooManyRequests, "Wait", TimeSpan.FromMilliseconds(retryAfterMs));
        _fakeHandler.EnqueueResponse(HttpStatusCode.OK, "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Success\"}]},\"finishReason\":\"STOP\"}]}");

        var client = CreateClientWithPolicy();
        var request = new GeminiGenerateRequest("model", "Hello", 0.2, 100);

        // Act
        var result = await client.GenerateContentAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _fakeHandler.RequestTimestamps.Should().HaveCount(2);
        
        var delay = _fakeHandler.RequestTimestamps[1] - _fakeHandler.RequestTimestamps[0];
        
        // The delay should be at least the Retry-After value
        delay.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(retryAfterMs - 15); // Small tolerance for test execution speed
    }
}
