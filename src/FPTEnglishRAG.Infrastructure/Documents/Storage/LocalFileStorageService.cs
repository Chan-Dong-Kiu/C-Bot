using FPTEnglishRAG.Application.Abstractions.Documents;

namespace FPTEnglishRAG.Infrastructure.Documents.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageDirectory;

    public LocalFileStorageService(string? baseDirectory = null)
    {
        _storageDirectory = baseDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FPTEnglishRAG",
            "Documents");

        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }
    }

    public async Task<string> StoreFileAsync(string sourceFilePath, Guid documentId, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException($"Không tìm thấy file nguồn: {sourceFilePath}");
        }

        var extension = Path.GetExtension(sourceFilePath);
        var targetFileName = $"{documentId:N}{extension}";
        var targetFilePath = Path.Combine(_storageDirectory, targetFileName);

        await using var sourceStream = File.OpenRead(sourceFilePath);
        await using var targetStream = File.Create(targetFilePath);
        await sourceStream.CopyToAsync(targetStream, cancellationToken);

        return targetFilePath;
    }

    public Task DeleteFileAsync(string storedPath, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(storedPath) && File.Exists(storedPath))
        {
            File.Delete(storedPath);
        }

        return Task.CompletedTask;
    }
}
