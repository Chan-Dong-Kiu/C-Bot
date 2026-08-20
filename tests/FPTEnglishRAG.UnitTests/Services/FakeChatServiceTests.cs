using System.Net.Http;
using FluentAssertions;
using FPTEnglishRAG.Application.Services;
using FPTEnglishRAG.Domain.Entities;
using Xunit;

namespace FPTEnglishRAG.UnitTests.Services;

public class FakeChatServiceTests
{
    private readonly FakeChatService _service = new(TimeSpan.Zero);

    [Fact]
    public async Task AskAsync_WithStandardQuestion_ReturnsGroundedAnswerAndCitations()
    {
        var result = await _service.AskAsync(
            "Cấu trúc câu điều kiện loại 2 là gì?",
            Array.Empty<ChatMessage>(),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.IsGrounded.Should().BeTrue();
        result.Text.Should().Contain("[S1]");
        result.Citations.Should().NotBeEmpty();
        result.Citations.Should().HaveCount(2);
        result.Citations[0].Label.Should().Be("[S1]");
    }

    [Fact]
    public async Task AskAsync_WhenQuestionContainsErrorKeyword_ThrowsHttpRequestException()
    {
        var act = () => _service.AskAsync(
            "Mô phỏng error kết nối",
            Array.Empty<ChatMessage>(),
            CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task AskAsync_WhenQuestionIsUnrelated_ReturnsUngroundedAnswer()
    {
        var result = await _service.AskAsync(
            "Chủ đề unknown không có trong tài liệu",
            Array.Empty<ChatMessage>(),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.IsGrounded.Should().BeFalse();
        result.Citations.Should().BeEmpty();
    }
}
