using System.Windows;
using System.Windows.Input;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views;

public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();
        DataContext = new ShellViewModel();
    }

    private void SignOut_Click(object sender, RoutedEventArgs e)
    {
        new MainWindow().Show();
        Close();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ShellViewModel viewModel)
        {
            return;
        }

        if (e.Key == Key.Escape && viewModel.IsFocusMode)
        {
            viewModel.ExitFocusCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F11 && viewModel.CurrentSectionTitle == "POS / Caja")
        {
            viewModel.ToggleFocusCommand.Execute(null);
            e.Handled = true;
        }
    }
}
