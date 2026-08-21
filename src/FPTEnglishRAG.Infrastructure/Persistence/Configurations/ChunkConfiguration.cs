using FPTEnglishRAG.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTEnglishRAG.Infrastructure.Persistence.Configurations;

internal sealed class ChunkConfiguration : IEntityTypeConfiguration<ChunkEntity>
{
    public void Configure(EntityTypeBuilder<ChunkEntity> builder)
    {
        builder.ToTable("Chunks");
        builder.HasKey(chunk => chunk.Id);
        builder.Property(chunk => chunk.Section).HasMaxLength(500);
        builder.Property(chunk => chunk.Content).IsRequired();
        builder.Property(chunk => chunk.ContentHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(chunk => new { chunk.DocumentId, chunk.Ordinal }).IsUnique();
        builder.HasOne(chunk => chunk.Embedding)
            .WithOne(embedding => embedding.Chunk)
            .HasForeignKey<EmbeddingEntity>(embedding => embedding.ChunkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
