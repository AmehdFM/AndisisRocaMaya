using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
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
    private string _agingSearchText = "";
    private DateTime _agingAsOfDate = DateTime.Today;
    private string _agingActionMessage = "Datos de demostración · plazo ilustrativo de 30 días";
    private const int DemoCreditTermDays = 30;
    private readonly ObservableCollection<CreditAgingCustomerViewModel> _allAgingCustomers = [];

    public OperationalDemoStore Store { get; } = OperationalDemoStore.Instance;
    public ListCollectionView CustomersWithCredit { get; }
    public ListCollectionView AgingCustomers { get; }
    public IReadOnlyList<CreditAgingBar> AgingBars { get; private set; } = [];
    public IReadOnlyList<CreditAgingRanking> AgingRankings { get; private set; } = [];
    public string AgingActionMessage
    {
        get => _agingActionMessage;
        private set => SetProperty(ref _agingActionMessage, value);
    }
    public string AgingSearchText
    {
        get => _agingSearchText;
        set
        {
            if (SetProperty(ref _agingSearchText, value))
            {
                AgingCustomers.Refresh();
                RefreshAgingSummary();
            }
        }
    }
    public DateTime AgingAsOfDate
    {
        get => _agingAsOfDate;
        set
        {
            if (SetProperty(ref _agingAsOfDate, value)) RefreshAgingCustomers();
        }
    }
    public IRelayCommand ExportAgingReportCommand { get; }
    public IRelayCommand PrintAgingReportCommand { get; }
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
        AgingCustomers = new ListCollectionView(_allAgingCustomers);
        AgingCustomers.Filter = FilterAgingCustomer;
        ExportAgingReportCommand = new RelayCommand(() => AgingActionMessage = "Demostración: la exportación a PDF no genera un archivo en este prototipo.");
        PrintAgingReportCommand = new RelayCommand(() => AgingActionMessage = "Demostración: la impresión no se envía a una impresora en este prototipo.");
        ShowCustomerListCommand = new RelayCommand(() => SelectedCustomer = null);
        Store.Customers.CollectionChanged += Customers_CollectionChanged;
        foreach (var customer in Store.Customers)
        {
            SubscribeCustomer(customer);
        }
        RefreshAgingCustomers();
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

    private bool FilterAgingCustomer(object item) => item is CreditAgingCustomerViewModel customer
        && (string.IsNullOrWhiteSpace(AgingSearchText)
            || customer.CustomerName.Contains(AgingSearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || customer.Phone.Contains(AgingSearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));

    private void SubscribeCustomer(CustomerRecord customer)
    {
        customer.PropertyChanged += Customer_PropertyChanged;
        customer.CreditHistory.CollectionChanged += CreditHistory_CollectionChanged;
    }

    private void RefreshAgingCustomers()
    {
        _allAgingCustomers.Clear();
        foreach (var customer in Store.Customers)
        {
            var row = BuildAgingCustomer(customer, AgingAsOfDate);
            if (row.Balance > 0m) _allAgingCustomers.Add(row);
        }
        AgingCustomers.Refresh();
        RefreshAgingSummary();
    }

    private void RefreshAgingSummary()
    {
        var rows = AgingCustomers.Cast<CreditAgingCustomerViewModel>().ToArray();
        var amounts = new (string Label, decimal Amount)[]
        {
            ("0–30 días", rows.Sum(row => row.Days0To30)),
            ("31–60 días", rows.Sum(row => row.Days31To60)),
            ("61–90 días", rows.Sum(row => row.Days61To90)),
            ("Más de 90 días", rows.Sum(row => row.Over90))
        };
        var maximum = amounts.Max(bar => bar.Amount);
        AgingBars = amounts.Select(bar =>
        {
            var share = maximum == 0m ? 0d : (double)(bar.Amount / maximum);
            return new CreditAgingBar(bar.Label, OperationalDemoStore.FormatMoney(bar.Amount),
                new GridLength(share, GridUnitType.Star), new GridLength(1d - share, GridUnitType.Star));
        }).ToArray();
        AgingRankings =
        [
            new("Mayores saldos pendientes", "Sin cuentas con saldo pendiente.",
                rows.OrderByDescending(row => row.Balance)
                    .ThenBy(row => row.CustomerName)
                    .Take(2)
                    .Select(row => new CreditAgingRankEntry(row.CustomerName, row.BalanceText))
                    .ToArray()),
            new("Más días de mora", "Sin cuentas vencidas.",
                rows.Where(row => row.DaysOverdue > 0)
                    .OrderByDescending(row => row.DaysOverdue)
                    .ThenByDescending(row => row.Balance)
                    .ThenBy(row => row.CustomerName)
                    .Take(2)
                    .Select(row => new CreditAgingRankEntry(row.CustomerName, row.DaysOverdueText))
                    .ToArray())
        ];
        OnPropertyChanged(nameof(AgingBars));
        OnPropertyChanged(nameof(AgingRankings));
    }


    private static CreditAgingCustomerViewModel BuildAgingCustomer(CustomerRecord customer, DateTime asOfDate)
    {
        var openCharges = new List<(DateTime Date, decimal Remaining)>();
        foreach (var movement in customer.CreditHistory.Where(movement => movement.Date.Date <= asOfDate.Date).OrderBy(movement => movement.Date))
        {
            if (movement.Type == CreditMovementType.Charge)
            {
                openCharges.Add((movement.Date.Date, movement.Amount));
                continue;
            }

            var paymentRemaining = movement.Amount;
            for (var index = 0; index < openCharges.Count && paymentRemaining > 0; index++)
            {
                var charge = openCharges[index];
                var applied = Math.Min(charge.Remaining, paymentRemaining);
                openCharges[index] = (charge.Date, charge.Remaining - applied);
                paymentRemaining -= applied;
            }
        }

        var outstanding = openCharges.Where(charge => charge.Remaining > 0).ToArray();
        decimal days0To30 = 0m, days31To60 = 0m, days61To90 = 0m, over90 = 0m;
        foreach (var charge in outstanding)
        {
            var ageDays = Math.Max(0, (asOfDate.Date - charge.Date).Days);
            if (ageDays <= 30) days0To30 += charge.Remaining;
            else if (ageDays <= 60) days31To60 += charge.Remaining;
            else if (ageDays <= 90) days61To90 += charge.Remaining;
            else over90 += charge.Remaining;
        }

        var oldest = outstanding.OrderBy(charge => charge.Date).FirstOrDefault();
        var dueDate = oldest.Remaining > 0 ? oldest.Date.AddDays(DemoCreditTermDays) : (DateTime?)null;
        var overdueDays = dueDate is DateTime due ? Math.Max(0, (asOfDate.Date - due.Date).Days) : 0;
        var status = overdueDays == 0 ? "Vigente" : overdueDays <= 30 ? "Mora de 1–30 días" : "Mora mayor a 30 días";

        return new CreditAgingCustomerViewModel(
            customer.Name,
            customer.Phone,
            days0To30 + days31To60 + days61To90 + over90,
            days0To30,
            days31To60,
            days61To90,
            over90,
            dueDate,
            overdueDays,
            status);
    }

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
                customer.CreditHistory.CollectionChanged += CreditHistory_CollectionChanged;
            }
        }
        CustomersWithCredit.Refresh();
        RefreshAgingCustomers();
    }

    private void Customer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CustomerRecord.Balance))
        {
            CustomersWithCredit.Refresh();
            RefreshAgingCustomers();
            OnPropertyChanged(nameof(SelectedCustomerBalanceText));
            OnPropertyChanged(nameof(HasSelectedCustomer));
        }
    }

    private void CreditHistory_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshAgingCustomers();
}

public sealed record CreditAgingCustomerViewModel(
    string CustomerName,
    string Phone,
    decimal Balance,
    decimal Days0To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Over90,
    DateTime? OldestDueDate,
    int DaysOverdue,
    string Status)
{
    public string BalanceText => OperationalDemoStore.FormatMoney(Balance);
    public string Days0To30Text => OperationalDemoStore.FormatMoney(Days0To30);
    public string Days31To60Text => OperationalDemoStore.FormatMoney(Days31To60);
    public string Days61To90Text => OperationalDemoStore.FormatMoney(Days61To90);
    public string Over90Text => OperationalDemoStore.FormatMoney(Over90);
    public string OldestDueDateText => OldestDueDate?.ToString("dd/MM/yyyy") ?? "—";
    public string DaysOverdueText => DaysOverdue == 0 ? "0 · vigente" : $"{DaysOverdue} días";
}

public sealed record CreditAgingBar(string Label, string Amount, GridLength FilledWidth, GridLength RemainingWidth);

public sealed record CreditAgingRanking(string Title, string EmptyMessage, IReadOnlyList<CreditAgingRankEntry> Rows);
public sealed record CreditAgingRankEntry(string CustomerName, string Value);
