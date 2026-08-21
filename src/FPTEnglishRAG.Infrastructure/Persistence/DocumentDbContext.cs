using FPTEnglishRAG.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FPTEnglishRAG.Infrastructure.Persistence;

public class DocumentDbContext : DbContext
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> Chunks => Set<DocumentChunk>();

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
    }
}
