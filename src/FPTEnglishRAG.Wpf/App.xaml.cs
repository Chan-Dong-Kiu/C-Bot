using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Application.Services;
using FPTEnglishRAG.Wpf.ViewModels;

namespace FPTEnglishRAG.Wpf;

public partial class App : System.Windows.Application
{
    private readonly IServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // 1. In-Memory Session Store
        services.AddSingleton<IChatSessionStore, InMemoryChatSessionStore>();

        // 2. Chat Service (FakeChatService for independent Person 1 UI development, ready to swap to RagChatService via DI)
        services.AddSingleton<IChatService, FakeChatService>();

        // 3. ViewModels
        services.AddSingleton<ChatViewModel>();
        services.AddSingleton<DocumentsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<AboutViewModel>();
        services.AddSingleton<ShellViewModel>();

        // 4. Windows
        services.AddSingleton<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
