namespace FPTEnglishRAG.Domain.Enums;

public enum GroundingReason
{
    RetrievedContextAvailable,
    GeneralKnowledgeAllowed,
    SourceDependentQuestionWithoutContext,
    GeneralKnowledgeDisabled
}
