using System.IO;
using FPTEnglishRAG.Infrastructure.VectorStore;

namespace FPTEnglishRAG.UnitTests.VectorStore;

public sealed class FloatVectorSerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_RoundTripsFloatValues()
    {
        float[] expected = [1.25f, -2.5f, 0f, float.MaxValue];

        byte[] bytes = FloatVectorSerializer.Serialize(expected);
        float[] actual = FloatVectorSerializer.Deserialize(bytes, expected.Length);

        Assert.Equal(expected, actual);
        Assert.Equal(expected.Length * sizeof(float), bytes.Length);
    }

    [Fact]
    public void Deserialize_Throws_WhenPayloadLengthDoesNotMatchDimensions()
    {
        Assert.Throws<InvalidDataException>(() =>
            FloatVectorSerializer.Deserialize(new byte[3], dimensions: 2));
    }
}
