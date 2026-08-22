using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Domain.Entities;
using FPTEnglishRAG.Domain.Enums;

namespace FPTEnglishRAG.Wpf.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly IChatService _chatService;
    private readonly IChatSessionStore _sessionStore;
    private CancellationTokenSource? _cts;

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string _inputQuestion = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    [NotifyCanExecuteChangedFor(nameof(RetryCommand))]
    private bool _isBusy;

    public bool IsNotBusy => !IsBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private CitationViewModel? _selectedCitation;

    [ObservableProperty]
    private bool _isCitationDialogOpen;

    public ChatViewModel(IChatService chatService, IChatSessionStore sessionStore)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));

        RestoreSession();
    }

    public void RestoreSession()
    {
        Messages.Clear();
        var session = _sessionStore.GetOrCreateActiveSession();
        foreach (var msg in session.Messages)
        {
            Messages.Add(new ChatMessageViewModel(msg));
        }
    }

    private bool CanSend() => !string.IsNullOrWhiteSpace(InputQuestion) && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(InputQuestion) || IsBusy)
        {
            return;
        }

        var question = InputQuestion.Trim();
        InputQuestion = string.Empty;
        ErrorMessage = null;

        // 1. Create and add User message
        var userMsgDomain = new ChatMessage(
            Guid.NewGuid(),
            ChatRole.User,
            question,
            DateTimeOffset.UtcNow,
            ChatMessageStatus.Completed,
            Array.Empty<Domain.ValueObjects.Citation>());

        _sessionStore.AddMessage(userMsgDomain);
        var userMsgVm = new ChatMessageViewModel(userMsgDomain);
        Messages.Add(userMsgVm);

        // 2. Create and add Assistant placeholder
        var assistantMsgDomain = new ChatMessage(
            Guid.NewGuid(),
            ChatRole.Assistant,
            "Đang tra cứu tài liệu & sinh câu trả lời...",
            DateTimeOffset.UtcNow,
            ChatMessageStatus.Sending,
            Array.Empty<Domain.ValueObjects.Citation>());

        _sessionStore.AddMessage(assistantMsgDomain);
        var assistantMsgVm = new ChatMessageViewModel(assistantMsgDomain);
        Messages.Add(assistantMsgVm);

        await ExecuteChatRequestAsync(question, userMsgDomain.Id, assistantMsgVm);
    }

    [RelayCommand]
    private void Stop()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
    }

    private bool CanRetry(ChatMessageViewModel? failedMessage)
    {
        if (IsBusy) return false;
        if (failedMessage != null) return failedMessage.IsFailed;
        return Messages.Any(m => m.IsFailed);
    }

    [RelayCommand(CanExecute = nameof(CanRetry))]
    private async Task RetryAsync(ChatMessageViewModel? targetFailedMessage)
    {
        if (IsBusy) return;

        var failedMsgVm = targetFailedMessage ?? Messages.LastOrDefault(m => m.IsFailed);
        if (failedMsgVm == null) return;

        // Find the corresponding user question right before this assistant message
        var failedIndex = Messages.IndexOf(failedMsgVm);
        string? questionToRetry = null;
        Guid? userMsgIdToExclude = null;

        for (int i = failedIndex - 1; i >= 0; i--)
        {
            if (Messages[i].IsUser)
            {
                questionToRetry = Messages[i].Content;
                userMsgIdToExclude = Messages[i].Id;
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(questionToRetry))
        {
            ErrorMessage = "Không tìm thấy câu hỏi trước đó để thử lại.";
            return;
        }

        ErrorMessage = null;
        failedMsgVm.UpdateStatus(
            ChatMessageStatus.Sending,
            "Đang thử lại kết nối & xử lý câu hỏi...",
            Array.Empty<CitationViewModel>());

        _sessionStore.UpdateMessage(failedMsgVm.Id, m => m with
        {
            Status = ChatMessageStatus.Sending,
            Content = failedMsgVm.Content,
            Citations = Array.Empty<Domain.ValueObjects.Citation>()
        });

        await ExecuteChatRequestAsync(questionToRetry, userMsgIdToExclude, failedMsgVm);
    }

    private async Task ExecuteChatRequestAsync(string question, Guid? currentUserMsgId, ChatMessageViewModel assistantMsgVm)
    {
        IsBusy = true;
        _cts = new CancellationTokenSource();

        try
        {
            // Build bounded history (up to 6 recent completed messages prior to current question)
            var activeSession = _sessionStore.GetOrCreateActiveSession();
            var recentHistory = activeSession.Messages
                .Where(m => m.Status == ChatMessageStatus.Completed 
                            && m.Id != assistantMsgVm.Id 
                            && (currentUserMsgId == null || m.Id != currentUserMsgId.Value))
                .TakeLast(6)
                .ToList();

            var answer = await _chatService.AskAsync(question, recentHistory, _cts.Token);

            var citationVms = answer.Citations.Select(c => new CitationViewModel(c)).ToList();
            assistantMsgVm.UpdateStatus(ChatMessageStatus.Completed, answer.Text, citationVms);

            _sessionStore.UpdateMessage(assistantMsgVm.Id, m => m with
            {
                Status = ChatMessageStatus.Completed,
                Content = answer.Text,
                Citations = answer.Citations
            });
        }
        catch (OperationCanceledException)
        {
            assistantMsgVm.UpdateStatus(
                ChatMessageStatus.Failed,
                "Yêu cầu đã bị hủy bởi người dùng.",
                Array.Empty<CitationViewModel>());

            _sessionStore.UpdateMessage(assistantMsgVm.Id, m => m with
            {
                Status = ChatMessageStatus.Failed,
                Content = assistantMsgVm.Content,
                Citations = Array.Empty<Domain.ValueObjects.Citation>()
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Đã xảy ra lỗi: {ex.Message}";
            assistantMsgVm.UpdateStatus(
                ChatMessageStatus.Failed,
                "Không thể tạo câu trả lời do sự cố kết nối hoặc lỗi dịch vụ. Bạn có thể nhấn 'Thử lại' để gửi lại yêu cầu.",
                Array.Empty<CitationViewModel>());

            _sessionStore.UpdateMessage(assistantMsgVm.Id, m => m with
            {
                Status = ChatMessageStatus.Failed,
                Content = assistantMsgVm.Content,
                Citations = Array.Empty<Domain.ValueObjects.Citation>()
            });
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void NewConversation()
    {
        Stop();
        _sessionStore.ClearActiveSession();
        Messages.Clear();
        ErrorMessage = null;
        IsBusy = false;
        SelectedCitation = null;
        IsCitationDialogOpen = false;
    }

    [RelayCommand]
    private void OpenCitation(CitationViewModel? citation)
    {
        if (citation == null) return;
        SelectedCitation = citation;
        IsCitationDialogOpen = true;
    }

    [RelayCommand]
    private void CloseCitation()
    {
        IsCitationDialogOpen = false;
        SelectedCitation = null;
    }

    [RelayCommand]
    private void CopyMessage(string? content)
    {
        if (string.IsNullOrEmpty(content)) return;
        try
        {
            Clipboard.SetText(content);
        }
        catch
        {
            // Clipboard access fallback in case of thread issues
        }
    }
}
