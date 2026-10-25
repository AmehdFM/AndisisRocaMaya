using System.Windows.Controls;
using System.Windows;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views;

public partial class PosView : UserControl
{
    public PosView()
    {
        InitializeComponent();
    }

    private void ChooseCustomer_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not PosViewModel viewModel)
        {
            return;
        }

        var dialog = new CustomerPickerWindow(viewModel.Customers, viewModel.SelectedCustomer)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            viewModel.SelectedCustomer = dialog.SelectedCustomer;
        }
    }

    private void Checkout_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not PosViewModel viewModel || viewModel.SelectedPaymentMethod != "Contado")
        {
            return;
        }

        var dialog = new CashCheckoutWindow(viewModel.TotalAmount)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            viewModel.ClearCartCommand.Execute(null);
        }
    }
}
