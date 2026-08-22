using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Application.Abstractions.Chat;
using FPTEnglishRAG.Application.Abstractions.RAG;
using FPTEnglishRAG.Application.Configuration;
using FPTEnglishRAG.Application.DTOs;
using FPTEnglishRAG.Domain.Entities;
using FPTEnglishRAG.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace FPTEnglishRAG.Application.Services;

public sealed class ChatService : IChatService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RagOptions _ragOptions;

    public ChatService(IServiceScopeFactory scopeFactory, RagOptions ragOptions)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _ragOptions = ragOptions ?? throw new ArgumentNullException(nameof(ragOptions));
    }

    public async Task<ChatAnswer> AskAsync(string question, IReadOnlyList<ChatMessage> recentHistory, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);

        var historySnapshots = recentHistory
            .Select(m => new ChatMessageSnapshot(
                Role: m.Role == ChatRole.User ? "user" : "model",
                Content: m.Content))
            .ToList();

        using var scope = _scopeFactory.CreateScope();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
        var retrievalService = scope.ServiceProvider.GetRequiredService<IRetrievalService>();
        var geminiService = scope.ServiceProvider.GetRequiredService<IGeminiService>();
        var groundingPolicy = scope.ServiceProvider.GetRequiredService<IQuestionGroundingPolicy>();

        var queryVector = await embeddingService.EmbedQueryAsync(question, ct);

        // Retrieve chunks
        var query = new RetrievalQuery(queryVector, "gemini-embedding-001", "v1");
        var searchResults = await retrievalService.RetrieveAsync(query, ct);
        
        var retrievedChunks = searchResults.Select(r => new RetrievedChunk(
            ChunkId: r.ChunkId.ToString(),
            Content: r.Content,
            SourceDocumentName: r.DocumentName,
            PageOrPosition: r.PageStart.ToString(),
            SimilarityScore: r.Score
        )).ToList();

        // Evaluate Grounding Policy
        var decision = groundingPolicy.Decide(
            question: question,
            hasRetrievedContext: retrievedChunks.Any(),
            knowledgeMode: KnowledgeMode.GroundedOnly); 

        if (!decision.MayCallGemini)
        {
            return new ChatAnswer(
                Text: GetInsufficientMessage(decision.Reason),
                Citations: Array.Empty<Domain.ValueObjects.Citation>(),
                IsGrounded: false);
        }

        // Generate answer
        var request = new ChatAnswerRequest(
            Question: question,
            RetrievedChunks: retrievedChunks,
            RecentMessages: historySnapshots,
            RelevanceThreshold: _ragOptions.RelevanceThreshold);

        var answerResult = await geminiService.GenerateAnswerAsync(request, ct);

        // Map Citations
        var domainCitations = answerResult.Citations.Select(dto => {
            var searchResult = searchResults.FirstOrDefault(r => r.ChunkId.ToString() == dto.ChunkId);
            return new Domain.ValueObjects.Citation(
                Label: dto.Label,
                DocumentName: searchResult?.DocumentName ?? "Unknown",
                Page: searchResult?.PageStart,
                Snippet: searchResult?.Content ?? string.Empty,
                DocumentId: searchResult?.DocumentId ?? Guid.Empty,
                ChunkId: searchResult?.ChunkId ?? Guid.Empty);
        }).ToList();

        return new ChatAnswer(
            Text: answerResult.Answer,
            Citations: domainCitations,
            IsGrounded: answerResult.IsGrounded);
    }

    private static string GetInsufficientMessage(GroundingReason reason)
    {
        return reason switch
        {
            GroundingReason.SourceDependentQuestionWithoutContext => 
                "C\u00e2u h\u1ecfi c\u1ee7a b\u1ea1n ph\u1ee5 thu\u1ed9c v\u00e0o m\u1ed9t t\u00e0i li\u1ec7u nh\u01b0ng kh\u00f4ng c\u00f3 d\u1eef li\u1ec7u n\u00e0o kh\u1edbp \u0111\u01b0\u1ee3c t\u00ecm th\u1ea5y trong h\u1ec7 th\u1ed1ng.",
            _ => 
                "T\u00e0i li\u1ec7u hi\u1ec7n c\u00f3 ch\u01b0a \u0111\u1ee7 th\u00f4ng tin \u0111\u1ec3 tr\u1ea3 l\u1eddi c\u00e2u h\u1ecfi n\u00e0y. B\u1ea1n vui l\u00f2ng import th\u00eam t\u00e0i li\u1ec7u li\u00ean quan \u0111\u1ebfn ch\u1ee7 \u0111\u1ec1 tr\u00ean."
        };
    }
}
