namespace FPTEnglishRAG.Application.Abstractions.Documents;

public interface IFileStorageService
{
    Task<string> StoreFileAsync(string sourceFilePath, Guid documentId, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string storedPath, CancellationToken cancellationToken = default);
}
