using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Application.Abstractions.Documents;
using FPTEnglishRAG.Application.Abstractions.Persistence;
using FPTEnglishRAG.Application.Configuration;
using FPTEnglishRAG.Application.Services;
using FPTEnglishRAG.Infrastructure;
using FPTEnglishRAG.Infrastructure.DependencyInjection;
using FPTEnglishRAG.Infrastructure.Documents.Chunking;
using FPTEnglishRAG.Infrastructure.Documents.Cleaning;
using FPTEnglishRAG.Infrastructure.Documents.Extraction;
using FPTEnglishRAG.Infrastructure.Documents.Storage;
using FPTEnglishRAG.Infrastructure.Documents.Validation;
using FPTEnglishRAG.Infrastructure.Persistence;
using FPTEnglishRAG.Infrastructure.Persistence.Repositories;
using FPTEnglishRAG.Wpf.ViewModels;

namespace FPTEnglishRAG.Wpf;

public partial class App : System.Windows.Application
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public App()
    {
        InitializeComponent();
        
        try
        {
            // Build Configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddUserSecrets<App>();
                
            _configuration = builder.Build();

            // Override Gemini:ApiKey if GEMINI_API_KEY env var is present (per AGENTS.md rule)
            var envApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (!string.IsNullOrWhiteSpace(envApiKey))
            {
                _configuration["Gemini:ApiKey"] = envApiKey;
            }

            var services = new ServiceCollection();
            ConfigureServices(services, _configuration);
            _serviceProvider = services.BuildServiceProvider();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"L\u1ed7i nghi\u00eam tr\u1ecdng khi t\u1ea1o DI Container:\n\n{ex}", "L\u1ed7i Kh\u1edfi \u0111\u1ed9ng", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
        }
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 0. Add Core Configuration
        services.AddSingleton(configuration);
        
        var ragOptions = configuration.GetSection("Rag").Get<RagOptions>() ?? new RagOptions();
        services.AddSingleton(ragOptions);

        var retrievalOptions = configuration.GetSection("Rag").Get<RetrievalOptions>() ?? new RetrievalOptions();
        
        // Register Infrastructure extensions from Person 3 & 4
        services.AddInfrastructure(configuration);
        services.AddVectorRetrieval(retrievalOptions);
        services.AddQuestionGroundingPolicy();

        // 1. In-Memory Session Store
        services.AddSingleton<IChatSessionStore, InMemoryChatSessionStore>();

        // 2. Chat Service
        var useFake = configuration.GetSection("Gemini").GetValue<bool>("UseFake");
        if (useFake)
        {
            services.AddSingleton<IChatService, FakeChatService>();
        }
        else
        {
            services.AddSingleton<IChatService, ChatService>();
        }

        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FPTEnglishRAG");
        Directory.CreateDirectory(appDataDir);
        var dbPath = Path.Combine(appDataDir, "app.db");

        services.AddDbContextFactory<DocumentDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Document Processing Services & Repositories
        services.AddSingleton<IDocumentRepository, SqliteDocumentRepository>();
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddSingleton<IDocumentValidator, FileSecurityValidator>();
        services.AddSingleton<IDocumentTextExtractor, PdfPigTextExtractor>();
        services.AddSingleton<IDocumentTextExtractor, PlainTextExtractor>();
        services.AddSingleton<ITextNormalizer, TextNormalizer>();
        services.AddSingleton<IChunkingService, SemanticChunker>();
        services.AddSingleton<IDocumentIngestionService, DocumentIngestionService>();

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

        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DocumentDbContext>>();
                using var db = dbFactory.CreateDbContext();
                // TODO: thay b?ng EF Core Migrations tru?c khi merge vo main, theo AGENTS.md - Database migrations are committed and reviewed
                db.Database.EnsureCreated();
            }

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"L\u1ed7i nghi\u00eam tr\u1ecdng khi kh\u1edfi \u0111\u1ed9ng:\n\n{ex}", "L\u1ed7i Kh\u1edfi \u0111\u1ed9ng", MessageBoxButton.OK, MessageBoxImage.Error);
            Current.Shutdown();
        }
    }
}
