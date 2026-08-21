using FPTEnglishRAG.Domain.Enums;

namespace FPTEnglishRAG.Application.DTOs;

public record FileValidationResult(
    bool IsValid,
    DocumentErrorCode ErrorCode,
    string? ErrorMessage,
    string? DetectedMimeType,
    string? Sha256Hash,
    long FileSizeBytes);
