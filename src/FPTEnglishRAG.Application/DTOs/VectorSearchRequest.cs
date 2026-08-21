namespace FPTEnglishRAG.Application.DTOs;

public sealed record VectorSearchRequest(
    ReadOnlyMemory<float> QueryVector,
    string Model,
    string IndexVersion,
    int TopK = 5,
    double Threshold = 0.70);
