using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.Views;

public partial class CreditPaymentWindow : Window
{
    private readonly decimal _balance;
    private string _amountInput = "0";

    public decimal PaymentAmount { get; private set; }

    public CreditPaymentWindow(string customerName, decimal balance)
    {
        InitializeComponent();
        _balance = balance;
        CustomerNameText.Text = customerName;
        BalanceText.Text = OperationalDemoStore.FormatMoney(balance);
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

    private void FullBalance_Click(object sender, RoutedEventArgs e)
    {
        _amountInput = _balance.ToString("0.00", CultureInfo.InvariantCulture);
        UpdateAmounts();
    }

    private void UpdateAmounts()
    {
        AmountBox.Text = _amountInput;
        decimal.TryParse(_amountInput, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount);

        RemainingBalanceText.Text = OperationalDemoStore.FormatMoney(Math.Max(0m, _balance - amount));
        ConfirmButton.IsEnabled = amount > 0 && amount <= _balance;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmButton.IsEnabled
            || !decimal.TryParse(_amountInput, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
        {
            return;
        }

        PaymentAmount = amount;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
