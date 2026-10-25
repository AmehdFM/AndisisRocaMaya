using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views;

public partial class CustomerPickerWindow : Window
{
    private readonly OperationalDemoStore _store;
    private readonly ObservableCollection<CustomerRecord> _customers;
    private ICollectionView? _customersView;

    public CustomerRecord? SelectedCustomer { get; private set; }

    public CustomerPickerWindow(
        OperationalDemoStore store,
        CustomerRecord? selectedCustomer)
    {
        InitializeComponent();
        _store = store;
        _customers = store.Customers;
        var customersView = new ListCollectionView(_customers);
        _customersView = customersView;
        customersView.Filter = FilterCustomer;
        CustomersList.ItemsSource = customersView;
        CustomersList.SelectedItem = selectedCustomer;
        Loaded += (_, _) => SearchBox.Focus();
    }

    private bool FilterCustomer(object item)
    {
        if (item is not CustomerRecord customer)
        {
            return false;
        }

        var filter = SearchBox.Text.Trim();
        return filter.Length == 0
            || customer.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
            || customer.Phone.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
            || customer.TaxOrIdentity.Contains(filter, StringComparison.CurrentCultureIgnoreCase);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _customersView?.Refresh();
    }

    private void ShowCreate_Click(object sender, RoutedEventArgs e)
    {
        BrowsePanel.Visibility = Visibility.Collapsed;
        BrowseActions.Visibility = Visibility.Collapsed;
        CreatePanel.Visibility = Visibility.Visible;
        CreateActions.Visibility = Visibility.Visible;
        NameBox.Focus();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        CreatePanel.Visibility = Visibility.Collapsed;
        CreateActions.Visibility = Visibility.Collapsed;
        BrowsePanel.Visibility = Visibility.Visible;
        BrowseActions.Visibility = Visibility.Visible;
        SearchBox.Focus();
    }

    private void Select_Click(object sender, RoutedEventArgs e)
    {
        if (CustomersList.SelectedItem is CustomerRecord customer)
        {
            SelectedCustomer = customer;
            DialogResult = true;
        }
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        var phone = PhoneBox.Text.Trim();
        var identification = IdentificationBox.Text.Trim();
        if (name.Length == 0 || phone.Length == 0 || identification.Length == 0
            || TypeBox.SelectedItem is not ComboBoxItem selectedType)
        {
            ValidationText.Visibility = Visibility.Visible;
            return;
        }

        var type = selectedType.Tag?.ToString() == "Company" ? CustomerType.Company : CustomerType.Person;
        var customer = _store.CreateCustomer(name, phone, type, identification);
        SelectedCustomer = customer;
        DialogResult = true;
    }

    private void TypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IdentificationLabel is not null && TypeBox.SelectedItem is ComboBoxItem selectedType)
        {
            IdentificationLabel.Text = selectedType.Tag?.ToString() == "Company" ? "RTN" : "Identidad";
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
