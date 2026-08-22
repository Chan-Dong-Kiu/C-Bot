using System.Buffers.Binary;

namespace FPTEnglishRAG.Infrastructure.VectorStore;

public static class CosineSimilarity
{
    public static double Calculate(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
    {
        if (left.Length != right.Length)
        {
            throw new ArgumentException("Vectors must have the same dimensions.");
        }

        if (left.IsEmpty)
        {
            throw new ArgumentException("Vectors cannot be empty.");
        }

        double dotProduct = 0;
        double leftMagnitudeSquared = 0;
        double rightMagnitudeSquared = 0;

        for (int index = 0; index < left.Length; index++)
        {
            dotProduct += left[index] * right[index];
            leftMagnitudeSquared += left[index] * left[index];
            rightMagnitudeSquared += right[index] * right[index];
        }

        if (leftMagnitudeSquared == 0 || rightMagnitudeSquared == 0)
        {
            return 0;
        }

        return dotProduct / Math.Sqrt(leftMagnitudeSquared * rightMagnitudeSquared);
    }

    public static double CalculateSerialized(
        ReadOnlySpan<float> queryVector,
        ReadOnlySpan<byte> storedVector)
    {
        if (queryVector.IsEmpty)
        {
            throw new ArgumentException("Query vector cannot be empty.", nameof(queryVector));
        }

        int expectedBytes = checked(queryVector.Length * sizeof(float));
        if (storedVector.Length != expectedBytes)
        {
            throw new ArgumentException(
                "Stored vector byte length must match the query dimensions.",
                nameof(storedVector));
        }

        double dotProduct = 0;
        double queryMagnitudeSquared = 0;
        double storedMagnitudeSquared = 0;

        for (int index = 0; index < queryVector.Length; index++)
        {
            float queryValue = queryVector[index];
            float storedValue = BinaryPrimitives.ReadSingleLittleEndian(
                storedVector.Slice(index * sizeof(float), sizeof(float)));
            dotProduct += queryValue * storedValue;
            queryMagnitudeSquared += queryValue * queryValue;
            storedMagnitudeSquared += storedValue * storedValue;
        }

        if (queryMagnitudeSquared == 0 || storedMagnitudeSquared == 0)
        {
            return 0;
        }

        return dotProduct / Math.Sqrt(queryMagnitudeSquared * storedMagnitudeSquared);
    }
}
