namespace FPTEnglishRAG.Application.Configuration;

public sealed class RetrievalOptions
{
    public const string SectionName = "Retrieval";
    public int TopK { get; init; } = 5;
    public double Threshold { get; init; } = 0.70;
    public int MaxChunksPerDocument { get; init; } = 3;
    public int CandidateMultiplier { get; init; } = 4;

    public void Validate()
    {
        if (TopK <= 0)
        {
            throw new InvalidOperationException("Retrieval TopK must be greater than zero.");
        }

        if (Threshold is < -1 or > 1)
        {
            throw new InvalidOperationException("Retrieval threshold must be between -1 and 1.");
        }

        if (MaxChunksPerDocument <= 0 || CandidateMultiplier <= 0)
        {
            throw new InvalidOperationException(
                "Retrieval limits and candidate multiplier must be greater than zero.");
        }
    }
}
