using System.Collections.Specialized;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.ViewModels;

public sealed class SalesViewModel : ObservableObject
{
    private string _searchText = "";
    private string _sessionSearchText = "";
    private SaleRecord? _selectedSale;
    private CashSessionRecord? _selectedSession;

    public OperationalDemoStore Store { get; } = OperationalDemoStore.Instance;
    public ListCollectionView Sales { get; }
    public ListCollectionView CashSessions { get; }
    public string SessionSearchText
    {
        get => _sessionSearchText;
        set
        {
            if (SetProperty(ref _sessionSearchText, value)) CashSessions.Refresh();
        }
    }
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                Sales.Refresh();
            }
        }
    }
    public SaleRecord? SelectedSale
    {
        get => _selectedSale;
        set
        {
            if (SetProperty(ref _selectedSale, value))
            {
                OnPropertyChanged(nameof(SelectedSaleLines));
            }
        }
    }
    public IEnumerable<SaleLineRecord> SelectedSaleLines => SelectedSale?.Lines ?? Array.Empty<SaleLineRecord>();
    public CashSessionRecord? SelectedSession
    {
        get => _selectedSession;
        set
        {
            if (SetProperty(ref _selectedSession, value))
            {
                OnPropertyChanged(nameof(SelectedOpeningCount));
                OnPropertyChanged(nameof(SelectedClosingCount));
            }
        }
    }
    public IEnumerable<CashDenominationCount> SelectedOpeningCount => SelectedSession?.OpeningCount ?? Array.Empty<CashDenominationCount>();
    public IEnumerable<CashDenominationCount> SelectedClosingCount => SelectedSession?.ClosingCount ?? Array.Empty<CashDenominationCount>();

    public SalesViewModel()
    {
        Sales = new ListCollectionView(Store.Sales);
        Sales.Filter = FilterSale;
        CashSessions = new ListCollectionView(Store.CashSessions);
        CashSessions.Filter = FilterSession;
        Store.Sales.CollectionChanged += Sales_CollectionChanged;
        Store.CashSessions.CollectionChanged += CashSessions_CollectionChanged;
        SelectedSale = Store.Sales.FirstOrDefault();
        SelectedSession = Store.CashSessions.FirstOrDefault();
    }

    private bool FilterSale(object item) => item is SaleRecord sale
        && (string.IsNullOrWhiteSpace(SearchText)
            || sale.Ticket.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || sale.CustomerName.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || sale.DateText.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || sale.PaymentText.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));

    private bool FilterSession(object item) => item is CashSessionRecord session
        && (string.IsNullOrWhiteSpace(SessionSearchText)
            || session.OpenedText.Contains(SessionSearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || session.ClosedText.Contains(SessionSearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));

    private void Sales_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Sales.Refresh();
    }

    private void CashSessions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(CashSessions));
        CashSessions.Refresh();
        if (e.NewItems is not null && e.NewItems.Count > 0)
        {
            SelectedSession = (CashSessionRecord)e.NewItems[0]!;
        }
    }
}
