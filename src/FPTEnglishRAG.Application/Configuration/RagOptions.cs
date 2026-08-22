using System.ComponentModel.DataAnnotations;

namespace FPTEnglishRAG.Application.Configuration;

/// <summary>
/// Strongly typed options for RAG retrieval and conversation-window behavior.
/// Bound from the <c>Rag</c> section of application configuration.
/// </summary>
/// <remarks>
/// These values govern retrieval quality and context size.
/// Any change to <see cref="RelevanceThreshold"/> requires evaluation against the golden query set.
/// Any change to <see cref="MaxContextTokens"/> may require review of the prompt template to
/// ensure the total prompt stays within the model input limit.
/// </remarks>
public sealed class RagOptions
{
    /// <summary>The configuration section name used for binding.</summary>
    public const string SectionName = "Rag";

    /// <summary>
    /// Gets or sets the number of top candidate chunks to retrieve from the vector store.
    /// Chunks are then filtered by <see cref="RelevanceThreshold"/> before being passed to the context builder.
    /// </summary>
    [Range(1, 20)]
    public int TopK { get; set; } = 5;

    /// <summary>
    /// Gets or sets the minimum cosine similarity score a retrieved chunk must reach to be
    /// included in the prompt context.
    /// </summary>
    /// <remarks>
    /// The default value of <c>0.70</c> is a starting point and must be calibrated against the
    /// golden evaluation dataset. Do not treat it as an absolute constant.
    /// If no chunks meet this threshold the pipeline must return the not-grounded fallback response
    /// without calling the generation model.
    /// </remarks>
    [Range(0.0, 1.0)]
    public double RelevanceThreshold { get; set; } = 0.70;

    /// <summary>
    /// Gets or sets the approximate maximum number of tokens allowed for the source context block
    /// passed to the generation model.
    /// </summary>
    /// <remarks>
    /// The context builder will include chunks in rank order until adding the next chunk would
    /// exceed this budget. Remaining chunks are silently omitted.
    /// </remarks>
    [Range(100, 8000)]
    public int MaxContextTokens { get; set; } = 3000;

    /// <summary>
    /// Gets or sets the maximum number of recent conversation messages to include as
    /// conversational context in the generation prompt.
    /// </summary>
    /// <remarks>
    /// Only the most recent N messages (user and assistant turns combined) are sent to the model.
    /// The current question always goes through retrieval regardless of history size.
    /// </remarks>
    [Range(0, 20)]
    public int MaxConversationMessages { get; set; } = 6;
}
