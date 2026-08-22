using System.Buffers.Binary;

namespace FPTEnglishRAG.Infrastructure.VectorStore;

public static class FloatVectorSerializer
{
    private const int BytesPerFloat = sizeof(float);

    public static byte[] Serialize(ReadOnlySpan<float> vector)
    {
        if (vector.IsEmpty)
        {
            throw new ArgumentException("Vector cannot be empty.", nameof(vector));
        }

        var bytes = new byte[vector.Length * BytesPerFloat];
        for (int index = 0; index < vector.Length; index++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(
                bytes.AsSpan(index * BytesPerFloat, BytesPerFloat),
                vector[index]);
        }

        return bytes;
    }

    public static float[] Deserialize(ReadOnlySpan<byte> bytes, int dimensions)
    {
        if (dimensions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dimensions));
        }

        if (bytes.Length != dimensions * BytesPerFloat)
        {
            throw new InvalidDataException(
                $"Vector payload has {bytes.Length} bytes but {dimensions * BytesPerFloat} were expected.");
        }

        var vector = new float[dimensions];
        for (int index = 0; index < dimensions; index++)
        {
            vector[index] = BinaryPrimitives.ReadSingleLittleEndian(
                bytes.Slice(index * BytesPerFloat, BytesPerFloat));
        }

        return vector;
    }
}
