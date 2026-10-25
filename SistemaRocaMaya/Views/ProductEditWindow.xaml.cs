using System.Globalization;
using System.Windows;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.Views;

public partial class ProductEditWindow : Window
{
    private readonly OperationalDemoStore _store;

    public ProductRecord? CreatedProduct { get; private set; }

    public ProductEditWindow(OperationalDemoStore store)
    {
        InitializeComponent();
        _store = store;
        PurchaseDatePicker.SelectedDate = DateTime.Today;
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var id = IdBox.Text.Trim();
        var name = NameBox.Text.Trim();
        if (id.Length == 0 || name.Length == 0
            || !decimal.TryParse(PriceBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var price)
            || price <= 0
            || !int.TryParse(QuantityBox.Text, out var quantity)
            || quantity < 0)
        {
            ValidationText.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            CreatedProduct = _store.CreateProduct(
                id,
                name,
                price,
                quantity,
                PurchaseDatePicker.SelectedDate ?? DateTime.Today,
                ExpirationDatePicker.SelectedDate);
            DialogResult = true;
        }
        catch (InvalidOperationException exception)
        {
            ValidationText.Text = exception.Message;
            ValidationText.Visibility = Visibility.Visible;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
