namespace FPTEnglishRAG.Application.DTOs;

/// <summary>
/// The output of <see cref="FPTEnglishRAG.Application.Abstractions.IContextBuilder"/>:
/// a token-budgeted, labeled source context ready for use in prompt construction.
/// </summary>
/// <param name="FormattedContext">
/// The fully formatted context block containing labeled source entries,
/// delimited to prevent prompt injection.
/// This string is placed verbatim inside the <c>&lt;SOURCE_CONTEXT&gt;</c> section of the prompt.
/// </param>
/// <param name="Sources">
/// The ordered list of source blocks included in this context window.
/// Each entry maps a label (e.g. <c>S1</c>) to its originating chunk.
/// This list is used by <see cref="FPTEnglishRAG.Application.Abstractions.ICitationValidator"/>
/// to validate and map citation labels from model output.
/// </param>
/// <param name="TotalTokenEstimate">
/// An approximate token count for the formatted context. Used for debugging and
/// ensuring context stays within the configured budget.
/// </param>
public sealed record BuiltContext(
    string FormattedContext,
    IReadOnlyList<SourceBlock> Sources,
    int TotalTokenEstimate);
