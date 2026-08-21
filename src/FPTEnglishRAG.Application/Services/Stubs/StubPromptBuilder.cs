// File: src/FPTEnglishRAG.Application/Services/Stubs/StubPromptBuilder.cs

using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Application.DTOs;

namespace FPTEnglishRAG.Application.Services.Stubs;

/// <summary>
/// Temporary stub for <see cref="IPromptBuilder"/>.
/// TODO: Replace with real implementation in Step 7.
/// </summary>
public sealed class StubPromptBuilder : IPromptBuilder
{
    /// <inheritdoc/>
    public string Build(ChatAnswerRequest request)
    {
        throw new NotImplementedException("Stub implementation. Real logic will be implemented in Step 7.");
    }
}
