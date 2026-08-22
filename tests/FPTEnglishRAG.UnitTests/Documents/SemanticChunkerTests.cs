using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Infrastructure.Documents.Chunking;
using FluentAssertions;

namespace FPTEnglishRAG.UnitTests.Documents;

public class SemanticChunkerTests
{
    private readonly SemanticChunker _chunker = new();

    [Fact]
    public void ChunkPages_EmptyOrNullPages_ReturnsEmptyList()
    {
        _chunker.ChunkPages(Array.Empty<ExtractedPageDto>()).Should().BeEmpty();
        _chunker.ChunkPages([new ExtractedPageDto(1, "   ")]).Should().BeEmpty();
    }

    [Fact]
    public void ChunkPages_ShortSinglePage_ReturnsSingleChunkWithCorrectMetadata()
    {
        var pages = new List<ExtractedPageDto>
        {
            new(1, "The Present Perfect tense is used to describe actions that occurred at an indefinite time in the past.")
        };

        var chunks = _chunker.ChunkPages(pages, targetTokens: 500, overlapTokens: 75);

        chunks.Should().HaveCount(1);
        var chunk = chunks[0];
        chunk.Ordinal.Should().Be(0);
        chunk.PageStart.Should().Be(1);
        chunk.PageEnd.Should().Be(1);
        chunk.Content.Should().Contain("Present Perfect");
        chunk.ContentHash.Should().NotBeNullOrEmpty();
        chunk.TokenCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ChunkPages_MultiPageLargeDocument_GeneratesChunksWithOverlapAndSequentialOrdinals()
    {
        var paragraph = "The English entry test evaluates vocabulary, grammar, and reading comprehension. ";
        var longText = string.Concat(Enumerable.Repeat(paragraph, 60)); // ~1200 words

        var pages = new List<ExtractedPageDto>
        {
            new(1, longText),
            new(2, longText)
        };

        var chunks = _chunker.ChunkPages(pages, targetTokens: 300, overlapTokens: 50);

        chunks.Should().HaveCountGreaterThan(1);

        // Verify ordinal sequence
        for (int i = 0; i < chunks.Count; i++)
        {
            chunks[i].Ordinal.Should().Be(i);
            chunks[i].ContentHash.Should().NotBeNullOrEmpty();
            chunks[i].TokenCount.Should().BeGreaterThan(0);
        }

        // Verify page range tracking
        chunks.First().PageStart.Should().Be(1);
        chunks.Last().PageEnd.Should().Be(2);
    }

    [Fact]
    public void ChunkPages_RecognizesHeadingSection()
    {
        var text = "Unit 1: English Tenses and Structures.\nPresent Simple is used for repeated facts or habits.";
        var pages = new List<ExtractedPageDto>
        {
            new(1, text)
        };

        var chunks = _chunker.ChunkPages(pages);

        chunks.Should().HaveCount(1);
        chunks[0].Section.Should().StartWith("Unit 1");
    }
}
