using FPTEnglishRAG.Application.DTOs;

namespace FPTEnglishRAG.Application.Abstractions;

/// <summary>
/// Builds grounded prompts from chat answer requests.
/// </summary>
public interface IPromptBuilder
{
    /// <summary>
    /// Builds the prompt sent to the generation model.
    /// </summary>
    /// <param name="request">The chat answer request.</param>
    /// <returns>A prompt string.</returns>
    string Build(ChatAnswerRequest request);
}
