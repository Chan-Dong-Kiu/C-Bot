using FPTEnglishRAG.Application.DTOs;

namespace FPTEnglishRAG.Application.Abstractions.Documents;

public interface IDocumentValidator
{
    ValueTask<FileValidationResult> ValidateAsync(string filePath, CancellationToken cancellationToken = default);
}
