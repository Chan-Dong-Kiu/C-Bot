using System.Windows.Controls;
using FPTEnglishRAG.Wpf.ViewModels;

namespace FPTEnglishRAG.Wpf.Views;

public partial class DocumentsView : UserControl
{
    public DocumentsView()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (DataContext is DocumentsViewModel vm && vm.Documents.Count == 0)
            {
                await vm.LoadDocumentsCommand.ExecuteAsync(null);
            }
        };
    }
}
