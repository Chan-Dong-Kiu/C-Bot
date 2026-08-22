using System.Globalization;
using System.Text;
using FPTEnglishRAG.Application.Abstractions.Chat;
using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Domain.Enums;

namespace FPTEnglishRAG.Application.Services.Chat;

public sealed class QuestionGroundingPolicy : IQuestionGroundingPolicy
{
    private static readonly string[] SourceDependentMarkers =
    [
        "theo tai lieu",
        "trong tai lieu",
        "theo de",
        "trong de",
        "cau so",
        "dap an",
        "doan van tren",
        "bai doc tren",
        "tai sao cau",
        "according to the document",
        "according to the passage",
        "in the document",
        "in the passage",
        "question number",
        "why is option",
        "why is answer"
    ];

    public GroundingDecision Decide(
        string question,
        bool hasRetrievedContext,
        KnowledgeMode knowledgeMode)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question is required.", nameof(question));
        }

        if (hasRetrievedContext)
        {
            return new GroundingDecision(
                AnswerGroundingMode.Grounded,
                GroundingReason.RetrievedContextAvailable,
                MayCallGemini: true,
                RequiresCitation: true);
        }

        if (knowledgeMode == KnowledgeMode.GroundedOnly)
        {
            return Insufficient(GroundingReason.GeneralKnowledgeDisabled);
        }

        string normalizedQuestion = Normalize(question);
        bool dependsOnMissingSource = SourceDependentMarkers.Any(normalizedQuestion.Contains);
        if (dependsOnMissingSource)
        {
            return Insufficient(GroundingReason.SourceDependentQuestionWithoutContext);
        }

        return new GroundingDecision(
            AnswerGroundingMode.GeneralKnowledge,
            GroundingReason.GeneralKnowledgeAllowed,
            MayCallGemini: true,
            RequiresCitation: false);
    }

    private static GroundingDecision Insufficient(GroundingReason reason)
    {
        return new GroundingDecision(
            AnswerGroundingMode.InsufficientSources,
            reason,
            MayCallGemini: false,
            RequiresCitation: false);
    }

    private static string Normalize(string value)
    {
        string decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (char character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                char normalizedCharacter = char.ToLowerInvariant(character) switch
                {
                    'đ' => 'd',
                    var characterValue => characterValue
                };
                builder.Append(normalizedCharacter);
            }
        }

        return string.Join(' ', builder.ToString()
            .Normalize(NormalizationForm.FormC)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
