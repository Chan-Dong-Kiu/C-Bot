using FPTEnglishRAG.Application.Abstractions.Persistence;
using FPTEnglishRAG.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace FPTEnglishRAG.Infrastructure.Persistence;

public sealed class DatabaseInitializer(
    IDbContextFactory<RagDbContext> contextFactory,
    SqlitePersistenceOptions options) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        options.Validate();
        string? directory = Path.GetDirectoryName(options.DatabasePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("SQLite database directory could not be resolved.");
        }

        Directory.CreateDirectory(directory);
        await using RagDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }
}
