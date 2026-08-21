using FPTEnglishRAG.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FPTEnglishRAG.IntegrationTests.Persistence;

internal sealed class SqliteTestDatabase : IDbContextFactory<RagDbContext>, IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private DbContextOptions<RagDbContext>? _options;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<RagDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using RagDbContext context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public RagDbContext CreateDbContext()
    {
        return new RagDbContext(
            _options ?? throw new InvalidOperationException("Test database is not initialized."));
    }

    public Task<RagDbContext> CreateDbContextAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateDbContext());
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
