using CommunityToolkit.Mvvm.ComponentModel;
using FPTEnglishRAG.Domain.ValueObjects;

namespace FPTEnglishRAG.Wpf.ViewModels;

public partial class CitationViewModel : ObservableObject
{
    [ObservableProperty]
    private string _label;

    [ObservableProperty]
    private string _documentName;

    [ObservableProperty]
    private int? _page;

    [ObservableProperty]
    private string _snippet;

    public Guid DocumentId { get; }
    public Guid ChunkId { get; }

    public string PageDisplay => Page.HasValue ? $"Trang {Page.Value}" : "Tài liệu văn bản (TXT)";

    public CitationViewModel(Citation citation)
    {
        ArgumentNullException.ThrowIfNull(citation);
        _label = citation.Label;
        _documentName = citation.DocumentName;
        _page = citation.Page;
        _snippet = citation.Snippet;
        DocumentId = citation.DocumentId;
        ChunkId = citation.ChunkId;
    }

    public Citation ToDomain() =>
        new(Label, DocumentName, Page, Snippet, DocumentId, ChunkId);
}
