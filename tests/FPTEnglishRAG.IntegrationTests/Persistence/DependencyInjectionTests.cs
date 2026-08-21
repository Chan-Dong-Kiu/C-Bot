using FPTEnglishRAG.Application.Abstractions.Persistence;
using FPTEnglishRAG.Application.Abstractions.Chat;
using FPTEnglishRAG.Application.Abstractions.RAG;
using FPTEnglishRAG.Application.Configuration;
using FPTEnglishRAG.Infrastructure.Configuration;
using FPTEnglishRAG.Infrastructure.DependencyInjection;
using FPTEnglishRAG.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FPTEnglishRAG.IntegrationTests.Persistence;

public sealed class DependencyInjectionTests
{
    [Fact]
    public async Task AddVectorPersistence_ResolvesServicesAndAppliesMigration()
    {
        string databaseDirectory = Path.Combine(
            Path.GetTempPath(),
            "FPTEnglishRAG.Tests",
            Guid.NewGuid().ToString("N"));
        string databasePath = Path.Combine(databaseDirectory, "integration.db");

        try
        {
            var services = new ServiceCollection();
            services.AddVectorPersistence(
                new SqlitePersistenceOptions { DatabasePath = databasePath },
                new RetrievalOptions());
            services.AddQuestionGroundingPolicy();

            await using (ServiceProvider provider = services.BuildServiceProvider())
            {
                await using AsyncServiceScope scope = provider.CreateAsyncScope();
                IDatabaseInitializer initializer = scope.ServiceProvider
                    .GetRequiredService<IDatabaseInitializer>();

                await initializer.InitializeAsync();

                Assert.True(File.Exists(databasePath));
                Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDocumentRepository>());
                Assert.NotNull(scope.ServiceProvider.GetRequiredService<IVectorStore>());
                Assert.NotNull(scope.ServiceProvider.GetRequiredService<IRetrievalService>());
                Assert.NotNull(scope.ServiceProvider.GetRequiredService<IQuestionGroundingPolicy>());

                IDbContextFactory<RagDbContext> contextFactory = scope.ServiceProvider
                    .GetRequiredService<IDbContextFactory<RagDbContext>>();
                await using RagDbContext context = await contextFactory.CreateDbContextAsync();
                Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync());
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
