using FPTEnglishRAG.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPTEnglishRAG.Infrastructure.Persistence.Configurations;

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<DocumentEntity>
{
    public void Configure(EntityTypeBuilder<DocumentEntity> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.DisplayName).HasMaxLength(260).IsRequired();
        builder.Property(document => document.StoredPath).HasMaxLength(1024).IsRequired();
        builder.Property(document => document.MimeType).HasMaxLength(100).IsRequired();
        builder.Property(document => document.Sha256).HasMaxLength(64).IsRequired();
        builder.HasIndex(document => document.Sha256).IsUnique();
        builder.Property(document => document.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(document => document.ErrorCode).HasMaxLength(100);
        builder.Property(document => document.CreatedAt).HasConversion(
            value => value.UtcTicks,
            ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
        builder.Property(document => document.UpdatedAt).HasConversion(
            value => value.UtcTicks,
            ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
        builder.HasMany(document => document.Chunks)
            .WithOne(chunk => chunk.Document)
            .HasForeignKey(chunk => chunk.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
