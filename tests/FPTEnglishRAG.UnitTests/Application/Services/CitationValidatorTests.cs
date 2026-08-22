// File: tests/FPTEnglishRAG.UnitTests/Application/Services/CitationValidatorTests.cs

using System.Collections.Generic;
using FluentAssertions;
using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Application.Services;
using Xunit;

namespace FPTEnglishRAG.UnitTests.Application.Services;

public class CitationValidatorTests
{
    private readonly CitationValidator _sut;

    public CitationValidatorTests()
    {
        _sut = new CitationValidator();
    }

    private static List<RetrievedChunk> CreateChunks(int count)
    {
        var chunks = new List<RetrievedChunk>();
        for (int i = 0; i < count; i++)
        {
            chunks.Add(new RetrievedChunk(
                ChunkId: $"chunk-{i}",
                Content: $"Test content {i}",
                SourceDocumentName: "doc.txt",
                PageOrPosition: "1",
                SimilarityScore: 0.9));
        }
        return chunks;
    }

    [Fact]
    public void ValidateAndMap_WhenOutputContainsValidLabels_ReturnsCorrectCitationsMappedToChunks()
    {
        // Arrange
        var chunks = CreateChunks(3);
        var output = "This is a fact [S1]. Here is another [S2].";

        // Act
        var result = _sut.ValidateAndMap(output, chunks);

        // Assert
        result.Should().HaveCount(2);
        
        result[0].Label.Should().Be("S1");
        result[0].ChunkId.Should().Be("chunk-0"); // chunk 1 maps to index 0

        result[1].Label.Should().Be("S2");
        result[1].ChunkId.Should().Be("chunk-1"); // chunk 2 maps to index 1
    }

    [Fact]
    public void ValidateAndMap_WhenOutputContainsLabelExceedingChunkCount_ExcludesInvalidLabel()
    {
        // Arrange
        var chunks = CreateChunks(2); // Only 2 chunks available
        var output = "This is valid [S1], but this is hallucinated [S9].";

        // Act
        var result = _sut.ValidateAndMap(output, chunks);

        // Assert
        result.Should().HaveCount(1);
        result[0].Label.Should().Be("S1");
        result[0].ChunkId.Should().Be("chunk-0");
    }

    [Fact]
    public void ValidateAndMap_WhenOutputContainsDuplicateLabel_ReturnsSingleCitation()
    {
        // Arrange
        var chunks = CreateChunks(3);
        var output = "First mention [S1]. Later mention [S1].";

        // Act
        var result = _sut.ValidateAndMap(output, chunks);

        // Assert
        result.Should().HaveCount(1);
        result[0].Label.Should().Be("S1");
        result[0].ChunkId.Should().Be("chunk-0");
    }

    [Fact]
    public void ValidateAndMap_WhenOutputContainsNoLabels_ReturnsEmptyList()
    {
        // Arrange
        var chunks = CreateChunks(3);
        var output = "This output has no citations at all.";

        // Act
        var result = _sut.ValidateAndMap(output, chunks);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAndMap_WhenChunksEmptyButOutputContainsLabels_ReturnsEmptyList()
    {
        // Arrange
        var chunks = CreateChunks(0); // Empty chunks
        var output = "Output with fake label [S1].";

        // Act
        var result = _sut.ValidateAndMap(output, chunks);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAndMap_PreservesFirstOccurrenceOrder_WhenMultipleLabelsPresent()
    {
        // Arrange
        var chunks = CreateChunks(5);
        var output = "This refers to second chunk [S2] first, then the first one [S1].";

        // Act
        var result = _sut.ValidateAndMap(output, chunks);

        // Assert
        result.Should().HaveCount(2);
        
        // Assert the order matches the occurrence in the string
        result[0].Label.Should().Be("S2");
        result[1].Label.Should().Be("S1");
    }

    [Theory]
    [InlineData("[S]")]
    [InlineData("[Sabc]")]
    [InlineData("[1]")]
    [InlineData("S1")]
    [InlineData("[S0]")] // Optional based on rules, but practically 1-based, S0 would fail bounds check if chunks[index-1] is -1. Wait, S0 -> index -1 which is invalid.
    public void ValidateAndMap_WhenLabelFormatMalformed_IgnoresMalformedPattern(string malformedLabel)
    {
        // Arrange
        var chunks = CreateChunks(3);
        var output = $"This is some text with {malformedLabel} pattern.";

        // Act
        var result = _sut.ValidateAndMap(output, chunks);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
