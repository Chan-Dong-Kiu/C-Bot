using CommunityToolkit.Mvvm.ComponentModel;
using FPTEnglishRAG.Domain.Entities;
using FPTEnglishRAG.Domain.Enums;

namespace FPTEnglishRAG.Wpf.ViewModels;

public partial class DocumentItemViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _mimeType = string.Empty;

    [ObservableProperty]
    private DocumentStatus _status;

    [ObservableProperty]
    private string? _errorCode;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _pageCount;

    [ObservableProperty]
    private int _chunkCount;

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private DateTime _updatedAt;

    public string StatusText => Status switch
    {
        DocumentStatus.Pending => "Chờ xử lý",
        DocumentStatus.Extracting => "Đang trích xuất",
        DocumentStatus.Chunking => "Đang phân đoạn",
        DocumentStatus.Embedding => "Đang tạo vector",
        DocumentStatus.Ready => "Sẵn sàng",
        DocumentStatus.Failed => "Thất bại",
        _ => Status.ToString()
    };

    public string StatusBadgeColor => Status switch
    {
        DocumentStatus.Pending => "#EAB308",     // Yellow
        DocumentStatus.Extracting => "#3B82F6",  // Blue
        DocumentStatus.Chunking => "#8B5CF6",    // Purple
        DocumentStatus.Embedding => "#F97316",   // Orange
        DocumentStatus.Ready => "#10B981",       // Green
        DocumentStatus.Failed => "#EF4444",      // Red
        _ => "#6B7280"
    };

    public bool CanRetry => Status == DocumentStatus.Failed;

    public static DocumentItemViewModel FromEntity(Document doc)
    {
        return new DocumentItemViewModel
        {
            Id = doc.Id,
            DisplayName = doc.DisplayName,
            MimeType = doc.MimeType,
            Status = doc.Status,
            ErrorCode = doc.ErrorCode,
            ErrorMessage = doc.ErrorMessage,
            PageCount = doc.PageCount,
            ChunkCount = doc.ChunkCount,
            CreatedAt = doc.CreatedAt.ToLocalTime(),
            UpdatedAt = doc.UpdatedAt.ToLocalTime()
        };
    }

    public void UpdateFromEntity(Document doc)
    {
        DisplayName = doc.DisplayName;
        MimeType = doc.MimeType;
        Status = doc.Status;
        ErrorCode = doc.ErrorCode;
        ErrorMessage = doc.ErrorMessage;
        PageCount = doc.PageCount;
        ChunkCount = doc.ChunkCount;
        UpdatedAt = doc.UpdatedAt.ToLocalTime();
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusBadgeColor));
        OnPropertyChanged(nameof(CanRetry));
    }
}
