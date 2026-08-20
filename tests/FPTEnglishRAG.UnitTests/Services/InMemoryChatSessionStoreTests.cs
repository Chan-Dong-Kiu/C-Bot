using FluentAssertions;
using FPTEnglishRAG.Application.Services;
using FPTEnglishRAG.Domain.Entities;
using FPTEnglishRAG.Domain.Enums;
using FPTEnglishRAG.Domain.ValueObjects;
using Xunit;

namespace FPTEnglishRAG.UnitTests.Services;

public class InMemoryChatSessionStoreTests
{
    private readonly InMemoryChatSessionStore _store = new();

    [Fact]
    public void GetOrCreateActiveSession_ReturnsValidSessionInstance()
    {
        var session = _store.GetOrCreateActiveSession();

        session.Should().NotBeNull();
        session.Messages.Should().BeEmpty();
    }

    [Fact]
    public void AddMessage_AppendsMessageToActiveSession()
    {
        var message = new ChatMessage(
            Guid.NewGuid(),
            ChatRole.User,
            "Hello test",
            DateTimeOffset.UtcNow,
            ChatMessageStatus.Completed,
            Array.Empty<Citation>());

        _store.AddMessage(message);

        var session = _store.GetOrCreateActiveSession();
        session.Messages.Should().HaveCount(1);
        session.Messages[0].Content.Should().Be("Hello test");
    }

    [Fact]
    public void UpdateMessage_ModifiesTargetMessageInSession()
    {
        var messageId = Guid.NewGuid();
        var message = new ChatMessage(
            messageId,
            ChatRole.Assistant,
            "Old content",
            DateTimeOffset.UtcNow,
            ChatMessageStatus.Sending,
            Array.Empty<Citation>());

        _store.AddMessage(message);

        _store.UpdateMessage(messageId, m => m with
        {
            Status = ChatMessageStatus.Completed,
            Content = "Updated content"
        });

        var session = _store.GetOrCreateActiveSession();
        session.Messages.Should().HaveCount(1);
        session.Messages[0].Status.Should().Be(ChatMessageStatus.Completed);
        session.Messages[0].Content.Should().Be("Updated content");
    }

    [Fact]
    public void ClearActiveSession_RemovesAllMessagesAndResetsSession()
    {
        var message = new ChatMessage(
            Guid.NewGuid(),
            ChatRole.User,
            "Hello",
            DateTimeOffset.UtcNow,
            ChatMessageStatus.Completed,
            Array.Empty<Citation>());

        _store.AddMessage(message);
        _store.ClearActiveSession();

        var session = _store.GetOrCreateActiveSession();
        session.Messages.Should().BeEmpty();
    }
}
