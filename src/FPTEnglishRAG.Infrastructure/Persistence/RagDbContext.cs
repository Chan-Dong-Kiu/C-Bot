using FPTEnglishRAG.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPTEnglishRAG.Infrastructure.Persistence;

public sealed class RagDbContext(DbContextOptions<RagDbContext> options) : DbContext(options)
{
    public DbSet<DocumentEntity> Documents => Set<DocumentEntity>();
    public DbSet<ChunkEntity> Chunks => Set<ChunkEntity>();
    public DbSet<EmbeddingEntity> Embeddings => Set<EmbeddingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RagDbContext).Assembly);
    }
}
