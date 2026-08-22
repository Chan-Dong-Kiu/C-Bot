using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Application.Abstractions.Documents;
using FPTEnglishRAG.Application.Abstractions.Persistence;
using FPTEnglishRAG.Application.Abstractions.RAG;
using FPTEnglishRAG.Application.Configuration;
using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Domain.Entities;
using FPTEnglishRAG.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FPTEnglishRAG.Application.Services;

public class DocumentIngestionService : IDocumentIngestionService
{
    private readonly IDocumentValidator _validator;
    private readonly IEnumerable<IDocumentTextExtractor> _extractors;
    private readonly ITextNormalizer _normalizer;
    private readonly IChunkingService _chunker;
    private readonly IFileStorageService _fileStorage;
    private readonly IDocumentRepository _repository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly VectorIndexOptions _vectorIndexOptions;
    private readonly ILogger<DocumentIngestionService>? _logger;

    public DocumentIngestionService(
        IDocumentValidator validator,
        IEnumerable<IDocumentTextExtractor> extractors,
        ITextNormalizer normalizer,
        IChunkingService chunker,
        IFileStorageService fileStorage,
        IDocumentRepository repository,
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        VectorIndexOptions vectorIndexOptions,
        ILogger<DocumentIngestionService>? logger = null)
    {
        _validator = validator;
        _extractors = extractors;
        _normalizer = normalizer;
        _chunker = chunker;
        _fileStorage = fileStorage;
        _repository = repository;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _vectorIndexOptions = vectorIndexOptions;
        _vectorIndexOptions.Validate();
        _logger = logger;
    }

    public async Task<Document> IngestDocumentAsync(
        string sourceFilePath,
        IProgress<IngestionProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var fileName = Path.GetFileName(sourceFilePath);
        var documentId = Guid.NewGuid();

        // 1. Kiểm tra tính hợp lệ của file
        var validation = await _validator.ValidateAsync(sourceFilePath, cancellationToken);
        if (!validation.IsValid)
        {
            var failedDoc = new Document
            {
                Id = documentId,
                DisplayName = fileName,
                StoredPath = string.Empty,
                MimeType = validation.DetectedMimeType ?? "application/octet-stream",
                Sha256 = validation.Sha256Hash ?? string.Empty,
                Status = DocumentStatus.Failed,
                ErrorCode = validation.ErrorCode.ToString(),
                ErrorMessage = validation.ErrorMessage
            };

            await _repository.AddAsync(failedDoc, cancellationToken);
            progress?.Report(new IngestionProgressReport(documentId, fileName, DocumentStatus.Failed, 100, validation.ErrorMessage ?? "File không hợp lệ"));
            return failedDoc;
        }

        // 2. Lưu file vào thư mục nội bộ AppData
        string storedPath;
        try
        {
            storedPath = await _fileStorage.StoreFileAsync(sourceFilePath, documentId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi khi lưu trữ file {FileName}", fileName);
            var failedDoc = new Document
            {
                Id = documentId,
                DisplayName = fileName,
                StoredPath = string.Empty,
                MimeType = validation.DetectedMimeType!,
                Sha256 = validation.Sha256Hash!,
                Status = DocumentStatus.Failed,
                ErrorCode = DocumentErrorCode.DatabaseError.ToString(),
                ErrorMessage = "Không thể lưu file vào thư mục ứng dụng."
            };
            await _repository.AddAsync(failedDoc, cancellationToken);
            return failedDoc;
        }

        // 3. Tạo Entity Document trạng thái Pending
        var document = new Document
        {
            Id = documentId,
            DisplayName = fileName,
            StoredPath = storedPath,
            MimeType = validation.DetectedMimeType!,
            Sha256 = validation.Sha256Hash!,
            Status = DocumentStatus.Pending
        };
        await _repository.AddAsync(document, cancellationToken);
        progress?.Report(new IngestionProgressReport(documentId, fileName, DocumentStatus.Pending, 10, "Đang khởi tạo tài liệu..."));

        return await ProcessDocumentPipelineAsync(document, progress, cancellationToken);
    }

    public async Task<Document> RetryIngestionAsync(
        Guid documentId,
        IProgress<IngestionProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var document = await _repository.GetByIdAsync(documentId, cancellationToken);
        if (document == null)
        {
            throw new InvalidOperationException($"Không tìm thấy tài liệu với ID: {documentId}");
        }

        if (!File.Exists(document.StoredPath))
        {
            document.MarkFailed(DocumentErrorCode.FileNotFound, "Không tìm thấy file nguồn đã lưu trong hệ thống để retry.");
            await _repository.UpdateAsync(document, cancellationToken);
            return document;
        }

        return await ProcessDocumentPipelineAsync(document, progress, cancellationToken);
    }

    public async Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _repository.GetByIdAsync(documentId, cancellationToken);
        if (document != null)
        {
            await _fileStorage.DeleteFileAsync(document.StoredPath, cancellationToken);
            await _repository.DeleteAsync(documentId, cancellationToken);
        }
    }

    private async Task<Document> ProcessDocumentPipelineAsync(
        Document document,
        IProgress<IngestionProgressReport>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Trích xuất nội dung (Extracting)
            document.MarkExtracting();
            await _repository.UpdateAsync(document, cancellationToken);
            progress?.Report(new IngestionProgressReport(document.Id, document.DisplayName, DocumentStatus.Extracting, 25, "Đang trích xuất nội dung văn bản..."));

            var extractor = _extractors.FirstOrDefault(e => e.CanHandle(document.MimeType))
                ?? throw new InvalidOperationException($"Không tìm thấy bộ trích xuất cho định dạng: {document.MimeType}");

            var rawPages = await extractor.ExtractPagesAsync(document.StoredPath, cancellationToken);
            if (rawPages.Count == 0 || rawPages.All(p => string.IsNullOrWhiteSpace(p.RawText)))
            {
                throw new InvalidOperationException("Tài liệu không chứa nội dung chữ có thể đọc (đây có thể là file PDF scan/ảnh chụp, chưa có lớp văn bản). Vui lòng dùng file PDF dạng chữ (text-based PDF) hoặc file TXT.");
            }

            // 2. Làm sạch & Chuẩn hóa (Cleaning)
            var normalizedPages = rawPages
                .Select(p => new ExtractedPageDto(p.PageNumber, _normalizer.Normalize(p.RawText)))
                .Where(p => !string.IsNullOrWhiteSpace(p.RawText))
                .ToList();

            // 3. Phân đoạn ngữ nghĩa (Chunking)
            document.MarkChunking(rawPages.Count);
            await _repository.UpdateAsync(document, cancellationToken);
            progress?.Report(new IngestionProgressReport(document.Id, document.DisplayName, DocumentStatus.Chunking, 50, "Đang phân chia đoạn văn bản ngữ nghĩa (chunking)..."));

            var chunkDtos = _chunker.ChunkPages(normalizedPages);
            if (chunkDtos.Count == 0)
            {
                throw new InvalidOperationException("Không thể tạo các đoạn văn bản từ nội dung đã trích xuất.");
            }

            var chunkEntities = chunkDtos.Select(c => new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                Ordinal = c.Ordinal,
                PageStart = c.PageStart,
                PageEnd = c.PageEnd,
                Section = c.Section,
                Content = c.Content,
                ContentHash = c.ContentHash,
                TokenCount = c.TokenCount
            }).ToList();

            // 4. Tạo Embedding (Embedding)
            document.MarkEmbedding(chunkEntities.Count);
            await _repository.UpdateAsync(document, cancellationToken);
            progress?.Report(new IngestionProgressReport(document.Id, document.DisplayName, DocumentStatus.Embedding, 75, "Đang tạo vector embeddings..."));

            var vectors = new List<float[]>(chunkEntities.Count);
            foreach (DocumentChunk chunk in chunkEntities)
            {
                vectors.Add(await _embeddingService.EmbedQueryAsync(chunk.Content, cancellationToken));
            }

            ValidateVectors(chunkEntities, vectors);

            // 5. Lưu Chunks vào Database
            await _repository.SaveChunksAsync(document.Id, chunkEntities, cancellationToken);

            for (int index = 0; index < chunkEntities.Count; index++)
            {
                float[] vector = vectors[index];
                await _vectorStore.UpsertAsync(
                    new VectorRecord(
                        chunkEntities[index].Id,
                        _vectorIndexOptions.EmbeddingModel,
                        vector.Length,
                        vector,
                        _vectorIndexOptions.IndexVersion),
                    cancellationToken);
            }

            // 6. Đánh dấu hoàn tất (Ready)
            document.MarkReady();
            await _repository.UpdateAsync(document, cancellationToken);
            progress?.Report(new IngestionProgressReport(document.Id, document.DisplayName, DocumentStatus.Ready, 100, "Xử lý tài liệu hoàn tất!"));

            return document;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Lỗi trong quá trình Ingestion cho tài liệu {DisplayName}", document.DisplayName);
            await _vectorStore.DeleteByDocumentIdAsync(document.Id, CancellationToken.None);
            DocumentErrorCode errorCode = document.Status switch
            {
                DocumentStatus.Embedding => DocumentErrorCode.EmbeddingFailed,
                DocumentStatus.Chunking => DocumentErrorCode.ChunkingFailed,
                _ => DocumentErrorCode.ExtractionFailed
            };
            document.MarkFailed(errorCode, ex.Message);
            await _repository.UpdateAsync(document, CancellationToken.None);
            progress?.Report(new IngestionProgressReport(document.Id, document.DisplayName, DocumentStatus.Failed, 100, $"Xử lý thất bại: {ex.Message}"));
            return document;
        }
    }

    private static void ValidateVectors(
        IReadOnlyList<DocumentChunk> chunks,
        IReadOnlyList<float[]> vectors)
    {
        if (vectors.Count != chunks.Count)
        {
            throw new InvalidOperationException("The embedding count must match the document chunk count.");
        }

        int dimensions = vectors.Count > 0 ? vectors[0].Length : 0;
        if (dimensions <= 0 || vectors.Any(vector => vector.Length != dimensions))
        {
            throw new InvalidOperationException("All document embeddings must be non-empty and have equal dimensions.");
        }
    }
}
