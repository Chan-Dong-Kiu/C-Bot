using CommunityToolkit.Mvvm.ComponentModel;

namespace FPTEnglishRAG.Wpf.ViewModels;

public partial class DocumentsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Quản lý Tài liệu (Documents Management)";

    [ObservableProperty]
    private string _description = "Module phụ trách bởi Người 2: Import PDF/TXT, Text Extraction (PdfPig), Chunking (500 tokens / 75 overlap), và Vector Indexing.";
}
