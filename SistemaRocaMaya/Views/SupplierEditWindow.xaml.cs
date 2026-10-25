using System.Windows;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.Views;

public partial class SupplierEditWindow : Window
{
    private readonly OperationalDemoStore _store;

    public SupplierRecord? CreatedSupplier { get; private set; }

    public SupplierEditWindow(OperationalDemoStore store)
    {
        InitializeComponent();
        _store = store;
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (NameBox.Text.Trim().Length == 0)
        {
            ValidationText.Visibility = Visibility.Visible;
            return;
        }

        CreatedSupplier = _store.CreateSupplier(NameBox.Text, PhoneBox.Text, ContactBox.Text);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
