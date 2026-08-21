namespace FPTEnglishRAG.Domain.Enums;

public enum DocumentStatus
{
    Pending,
    Extracting,
    Chunking,
    Embedding,
    Ready,
    Failed
}
