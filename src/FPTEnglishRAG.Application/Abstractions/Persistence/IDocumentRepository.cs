using FPTEnglishRAG.Domain.Entities;
using FPTEnglishRAG.Domain.Enums;

namespace FPTEnglishRAG.Application.Abstractions.Persistence;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken = default);

    Task<Document?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsByHashAsync(string sha256, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(
        Guid documentId,
        DocumentStatus status,
        string? errorCode = null,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default);
}
