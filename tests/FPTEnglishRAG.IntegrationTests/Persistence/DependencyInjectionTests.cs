using FPTEnglishRAG.Application.Abstractions.Persistence;
using FPTEnglishRAG.Application.Abstractions.Chat;
using FPTEnglishRAG.Application.Abstractions.RAG;
using FPTEnglishRAG.Application.Configuration;
using FPTEnglishRAG.Infrastructure.DependencyInjection;
using FPTEnglishRAG.Infrastructure.Persistence;
using FPTEnglishRAG.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FPTEnglishRAG.IntegrationTests.Persistence;

public sealed class DependencyInjectionTests
{
    [Fact]
    public async Task AddVectorRetrieval_UsesSharedDocumentDbContext()
    {
        string databaseDirectory = Path.Combine(
            Path.GetTempPath(),
            "FPTEnglishRAG.Tests",
            Guid.NewGuid().ToString("N"));
        string databasePath = Path.Combine(databaseDirectory, "integration.db");

        try
        {
            Directory.CreateDirectory(databaseDirectory);
            var services = new ServiceCollection();
            services.AddDbContextFactory<DocumentDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));
            services.AddScoped<IDocumentRepository, SqliteDocumentRepository>();
            services.AddVectorRetrieval(new RetrievalOptions());
            services.AddQuestionGroundingPolicy();

            await using (ServiceProvider provider = services.BuildServiceProvider())
            {
                await using AsyncServiceScope scope = provider.CreateAsyncScope();
                IDbContextFactory<DocumentDbContext> contextFactory = scope.ServiceProvider
                    .GetRequiredService<IDbContextFactory<DocumentDbContext>>();
                await using DocumentDbContext context = await contextFactory.CreateDbContextAsync();
                await context.Database.EnsureCreatedAsync();

                Assert.True(File.Exists(databasePath));
                Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDocumentRepository>());
                Assert.NotNull(scope.ServiceProvider.GetRequiredService<IVectorStore>());
                Assert.NotNull(scope.ServiceProvider.GetRequiredService<IRetrievalService>());
                Assert.NotNull(scope.ServiceProvider.GetRequiredService<IQuestionGroundingPolicy>());

                Assert.True(await context.Database.CanConnectAsync());
                Assert.Contains("Embeddings", context.Model.GetEntityTypes().Select(type => type.GetTableName()));
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(databaseDirectory))
            {
                Directory.Delete(databaseDirectory, recursive: true);
            }
        }
    }
}
