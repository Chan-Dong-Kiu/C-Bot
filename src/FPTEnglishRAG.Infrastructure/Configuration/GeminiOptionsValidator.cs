// File: src/FPTEnglishRAG.Infrastructure/Configuration/GeminiOptionsValidator.cs

using Microsoft.Extensions.Options;

namespace FPTEnglishRAG.Infrastructure.Configuration;

/// <summary>
/// Custom validator for <see cref="GeminiOptions"/> that ensures all required properties
/// are valid and that the API key is securely present before the application starts.
/// </summary>
public sealed class GeminiOptionsValidator : IValidateOptions<GeminiOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, GeminiOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            errors.Add("Endpoint is required and cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.ChatModel))
        {
            errors.Add("ChatModel is required and cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.EmbeddingModel))
        {
            errors.Add("EmbeddingModel is required and cannot be empty.");
        }

        if (options.TimeoutSeconds <= 0)
        {
            errors.Add("TimeoutSeconds must be greater than 0.");
        }

        if (options.EmbeddingTimeoutSeconds <= 0)
        {
            errors.Add("EmbeddingTimeoutSeconds must be greater than 0.");
        }

        if (options.MaxRetries <= 0) // Prompt requested > 0
        {
            errors.Add("MaxRetries must be greater than 0.");
        }

        if (!options.UseFake && string.IsNullOrWhiteSpace(options.ApiKey))
        {
            errors.Add("Gemini API key is missing. Please set the 'Gemini:ApiKey' value via user-secrets or use the 'GEMINI_API_KEY' environment variable. Do not hardcode it in the source.");
        }

        if (errors.Count > 0)
        {
            return ValidateOptionsResult.Fail(errors);
        }

        return ValidateOptionsResult.Success;
    }
}
