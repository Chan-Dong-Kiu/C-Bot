// File: src/FPTEnglishRAG.Infrastructure/DependencyInjection.cs

using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Application.Testing;
using FPTEnglishRAG.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FPTEnglishRAG.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure services in the DI container.
/// Called once from the WPF composition root (<c>App.xaml.cs</c>).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Gemini and Embedding services — either fake or real — based on configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set <c>Gemini:UseFake = true</c> in <c>appsettings.Development.json</c> or the environment
    /// variable <c>Gemini__UseFake=true</c> to enable offline development without an API key.
    /// </para>
    /// <para>
    /// The fake registrations are intentionally kept in this file (not removed) because:
    /// <list type="bullet">
    ///   <item>M1 (Chat UI) needs to develop and run the app offline before Step 6 is complete.</item>
    ///   <item>Unit and integration tests depend on the fake implementations from
    ///         <c>FPTEnglishRAG.Application.Testing</c>.</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="configuration">The application configuration used to resolve option values.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ------------------------------------------------------------------
        // Bind and validate GeminiOptions at startup.
        // A missing or invalid configuration value will cause a fast failure
        // before any user request is processed.
        // ------------------------------------------------------------------
        services.AddOptions<GeminiOptions>()
            .Bind(configuration.GetSection(GeminiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // ------------------------------------------------------------------
        // Read UseFake now (before the container is built) so we can choose
        // which concrete types to register.
        // ------------------------------------------------------------------
        var geminiSection = configuration.GetSection(GeminiOptions.SectionName);
        var useFake = geminiSection.GetValue<bool>(nameof(GeminiOptions.UseFake));

        if (useFake)
        {
            // ----------------------------------------------------------------
            // FAKE registrations — offline / development / test mode.
            // No HTTP calls, no API key required.
            //
            // TODO (Step 6 — M4): Replace the IGeminiService line below with:
            //   services.AddHttpClient<IGeminiClient, GeminiClient>(...);
            //   services.AddScoped<IGeminiService, GeminiService>();
            // Keep the fake branch intact; do NOT delete it.
            //
            // TODO (Step 9 — M4): Replace the IEmbeddingService line below with:
            //   services.AddScoped<IEmbeddingService, EmbeddingService>();
            // Keep the fake branch intact; do NOT delete it.
            // ----------------------------------------------------------------
            services.AddSingleton<IGeminiService, FakeGeminiService>();
            services.AddSingleton<IEmbeddingService, FakeEmbeddingService>();
        }
        else
        {
            // ----------------------------------------------------------------
            // REAL registrations — uncomment each line when the implementation
            // is complete and passes its integration tests.
            // ----------------------------------------------------------------

            // TODO (Step 6 — M4): Uncomment when GeminiClient + GeminiService are done.
            // services.AddHttpClient<IGeminiClient, GeminiClient>()
            //     .ConfigureHttpClient((sp, client) =>
            //     {
            //         var opts = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;
            //         client.BaseAddress = new Uri(opts.Endpoint);
            //         client.Timeout = TimeSpan.FromSeconds(opts.GenerationTimeoutSeconds);
            //     });
            // services.AddScoped<IGeminiService, GeminiService>();

            // TODO (Step 9 — M4): Uncomment when EmbeddingService is done.
            // services.AddScoped<IEmbeddingService, EmbeddingService>();

            // Temporary fallback: use fakes until real implementations are registered above.
            // Remove these two lines once both TODOs above are resolved.
            services.AddSingleton<IGeminiService, FakeGeminiService>();
            services.AddSingleton<IEmbeddingService, FakeEmbeddingService>();
        }

        return services;
    }
}
