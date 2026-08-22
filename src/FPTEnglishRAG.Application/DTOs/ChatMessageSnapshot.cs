namespace FPTEnglishRAG.Application.DTOs;

/// <summary>
/// A bounded conversation message snapshot used as generation context.
/// </summary>
/// <param name="Role">The message role.</param>
/// <param name="Content">The message content.</param>
public sealed record ChatMessageSnapshot(string Role, string Content);
