namespace FPTEnglishRAG.Application.Abstractions.Documents;

public interface ITextNormalizer
{
    string Normalize(string text);
}
