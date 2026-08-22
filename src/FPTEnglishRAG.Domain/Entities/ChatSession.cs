namespace FPTEnglishRAG.Domain.Entities;

public class ChatSession
{
    private readonly List<ChatMessage> _messages = [];
    private readonly object _lock = new();

    public Guid Id { get; }
    public DateTimeOffset CreatedAt { get; }

    public ChatSession(Guid? id = null, DateTimeOffset? createdAt = null)
    {
        Id = id ?? Guid.NewGuid();
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
    }

    public IReadOnlyList<ChatMessage> Messages
    {
        get
        {
            lock (_lock)
            {
                return _messages.ToList().AsReadOnly();
            }
        }
    }

    public void AddMessage(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        lock (_lock)
        {
            _messages.Add(message);
        }
    }

    public void UpdateMessage(Guid messageId, Func<ChatMessage, ChatMessage> updateFunc)
    {
        ArgumentNullException.ThrowIfNull(updateFunc);
        lock (_lock)
        {
            var index = _messages.FindIndex(m => m.Id == messageId);
            if (index != -1)
            {
                _messages[index] = updateFunc(_messages[index]);
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _messages.Clear();
        }
    }
}
