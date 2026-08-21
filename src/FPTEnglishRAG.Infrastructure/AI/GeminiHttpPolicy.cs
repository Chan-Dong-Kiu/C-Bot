// File: src/FPTEnglishRAG.Infrastructure/AI/GeminiHttpPolicy.cs

using System.Net;
using FPTEnglishRAG.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace FPTEnglishRAG.Infrastructure.AI;

/// <summary>
/// Defines resilience policies (retry and backoff) for the Gemini API HTTP client using Polly.
/// </summary>
internal static class GeminiHttpPolicy
{
    /// <summary>
    /// Creates a retry policy that handles transient errors (5xx, timeouts) and 429 Too Many Requests.
    /// It applies exponential backoff with jitter and respects the Retry-After header.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<GeminiOptions>>().Value;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("GeminiHttpClient.RetryPolicy");

        return HttpPolicyExtensions
            // Handle 5xx and 408
            .HandleTransientHttpError()
            // Specifically handle 429 Too Many Requests
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: options.MaxRetries,
                sleepDurationProvider: (retryAttempt, response, context) =>
                {
                    // 1. Prefer Retry-After header if present
                    var result = response.Result;
                    if (result?.Headers.RetryAfter != null)
                    {
                        if (result.Headers.RetryAfter.Delta.HasValue)
                        {
                            return result.Headers.RetryAfter.Delta.Value;
                        }
                        if (result.Headers.RetryAfter.Date.HasValue)
                        {
                            var delay = result.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
                            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
                        }
                    }

                    // 2. Exponential backoff: 1s, 2s, 4s, 8s...
                    var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1));

                    // 3. Add Jitter to prevent thundering herd (0 to 500ms)
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500));

                    return baseDelay + jitter;
                },
                onRetryAsync: (outcome, timespan, retryAttempt, context) =>
                {
                    // Log the retry event without logging sensitive information
                    var statusCode = outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.GetType().Name ?? "Unknown";

                    logger.LogWarning(
                        "Delaying for {DelayMs}ms, then making retry {RetryAttempt}. Reason: {Reason}",
                        timespan.TotalMilliseconds,
                        retryAttempt,
                        statusCode);

                    return Task.CompletedTask;
                });
    }
}
