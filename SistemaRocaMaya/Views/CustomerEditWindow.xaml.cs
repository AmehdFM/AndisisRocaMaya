using System.Windows;
using System.Windows.Controls;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.Views;

public partial class CustomerEditWindow : Window
{
    private readonly OperationalDemoStore _store;
    public CustomerRecord? CreatedCustomer { get; private set; }

    public CustomerEditWindow(OperationalDemoStore store, CustomerRecord? customer = null)
    {
        InitializeComponent();
        _store = store;
        if (customer is not null)
        {
            TitleText.Text = "Ficha del cliente";
            NameBox.Text = customer.Name;
            PhoneBox.Text = customer.Phone;
            TypeBox.SelectedIndex = customer.Type == CustomerType.Person ? 0 : 1;
            IdentificationBox.Text = customer.TaxOrIdentity;
            NameBox.IsReadOnly = true;
            PhoneBox.IsReadOnly = true;
            IdentificationBox.IsReadOnly = true;
            TypeBox.IsEnabled = false;
            PrimaryButton.Visibility = Visibility.Collapsed;
            CancelButton.Content = "Cerrar";
        }
    }

    private void TypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IdentificationLabel is not null && TypeBox.SelectedItem is ComboBoxItem type)
        {
            IdentificationLabel.Text = type.Tag?.ToString() == "Company" ? "RTN" : "Identidad";
        }
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (NameBox.Text.Trim().Length == 0
            || PhoneBox.Text.Trim().Length == 0
            || IdentificationBox.Text.Trim().Length == 0
            || TypeBox.SelectedItem is not ComboBoxItem selectedType)
        {
            ValidationText.Visibility = Visibility.Visible;
            return;
        }

        var type = selectedType.Tag?.ToString() == "Company" ? CustomerType.Company : CustomerType.Person;
        CreatedCustomer = _store.CreateCustomer(
            NameBox.Text,
            PhoneBox.Text,
            type,
            IdentificationBox.Text);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
