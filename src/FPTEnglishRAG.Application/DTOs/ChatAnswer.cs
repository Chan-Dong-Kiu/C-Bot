using FPTEnglishRAG.Domain.ValueObjects;

namespace FPTEnglishRAG.Application.DTOs;

public record ChatAnswer(
    string Text,
    IReadOnlyList<Citation> Citations,
    bool IsGrounded);
