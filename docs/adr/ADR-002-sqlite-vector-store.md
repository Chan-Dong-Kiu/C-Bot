# ADR-002: SQLite local vector store

## Status

Proposed for team review.

## Context

The MVP is a single-user Windows desktop application for a corpus of up to approximately 10,000 chunks. It needs durable document metadata, local vector persistence, deterministic offline tests, and a simple clean-machine demo without an additional database service.

## Decision

- Use EF Core 10 with SQLite for documents, chunks, and embeddings.
- Store embeddings as little-endian `float32` BLOB values.
- Persist the embedding model, dimensions, and index version with every vector.
- Calculate cosine similarity in process and return only `Ready` documents.
- Apply threshold filtering, ranking, de-duplication, and per-document limits through the retrieval boundary.
- Keep `IVectorStore` as the replaceable application port so a dedicated vector database can be introduced later.

## Consequences

### Benefits

- No SQLite server or Docker service is required.
- Migrations, relational metadata, cascade deletion, and vectors share one local database.
- Tests can use SQLite in-memory and do not require a Gemini key or network access.
- A 10,000-vector, 768-dimension benchmark enforces the MVP retrieval target.

### Trade-offs

- Search is an exact linear scan rather than approximate nearest-neighbor search.
- Vector search cost grows with the corpus size.
- Moving to Qdrant or another vector engine will require a new `IVectorStore` implementation and re-indexing.

## Index compatibility

Vectors with different model names, dimensions, or index versions are never mixed in one query. Changing the embedding model, dimensions, normalization, chunking behavior, or distance metric requires a new index version and re-indexing of the affected documents.

## Local data and secrets

The default database location is `%LOCALAPPDATA%\FPTEnglishRAG\fptenglishrag.db`. SQLite files and local settings are ignored by Git. Gemini keys must use user-secrets or the `GEMINI_API_KEY` environment variable and are not part of this component.
