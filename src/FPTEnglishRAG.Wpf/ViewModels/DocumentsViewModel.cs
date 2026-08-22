using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FPTEnglishRAG.Application.Abstractions.Documents;
using FPTEnglishRAG.Application.Abstractions.Persistence;
using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Domain.Entities;
using Microsoft.Win32;

namespace FPTEnglishRAG.Wpf.ViewModels;

public partial class DocumentsViewModel : ObservableObject
{
    private readonly IDocumentIngestionService _ingestionService;
    private readonly IDocumentRepository _documentRepository;

    [ObservableProperty]
    private ObservableCollection<DocumentItemViewModel> _documents = new();

    [ObservableProperty]
    private ObservableCollection<DocumentItemViewModel> _filteredDocuments = new();

    [ObservableProperty]
    private DocumentItemViewModel? _selectedDocument;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _currentProgressStep = string.Empty;

    [ObservableProperty]
    private string _currentProcessingFileName = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string? _errorMessage;

    // --- Document Content Preview / Details Inspector ---
    [ObservableProperty]
    private bool _isPreviewOpen;

    [ObservableProperty]
    private DocumentItemViewModel? _viewingDocument;

    [ObservableProperty]
    private ObservableCollection<DocumentChunk> _viewingDocumentChunks = new();

    [ObservableProperty]
    private bool _isLoadingChunks;

    public Func<string, string, bool>? ConfirmDeleteDialog { get; set; }

    public DocumentsViewModel(
        IDocumentIngestionService ingestionService,
        IDocumentRepository documentRepository)
    {
        _ingestionService = ingestionService;
        _documentRepository = documentRepository;
        ConfirmDeleteDialog = (title, message) =>
            MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    [RelayCommand]
    public async Task LoadDocumentsAsync()
    {
        try
        {
            IsBusy = true;
            HasError = false;
            var entities = await _documentRepository.GetAllAsync();

            Documents.Clear();
            foreach (var doc in entities)
            {
                Documents.Add(DocumentItemViewModel.FromEntity(doc));
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Không thể tải danh sách tài liệu: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ViewDocumentDetailsAsync(DocumentItemViewModel? item)
    {
        if (item == null)
        {
            return;
        }

        ViewingDocument = item;
        ViewingDocumentChunks.Clear();
        IsPreviewOpen = true;
        IsLoadingChunks = true;

        try
        {
            var chunks = await _documentRepository.GetChunksByDocumentIdAsync(item.Id);
            foreach (var chunk in chunks)
            {
                ViewingDocumentChunks.Add(chunk);
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Lỗi khi tải nội dung chi tiết: {ex.Message}";
        }
        finally
        {
            IsLoadingChunks = false;
        }
    }

    [RelayCommand]
    public void ClosePreview()
    {
        IsPreviewOpen = false;
        ViewingDocument = null;
        ViewingDocumentChunks.Clear();
    }

    [RelayCommand]
    public async Task ImportDocumentAsync()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Chọn tài liệu học tập (PDF hoặc TXT)",
            Filter = "Tài liệu học tập (*.pdf;*.txt)|*.pdf;*.txt|PDF Document (*.pdf)|*.pdf|Text File (*.txt)|*.txt",
            Multiselect = true
        };

        if (openFileDialog.ShowDialog() != true || openFileDialog.FileNames.Length == 0)
        {
            return;
        }

        IsBusy = true;
        HasError = false;

        foreach (var filePath in openFileDialog.FileNames)
        {
            var fileName = Path.GetFileName(filePath);
            CurrentProcessingFileName = fileName;
            ProgressPercentage = 0;
            CurrentProgressStep = "Đang bắt đầu xử lý...";

            var progress = new Progress<IngestionProgressReport>(report =>
            {
                ProgressPercentage = report.ProgressPercentage;
                CurrentProgressStep = report.CurrentStepDescription;
            });

            try
            {
                var processedDoc = await _ingestionService.IngestDocumentAsync(filePath, progress);

                // Cập nhật hoặc thêm mới vào danh sách
                var existing = Documents.FirstOrDefault(d => d.Id == processedDoc.Id);
                if (existing != null)
                {
                    existing.UpdateFromEntity(processedDoc);
                }
                else
                {
                    Documents.Insert(0, DocumentItemViewModel.FromEntity(processedDoc));
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Lỗi khi import file '{fileName}': {ex.Message}";
            }
        }

        IsBusy = false;
        CurrentProcessingFileName = string.Empty;
        CurrentProgressStep = string.Empty;
        ProgressPercentage = 0;
    }

    [RelayCommand]
    public async Task RetryDocumentAsync(DocumentItemViewModel? item)
    {
        if (item == null)
        {
            return;
        }

        IsBusy = true;
        HasError = false;
        CurrentProcessingFileName = item.DisplayName;
        ProgressPercentage = 0;
        CurrentProgressStep = "Đang thử lại...";

        var progress = new Progress<IngestionProgressReport>(report =>
        {
            ProgressPercentage = report.ProgressPercentage;
            CurrentProgressStep = report.CurrentStepDescription;
        });

        try
        {
            var result = await _ingestionService.RetryIngestionAsync(item.Id, progress);
            item.UpdateFromEntity(result);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Lỗi khi thử lại xử lý tài liệu '{item.DisplayName}': {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            CurrentProcessingFileName = string.Empty;
            CurrentProgressStep = string.Empty;
            ProgressPercentage = 0;
        }
    }

    [RelayCommand]
    public async Task DeleteDocumentAsync(DocumentItemViewModel? item)
    {
        if (item == null)
        {
            return;
        }

        bool confirmed = ConfirmDeleteDialog?.Invoke(
            "Xác nhận xóa tài liệu",
            $"Bạn có chắc chắn muốn xóa tài liệu '{item.DisplayName}' cùng toàn bộ dữ liệu phân đoạn và vector liên quan?") ?? true;

        if (!confirmed)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await _ingestionService.DeleteDocumentAsync(item.Id);
            Documents.Remove(item);
            if (ViewingDocument?.Id == item.Id)
            {
                ClosePreview();
            }
            ApplyFilter();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Không thể xóa tài liệu '{item.DisplayName}': {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilter()
    {
        FilteredDocuments.Clear();
        var query = SearchText?.Trim() ?? string.Empty;

        var items = string.IsNullOrWhiteSpace(query)
            ? Documents
            : Documents.Where(d => d.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                                || d.StatusText.Contains(query, StringComparison.OrdinalIgnoreCase));

        foreach (var item in items)
        {
            FilteredDocuments.Add(item);
        }
    }
}
