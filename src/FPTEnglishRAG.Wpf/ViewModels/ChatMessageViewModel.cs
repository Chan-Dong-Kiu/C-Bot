using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FPTEnglishRAG.Domain.Entities;
using FPTEnglishRAG.Domain.Enums;

namespace FPTEnglishRAG.Wpf.ViewModels;

public partial class ChatMessageViewModel : ObservableObject
{
    public Guid Id { get; }
    public ChatRole Role { get; }
    public DateTimeOffset CreatedAt { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSending))]
    [NotifyPropertyChangedFor(nameof(IsCompleted))]
    [NotifyPropertyChangedFor(nameof(IsFailed))]
    private ChatMessageStatus _status;

    [ObservableProperty]
    private string _content;

    public ObservableCollection<CitationViewModel> Citations { get; } = [];

    public bool IsUser => Role == ChatRole.User;
    public bool IsAssistant => Role == ChatRole.Assistant;

    public bool IsSending => Status == ChatMessageStatus.Sending;
    public bool IsCompleted => Status == ChatMessageStatus.Completed;
    public bool IsFailed => Status == ChatMessageStatus.Failed;

    public string FormattedTime => CreatedAt.ToLocalTime().ToString("HH:mm");

    public ChatMessageViewModel(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        Id = message.Id;
        Role = message.Role;
        CreatedAt = message.CreatedAt;
        _status = message.Status;
        _content = message.Content;

        if (message.Citations != null)
        {
            foreach (var citation in message.Citations)
            {
                Citations.Add(new CitationViewModel(citation));
            }
        }
    }

    public ChatMessageViewModel(
        Guid id,
        ChatRole role,
        string content,
        ChatMessageStatus status,
        DateTimeOffset? createdAt = null,
        IEnumerable<CitationViewModel>? citations = null)
    {
        Id = id;
        Role = role;
        _content = content;
        _status = status;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;

        if (citations != null)
        {
            foreach (var c in citations)
            {
                Citations.Add(c);
            }
        }
    }

    public void UpdateStatus(ChatMessageStatus newStatus, string? newContent = null, IEnumerable<CitationViewModel>? newCitations = null)
    {
        Status = newStatus;
        if (newContent != null)
        {
            Content = newContent;
        }

        if (newCitations != null)
        {
            Citations.Clear();
            foreach (var c in newCitations)
            {
                Citations.Add(c);
            }
        }
    }

    public ChatMessage ToDomain()
    {
        var domainCitations = Citations.Select(c => c.ToDomain()).ToList();
        return new ChatMessage(Id, Role, Content, CreatedAt, Status, domainCitations);
    }
}
