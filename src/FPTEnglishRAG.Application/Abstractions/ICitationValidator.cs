using FPTEnglishRAG.Application.DTOs;

namespace FPTEnglishRAG.Application.Abstractions;

/// <summary>
/// Validates citation labels in model output and maps them to retrieved chunks.
/// </summary>
public interface ICitationValidator
{
    /// <summary>
    /// Validates citations from model output against the provided retrieved chunks.
    /// </summary>
    /// <param name="modelOutput">The generated model output.</param>
    /// <param name="retrievedChunks">The retrieved chunks available for citation.</param>
    /// <returns>The mapped citations that reference valid retrieved chunks.</returns>
    List<Citation> ValidateAndMap(string modelOutput, IReadOnlyList<RetrievedChunk> retrievedChunks);
}
