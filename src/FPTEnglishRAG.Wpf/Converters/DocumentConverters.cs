using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using FPTEnglishRAG.Domain.Enums;

namespace FPTEnglishRAG.Wpf.Converters;

public class DocumentStatusToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush PendingBrush = new(Color.FromRgb(0xEA, 0xB3, 0x08));     // Yellow
    private static readonly SolidColorBrush ExtractingBrush = new(Color.FromRgb(0x3B, 0x82, 0xF6));  // Blue
    private static readonly SolidColorBrush ChunkingBrush = new(Color.FromRgb(0x8B, 0x5C, 0xF6));    // Purple
    private static readonly SolidColorBrush EmbeddingBrush = new(Color.FromRgb(0xF9, 0x73, 0x16));   // Orange
    private static readonly SolidColorBrush ReadyBrush = new(Color.FromRgb(0x10, 0xB9, 0x81));       // Green
    private static readonly SolidColorBrush FailedBrush = new(Color.FromRgb(0xEF, 0x44, 0x44));      // Red
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(0x6B, 0x72, 0x80));

    static DocumentStatusToBrushConverter()
    {
        PendingBrush.Freeze();
        ExtractingBrush.Freeze();
        ChunkingBrush.Freeze();
        EmbeddingBrush.Freeze();
        ReadyBrush.Freeze();
        FailedBrush.Freeze();
        DefaultBrush.Freeze();
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DocumentStatus status)
        {
            return status switch
            {
                DocumentStatus.Pending => PendingBrush,
                DocumentStatus.Extracting => ExtractingBrush,
                DocumentStatus.Chunking => ChunkingBrush,
                DocumentStatus.Embedding => EmbeddingBrush,
                DocumentStatus.Ready => ReadyBrush,
                DocumentStatus.Failed => FailedBrush,
                _ => DefaultBrush
            };
        }

        if (value is string hexString && !string.IsNullOrWhiteSpace(hexString))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hexString);
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                return brush;
            }
            catch
            {
                return DefaultBrush;
            }
        }

        return DefaultBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
