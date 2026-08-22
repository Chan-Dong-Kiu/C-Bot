using FPTEnglishRAG.Domain.Enums;

namespace FPTEnglishRAG.Application.DTOs;

public record IngestionProgressReport(
    Guid DocumentId,
    string DisplayName,
    DocumentStatus Status,
    double ProgressPercentage,
    string CurrentStepDescription);
