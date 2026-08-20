using FPTEnglishRAG.Domain.Entities;

namespace FPTEnglishRAG.Application.Abstractions;

public interface IChatSessionStore
{
    ChatSession GetOrCreateActiveSession();
    void AddMessage(ChatMessage message);
    void UpdateMessage(Guid messageId, Func<ChatMessage, ChatMessage> mutate);
    void UpdateMessage(ChatMessage updatedMessage);
    void ClearActiveSession();
}
