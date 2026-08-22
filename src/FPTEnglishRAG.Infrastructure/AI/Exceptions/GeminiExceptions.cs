// File: src/FPTEnglishRAG.Infrastructure/AI/Exceptions/GeminiExceptions.cs

namespace FPTEnglishRAG.Infrastructure.AI.Exceptions;

/// <summary>
/// Base exception for all Gemini API errors.
/// </summary>
public abstract class GeminiApiException : Exception
{
    protected GeminiApiException(string message) : base(message) { }
    protected GeminiApiException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when the Gemini API returns a 401 or 403 status code, indicating an invalid or missing API key.
/// </summary>
public sealed class GeminiAuthenticationException : GeminiApiException
{
    public GeminiAuthenticationException(string message) : base(message) { }
}

/// <summary>
/// Thrown when the Gemini API returns a 429 status code, indicating quota has been exceeded.
/// </summary>
public sealed class GeminiRateLimitException : GeminiApiException
{
    public GeminiRateLimitException(string message) : base(message) { }
}

/// <summary>
/// Thrown when the Gemini API returns a 5xx status code or a network timeout occurs.
/// </summary>
public sealed class GeminiTransientException : GeminiApiException
{
    public GeminiTransientException(string message) : base(message) { }
    public GeminiTransientException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when the Gemini API returns a 400 status code, indicating a malformed request or invalid parameters.
/// </summary>
public sealed class GeminiInvalidRequestException : GeminiApiException
{
    public GeminiInvalidRequestException(string message) : base(message) { }
}

/// <summary>
/// Thrown when the Gemini API returns an unexpected status code not covered by other specific exceptions.
/// </summary>
public sealed class GeminiUnexpectedException : GeminiApiException
{
    public GeminiUnexpectedException(string message) : base(message) { }
}
