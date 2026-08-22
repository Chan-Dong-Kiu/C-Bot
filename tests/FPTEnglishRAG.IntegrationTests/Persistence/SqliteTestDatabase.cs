using FPTEnglishRAG.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FPTEnglishRAG.IntegrationTests.Persistence;

internal sealed class SqliteTestDatabase : IDbContextFactory<DocumentDbContext>, IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private DbContextOptions<DocumentDbContext>? _options;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<DocumentDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using DocumentDbContext context = CreateDbContext();
        await context.Database.EnsureCreatedAsync();
    }

    public DocumentDbContext CreateDbContext()
    {
        return new DocumentDbContext(
            _options ?? throw new InvalidOperationException("Test database is not initialized."));
    }

    public Task<DocumentDbContext> CreateDbContextAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateDbContext());
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
