using FluentAssertions;
using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Domain.Entities;
using FPTEnglishRAG.Domain.Enums;
using FPTEnglishRAG.Domain.ValueObjects;
using FPTEnglishRAG.Wpf.ViewModels;
using Moq;

namespace FPTEnglishRAG.UnitTests.ViewModels;

public class ChatViewModelTests
{
    private readonly Mock<IChatService> _mockChatService;
    private readonly Mock<IChatSessionStore> _mockSessionStore;

    public ChatViewModelTests()
    {
        _mockChatService = new Mock<IChatService>();
        _mockSessionStore = new Mock<IChatSessionStore>();

        _mockSessionStore.Setup(s => s.GetOrCreateActiveSession())
            .Returns(new ChatSession());
    }

    [Fact]
    public void InitialState_WhenEmptySession_ShowWelcomeScreenIsTrue()
    {
        var vm = new ChatViewModel(_mockChatService.Object, _mockSessionStore.Object);

        vm.Messages.Should().BeEmpty();
        vm.ShowWelcomeScreen.Should().BeTrue();
        vm.SamplePrompts.Should().NotBeEmpty();
    }

    [Fact]
    public void WhenMessageAdded_ShowWelcomeScreenBecomesFalse()
    {
        var vm = new ChatViewModel(_mockChatService.Object, _mockSessionStore.Object);

        var msg = new ChatMessage(Guid.NewGuid(), ChatRole.User, "Hello", DateTimeOffset.UtcNow, ChatMessageStatus.Completed, Array.Empty<Citation>());
        vm.Messages.Add(new ChatMessageViewModel(msg));

        vm.ShowWelcomeScreen.Should().BeFalse();
    }

    [Fact]
    public void NewConversation_ClearsMessagesAndResetsShowWelcomeScreen()
    {
        var vm = new ChatViewModel(_mockChatService.Object, _mockSessionStore.Object);

        var msg = new ChatMessage(Guid.NewGuid(), ChatRole.User, "Hello", DateTimeOffset.UtcNow, ChatMessageStatus.Completed, Array.Empty<Citation>());
        vm.Messages.Add(new ChatMessageViewModel(msg));
        vm.ShowWelcomeScreen.Should().BeFalse();

        vm.NewConversationCommand.Execute(null);

        vm.Messages.Should().BeEmpty();
        vm.ShowWelcomeScreen.Should().BeTrue();
    }

    [Fact]
    public void UseSamplePrompt_SetsInputQuestionWithoutBullet()
    {
        var vm = new ChatViewModel(_mockChatService.Object, _mockSessionStore.Object);
        var sample = vm.SamplePrompts[0];

        vm.UseSamplePromptCommand.Execute(sample);

        vm.InputQuestion.Should().NotStartWith("•");
        vm.InputQuestion.Should().Be("Cấu trúc câu điều kiện loại 2 dùng trong trường hợp nào và có ví dụ gì?");
    }
}
