using FPTEnglishRAG.Application.Services.Chat;
using FPTEnglishRAG.Domain.Enums;

namespace FPTEnglishRAG.UnitTests.Chat;

public sealed class QuestionGroundingPolicyTests
{
    private readonly QuestionGroundingPolicy _policy = new();

    [Fact]
    public void Decide_UsesGroundedAnswer_WhenContextExists()
    {
        var result = _policy.Decide(
            "Tại sao đáp án B sai?",
            hasRetrievedContext: true,
            KnowledgeMode.AllowGeneralKnowledge);

        Assert.Equal(AnswerGroundingMode.Grounded, result.Mode);
        Assert.True(result.MayCallGemini);
        Assert.True(result.RequiresCitation);
    }

    [Theory]
    [InlineData("Resilient nghĩa là gì?")]
    [InlineData("Hãy cho tôi một ví dụ về thì hiện tại hoàn thành")]
    [InlineData("How do I use the present perfect?")]
    public void Decide_AllowsGeneralKnowledge_ForIndependentLearningQuestions(string question)
    {
        var result = _policy.Decide(
            question,
            hasRetrievedContext: false,
            KnowledgeMode.AllowGeneralKnowledge);

        Assert.Equal(AnswerGroundingMode.GeneralKnowledge, result.Mode);
        Assert.True(result.MayCallGemini);
        Assert.False(result.RequiresCitation);
    }

    [Theory]
    [InlineData("Theo tài liệu, đáp án câu 12 là gì?")]
    [InlineData("Tại sao đáp án B sai?")]
    [InlineData("According to the passage, why is option C incorrect?")]
    public void Decide_RejectsSourceDependentQuestion_WhenContextIsMissing(string question)
    {
        var result = _policy.Decide(
            question,
            hasRetrievedContext: false,
            KnowledgeMode.AllowGeneralKnowledge);

        Assert.Equal(AnswerGroundingMode.InsufficientSources, result.Mode);
        Assert.False(result.MayCallGemini);
        Assert.False(result.RequiresCitation);
        Assert.Equal(GroundingReason.SourceDependentQuestionWithoutContext, result.Reason);
    }

    [Fact]
    public void Decide_RejectsFallback_WhenGroundedOnlyIsConfigured()
    {
        var result = _policy.Decide(
            "Resilient nghĩa là gì?",
            hasRetrievedContext: false,
            KnowledgeMode.GroundedOnly);

        Assert.Equal(AnswerGroundingMode.InsufficientSources, result.Mode);
        Assert.Equal(GroundingReason.GeneralKnowledgeDisabled, result.Reason);
    }
}
