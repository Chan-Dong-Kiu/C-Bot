using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FPTEnglishRAG.Infrastructure.Persistence;

public sealed class RagDbContextFactory : IDesignTimeDbContextFactory<RagDbContext>
{
    public RagDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RagDbContext>()
            .UseSqlite("Data Source=fptenglishrag.db")
            .Options;

        return new RagDbContext(options);
    }
}
