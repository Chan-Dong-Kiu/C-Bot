namespace FPTEnglishRAG.Application.DTOs;

public sealed record RetrievalQuery(
    ReadOnlyMemory<float> Vector,
    string Model,
    string IndexVersion);
