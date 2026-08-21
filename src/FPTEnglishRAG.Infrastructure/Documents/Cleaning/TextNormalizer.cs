using System.Text;
using System.Text.RegularExpressions;
using FPTEnglishRAG.Application.Abstractions.Documents;

namespace FPTEnglishRAG.Infrastructure.Documents.Cleaning;

public partial class TextNormalizer : ITextNormalizer
{
    [GeneratedRegex(@"\b(\w+)-\r?\n(\w+)\b")]
    private static partial Regex HyphenLineBreakRegex();

    [GeneratedRegex(@"[^\S\r\n]+")]
    private static partial Regex HorizontalWhitespaceRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MultipleBlankLinesRegex();

    public string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // 1. Chuẩn hóa Unicode sang dạng NFC (Form C)
        var normalized = text.Normalize(NormalizationForm.FormC);

        // 2. Loại bỏ các ký tự điều khiển lạ (non-printable control chars), giữ lại \n, \r, \t
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (char.IsControl(ch))
            {
                if (ch is '\n' or '\r' or '\t')
                {
                    sb.Append(ch);
                }
            }
            else
            {
                sb.Append(ch);
            }
        }
        normalized = sb.ToString();

        // 3. Nối các từ bị ngắt bởi gạch nối xuống dòng (hyphenated word wrap: word-\nbreak -> wordbreak)
        normalized = HyphenLineBreakRegex().Replace(normalized, "$1$2");

        // 4. Chuẩn hóa xuống dòng về \n
        normalized = normalized.Replace("\r\n", "\n").Replace("\r", "\n");

        // 5. Chuẩn hóa khoảng trắng ngang (spaces, tabs) thành 1 dấu cách duy nhất
        normalized = HorizontalWhitespaceRegex().Replace(normalized, " ");

        // 6. Rút gọn các dòng trống liên tiếp (3 dòng trống trở lên -> 2 dòng trống \n\n) để bảo tồn ranh giới đoạn văn
        normalized = MultipleBlankLinesRegex().Replace(normalized, "\n\n");

        return normalized.Trim();
    }
}
