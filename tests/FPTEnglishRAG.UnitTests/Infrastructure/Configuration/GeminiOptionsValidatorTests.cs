// File: tests/FPTEnglishRAG.UnitTests/Infrastructure/Configuration/GeminiOptionsValidatorTests.cs

using FluentAssertions;
using FPTEnglishRAG.Infrastructure.Configuration;
using Xunit;

namespace FPTEnglishRAG.UnitTests.Infrastructure.Configuration;

public class GeminiOptionsValidatorTests
{
    private readonly GeminiOptionsValidator _sut;

    public GeminiOptionsValidatorTests()
    {
        _sut = new GeminiOptionsValidator();
    }

    private static GeminiOptions CreateValidOptions()
    {
        return new GeminiOptions
        {
            ApiKey = "VALID_API_KEY",
            Endpoint = "https://api.gemini.com/v1beta",
            ChatModel = "gemini-test-chat",
            EmbeddingModel = "gemini-test-embed",
            TimeoutSeconds = 30,
            EmbeddingTimeoutSeconds = 15,
            MaxRetries = 3,
            UseFake = false
        };
    }

    [Fact]
    public void Validate_WhenAllFieldsValid_ReturnsSuccess()
    {
        // Arrange
        var options = CreateValidOptions();

        // Act
        var result = _sut.Validate(string.Empty, options);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Failed.Should().BeFalse();
        result.FailureMessage.Should().BeNull();
    }

    [Fact]
    public void Validate_WhenApiKeyEmpty_ReturnsFailWithMessageNotContainingActualValue()
    {
        // Arrange
        var options = CreateValidOptions();
        options.ApiKey = "   "; // whitespace to trigger IsNullOrWhiteSpace

        // Act
        var result = _sut.Validate(string.Empty, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Failed.Should().BeTrue();
        
        var message = result.FailureMessage;
        message.Should().NotBeNullOrWhiteSpace();
        message.Should().Contain("user-secrets");
        message.Should().Contain("GEMINI_API_KEY");
        // Ensure the actual value is not echoed back
        message.Should().NotContain("   ");
    }

    [Fact]
    public void Validate_WhenChatModelEmpty_ReturnsFail()
    {
        // Arrange
        var options = CreateValidOptions();
        options.ChatModel = "";

        // Act
        var result = _sut.Validate(string.Empty, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("ChatModel");
    }

    [Fact]
    public void Validate_WhenEmbeddingModelEmpty_ReturnsFail()
    {
        // Arrange
        var options = CreateValidOptions();
        options.EmbeddingModel = "";

        // Act
        var result = _sut.Validate(string.Empty, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("EmbeddingModel");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenTimeoutSecondsIsZeroOrNegative_ReturnsFail(int invalidTimeout)
    {
        // Arrange
        var options = CreateValidOptions();
        options.TimeoutSeconds = invalidTimeout;

        // Act
        var result = _sut.Validate(string.Empty, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("TimeoutSeconds must be greater than 0");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenEmbeddingTimeoutSecondsIsZeroOrNegative_ReturnsFail(int invalidTimeout)
    {
        // Arrange
        var options = CreateValidOptions();
        options.EmbeddingTimeoutSeconds = invalidTimeout;

        // Act
        var result = _sut.Validate(string.Empty, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("EmbeddingTimeoutSeconds must be greater than 0");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenMaxRetriesIsZeroOrNegative_ReturnsFail(int invalidRetries)
    {
        // Arrange
        var options = CreateValidOptions();
        options.MaxRetries = invalidRetries;

        // Act
        var result = _sut.Validate(string.Empty, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("MaxRetries must be greater than 0");
    }

    [Fact]
    public void Validate_WhenMultipleFieldsInvalid_ReturnsFailWithAllErrorsCombined()
    {
        // Arrange
        var options = CreateValidOptions();
        options.ApiKey = "";
        options.ChatModel = "";

        // Act
        var result = _sut.Validate(string.Empty, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        
        var message = result.FailureMessage;
        message.Should().NotBeNull();
        
        // Assert multiple errors are present in the combined message
        message.Should().Contain("Gemini API key is missing");
        message.Should().Contain("ChatModel is required");
    }
}
