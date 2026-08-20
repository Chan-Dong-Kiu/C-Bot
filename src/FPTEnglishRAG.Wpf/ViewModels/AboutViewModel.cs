using CommunityToolkit.Mvvm.ComponentModel;

namespace FPTEnglishRAG.Wpf.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    [ObservableProperty]
    private string _appName = "FPTEnglishRAG";

    [ObservableProperty]
    private string _version = "1.0.0 (MVP)";

    [ObservableProperty]
    private string _description = "Trợ lý học tập & ôn thi tiếng Anh đầu vào Đại học FPT tích hợp công nghệ RAG (Retrieval-Augmented Generation).";
}
