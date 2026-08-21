// File: src/FPTEnglishRAG.Application/Services/Stubs/StubCitationValidator.cs

using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Application.DTOs;

namespace FPTEnglishRAG.Application.Services.Stubs;

/// <summary>
/// Temporary stub for <see cref="ICitationValidator"/>.
/// TODO: Replace with real implementation in Step 8.
/// </summary>
public sealed class StubCitationValidator : ICitationValidator
{
    /// <inheritdoc/>
    public List<Citation> ValidateAndMap(string modelOutput, IReadOnlyList<RetrievedChunk> retrievedChunks)
    {
        throw new NotImplementedException("Stub implementation. Real logic will be implemented in Step 8.");
    }
}
