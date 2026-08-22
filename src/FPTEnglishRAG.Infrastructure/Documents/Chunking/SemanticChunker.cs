using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FPTEnglishRAG.Application.Abstractions.Documents;
using FPTEnglishRAG.Application.DTOs;

namespace FPTEnglishRAG.Infrastructure.Documents.Chunking;

public partial class SemanticChunker : IChunkingService
{
    [GeneratedRegex(@"(?<=[.!?])\s+|\n{2,}")]
    private static partial Regex SentenceSplitRegex();

    [GeneratedRegex(@"^(#+\s+|Unit\s+\d+|Chapter\s+\d+|Part\s+\d+|Section\s+\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex HeadingRegex();

    public IReadOnlyList<ChunkResultDto> ChunkPages(
        IReadOnlyList<ExtractedPageDto> pages,
        int targetTokens = 500,
        int overlapTokens = 75,
        int minTokens = 100,
        int maxTokens = 700)
    {
        if (pages == null || pages.Count == 0)
        {
            return Array.Empty<ChunkResultDto>();
        }

        // 1. Tách tất cả các trang thành danh sách câu/đoạn kèm thông tin trang
        var sentenceItems = new List<SentenceItem>();
        foreach (var page in pages)
        {
            if (string.IsNullOrWhiteSpace(page.RawText))
            {
                continue;
            }

            var parts = SentenceSplitRegex().Split(page.RawText);
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    sentenceItems.Add(new SentenceItem(trimmed, page.PageNumber, EstimateTokenCount(trimmed)));
                }
            }
        }

        if (sentenceItems.Count == 0)
        {
            return Array.Empty<ChunkResultDto>();
        }

        var chunks = new List<ChunkResultDto>();
        int currentIndex = 0;
        int ordinal = 0;

        while (currentIndex < sentenceItems.Count)
        {
            var chunkSentences = new List<SentenceItem>();
            int currentTokens = 0;
            int startIndex = currentIndex;
            int pageStart = sentenceItems[currentIndex].PageNumber;
            int pageEnd = pageStart;

            // Gom các câu vào chunk hiện tại
            while (currentIndex < sentenceItems.Count)
            {
                var nextItem = sentenceItems[currentIndex];
                int nextTotalTokens = currentTokens + nextItem.TokenCount;

                if (chunkSentences.Count > 0 && nextTotalTokens > targetTokens)
                {
                    // Nếu đã đạt targetTokens, dừng chunk hiện tại
                    break;
                }

                chunkSentences.Add(nextItem);
                currentTokens += nextItem.TokenCount;
                pageEnd = nextItem.PageNumber;
                currentIndex++;

                if (currentTokens >= maxTokens)
                {
                    break;
                }
            }

            if (chunkSentences.Count == 0 && currentIndex < sentenceItems.Count)
            {
                var singleItem = sentenceItems[currentIndex];
                chunkSentences.Add(singleItem);
                currentTokens += singleItem.TokenCount;
                pageEnd = singleItem.PageNumber;
                currentIndex++;
            }

            // Tạo nội dung chunk
            var chunkText = string.Join(" ", chunkSentences.Select(s => s.Text)).Trim();
            var section = ExtractSectionHeading(chunkText);
            var contentHash = ComputeSha256(chunkText);

            chunks.Add(new ChunkResultDto(
                Ordinal: ordinal++,
                PageStart: pageStart,
                PageEnd: pageEnd,
                Section: section,
                Content: chunkText,
                ContentHash: contentHash,
                TokenCount: currentTokens));

            // Nếu đã duyệt hết tất cả câu thì kết thúc
            if (currentIndex >= sentenceItems.Count)
            {
                break;
            }

            // Tính toán overlap cho chunk kế tiếp bằng cách lùi lại từ cuối chunk hiện tại
            int overlapCount = 0;
            int overlapIndex = currentIndex - 1;

            while (overlapIndex > startIndex && overlapCount < overlapTokens)
            {
                overlapCount += sentenceItems[overlapIndex].TokenCount;
                if (overlapCount <= overlapTokens)
                {
                    overlapIndex--;
                }
                else
                {
                    break;
                }
            }

            // Đảm bảo luôn tiến về phía trước ít nhất 1 câu để tránh vòng lặp vô tận
            int nextStartIndex = Math.Max(startIndex + 1, overlapIndex + 1);
            currentIndex = nextStartIndex;
        }

        return chunks;
    }

    public static int EstimateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        // Ước tính token cho text tiếng Anh & tiếng Việt: ~4 ký tự hoặc 0.75 từ / token
        int charCount = text.Length;
        int wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        int estimated = (int)Math.Ceiling(Math.Max(charCount / 4.0, wordCount * 1.3));
        return Math.Max(1, estimated);
    }

    private static string? ExtractSectionHeading(string text)
    {
        var firstLine = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (firstLine != null && HeadingRegex().IsMatch(firstLine))
        {
            return firstLine.Length > 80 ? firstLine[..80] + "..." : firstLine;
        }
        return null;
    }

    private static string ComputeSha256(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    private record SentenceItem(string Text, int PageNumber, int TokenCount);
}
