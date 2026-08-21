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
}
