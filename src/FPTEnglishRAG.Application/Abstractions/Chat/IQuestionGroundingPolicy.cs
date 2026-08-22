using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Domain.Enums;

namespace FPTEnglishRAG.Application.Abstractions.Chat;

public interface IQuestionGroundingPolicy
{
    GroundingDecision Decide(
        string question,
        bool hasRetrievedContext,
        KnowledgeMode knowledgeMode);
}
