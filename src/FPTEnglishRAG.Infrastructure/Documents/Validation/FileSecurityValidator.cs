using System.Security.Cryptography;
using FPTEnglishRAG.Application.Abstractions.Documents;
using FPTEnglishRAG.Application.Abstractions.Persistence;
using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Domain.Enums;

namespace FPTEnglishRAG.Infrastructure.Documents.Validation;

public class FileSecurityValidator : IDocumentValidator
{
    public const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB (nâng lên 50MB cho tài liệu ôn tập lớn)
    private static readonly byte[] PdfHeaderPattern = [0x25, 0x50, 0x44, 0x46]; // "%PDF"

    private readonly IDocumentRepository _documentRepository;

    public FileSecurityValidator(IDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async ValueTask<FileValidationResult> ValidateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return new FileValidationResult(false, DocumentErrorCode.FileNotFound, "Đường dẫn file không được để trống.", null, null, 0);
        }

        if (!File.Exists(filePath))
        {
            return new FileValidationResult(false, DocumentErrorCode.FileNotFound, $"Không tìm thấy file: {filePath}", null, null, 0);
        }

        var fileInfo = new FileInfo(filePath);

        if (fileInfo.Length == 0)
        {
            return new FileValidationResult(false, DocumentErrorCode.CorruptedFile, "File được chọn rỗng (0 bytes).", null, null, 0);
        }

        if (fileInfo.Length > MaxFileSizeBytes)
        {
            return new FileValidationResult(
                false,
                DocumentErrorCode.FileTooLarge,
                $"Dung lượng file vượt quá giới hạn 50MB (Dung lượng thực tế: {fileInfo.Length / (1024.0 * 1024.0):F2}MB).",
                null,
                null,
                fileInfo.Length);
        }

        var extension = fileInfo.Extension.ToLowerInvariant();
        string detectedMimeType;

        if (extension == ".pdf")
        {
            detectedMimeType = "application/pdf";
            // Check PDF Magic Header trong 1024 bytes đầu tiên theo chuẩn ISO PDF
            var buffer = new byte[Math.Min(1024, (int)fileInfo.Length)];
            await using var stream = File.OpenRead(filePath);
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            
            bool hasPdfHeader = false;
            for (int i = 0; i <= bytesRead - PdfHeaderPattern.Length; i++)
            {
                if (buffer[i] == PdfHeaderPattern[0] &&
                    buffer[i + 1] == PdfHeaderPattern[1] &&
                    buffer[i + 2] == PdfHeaderPattern[2] &&
                    buffer[i + 3] == PdfHeaderPattern[3])
                {
                    hasPdfHeader = true;
                    break;
                }
            }

            if (!hasPdfHeader)
            {
                return new FileValidationResult(
                    false, 
                    DocumentErrorCode.CorruptedFile, 
                    "Tệp tin không phải là định dạng PDF hợp lệ (không tìm thấy tiêu đề %PDF chuẩn).", 
                    detectedMimeType, 
                    null, 
                    fileInfo.Length);
            }
        }
        else if (extension == ".txt")
        {
            detectedMimeType = "text/plain";
        }
        else
        {
            return new FileValidationResult(
                false, 
                DocumentErrorCode.UnsupportedFormat, 
                $"Định dạng file '{extension}' không được hỗ trợ. Ứng dụng hiện hỗ trợ tệp .pdf và .txt.", 
                null, 
                null, 
                fileInfo.Length);
        }

        // Tính SHA-256
        string sha256;
        await using (var hashStream = File.OpenRead(filePath))
        {
            var hashBytes = await SHA256.HashDataAsync(hashStream, cancellationToken);
            sha256 = Convert.ToHexStringLower(hashBytes);
        }

        // Kiểm tra trùng lặp với các file đã Ready
        var existingDoc = await _documentRepository.GetBySha256Async(sha256, cancellationToken);
        if (existingDoc != null && existingDoc.Status == DocumentStatus.Ready)
        {
            return new FileValidationResult(
                false,
                DocumentErrorCode.DuplicateFile,
                $"Tài liệu này đã tồn tại trong hệ thống với tên '{existingDoc.DisplayName}' (mã băm SHA-256 trùng khớp).",
                detectedMimeType,
                sha256,
                fileInfo.Length);
        }

        return new FileValidationResult(true, DocumentErrorCode.None, null, detectedMimeType, sha256, fileInfo.Length);
    }
}
