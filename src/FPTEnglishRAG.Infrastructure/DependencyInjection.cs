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
public static class InfrastructureServiceCollectionExtensions
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
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddPolicyHandler((sp, request) => GeminiHttpPolicy.GetRetryPolicy(sp));

            // Register Real GeminiService alongside the Fake registration comments
            services.AddScoped<IGeminiService, GeminiService>();

            // Register Real EmbeddingService
            services.AddScoped<IEmbeddingService, EmbeddingService>();
            
            // Real PromptBuilder
            services.AddSingleton<IPromptBuilder, FPTEnglishRAG.Application.Services.PromptBuilder>();
            
            // Real CitationValidator
            services.AddSingleton<ICitationValidator, FPTEnglishRAG.Application.Services.CitationValidator>();
        }

        return services;
    }
}
