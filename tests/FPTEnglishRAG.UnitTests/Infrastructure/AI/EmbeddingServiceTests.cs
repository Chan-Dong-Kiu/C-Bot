// File: tests/FPTEnglishRAG.UnitTests/Infrastructure/AI/EmbeddingServiceTests.cs

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FPTEnglishRAG.Infrastructure.AI;
using FPTEnglishRAG.Infrastructure.AI.Exceptions;
using FPTEnglishRAG.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace FPTEnglishRAG.UnitTests.Infrastructure.AI;

public class EmbeddingServiceTests
{
    private readonly Mock<IGeminiClient> _mockClient;
    private readonly IOptions<GeminiOptions> _options;
    private readonly EmbeddingService _sut;

    public EmbeddingServiceTests()
    {
        _mockClient = new Mock<IGeminiClient>();
        var options = new GeminiOptions
        {
            EmbeddingModel = "test-embed-model"
        };
        _options = Options.Create(options);

        _sut = new EmbeddingService(_mockClient.Object, _options);
    }

    [Fact]
    public async Task EmbedQueryAsync_WhenTextValid_ReturnsVectorFromClient()
    {
        // Arrange
        var text = "Hello world";
        var expectedVector = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
        var mockResponse = new GeminiEmbedResponse(expectedVector);

        _mockClient
            .Setup(x => x.EmbedContentAsync(
                It.Is<GeminiEmbedRequest>(r => r.Content == text && r.Model == "test-embed-model"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _sut.EmbedQueryAsync(text, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedVector);
        _mockClient.Verify(x => x.EmbedContentAsync(
            It.Is<GeminiEmbedRequest>(r => r.Content == text), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmbedQueryAsync_WhenTextIsNullOrWhitespace_ThrowsArgumentException(string? invalidText)
    {
        // Act
        Func<Task> act = async () => await _sut.EmbedQueryAsync(invalidText, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Query text cannot be null or whitespace.*");

        _mockClient.Verify(
            x => x.EmbedContentAsync(It.IsAny<GeminiEmbedRequest>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task EmbedQueryAsync_WhenClientThrowsGeminiRateLimitException_PropagatesException()
    {
        // Arrange
        var text = "Hello world";
        
        _mockClient
            .Setup(x => x.EmbedContentAsync(It.IsAny<GeminiEmbedRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GeminiRateLimitException("Rate limit exceeded"));

        // Act
        Func<Task> act = async () => await _sut.EmbedQueryAsync(text, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<GeminiRateLimitException>();
    }

    [Fact]
    public async Task EmbedQueryAsync_WhenCancellationRequested_PassesCancellationTokenToClient()
    {
        // Arrange
        var text = "Hello world";
        using var cts = new CancellationTokenSource();
        var expectedToken = cts.Token;
        
        _mockClient
            .Setup(x => x.EmbedContentAsync(It.IsAny<GeminiEmbedRequest>(), expectedToken))
            .ReturnsAsync(new GeminiEmbedResponse(new float[] { 0.1f }));

        // Act
        await _sut.EmbedQueryAsync(text, expectedToken);

        // Assert
        _mockClient.Verify(
            x => x.EmbedContentAsync(It.IsAny<GeminiEmbedRequest>(), It.Is<CancellationToken>(t => t == expectedToken)), 
            Times.Once);
    }

    [Fact]
    public async Task EmbedQueryAsync_PassesExactTextToClient_WithoutModification()
    {
        // Arrange
        var exactText = "  Hello \n \t world  "; // Leading/trailing spaces, newlines, tabs
        
        _mockClient
            .Setup(x => x.EmbedContentAsync(It.IsAny<GeminiEmbedRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeminiEmbedResponse(new float[] { 0.1f }));

        // Act
        await _sut.EmbedQueryAsync(exactText, CancellationToken.None);

        // Assert
        _mockClient.Verify(
            x => x.EmbedContentAsync(It.Is<GeminiEmbedRequest>(r => r.Content == exactText), It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}
