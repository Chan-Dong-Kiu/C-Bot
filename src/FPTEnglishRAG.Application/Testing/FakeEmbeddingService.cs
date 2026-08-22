// File: src/FPTEnglishRAG.Application/Testing/FakeEmbeddingService.cs

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using FPTEnglishRAG.Application.Abstractions;

namespace FPTEnglishRAG.Application.Testing;

/// <summary>
/// Fake implementation of <see cref="IEmbeddingService"/> for development and unit testing.
/// </summary>
/// <remarks>
/// <para>
/// This class must NOT be used in production. It exists solely to allow:
/// <list type="bullet">
///   <item>M1 (Chat UI) and M3 (Retrieval) to develop and test offline without a live Gemini API key.</item>
///   <item>Unit and integration tests to run fully deterministically and without network calls.</item>
/// </list>
/// </para>
/// <para>
/// Vectors are derived from the SHA-256 hash of the input text so that the same query always
/// produces the same vector across all test runs and machines. Vectors are L2-normalized to
/// unit length, making them compatible with cosine-similarity retrieval logic.
/// </para>
/// <para>
/// <see cref="EmbeddingDimensions"/> is intentionally set to match the real
/// <c>gemini-embedding-001</c> model output (768). If the model is replaced, update this
/// constant and re-index all documents.
/// </para>
/// </remarks>
public sealed class FakeEmbeddingService : IEmbeddingService
{
    // -------------------------------------------------------------------------
    // Adjust this constant when changing the embedding model.
    // gemini-embedding-001 → 768 dimensions.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Number of dimensions in the output embedding vector.
    /// Must match the real embedding model configured in <c>appsettings.json</c>.
    /// </summary>
    public const int EmbeddingDimensions = 768;

    /// <inheritdoc/>
    /// <remarks>
    /// Returns a deterministic, unit-length float[] of length <see cref="EmbeddingDimensions"/>
    /// derived from the SHA-256 hash of <paramref name="query"/>.
    /// No HTTP call is made.
    /// </remarks>
    public Task<float[]> EmbedQueryAsync(string query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(DeterministicUnitVector(query));
    }

    /// <summary>
    /// Produces a stable, L2-normalized embedding vector for <paramref name="text"/>
    /// using iterated SHA-256 hashing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The algorithm:
    /// <list type="number">
    ///   <item>Compute a 32-byte seed = SHA-256(<paramref name="text"/>).</item>
    ///   <item>
    ///     Generate <see cref="EmbeddingDimensions"/> floats by hashing
    ///     <c>seed ‖ round_index</c> (big-endian int32) in successive rounds,
    ///     mapping each output byte to <c>[-1, 1]</c>.
    ///   </item>
    ///   <item>L2-normalize the resulting vector so its magnitude equals 1.0.</item>
    /// </list>
    /// </para>
    /// <para>
    /// Properties: deterministic, collision-resistant at the byte level, and unit-length —
    /// making it safe to use with any cosine-similarity implementation.
    /// </para>
    /// </remarks>
    /// <param name="text">The text to embed.</param>
    /// <returns>
    /// A float array of length <see cref="EmbeddingDimensions"/> with magnitude ≈ 1.0.
    /// </returns>
    public static float[] DeterministicUnitVector(string text)
    {
        // Step 1: derive a 32-byte seed from the input.
        Span<byte> seed = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(text), seed);

        // Step 2: expand seed to EmbeddingDimensions floats via iterated hashing.
        //         Each round hashes (seed ‖ round_number) → 32 bytes → up to 32 floats.
        var raw = new float[EmbeddingDimensions];
        Span<byte> roundInput = stackalloc byte[36]; // 32 (seed) + 4 (int32 round)
        seed.CopyTo(roundInput);

        int floatIndex = 0;
        for (int round = 0; floatIndex < EmbeddingDimensions; round++)
        {
            BinaryPrimitives.WriteInt32BigEndian(roundInput[32..], round);

            Span<byte> hashed = stackalloc byte[32];
            SHA256.HashData(roundInput, hashed);

            foreach (var b in hashed)
            {
                if (floatIndex >= EmbeddingDimensions) break;
                // Map [0, 255] → [-1.0, 1.0]
                raw[floatIndex++] = b / 127.5f - 1.0f;
            }
        }

        // Step 3: L2-normalize so magnitude = 1.0 (required for cosine similarity).
        float sumSq = 0f;
        foreach (var v in raw) sumSq += v * v;
        float magnitude = MathF.Sqrt(sumSq);

        if (magnitude > float.Epsilon)
            for (int i = 0; i < EmbeddingDimensions; i++)
                raw[i] /= magnitude;

        return raw;
    }
}
