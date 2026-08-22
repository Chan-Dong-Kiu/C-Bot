using FPTEnglishRAG.Domain.Entities;
using FPTEnglishRAG.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPTEnglishRAG.Infrastructure.Persistence;

public class DocumentDbContext : DbContext
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> Chunks => Set<DocumentChunk>();
    public DbSet<EmbeddingEntity> Embeddings => Set<EmbeddingEntity>();

    public DocumentDbContext(DbContextOptions<DocumentDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.DisplayName).IsRequired().HasMaxLength(255);
            entity.Property(d => d.StoredPath).IsRequired().HasMaxLength(500);
            entity.Property(d => d.MimeType).IsRequired().HasMaxLength(100);
            entity.Property(d => d.Sha256).IsRequired().HasMaxLength(64);
            entity.HasIndex(d => d.Sha256);
            entity.Property(d => d.Status).HasConversion<int>();
            entity.Property(d => d.ErrorCode).HasMaxLength(100);
            entity.Property(d => d.ErrorMessage).HasMaxLength(1000);

            entity.HasMany(d => d.Chunks)
                  .WithOne(c => c.Document)
                  .HasForeignKey(c => c.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Content).IsRequired();
            entity.Property(c => c.ContentHash).IsRequired().HasMaxLength(64);
            entity.Property(c => c.Section).HasMaxLength(255);
            entity.HasIndex(c => c.DocumentId);
            entity.HasIndex(c => c.Ordinal);
        });

        modelBuilder.Entity<EmbeddingEntity>(entity =>
        {
            entity.ToTable("Embeddings");
            entity.HasKey(embedding => embedding.ChunkId);
            entity.Property(embedding => embedding.Model).HasMaxLength(200).IsRequired();
            entity.Property(embedding => embedding.Vector).IsRequired();
            entity.Property(embedding => embedding.IndexVersion).HasMaxLength(100).IsRequired();
            entity.Property(embedding => embedding.CreatedAt).HasConversion(
                value => value.UtcTicks,
                ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
            entity.HasIndex(embedding => new
            {
                embedding.Model,
                embedding.Dimensions,
                embedding.IndexVersion
            });
            entity.HasOne(embedding => embedding.Chunk)
                .WithOne()
                .HasForeignKey<EmbeddingEntity>(embedding => embedding.ChunkId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
