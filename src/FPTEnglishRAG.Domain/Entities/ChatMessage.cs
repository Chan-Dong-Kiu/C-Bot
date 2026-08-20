using FPTEnglishRAG.Domain.Enums;
using FPTEnglishRAG.Domain.ValueObjects;

namespace FPTEnglishRAG.Domain.Entities;

public record ChatMessage(
    Guid Id,
    ChatRole Role,
    string Content,
    DateTimeOffset CreatedAt,
    ChatMessageStatus Status,
    IReadOnlyList<Citation> Citations);
