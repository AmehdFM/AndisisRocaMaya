using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.ViewModels;

public sealed class CreditsViewModel : ObservableObject
{
    private string _searchText = "";
    private string _historySearchText = "";
    private string _statusMessage = "";
    private CustomerRecord? _selectedCustomer;

    public OperationalDemoStore Store { get; } = OperationalDemoStore.Instance;
    public ListCollectionView CustomersWithCredit { get; }
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }
    public CustomerRecord? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (SetProperty(ref _selectedCustomer, value))
            {
                SelectedCustomerHistory = new ListCollectionView(value?.CreditHistory ?? new System.Collections.ObjectModel.ObservableCollection<CreditMovementRecord>());
                SelectedCustomerHistory.Filter = FilterMovement;
                OnPropertyChanged(nameof(SelectedCustomerBalanceText));
                OnPropertyChanged(nameof(SelectedCustomerHistory));
                OnPropertyChanged(nameof(HasSelectedCustomer));
                OnPropertyChanged(nameof(IsCustomerSelected));
            }
        }
    }
    public string SelectedCustomerBalanceText => OperationalDemoStore.FormatMoney(SelectedCustomer?.Balance ?? 0m);
    public ListCollectionView SelectedCustomerHistory { get; private set; } = new(Array.Empty<CreditMovementRecord>());
    public string HistorySearchText
    {
        get => _historySearchText;
        set
        {
            if (SetProperty(ref _historySearchText, value)) SelectedCustomerHistory.Refresh();
        }
    }
    public bool HasSelectedCustomer => SelectedCustomer is not null && SelectedCustomer.Balance > 0;
    public IRelayCommand ShowCustomerListCommand { get; }
    public bool IsCustomerSelected => SelectedCustomer is not null;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                CustomersWithCredit.Refresh();
            }
        }
    }

    public CreditsViewModel()
    {
        CustomersWithCredit = new ListCollectionView(Store.Customers);
        CustomersWithCredit.Filter = FilterCustomer;
        ShowCustomerListCommand = new RelayCommand(() => SelectedCustomer = null);
        Store.Customers.CollectionChanged += Customers_CollectionChanged;
        foreach (var customer in Store.Customers)
        {
            customer.PropertyChanged += Customer_PropertyChanged;
        }
    }

    private bool FilterCustomer(object item) => item is CustomerRecord customer
        && customer.Balance > 0
        && (string.IsNullOrWhiteSpace(SearchText)
            || customer.Name.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || customer.Phone.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || customer.TaxOrIdentity.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));

    private bool FilterMovement(object item) => item is CreditMovementRecord movement
        && (string.IsNullOrWhiteSpace(HistorySearchText)
            || movement.Description.Contains(HistorySearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || movement.TypeLabel.Contains(HistorySearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || movement.Date.ToString("dd/MM/yyyy").Contains(HistorySearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));

    public void RecordPayment(decimal amount)
    {
        if (SelectedCustomer is null)
        {
            return;
        }

        try
        {
            Store.RecordCreditPayment(SelectedCustomer, amount);
            StatusMessage = "Abono registrado.";
            OnPropertyChanged(nameof(SelectedCustomerBalanceText));
            OnPropertyChanged(nameof(HasSelectedCustomer));
            CustomersWithCredit.Refresh();
        }
        catch (InvalidOperationException exception)
        {
            StatusMessage = exception.Message;
        }
    }

    private void Customers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (CustomerRecord customer in e.NewItems)
            {
                customer.PropertyChanged += Customer_PropertyChanged;
            }
        }
        CustomersWithCredit.Refresh();
    }

    private void Customer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CustomerRecord.Balance))
        {
            CustomersWithCredit.Refresh();
            OnPropertyChanged(nameof(SelectedCustomerBalanceText));
            OnPropertyChanged(nameof(HasSelectedCustomer));
        }
    }
}
