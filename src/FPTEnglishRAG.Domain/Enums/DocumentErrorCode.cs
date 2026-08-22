namespace FPTEnglishRAG.Domain.Enums;

public enum DocumentErrorCode
{
    None = 0,
    FileNotFound = 1,
    FileTooLarge = 2,
    UnsupportedFormat = 3,
    CorruptedFile = 4,
    DuplicateFile = 5,
    ExtractionFailed = 6,
    ChunkingFailed = 7,
    EmbeddingFailed = 8,
    DatabaseError = 9
}
