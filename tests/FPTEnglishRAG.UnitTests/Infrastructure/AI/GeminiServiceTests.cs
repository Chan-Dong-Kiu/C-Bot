// File: tests/FPTEnglishRAG.UnitTests/Infrastructure/AI/GeminiServiceTests.cs

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Infrastructure.AI;
using FPTEnglishRAG.Infrastructure.AI.Exceptions;
using FPTEnglishRAG.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace FPTEnglishRAG.UnitTests.Infrastructure.AI;

public class GeminiServiceTests
{
    private readonly Mock<IGeminiClient> _mockClient;
    private readonly Mock<IPromptBuilder> _mockPromptBuilder;
    private readonly Mock<ICitationValidator> _mockCitationValidator;
    private readonly Mock<ILogger<GeminiService>> _mockLogger;
    private readonly IOptions<GeminiOptions> _options;

    public GeminiServiceTests()
    {
        _mockClient = new Mock<IGeminiClient>();
        _mockPromptBuilder = new Mock<IPromptBuilder>();
        _mockCitationValidator = new Mock<ICitationValidator>();
        _mockLogger = new Mock<ILogger<GeminiService>>();

        var geminiOptions = new GeminiOptions
        {
            ChatModel = "test-model",
            Temperature = 0.2,
            MaxOutputTokens = 1000
        };
        _options = Options.Create(geminiOptions);
    }

    private GeminiService CreateSut()
    {
        return new GeminiService(
            _mockClient.Object,
            _mockPromptBuilder.Object,
            _mockCitationValidator.Object,
            _options,
            _mockLogger.Object);
    }

    private static ChatAnswerRequest CreateRequest(double threshold, params double[] chunkScores)
    {
        var chunks = new List<RetrievedChunk>();
        for (int i = 0; i < chunkScores.Length; i++)
        {
            chunks.Add(new RetrievedChunk(
                ChunkId: $"chunk-{i}",
                Content: $"Test content {i}",
                SourceDocumentName: "doc.txt",
                PageOrPosition: "1",
                SimilarityScore: chunkScores[i]));
        }

        return new ChatAnswerRequest(
            Question: "Test question?",
            RetrievedChunks: chunks,
            RecentMessages: new List<ChatMessageSnapshot>(),
            RelevanceThreshold: threshold);
    }

    [Fact]
    public async Task GenerateAnswerAsync_WhenRetrievedChunksEmpty_ReturnsNotGroundedWithoutCallingClient()
    {
        // Arrange
        var sut = CreateSut();
        var request = CreateRequest(threshold: 0.7); // Empty chunks

        // Act
        var result = await sut.GenerateAnswerAsync(request, CancellationToken.None);

        // Assert
        result.IsGrounded.Should().BeFalse();
        result.Answer.Should().Be("The provided materials do not contain enough information to answer this question.");
        _mockClient.Verify(
            x => x.GenerateContentAsync(It.IsAny<GeminiGenerateRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GenerateAnswerAsync_WhenAllChunksBelowThreshold_ReturnsNotGroundedWithoutCallingClient()
    {
        // Arrange
        var sut = CreateSut();
        var request = CreateRequest(threshold: 0.8, chunkScores: new[] { 0.5, 0.6, 0.79 });

        // Act
        var result = await sut.GenerateAnswerAsync(request, CancellationToken.None);

        // Assert
        result.IsGrounded.Should().BeFalse();
        _mockClient.Verify(
            x => x.GenerateContentAsync(It.IsAny<GeminiGenerateRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GenerateAnswerAsync_WhenAtLeastOneChunkAboveThreshold_CallsPromptBuilderAndClient()
    {
        // Arrange
        var sut = CreateSut();
        var request = CreateRequest(threshold: 0.7, chunkScores: new[] { 0.5, 0.75 });
        
        var expectedPrompt = "Test prompt";
        _mockPromptBuilder.Setup(x => x.Build(request)).Returns(expectedPrompt);

        var expectedResponse = new GeminiGenerateResponse("Mock answer", "STOP");
        _mockClient.Setup(x => x.GenerateContentAsync(It.IsAny<GeminiGenerateRequest>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedResponse);

        var expectedCitations = new List<Citation> { new Citation("S1", "chunk-1") };
        _mockCitationValidator.Setup(x => x.ValidateAndMap("Mock answer", request.RetrievedChunks))
                              .Returns(expectedCitations);

        // Act
        var result = await sut.GenerateAnswerAsync(request, CancellationToken.None);

        // Assert
        _mockPromptBuilder.Verify(x => x.Build(request), Times.Once);
        
        _mockClient.Verify(x => x.GenerateContentAsync(
            It.Is<GeminiGenerateRequest>(r => r.Prompt == expectedPrompt && r.Model == "test-model"), 
            It.IsAny<CancellationToken>()), Times.Once);

        result.IsGrounded.Should().BeTrue();
        result.Answer.Should().Be("Mock answer");
        result.Citations.Should().BeEquivalentTo(expectedCitations);
    }

    [Fact]
    public async Task GenerateAnswerAsync_WhenClientThrowsGeminiAuthenticationException_PropagatesException()
    {
        // Arrange
        var sut = CreateSut();
        var request = CreateRequest(threshold: 0.5, chunkScores: new[] { 0.8 });
        
        _mockClient.Setup(x => x.GenerateContentAsync(It.IsAny<GeminiGenerateRequest>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new GeminiAuthenticationException("Auth failed"));

        // Act
        Func<Task> act = async () => await sut.GenerateAnswerAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<GeminiAuthenticationException>();
        _mockCitationValidator.Verify(
            x => x.ValidateAndMap(It.IsAny<string>(), It.IsAny<IReadOnlyList<RetrievedChunk>>()), 
            Times.Never);
    }

    [Fact]
    public async Task GenerateAnswerAsync_WhenClientThrowsGeminiRateLimitException_PropagatesException()
    {
        // Arrange
        var sut = CreateSut();
        var request = CreateRequest(threshold: 0.5, chunkScores: new[] { 0.8 });

        _mockClient.Setup(x => x.GenerateContentAsync(It.IsAny<GeminiGenerateRequest>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new GeminiRateLimitException("Rate limited"));

        // Act
        Func<Task> act = async () => await sut.GenerateAnswerAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<GeminiRateLimitException>();
        _mockCitationValidator.Verify(
            x => x.ValidateAndMap(It.IsAny<string>(), It.IsAny<IReadOnlyList<RetrievedChunk>>()), 
            Times.Never);
    }

    [Fact]
    public async Task GenerateAnswerAsync_WhenCancellationRequested_PropagatesCancellation()
    {
        // Arrange
        var sut = CreateSut();
        var request = CreateRequest(threshold: 0.5, chunkScores: new[] { 0.8 });
        
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancel

        _mockClient.Setup(x => x.GenerateContentAsync(It.IsAny<GeminiGenerateRequest>(), cts.Token))
                   .ThrowsAsync(new OperationCanceledException(cts.Token));

        // Act
        Func<Task> act = async () => await sut.GenerateAnswerAsync(request, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GenerateAnswerAsync_PassesRetrievedChunksToCitationValidator_UnchangedOrder()
    {
        // Arrange
        var sut = CreateSut();
        var request = CreateRequest(threshold: 0.5, chunkScores: new[] { 0.6, 0.9, 0.8 });
        
        _mockPromptBuilder.Setup(x => x.Build(request)).Returns("dummy");
        _mockClient.Setup(x => x.GenerateContentAsync(It.IsAny<GeminiGenerateRequest>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new GeminiGenerateResponse("Answer", "STOP"));

        // Act
        await sut.GenerateAnswerAsync(request, CancellationToken.None);

        // Assert
        _mockCitationValidator.Verify(x => x.ValidateAndMap(
            It.IsAny<string>(), 
            It.Is<IReadOnlyList<RetrievedChunk>>(chunks => 
                chunks.Count == 3 && 
                chunks[0].SimilarityScore == 0.6 &&
                chunks[1].SimilarityScore == 0.9 &&
                chunks[2].SimilarityScore == 0.8)), 
            Times.Once);
    }
}
