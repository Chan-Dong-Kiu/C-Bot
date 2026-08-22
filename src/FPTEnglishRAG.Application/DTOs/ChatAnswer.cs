using FPTEnglishRAG.Domain.ValueObjects;

namespace FPTEnglishRAG.Application.DTOs;

public record ChatAnswer(
    string Text,
    IReadOnlyList<FPTEnglishRAG.Domain.ValueObjects.Citation> Citations,
    bool IsGrounded);
