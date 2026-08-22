using FPTEnglishRAG.Domain.Enums;

namespace FPTEnglishRAG.Application.DTOs;

public sealed record GroundingDecision(
    AnswerGroundingMode Mode,
    GroundingReason Reason,
    bool MayCallGemini,
    bool RequiresCitation);
