using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace SistemaRocaMaya.Views;

public partial class CashCheckoutWindow : Window
{
    private readonly decimal _total;
    private string _amountInput = "0";

    public CashCheckoutWindow(decimal total)
    {
        InitializeComponent();
        _total = total;
        TotalText.Text = FormatMoney(_total);
        UpdateAmounts();
    }

    private void Key_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string key })
        {
            return;
        }

        if (key == "C")
        {
            _amountInput = "0";
        }
        else if (key == "⌫")
        {
            _amountInput = _amountInput.Length > 1 ? _amountInput[..^1] : "0";
        }
        else if (key == ".")
        {
            if (!_amountInput.Contains('.'))
            {
                _amountInput += ".";
            }
        }
        else if (_amountInput.Length < 12)
        {
            var decimalIndex = _amountInput.IndexOf('.');
            if (decimalIndex >= 0 && _amountInput.Length - decimalIndex > 2)
            {
                return;
            }

            _amountInput = _amountInput == "0" ? key : _amountInput + key;
        }

        UpdateAmounts();
    }

    private void Exact_Click(object sender, RoutedEventArgs e)
    {
        _amountInput = _total.ToString("0.00", CultureInfo.InvariantCulture);
        UpdateAmounts();
    }

    private void UpdateAmounts()
    {
        AmountBox.Text = _amountInput;
        decimal.TryParse(_amountInput, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var received);

        var isEnough = received >= _total;
        DifferenceLabel.Text = isEnough ? "Cambio a entregar" : "Falta por cobrar";
        DifferenceText.Text = FormatMoney(Math.Abs(_total - received));
        ConfirmButton.IsEnabled = _total > 0 && isEnough;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (ConfirmButton.IsEnabled)
        {
            DialogResult = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private static string FormatMoney(decimal amount) => $"L {amount:0.00}";
}
