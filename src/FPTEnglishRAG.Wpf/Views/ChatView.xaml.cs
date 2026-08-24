using System;
using System.Windows.Controls;
using System.Windows.Input;
using FPTEnglishRAG.Wpf.ViewModels;

namespace FPTEnglishRAG.Wpf.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
    }

    private void QuestionTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                // Shift + Enter -> insert newline
                var textBox = (TextBox)sender;
                int caretIndex = textBox.SelectionStart + Environment.NewLine.Length;
                textBox.SelectedText = Environment.NewLine;
                textBox.CaretIndex = caretIndex;
                e.Handled = true;
            }
            else
            {
                // Enter -> send question
                e.Handled = true;
                if (DataContext is ChatViewModel viewModel && viewModel.SendCommand.CanExecute(null))
                {
                    viewModel.SendCommand.Execute(null);
                }
            }
        }
    }
}
