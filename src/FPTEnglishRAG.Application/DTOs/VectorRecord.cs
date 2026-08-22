namespace FPTEnglishRAG.Application.DTOs;

public sealed record VectorRecord(
    Guid ChunkId,
    string Model,
    int Dimensions,
    ReadOnlyMemory<float> Vector,
    string IndexVersion);
