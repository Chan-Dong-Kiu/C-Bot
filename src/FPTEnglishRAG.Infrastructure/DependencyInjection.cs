// File: src/FPTEnglishRAG.Infrastructure/DependencyInjection.cs

using System.Net.Http;
using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Application.Testing;
using FPTEnglishRAG.Infrastructure.AI;
using FPTEnglishRAG.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FPTEnglishRAG.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure services in the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Gemini and Embedding services - either fake or real - based on configuration.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<GeminiOptions>, GeminiOptionsValidator>();

        // ------------------------------------------------------------------
        // Bind Options, PostConfigure to read env var fallback, then Validate
        // ------------------------------------------------------------------
        services.AddOptions<GeminiOptions>()
            .Bind(configuration.GetSection(GeminiOptions.SectionName))
            .PostConfigure(options =>
            {
                // Fallback to GEMINI_API_KEY environment variable if user-secrets didn't provide it
                if (string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    options.ApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty;
                }
            })
            .ValidateOnStart();

        var geminiSection = configuration.GetSection(GeminiOptions.SectionName);
        var useFake = geminiSection.GetValue<bool>(nameof(GeminiOptions.UseFake));

        if (useFake)
        {
            services.AddSingleton<IGeminiService, FakeGeminiService>();
            services.AddSingleton<IEmbeddingService, FakeEmbeddingService>();
        }
        else
        {
            // Register IGeminiClient with typed HttpClient and Polly policies
            services.AddHttpClient<IGeminiClient, GeminiClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;
                client.BaseAddress = new Uri(options.Endpoint.TrimEnd('/') + "/");
                
                // Use the Timeout configured for Generation as the baseline HTTP timeout.
                // We use TimeoutSeconds (which is for generation) because it's usually longer.
                // The per-request timeout differences (generation vs embedding) should ideally be 
                // controlled via CancellationTokens inside the GeminiClient methods, 
                // but setting this prevents indefinite hangs.
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddPolicyHandler((sp, request) => GeminiHttpPolicy.GetRetryPolicy(sp));

            // TODO (Step 6): Uncomment when GeminiService is ready
            // services.AddScoped<IGeminiService, GeminiService>();

            // TODO (Step 9): Uncomment when EmbeddingService is ready
            // services.AddScoped<IEmbeddingService, EmbeddingService>();

            // Remove these fakes once real services are implemented above
            services.AddSingleton<IGeminiService, FakeGeminiService>();
            services.AddSingleton<IEmbeddingService, FakeEmbeddingService>();
        }

        return services;
    }
}
