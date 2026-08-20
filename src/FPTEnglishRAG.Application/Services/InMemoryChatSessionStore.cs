using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Domain.Entities;

namespace FPTEnglishRAG.Application.Services;

public class InMemoryChatSessionStore : IChatSessionStore
{
    private ChatSession _activeSession;
    private readonly object _lock = new();

    public InMemoryChatSessionStore()
    {
        _activeSession = new ChatSession();
    }

    public ChatSession GetOrCreateActiveSession()
    {
        lock (_lock)
        {
            return _activeSession;
        }
    }

    public void AddMessage(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        lock (_lock)
        {
            _activeSession.AddMessage(message);
        }
    }

    public void UpdateMessage(Guid messageId, Func<ChatMessage, ChatMessage> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        lock (_lock)
        {
            _activeSession.UpdateMessage(messageId, mutate);
        }
    }

    public void UpdateMessage(ChatMessage updatedMessage)
    {
        ArgumentNullException.ThrowIfNull(updatedMessage);
        lock (_lock)
        {
            _activeSession.UpdateMessage(updatedMessage.Id, _ => updatedMessage);
        }
    }

    public void ClearActiveSession()
    {
        lock (_lock)
        {
            _activeSession = new ChatSession();
        }
    }
}
