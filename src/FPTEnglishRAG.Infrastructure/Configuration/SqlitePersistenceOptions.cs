namespace FPTEnglishRAG.Infrastructure.Configuration;

public sealed class SqlitePersistenceOptions
{
    public string DatabasePath { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FPTEnglishRAG",
        "fptenglishrag.db");

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DatabasePath))
        {
            throw new InvalidOperationException("SQLite database path is required.");
        }

        if (!Path.IsPathFullyQualified(DatabasePath))
        {
            throw new InvalidOperationException("SQLite database path must be absolute.");
        }
    }
}
