using FPTEnglishRAG.Application.Abstractions.Persistence;
using FPTEnglishRAG.Application.Abstractions.Chat;
using FPTEnglishRAG.Application.Abstractions.RAG;
using FPTEnglishRAG.Application.Configuration;
using FPTEnglishRAG.Application.Services.RAG;
using FPTEnglishRAG.Application.Services.Chat;
using FPTEnglishRAG.Infrastructure.Configuration;
using FPTEnglishRAG.Infrastructure.Persistence;
using FPTEnglishRAG.Infrastructure.Persistence.Repositories;
using FPTEnglishRAG.Infrastructure.VectorStore;
using Microsoft.EntityFrameworkCore;
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

    public static IServiceCollection AddVectorPersistence(
        this IServiceCollection services,
        SqlitePersistenceOptions persistenceOptions,
        RetrievalOptions retrievalOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(persistenceOptions);
        ArgumentNullException.ThrowIfNull(retrievalOptions);
        persistenceOptions.Validate();
        retrievalOptions.Validate();

        services.AddSingleton(persistenceOptions);
        services.AddSingleton(retrievalOptions);
        services.AddDbContextFactory<RagDbContext>(builder =>
            builder.UseSqlite($"Data Source={persistenceOptions.DatabasePath}"));
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IVectorStore, SqliteVectorStore>();
        services.AddScoped<IRetrievalService, RetrievalService>();

        return services;
    }
}
