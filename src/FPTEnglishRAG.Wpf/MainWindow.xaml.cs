using System.Windows;
using FPTEnglishRAG.Wpf.ViewModels;

namespace FPTEnglishRAG.Wpf;

public partial class MainWindow : Window
{
    public DocumentsViewModel DocumentsVm { get; }

    public MainWindow(DocumentsViewModel documentsVm)
    {
        InitializeComponent();
        DocumentsVm = documentsVm;
        DataContext = this;
    }
}
