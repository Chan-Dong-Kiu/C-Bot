using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Domain.Entities;
using Citation = FPTEnglishRAG.Domain.ValueObjects.Citation;

namespace FPTEnglishRAG.Application.Services;

public class FakeChatService : IChatService
{
    private readonly TimeSpan _delay;

    public FakeChatService(TimeSpan? delay = null)
    {
        _delay = delay ?? TimeSpan.FromMilliseconds(600);
    }

    public async Task<ChatAnswer> AskAsync(
        string question,
        IReadOnlyList<ChatMessage> recentHistory,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);

        // Simulate async processing
        if (_delay > TimeSpan.Zero)
        {
            await Task.Delay(_delay, ct);
        }

        var normalized = question.Trim().ToLowerInvariant();

        // Simulate failure if keyword present
        if (normalized.Contains("error") || normalized.Contains("fail") || normalized.Contains("lỗi"))
        {
            throw new HttpRequestException("Simulated API failure: Unable to reach Gemini endpoint (503 Service Unavailable).");
        }

        // Simulate ungrounded fallback
        if (normalized.Contains("không có") || normalized.Contains("unrelated") || normalized.Contains("unknown"))
        {
            return new ChatAnswer(
                "Tài liệu hiện có chưa đủ thông tin để trả lời câu hỏi này. Bạn vui lòng import thêm tài liệu liên quan đến chủ đề trên.",
                Array.Empty<Citation>(),
                IsGrounded: false);
        }

        // Default smart mock answers for FPT English assessment
        var docId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var docId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var chunkId1 = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var chunkId2 = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var citations = new List<Citation>
        {
            new(
                Label: "[S1]",
                DocumentName: "FPT_English_Entry_Test_Preparation_Guide.pdf",
                Page: 14,
                Snippet: "In conditional sentences type 2, the if-clause uses the simple past tense (were/V2), while the main clause uses would + bare infinitive to describe hypothetical situations in the present or future.",
                DocumentId: docId1,
                ChunkId: chunkId1),
            new(
                Label: "[S2]",
                DocumentName: "Grammar_And_Vocabulary_Workbook_2025.txt",
                Page: null,
                Snippet: "Example: 'If I had a million dollars, I would travel around the world.' Note that 'were' is conventionally used for all subjects in formal contexts.",
                DocumentId: docId2,
                ChunkId: chunkId2)
        };

        var answerText =
            $"Dựa vào tài liệu ôn tập tiếng Anh đầu vào [S1], cấu trúc câu điều kiện loại 2 được dùng để diễn tả một giả định không có thật ở hiện tại:\n\n" +
            $"• **Mệnh đề If**: `If + S + V-ed/V2` (với to be dùng `were` cho mọi ngôi).\n" +
            $"• **Mệnh đề chính**: `S + would / could + V (nguyên thể)` [S1].\n\n" +
            $"Ví dụ cụ thể từ giáo trình [S2]: *\"If I had a million dollars, I would travel around the world.\"*\n\n" +
            $"Lưu ý: Đối với câu hỏi của bạn *\"{question.Trim()}\"*, hãy chú ý dạng chia động từ và ngữ cảnh câu hỏi trong đề thi FPT.";

        return new ChatAnswer(answerText, citations, IsGrounded: true);
    }
}
