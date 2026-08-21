// File: tests/FPTEnglishRAG.UnitTests/Infrastructure/AI/FakeHttpMessageHandler.cs

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FPTEnglishRAG.UnitTests.Infrastructure.AI;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();
    public List<HttpRequestMessage> Requests { get; } = new();
    public List<DateTime> RequestTimestamps { get; } = new();

    public void EnqueueResponse(HttpResponseMessage response)
    {
        _responses.Enqueue(_ => response);
    }

    public void EnqueueResponse(HttpStatusCode statusCode, string content, TimeSpan? retryAfter = null)
    {
        _responses.Enqueue(_ =>
        {
            var res = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            };
            if (retryAfter.HasValue)
            {
                res.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(retryAfter.Value);
            }
            return res;
        });
    }

    public void EnqueueException(Exception ex)
    {
        _responses.Enqueue(_ => throw ex);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestTimestamps.Add(DateTime.UtcNow);

        // Check cancellation before returning response
        cancellationToken.ThrowIfCancellationRequested();

        if (_responses.Count > 0)
        {
            var responseFactory = _responses.Dequeue();
            return Task.FromResult(responseFactory(request));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
