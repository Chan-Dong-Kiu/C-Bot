using FPTEnglishRAG.Application.DTOs;

namespace FPTEnglishRAG.Application.Abstractions.Documents;

public interface IChunkingService
{
    IReadOnlyList<ChunkResultDto> ChunkPages(
        IReadOnlyList<ExtractedPageDto> pages,
        int targetTokens = 500,
        int overlapTokens = 75,
        int minTokens = 100,
        int maxTokens = 700);
}
