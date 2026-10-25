using System.Windows.Controls;
using System.Windows.Input;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContext = SettingsViewModel.Instance;
    }

    private void SaveAuthorizationCode_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel && viewModel.UpdateAuthorizationCode(AuthorizationCodeBox.Password))
        {
            AuthorizationCodeBox.Clear();
        }
    }

    private void AuthorizationCode_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = e.Text.Any(character => !char.IsDigit(character));
    }
}
