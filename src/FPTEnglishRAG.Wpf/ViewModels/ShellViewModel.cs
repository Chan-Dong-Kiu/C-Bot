using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FPTEnglishRAG.Wpf.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    public ChatViewModel ChatVm { get; }
    public DocumentsViewModel DocumentsVm { get; }
    public SettingsViewModel SettingsVm { get; }
    public AboutViewModel AboutVm { get; }

    [ObservableProperty]
    private ObservableObject _currentViewModel;

    [ObservableProperty]
    private string _activeTab = "Chat";

    public ShellViewModel(
        ChatViewModel chatVm,
        DocumentsViewModel documentsVm,
        SettingsViewModel settingsVm,
        AboutViewModel aboutVm)
    {
        ChatVm = chatVm ?? throw new ArgumentNullException(nameof(chatVm));
        DocumentsVm = documentsVm ?? throw new ArgumentNullException(nameof(documentsVm));
        SettingsVm = settingsVm ?? throw new ArgumentNullException(nameof(settingsVm));
        AboutVm = aboutVm ?? throw new ArgumentNullException(nameof(aboutVm));

        _currentViewModel = ChatVm;
    }

    [RelayCommand]
    public void Navigate(string? targetTab)
    {
        if (string.IsNullOrWhiteSpace(targetTab)) return;

        ActiveTab = targetTab;
        CurrentViewModel = targetTab switch
        {
            "Chat" => ChatVm,
            "Documents" => DocumentsVm,
            "Settings" => SettingsVm,
            "About" => AboutVm,
            _ => ChatVm
        };
    }
}
