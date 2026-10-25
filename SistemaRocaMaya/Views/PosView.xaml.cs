using System.Windows.Controls;
using System.Windows;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;
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

        var dialog = new CustomerPickerWindow(OperationalDemoStore.Instance, viewModel.SelectedCustomer)
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
        if (DataContext is not PosViewModel viewModel)
        {
            return;
        }

        if (!viewModel.IsCashOpen || viewModel.CartItems.Count == 0)
        {
            return;
        }

        var store = OperationalDemoStore.Instance;
        if (viewModel.SelectedPaymentMethod == "Contado")
        {
            var cashDialog = new CashCheckoutWindow(viewModel.TotalAmount)
            {
                Owner = Window.GetWindow(this)
            };
            if (cashDialog.ShowDialog() != true)
            {
                return;
            }
        }

        if (viewModel.SelectedPaymentMethod == "Crédito")
        {
            if (viewModel.SelectedCustomer is null)
            {
                MessageBox.Show(Window.GetWindow(this), "Selecciona un cliente para registrar la venta a crédito.",
                    "Cliente requerido", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!viewModel.CanSellOnCredit)
            {
                var authorization = new AuthorizationCodeWindow("conceder esta venta a crédito")
                {
                    Owner = Window.GetWindow(this)
                };
                if (authorization.ShowDialog() != true) return;
            }

            var confirm = MessageBox.Show(Window.GetWindow(this),
                $"¿Registrar {OperationalDemoStore.FormatMoney(viewModel.TotalAmount)} a crédito para {viewModel.SelectedCustomer.Name}?",
                "Confirmar crédito", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }
        }

        var paymentType = viewModel.SelectedPaymentMethod == "Crédito"
            ? SalePaymentType.Credit
            : SalePaymentType.Cash;
        try
        {
            store.RecordSale(
                paymentType,
                paymentType == SalePaymentType.Credit ? viewModel.SelectedCustomer : null,
                viewModel.CartItems.Select(item => new SaleLineInput(item.Product, item.Quantity, item.UnitPrice)).ToArray());
            viewModel.ClearCartCommand.Execute(null);
        }
        catch (InvalidOperationException exception)
        {
            MessageBox.Show(Window.GetWindow(this), exception.Message, "No se pudo registrar la venta",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ToggleCashSession_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not PosViewModel viewModel) return;

        var store = OperationalDemoStore.Instance;
        var isClosing = store.IsCashOpen;
        if (!isClosing && !viewModel.CanOpenCash)
        {
            var authorization = new AuthorizationCodeWindow("abrir la caja")
            {
                Owner = Window.GetWindow(this)
            };
            if (authorization.ShowDialog() != true) return;
        }

        var expectedAmount = store.ActiveCashSession?.ExpectedAmount ?? 0m;
        var dialog = new CashSessionWindow(isClosing, expectedAmount)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            if (isClosing)
            {
                store.CloseCashSession(dialog.CashCounts);
            }
            else
            {
                store.OpenCashSession(dialog.CashCounts);
            }
        }
        catch (InvalidOperationException exception)
        {
            MessageBox.Show(Window.GetWindow(this), exception.Message, "Caja", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
