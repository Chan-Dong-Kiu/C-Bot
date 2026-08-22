using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Domain.Entities;

namespace FPTEnglishRAG.Application.Abstractions;

public interface IChatService
{
    Task<ChatAnswer> AskAsync(string question, IReadOnlyList<ChatMessage> recentHistory, CancellationToken ct);
}
