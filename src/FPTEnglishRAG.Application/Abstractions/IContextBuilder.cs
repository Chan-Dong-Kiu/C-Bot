using FPTEnglishRAG.Application.DTOs;

namespace FPTEnglishRAG.Application.Abstractions;

/// <summary>
/// Builds a token-budgeted, labeled source context from retrieved chunks for use in prompt construction.
/// </summary>
/// <remarks>
/// The context builder is responsible for:
/// <list type="bullet">
///   <item>Assigning deterministic <c>[S1]</c>, <c>[S2]</c>, ... labels in retrieval-rank order.</item>
///   <item>Enforcing the configured token budget so the context does not exceed the model input limit.</item>
///   <item>Formatting the context with explicit delimiters to prevent prompt injection from document content.</item>
/// </list>
/// Implementations must treat all chunk content as untrusted data and must not execute or interpret it.
/// </remarks>
public interface IContextBuilder
{
    /// <summary>
    /// Builds a labeled, token-budgeted source context from the provided retrieved chunks.
    /// </summary>
    /// <param name="chunks">
    /// The retrieved chunks to include, already filtered and ranked by the retrieval service.
    /// Chunks are labeled in the order they appear in this list.
    /// </param>
    /// <param name="maxTokens">
    /// The maximum number of tokens allowed for the formatted context block.
    /// Chunks that would exceed this budget are omitted.
    /// </param>
    /// <returns>
    /// A <see cref="BuiltContext"/> containing the formatted context string and the ordered
    /// list of source blocks that were included within the token budget.
    /// </returns>
    BuiltContext Build(IReadOnlyList<RetrievedChunk> chunks, int maxTokens);
}
