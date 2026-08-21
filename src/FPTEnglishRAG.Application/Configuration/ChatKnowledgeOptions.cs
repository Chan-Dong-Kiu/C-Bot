using FPTEnglishRAG.Domain.Enums;

namespace FPTEnglishRAG.Application.Configuration;

public sealed class ChatKnowledgeOptions
{
    public const string SectionName = "ChatKnowledge";

    public KnowledgeMode Mode { get; init; } = KnowledgeMode.AllowGeneralKnowledge;
}
