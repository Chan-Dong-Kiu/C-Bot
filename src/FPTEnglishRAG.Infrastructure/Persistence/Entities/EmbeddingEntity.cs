namespace FPTEnglishRAG.Infrastructure.Persistence.Entities;

public sealed class EmbeddingEntity
{
    public Guid ChunkId { get; set; }
    public string Model { get; set; } = string.Empty;
    public int Dimensions { get; set; }
    public byte[] Vector { get; set; } = Array.Empty<byte>();
    public string IndexVersion { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public ChunkEntity Chunk { get; set; } = null!;
}
