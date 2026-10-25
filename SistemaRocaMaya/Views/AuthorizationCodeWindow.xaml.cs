using System.Windows;
using System.Windows.Input;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views;

public partial class AuthorizationCodeWindow : Window
{
    public AuthorizationCodeWindow(string operation)
    {
        InitializeComponent();
        OperationText.Text = $"Ingresa el código del supervisor para {operation}.";
        Loaded += (_, _) => CodeBox.Focus();
    }

    private void Authorize_Click(object sender, RoutedEventArgs e)
    {
        if (SettingsViewModel.Instance.IsAuthorizationCodeValid(CodeBox.Password))
        {
            DialogResult = true;
            return;
        }

        ErrorText.Text = "Código incorrecto. La operación no se realizó.";
        ErrorText.Visibility = Visibility.Visible;
        CodeBox.Clear();
        CodeBox.Focus();
    }

    private void CodeBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = e.Text.Any(character => !char.IsDigit(character));
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
