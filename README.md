# FPTEnglishRAG (english-entry-rag-chatbot-assistant)

FPTEnglishRAG is a Windows desktop learning assistant designed for students preparing for the FPT University English entry assessment. Built with WPF, .NET 10, SQLite, and Google Gemini API, it provides a retrieval-augmented generation (RAG) pipeline to deliver accurate, document-grounded answers with source citations.

---

## 🌟 Key Features

- **Document Ingestion & Indexing**: Import PDF and TXT documents, extract text, normalize, clean, and chunk semantically (500 tokens target with 75 token overlap).
- **Local Vector Search**: In-process cosine similarity search using local SQLite vector embeddings store.
- **RAG Answer Generation**: Integrates with Google Gemini API to produce grounded responses in Vietnamese or English.
- **Source Citations**: Displays explicit source references (e.g. `[S1]`) for every answer.
- **Interactive Chat Interface**: Modern WPF interface with CommunityToolkit.Mvvm for responsive user interaction and session chat history management.
- **Privacy & Security**: Keeps API keys strictly local (`user-secrets` / environment variables) and prevents document instruction injection.

---

## 🏗 Architecture & Tech Stack

The solution follows Clean Architecture principles:

```text
FPTEnglishRAG.Wpf -> FPTEnglishRAG.Application -> FPTEnglishRAG.Domain
FPTEnglishRAG.Infrastructure -> FPTEnglishRAG.Application + FPTEnglishRAG.Domain
```

- **Framework**: .NET 10 LTS (C# with Nullable Reference Types enabled)
- **UI Layer**: WPF + `CommunityToolkit.Mvvm`
- **Database & Persistence**: SQLite + Entity Framework Core
- **PDF Processing**: `PdfPig`
- **AI / LLM Integration**: Google Gemini API via typed `HttpClient`
- **Testing**: xUnit, FluentAssertions, Moq/NSubstitute

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 / Rider / VS Code with C# Dev Kit
- Google Gemini API Key

### Setup API Key

Configure your Gemini API key in your user secrets or environment variables:

```bash
# Environment Variable
setx GEMINI_API_KEY "your_gemini_api_key_here"
```

or via .NET User Secrets:

```bash
cd src/FPTEnglishRAG.Wpf
dotnet user-secrets set "Gemini:ApiKey" "your_gemini_api_key_here"
```

### Build & Run

```bash
# Clone the repository
git clone https://github.com/Chan-Dong-Kiu/C-Bot.git
cd C-Bot

# Restore dependencies & build
dotnet build

# Run unit & integration tests
dotnet test

# Run the WPF Application
dotnet run --project src/FPTEnglishRAG.Wpf
```

---

## 📂 Project Structure

```text
src/
  ├── FPTEnglishRAG.Domain/          # Entities, Value Objects, Enums, Domain Rules
  ├── FPTEnglishRAG.Application/     # Use Cases, DTOs, Interfaces, Service Orchestration
  ├── FPTEnglishRAG.Infrastructure/  # Gemini API, Vector Store, SQLite EF Core, Document Parsers
  └── FPTEnglishRAG.Wpf/             # Views, ViewModels, XAML Resources, Dependency Injection
tests/
  ├── FPTEnglishRAG.UnitTests/       # Unit tests for domain logic & chunking/RAG
  └── FPTEnglishRAG.IntegrationTests/ # Integration tests for repositories & parsers
```

---

## 📜 License

This project is developed for educational purposes supporting FPT University entry assessment preparation.
