using FPTEnglishRAG.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTEnglishRAG.Infrastructure.Persistence.Configurations;

internal sealed class EmbeddingConfiguration : IEntityTypeConfiguration<EmbeddingEntity>
{
    public void Configure(EntityTypeBuilder<EmbeddingEntity> builder)
    {
        builder.ToTable("Embeddings");
        builder.HasKey(embedding => embedding.ChunkId);
        builder.Property(embedding => embedding.Model).HasMaxLength(200).IsRequired();
        builder.Property(embedding => embedding.Vector).IsRequired();
        builder.Property(embedding => embedding.IndexVersion).HasMaxLength(100).IsRequired();
        builder.Property(embedding => embedding.CreatedAt).HasConversion(
            value => value.UtcTicks,
            ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
        builder.HasIndex(embedding => new
        {
            embedding.Model,
            embedding.Dimensions,
            embedding.IndexVersion
        });
    }
}
