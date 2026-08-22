using System.IO;
using System.Windows;
using FPTEnglishRAG.Application.Abstractions.Documents;
using FPTEnglishRAG.Application.Abstractions.Persistence;
using FPTEnglishRAG.Application.Services;
using FPTEnglishRAG.Infrastructure.Documents.Chunking;
using FPTEnglishRAG.Infrastructure.Documents.Cleaning;
using FPTEnglishRAG.Infrastructure.Documents.Extraction;
using FPTEnglishRAG.Infrastructure.Documents.Storage;
using FPTEnglishRAG.Infrastructure.Documents.Validation;
using FPTEnglishRAG.Infrastructure.Persistence;
using FPTEnglishRAG.Infrastructure.Persistence.Repositories;
using FPTEnglishRAG.Wpf.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FPTEnglishRAG.Wpf;

public partial class App : System.Windows.Application
{
    private readonly IServiceProvider _serviceProvider;

    public App()
    {
        InitializeComponent();
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
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

        // ViewModels
        services.AddSingleton<DocumentsViewModel>();

        // MainWindow
        services.AddSingleton<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        using (var scope = _serviceProvider.CreateScope())
        {
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DocumentDbContext>>();
            using var db = dbFactory.CreateDbContext();
            db.Database.EnsureCreated();
        }

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
