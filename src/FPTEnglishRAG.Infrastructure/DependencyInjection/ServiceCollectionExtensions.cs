using FPTEnglishRAG.Application.Abstractions.Chat;
using FPTEnglishRAG.Application.Abstractions.RAG;
using FPTEnglishRAG.Application.Configuration;
using FPTEnglishRAG.Application.Services.RAG;
using FPTEnglishRAG.Application.Services.Chat;
using FPTEnglishRAG.Infrastructure.VectorStore;
using Microsoft.Extensions.DependencyInjection;

namespace FPTEnglishRAG.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddQuestionGroundingPolicy(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IQuestionGroundingPolicy, QuestionGroundingPolicy>();
        return services;
    }

    public static IServiceCollection AddVectorRetrieval(
        this IServiceCollection services,
        RetrievalOptions retrievalOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(retrievalOptions);
        retrievalOptions.Validate();

        services.AddSingleton(retrievalOptions);
        services.AddSingleton<IVectorStore, SqliteVectorStore>();
        services.AddSingleton<IRetrievalService, RetrievalService>();

        return services;
    }
}
