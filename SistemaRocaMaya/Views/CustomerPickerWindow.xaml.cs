using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views;

public partial class CustomerPickerWindow : Window
{
    private readonly ObservableCollection<PosCustomerViewModel> _customers;
    private ICollectionView? _customersView;

    public PosCustomerViewModel? SelectedCustomer { get; private set; }

    public CustomerPickerWindow(
        ObservableCollection<PosCustomerViewModel> customers,
        PosCustomerViewModel? selectedCustomer)
    {
        InitializeComponent();
        _customers = customers;
        var customersView = CollectionViewSource.GetDefaultView(_customers);
        _customersView = customersView;
        customersView.Filter = FilterCustomer;
        CustomersList.ItemsSource = customersView;
        CustomersList.SelectedItem = selectedCustomer;
        Loaded += (_, _) => SearchBox.Focus();
    }

    private bool FilterCustomer(object item)
    {
        if (item is not PosCustomerViewModel customer)
        {
            return false;
        }

        var filter = SearchBox.Text.Trim();
        return filter.Length == 0
            || customer.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
            || customer.Phone.Contains(filter, StringComparison.CurrentCultureIgnoreCase);
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
        if (CustomersList.SelectedItem is PosCustomerViewModel customer)
        {
            SelectedCustomer = customer;
            DialogResult = true;
        }
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        var phone = PhoneBox.Text.Trim();
        if (name.Length == 0 || phone.Length == 0)
        {
            ValidationText.Visibility = Visibility.Visible;
            return;
        }

        var customer = new PosCustomerViewModel(name, phone);
        _customers.Add(customer);
        SelectedCustomer = customer;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
