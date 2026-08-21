using System.Security.Cryptography;
using System.Text;
using FPTEnglishRAG.Application.Abstractions;

namespace FPTEnglishRAG.UnitTests.Fakes;

/// <summary>
/// Deterministic, in-memory fake for <see cref="IEmbeddingService"/> used in unit tests.
/// </summary>
/// <remarks>
/// <para>
/// Tests can pre-register exact query → vector mappings via <see cref="Register"/>.
/// Any unregistered query falls back to <see cref="DeterministicUnitVector"/>, which
/// produces a stable, normalized float[] derived from the SHA-256 hash of the input text.
/// The same string always produces the same vector across test runs.
/// </para>
/// <para>
/// Every call is recorded in <see cref="QueriesEmbedded"/> so tests can assert
/// which queries were embedded and in what order.
/// </para>
/// <para>
/// Set <see cref="ThrowOnNextCall"/> before a call to simulate a transient API failure.
/// </para>
/// </remarks>
public sealed class FakeEmbeddingService : IEmbeddingService
{
    /// <summary>
    /// The number of dimensions produced by <see cref="DeterministicUnitVector"/>.
    /// Keep small to make test vectors easy to reason about; real model is 768.
    /// </summary>
    public const int Dimensions = 8;

    private readonly Dictionary<string, float[]> _registry = new(StringComparer.Ordinal);

    /// <summary>Gets the ordered list of queries that were passed to <see cref="EmbedQueryAsync"/>.</summary>
    public List<string> QueriesEmbedded { get; } = [];

    /// <summary>
    /// When set, the next call to <see cref="EmbedQueryAsync"/> throws this exception
    /// and then clears the property so subsequent calls succeed normally.
    /// </summary>
    public Exception? ThrowOnNextCall { get; set; }

    /// <summary>
    /// Pre-registers a deterministic mapping from <paramref name="text"/> to <paramref name="vector"/>.
    /// </summary>
    /// <param name="text">The exact query string to match.</param>
    /// <param name="vector">
    /// A unit-length float array of length <see cref="Dimensions"/>.
    /// Use <see cref="DeterministicUnitVector"/> to generate valid test vectors.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="vector"/> does not have exactly <see cref="Dimensions"/> elements.
    /// </exception>
    public void Register(string text, float[] vector)
    {
        if (vector.Length != Dimensions)
            throw new ArgumentException(
                $"Vector must have exactly {Dimensions} dimensions; got {vector.Length}.",
                nameof(vector));

        _registry[text] = vector;
    }

    /// <inheritdoc/>
    public Task<float[]> EmbedQueryAsync(string query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ThrowOnNextCall is { } ex)
        {
            ThrowOnNextCall = null;
            throw ex;
        }

        QueriesEmbedded.Add(query);

        var vector = _registry.TryGetValue(query, out var registered)
            ? registered
            : DeterministicUnitVector(query);

        return Task.FromResult(vector);
    }

    /// <summary>
    /// Produces a stable, normalized embedding vector for <paramref name="text"/> using its SHA-256 hash.
    /// </summary>
    /// <remarks>
    /// The same input always produces the same output, making tests fully deterministic.
    /// The returned vector has length <see cref="Dimensions"/> and magnitude ≈ 1.0 so it is
    /// compatible with cosine-similarity retrieval logic.
    /// </remarks>
    /// <param name="text">The text to hash into a vector.</param>
    /// <returns>A unit-length float array of length <see cref="Dimensions"/>.</returns>
    public static float[] DeterministicUnitVector(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));

        var raw = new float[Dimensions];
        for (var i = 0; i < Dimensions; i++)
            raw[i] = bytes[i] / 127.5f - 1f; // map [0,255] → [-1, 1]

        var magnitude = MathF.Sqrt(raw.Sum(v => v * v));

        // Guard against a zero-vector (astronomically unlikely with SHA-256 output).
        if (magnitude < float.Epsilon)
            raw[0] = 1f;
        else
            for (var i = 0; i < Dimensions; i++)
                raw[i] /= magnitude;

        return raw;
    }
}
