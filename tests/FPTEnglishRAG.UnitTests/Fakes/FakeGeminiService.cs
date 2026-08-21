using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Application.DTOs;

namespace FPTEnglishRAG.UnitTests.Fakes;

/// <summary>
/// Configurable, in-memory fake for <see cref="IGeminiService"/> used in unit tests.
/// </summary>
/// <remarks>
/// <para>
/// Pre-queue specific <see cref="ChatAnswerResult"/> values via <see cref="EnqueueResult"/>.
/// Each call to <see cref="GenerateAnswerAsync"/> dequeues one result in FIFO order.
/// When the queue is empty the fake returns <see cref="ChatAnswerResult.NotGrounded()"/>,
/// making no-setup tests safe by default.
/// </para>
/// <para>
/// Every call is recorded in <see cref="ReceivedRequests"/> so tests can assert
/// what question, chunks and history were sent to the service.
/// </para>
/// <para>
/// Set <see cref="ThrowOnNextCall"/> before a call to simulate Gemini API failures
/// (transient error, quota exceeded, etc.) and verify that callers handle them correctly.
/// </para>
/// <para>
/// Use <see cref="CallCount"/> for simple "was it called?" assertions without inspecting
/// the full request list.
/// </para>
/// </remarks>
public sealed class FakeGeminiService : IGeminiService
{
    private readonly Queue<ChatAnswerResult> _resultQueue = new();
    private readonly List<ChatAnswerRequest> _receivedRequests = [];

    /// <summary>Gets all requests received by <see cref="GenerateAnswerAsync"/> in call order.</summary>
    public IReadOnlyList<ChatAnswerRequest> ReceivedRequests => _receivedRequests;

    /// <summary>Gets the total number of times <see cref="GenerateAnswerAsync"/> was invoked.</summary>
    public int CallCount => _receivedRequests.Count;

    /// <summary>
    /// When set, the next call to <see cref="GenerateAnswerAsync"/> throws this exception
    /// and then clears the property so subsequent calls proceed normally.
    /// </summary>
    /// <remarks>
    /// Use to simulate transient failures, quota errors, or timeout scenarios.
    /// The request is still recorded in <see cref="ReceivedRequests"/> before the throw.
    /// </remarks>
    public Exception? ThrowOnNextCall { get; set; }

    /// <summary>
    /// Enqueues a <see cref="ChatAnswerResult"/> to be returned by the next call to
    /// <see cref="GenerateAnswerAsync"/>.
    /// </summary>
    /// <remarks>
    /// Results are dequeued in FIFO order. Enqueue multiple results to script
    /// a sequence of responses across multiple calls.
    /// </remarks>
    /// <param name="result">The result to return.</param>
    public void EnqueueResult(ChatAnswerResult result) => _resultQueue.Enqueue(result);

    /// <summary>
    /// Convenience overload that enqueues a grounded result with the given answer text and no citations.
    /// </summary>
    /// <param name="answer">The answer text to return.</param>
    public void EnqueueGroundedAnswer(string answer) =>
        EnqueueResult(new ChatAnswerResult(answer, [], IsGrounded: true));

    /// <summary>Resets all recorded calls and queued results.</summary>
    public void Reset()
    {
        _resultQueue.Clear();
        _receivedRequests.Clear();
        ThrowOnNextCall = null;
    }

    /// <inheritdoc/>
    public Task<ChatAnswerResult> GenerateAnswerAsync(
        ChatAnswerRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _receivedRequests.Add(request);

        if (ThrowOnNextCall is { } ex)
        {
            ThrowOnNextCall = null;
            throw ex;
        }

        var result = _resultQueue.TryDequeue(out var queued)
            ? queued
            : ChatAnswerResult.NotGrounded();

        return Task.FromResult(result);
    }
}
