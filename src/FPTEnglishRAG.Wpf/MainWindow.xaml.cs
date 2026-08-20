using System.Windows;
using FPTEnglishRAG.Wpf.ViewModels;

namespace FPTEnglishRAG.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}