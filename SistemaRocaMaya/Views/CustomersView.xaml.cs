using System.Windows;
using System.Windows.Controls;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views;

public partial class CustomersView : UserControl
{
    public CustomersView()
    {
        InitializeComponent();
        DataContext = new CustomersViewModel();
        CreateCustomerButton.Visibility = SettingsViewModel.Instance.CurrentUser?.Rol?.PuedeCrearClientes == true
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CreateCustomer_Click(object sender, RoutedEventArgs e)
    {
        if (SettingsViewModel.Instance.CurrentUser?.Rol?.PuedeCrearClientes != true) return;

        var dialog = new CustomerEditWindow(OperationalDemoStore.Instance)
        {
            Owner = Window.GetWindow(this)
        };
        dialog.ShowDialog();
    }

    private void ViewCustomer_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: CustomerRecord customer })
        {
            return;
        }

        var dialog = new CustomerEditWindow(OperationalDemoStore.Instance, customer)
        {
            Owner = Window.GetWindow(this)
        };
        dialog.ShowDialog();
    }
}
