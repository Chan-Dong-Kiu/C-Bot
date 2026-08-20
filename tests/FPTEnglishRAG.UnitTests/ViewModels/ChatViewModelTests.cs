using System.Net.Http;
using FluentAssertions;
using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Application.Services;
using FPTEnglishRAG.Domain.Entities;
using FPTEnglishRAG.Domain.Enums;
using FPTEnglishRAG.Domain.ValueObjects;
using FPTEnglishRAG.Wpf.ViewModels;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FPTEnglishRAG.UnitTests.ViewModels;

public class ChatViewModelTests
{
    private readonly IChatService _chatService = Substitute.For<IChatService>();
    private readonly IChatSessionStore _sessionStore = new InMemoryChatSessionStore();

    [Fact]
    public async Task SendAsync_WhenSuccessful_AddsMessagesAndSetsCompletedStateWithCitations()
    {
        // Arrange
        var citations = new List<Citation>
        {
            new("[S1]", "TestDoc.pdf", 5, "Sample snippet", Guid.NewGuid(), Guid.NewGuid())
        };
        var expectedAnswer = new ChatAnswer("Test grounded response [S1]", citations, true);

        _chatService.AskAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(expectedAnswer);

        var viewModel = new ChatViewModel(_chatService, _sessionStore)
        {
            InputQuestion = "What is type 2 conditional?"
        };

        // Act
        await viewModel.SendCommand.ExecuteAsync(null);

        // Assert
        viewModel.Messages.Should().HaveCount(2);

        var userMessage = viewModel.Messages[0];
        userMessage.Role.Should().Be(ChatRole.User);
        userMessage.Content.Should().Be("What is type 2 conditional?");
        userMessage.Status.Should().Be(ChatMessageStatus.Completed);

        var assistantMessage = viewModel.Messages[1];
        assistantMessage.Role.Should().Be(ChatRole.Assistant);
        assistantMessage.Content.Should().Be("Test grounded response [S1]");
        assistantMessage.Status.Should().Be(ChatMessageStatus.Completed);
        assistantMessage.Citations.Should().HaveCount(1);
        assistantMessage.Citations[0].Label.Should().Be("[S1]");

        viewModel.InputQuestion.Should().BeEmpty();
        viewModel.IsBusy.Should().BeFalse();
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_WhenServiceThrowsException_SetsAssistantMessageToFailedAndSetsErrorMessage()
    {
        // Arrange
        _chatService.AskAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection timeout to Gemini API"));

        var viewModel = new ChatViewModel(_chatService, _sessionStore)
        {
            InputQuestion = "Sample question"
        };

        // Act
        await viewModel.SendCommand.ExecuteAsync(null);

        // Assert
        viewModel.Messages.Should().HaveCount(2);

        var assistantMessage = viewModel.Messages[1];
        assistantMessage.Status.Should().Be(ChatMessageStatus.Failed);
        assistantMessage.IsFailed.Should().BeTrue();
        assistantMessage.Content.Should().Contain("Không thể tạo câu trả lời");

        viewModel.ErrorMessage.Should().NotBeNull();
        viewModel.ErrorMessage.Should().Contain("Connection timeout");
        viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task RetryAsync_WhenFailed_RetriesQuestionWithoutDuplicatingUserMessage()
    {
        // Arrange: first fail
        _chatService.AskAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Temporary network glitch"));

        var viewModel = new ChatViewModel(_chatService, _sessionStore)
        {
            InputQuestion = "Original User Question"
        };

        await viewModel.SendCommand.ExecuteAsync(null);
        viewModel.Messages.Should().HaveCount(2);
        viewModel.Messages[1].Status.Should().Be(ChatMessageStatus.Failed);

        // Arrange: second attempt succeeds
        var expectedAnswer = new ChatAnswer("Success after retry", Array.Empty<Citation>(), true);
        _chatService.AskAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(expectedAnswer);

        // Act
        await viewModel.RetryCommand.ExecuteAsync(viewModel.Messages[1]);

        // Assert: total count should still be 2 (no duplicate user message!)
        viewModel.Messages.Should().HaveCount(2);
        viewModel.Messages[0].Content.Should().Be("Original User Question");
        viewModel.Messages[1].Status.Should().Be(ChatMessageStatus.Completed);
        viewModel.Messages[1].Content.Should().Be("Success after retry");
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task BoundedHistory_PassesAtMost6RecentCompletedMessagesToChatService()
    {
        // Arrange: Populate session with 10 completed messages
        for (int i = 1; i <= 10; i++)
        {
            var role = i % 2 == 1 ? ChatRole.User : ChatRole.Assistant;
            _sessionStore.AddMessage(new ChatMessage(
                Guid.NewGuid(),
                role,
                $"Message {i}",
                DateTimeOffset.UtcNow,
                ChatMessageStatus.Completed,
                Array.Empty<Citation>()));
        }

        IReadOnlyList<ChatMessage>? capturedHistory = null;
        _chatService.AskAsync(
                Arg.Any<string>(),
                Arg.Do<IReadOnlyList<ChatMessage>>(h => capturedHistory = h),
                Arg.Any<CancellationToken>())
            .Returns(new ChatAnswer("Answer", Array.Empty<Citation>(), true));

        var viewModel = new ChatViewModel(_chatService, _sessionStore)
        {
            InputQuestion = "New Question 11"
        };

        // Act
        await viewModel.SendCommand.ExecuteAsync(null);

        // Assert
        capturedHistory.Should().NotBeNull();
        capturedHistory.Should().HaveCount(6);
        capturedHistory![0].Content.Should().Be("Message 5");
        capturedHistory[5].Content.Should().Be("Message 10");
    }

    [Fact]
    public void NewConversation_ClearsMessagesAndSessionStore()
    {
        // Arrange
        _sessionStore.AddMessage(new ChatMessage(
            Guid.NewGuid(),
            ChatRole.User,
            "History msg",
            DateTimeOffset.UtcNow,
            ChatMessageStatus.Completed,
            Array.Empty<Citation>()));

        var viewModel = new ChatViewModel(_chatService, _sessionStore);
        viewModel.Messages.Should().HaveCount(1);

        // Act
        viewModel.NewConversationCommand.Execute(null);

        // Assert
        viewModel.Messages.Should().BeEmpty();
        _sessionStore.GetOrCreateActiveSession().Messages.Should().BeEmpty();
    }

    [Fact]
    public void NavigationRetention_WhenRestoredFromSessionStore_PreservesExistingMessages()
    {
        // Arrange: User had a conversation
        _sessionStore.AddMessage(new ChatMessage(
            Guid.NewGuid(),
            ChatRole.User,
            "Nav Question",
            DateTimeOffset.UtcNow,
            ChatMessageStatus.Completed,
            Array.Empty<Citation>()));
        _sessionStore.AddMessage(new ChatMessage(
            Guid.NewGuid(),
            ChatRole.Assistant,
            "Nav Answer",
            DateTimeOffset.UtcNow,
            ChatMessageStatus.Completed,
            Array.Empty<Citation>()));

        // Act: A new ViewModel is created (simulating navigation)
        var restoredViewModel = new ChatViewModel(_chatService, _sessionStore);

        // Assert: Messages are retained from the session store
        restoredViewModel.Messages.Should().HaveCount(2);
        restoredViewModel.Messages[0].Content.Should().Be("Nav Question");
        restoredViewModel.Messages[1].Content.Should().Be("Nav Answer");
    }

    [Fact]
    public void OpenAndCloseCitation_UpdatesSelectedCitationAndDialogState()
    {
        var viewModel = new ChatViewModel(_chatService, _sessionStore);
        var citation = new Citation("[S1]", "Doc.pdf", 10, "Snippet text", Guid.NewGuid(), Guid.NewGuid());
        var citationVm = new CitationViewModel(citation);

        // Open
        viewModel.OpenCitationCommand.Execute(citationVm);
        viewModel.IsCitationDialogOpen.Should().BeTrue();
        viewModel.SelectedCitation.Should().Be(citationVm);

        // Close
        viewModel.CloseCitationCommand.Execute(null);
        viewModel.IsCitationDialogOpen.Should().BeFalse();
        viewModel.SelectedCitation.Should().BeNull();
    }
}
