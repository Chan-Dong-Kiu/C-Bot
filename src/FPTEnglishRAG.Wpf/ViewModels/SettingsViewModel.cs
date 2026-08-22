using CommunityToolkit.Mvvm.ComponentModel;

namespace FPTEnglishRAG.Wpf.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Cấu hình Hệ thống (System Settings)";

    [ObservableProperty]
    private string _description = "Module phụ trách bởi Người 4: Cấu hình Google Gemini API Key (user-secrets/env), Model generation & embedding, Timeout, Top-K & Threshold.";
}
