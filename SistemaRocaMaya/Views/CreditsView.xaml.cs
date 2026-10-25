using System.Windows;
using System.Windows.Controls;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views;

public partial class CreditsView : UserControl
{
    public CreditsView()
    {
        InitializeComponent();
        DataContext = new CreditsViewModel();
    }

    private void RecordPayment_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not CreditsViewModel viewModel || viewModel.SelectedCustomer is not { } customer)
        {
            return;
        }

        var dialog = new CreditPaymentWindow(customer.Name, customer.Balance)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            viewModel.RecordPayment(dialog.PaymentAmount);
        }
    }
}
