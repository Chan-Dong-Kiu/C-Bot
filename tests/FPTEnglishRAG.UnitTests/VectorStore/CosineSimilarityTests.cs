using FPTEnglishRAG.Infrastructure.VectorStore;

namespace FPTEnglishRAG.UnitTests.VectorStore;

public sealed class CosineSimilarityTests
{
    [Fact]
    public void Calculate_ReturnsOne_ForIdenticalVectors()
    {
        double score = CosineSimilarity.Calculate([1f, 2f, 3f], [1f, 2f, 3f]);

        Assert.Equal(1d, score, precision: 10);
    }

    [Fact]
    public void Calculate_ReturnsZero_ForOrthogonalVectors()
    {
        double score = CosineSimilarity.Calculate([1f, 0f], [0f, 1f]);

        Assert.Equal(0d, score);
    }

    [Fact]
    public void Calculate_ReturnsZero_WhenEitherVectorHasNoMagnitude()
    {
        double score = CosineSimilarity.Calculate([0f, 0f], [1f, 1f]);

        Assert.Equal(0d, score);
    }

    [Fact]
    public void Calculate_Throws_WhenDimensionsDiffer()
    {
        Assert.Throws<ArgumentException>(() =>
            CosineSimilarity.Calculate([1f], [1f, 2f]));
    }
}
