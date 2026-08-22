# Vector retrieval development guide

## Responsibilities

`IDocumentRepository` manages document metadata, chunks, duplicate-hash checks, processing status, and cascade deletion. `IVectorStore` manages embedding upsert/search/delete. `IRetrievalService` applies retrieval policy such as top-K, threshold, duplicate-content removal, and per-document limits.

## Default configuration

```text
TopK: 5
Threshold: 0.70
MaxChunksPerDocument: 3
CandidateMultiplier: 4
```

The embedding model and dimensions are supplied by the embedding component. The planned Gemini configuration uses 768 dimensions, but storage validates the dimensions supplied with every vector instead of hard-coding that value.

## Application startup

Register the component at the WPF composition root:

```csharp
services.AddDbContextFactory<DocumentDbContext>(options =>
    options.UseSqlite(connectionString));
services.AddVectorRetrieval(new RetrievalOptions());
```

Document processing and vector retrieval intentionally share `DocumentDbContext`. The WPF composition root owns the connection string and database initialization; the vector module only registers `IVectorStore` and `IRetrievalService`.

## Migrations

```powershell
dotnet ef migrations add <MigrationName> `
    --context DocumentDbContext `
    --project src\FPTEnglishRAG.Infrastructure\FPTEnglishRAG.Infrastructure.csproj
```

Create migrations only from the shared context after the team replaces the current `EnsureCreated` startup flow. Never restore the removed standalone `RagDbContext` migration because it duplicates the document tables.

## Tests

```powershell
dotnet test GroupProject.slnx
```

The default suite is offline. Integration tests use SQLite in-memory or a unique temporary database. The performance test indexes 10,000 vectors with 768 dimensions and requires search to complete in under 500 milliseconds on the development machine.

## Integration boundaries

- M2 supplies validated `Document` and `DocumentChunk` data.
- M4 supplies document/query embeddings with model name and dimensions.
- M3 persists and retrieves candidate chunks.
- M1 consumes retrieval results through the chat orchestration layer and displays citations.

## Hybrid Gemini fallback

After retrieval, `IQuestionGroundingPolicy` selects one of three outcomes:

- `Grounded`: retrieved context exists; Gemini may answer and citations are required.
- `GeneralKnowledge`: context is missing, general knowledge is enabled, and the question is independent (for example vocabulary meaning, common grammar, examples, translation, or conversation practice). Gemini may answer, but the UI must label the response as general Gemini knowledge and must not attach citations.
- `InsufficientSources`: the question depends on a document, passage, exam question, answer choice, or strict `GroundedOnly` mode. Gemini generation is not called.

The policy normalizes Vietnamese diacritics and checks Vietnamese/English source-dependent markers. It is a deterministic safety policy, not an AI classifier. M1 and M4 should keep the final UI/prompt behavior aligned with the returned `GroundingDecision`.
