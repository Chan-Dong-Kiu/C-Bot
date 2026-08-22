using System.IO;
using FPTEnglishRAG.Domain.Entities;
using FPTEnglishRAG.Domain.Enums;
using FPTEnglishRAG.Infrastructure.Persistence;
using FPTEnglishRAG.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FPTEnglishRAG.IntegrationTests.Persistence;

public class SqliteDocumentRepositoryIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IDbContextFactory<DocumentDbContext> _contextFactory;
    private readonly SqliteDocumentRepository _repository;

    public SqliteDocumentRepositoryIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DocumentDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Tạo schema trong in-memory SQLite
        using (var initialContext = new DocumentDbContext(options))
        {
            initialContext.Database.EnsureCreated();
        }

        var mockFactory = new TestDbContextFactory(options);
        _contextFactory = mockFactory;
        _repository = new SqliteDocumentRepository(_contextFactory);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task AddAndGetByIdAsync_WorksCorrectly()
    {
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            DisplayName = "GrammarGuide.pdf",
            StoredPath = "C:\\appdata\\doc.pdf",
            MimeType = "application/pdf",
            Sha256 = "hash123456",
            Status = DocumentStatus.Pending
        };

        await _repository.AddAsync(doc);
        var retrieved = await _repository.GetByIdAsync(doc.Id);

        retrieved.Should().NotBeNull();
        retrieved!.DisplayName.Should().Be("GrammarGuide.pdf");
        retrieved.Sha256.Should().Be("hash123456");
    }

    [Fact]
    public async Task SaveChunksAsync_PersistsChunksAndCascadeDeleteWorks()
    {
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            DisplayName = "TestDoc.txt",
            StoredPath = "C:\\appdata\\test.txt",
            MimeType = "text/plain",
            Sha256 = "hash_chunks",
            Status = DocumentStatus.Ready,
            ChunkCount = 2
        };

        await _repository.AddAsync(doc);

        var chunks = new List<DocumentChunk>
        {
            new() { Id = Guid.NewGuid(), DocumentId = doc.Id, Ordinal = 0, PageStart = 1, PageEnd = 1, Content = "Chunk 0", ContentHash = "h0", TokenCount = 20 },
            new() { Id = Guid.NewGuid(), DocumentId = doc.Id, Ordinal = 1, PageStart = 1, PageEnd = 1, Content = "Chunk 1", ContentHash = "h1", TokenCount = 25 }
        };

        await _repository.SaveChunksAsync(doc.Id, chunks);

        var retrievedChunks = await _repository.GetChunksByDocumentIdAsync(doc.Id);
        retrievedChunks.Should().HaveCount(2);

        // Test cascade delete
        await _repository.DeleteAsync(doc.Id);
        var afterDeleteDoc = await _repository.GetByIdAsync(doc.Id);
        var afterDeleteChunks = await _repository.GetChunksByDocumentIdAsync(doc.Id);

        afterDeleteDoc.Should().BeNull();
        afterDeleteChunks.Should().BeEmpty();
    }

    private class TestDbContextFactory : IDbContextFactory<DocumentDbContext>
    {
        private readonly DbContextOptions<DocumentDbContext> _options;

        public TestDbContextFactory(DbContextOptions<DocumentDbContext> options)
        {
            _options = options;
        }

        public DocumentDbContext CreateDbContext() => new(_options);
        public Task<DocumentDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new DocumentDbContext(_options));
    }
}
