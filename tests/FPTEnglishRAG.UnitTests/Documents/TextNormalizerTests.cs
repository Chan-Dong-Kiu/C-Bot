using FPTEnglishRAG.Infrastructure.Documents.Cleaning;
using FluentAssertions;

namespace FPTEnglishRAG.UnitTests.Documents;

public class TextNormalizerTests
{
    private readonly TextNormalizer _normalizer = new();

    [Fact]
    public void Normalize_NullOrWhitespace_ReturnsEmpty()
    {
        _normalizer.Normalize("").Should().BeEmpty();
        _normalizer.Normalize("   \t  \n  ").Should().BeEmpty();
    }

    [Fact]
    public void Normalize_HyphenatedLineBreak_JoinsWords()
    {
        var input = "This is an impor-\ntant docu-\r\nment for assessment.";
        var expected = "This is an important document for assessment.";

        var result = _normalizer.Normalize(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void Normalize_MultipleSpacesAndTabs_CollapsesToSingleSpace()
    {
        var input = "English    Grammar\t\tPractice   Test";
        var expected = "English Grammar Practice Test";

        var result = _normalizer.Normalize(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void Normalize_ConsecutiveBlankLines_PreservesParagraphStructure()
    {
        var input = "Paragraph 1\n\n\n\n\nParagraph 2";
        var expected = "Paragraph 1\n\nParagraph 2";

        var result = _normalizer.Normalize(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void Normalize_VietnameseUnicode_NormalizesToFormC()
    {
        // Chữ "Tiếng Việt" dạng decomposed (NFD)
        var nfd = "Tie\u0302\u0301ng Vie\u0323\u0302t";
        var result = _normalizer.Normalize(nfd);

        result.Should().Be("Tiếng Việt");
    }

    [Fact]
    public void Normalize_StripsNonPrintableControlChars()
    {
        var input = "Clean\x00\x01\x07 Text\x1F Here";
        var result = _normalizer.Normalize(input);

        result.Should().Be("Clean Text Here");
    }
}
