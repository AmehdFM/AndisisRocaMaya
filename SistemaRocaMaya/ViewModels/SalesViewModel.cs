using System.Collections.Specialized;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.ViewModels;

public sealed class SalesViewModel : ObservableObject
{
    private string _searchText = "";
    private string _sessionSearchText = "";
    private DateTime? _sessionStartDate = DateTime.Today.AddDays(-30);
    private DateTime? _sessionEndDate = DateTime.Today;
    private string _cashReportActionMessage = "Datos de demostración · Los cierres se conservan en memoria durante esta sesión.";
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

    public DateTime? SessionStartDate
    {
        get => _sessionStartDate;
        set
        {
            if (SetProperty(ref _sessionStartDate, value)) CashSessions.Refresh();
        }
    }

    public DateTime? SessionEndDate
    {
        get => _sessionEndDate;
        set
        {
            if (SetProperty(ref _sessionEndDate, value)) CashSessions.Refresh();
        }
    }

    public string CashReportIdentity => "Consulfarma Rocamaya · Pulpería Santa Lucía";
    public string CashReportSubtitle => "Conciliación del efectivo por turno · Área responsable: Caja";
    public string CashReportIssuedAtText => $"Emitido: {DateTime.Now:dd/MM/yyyy HH:mm}";
    public string CashReportActionMessage
    {
        get => _cashReportActionMessage;
        private set => SetProperty(ref _cashReportActionMessage, value);
    }

    public IRelayCommand ExportCashReportCommand { get; }
    public IRelayCommand PrintCashReportCommand { get; }
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
                OnPropertyChanged(nameof(SelectedCashSales));
            }
        }
    }
    public IEnumerable<CashDenominationCount> SelectedOpeningCount => SelectedSession?.OpeningCount ?? Array.Empty<CashDenominationCount>();
    public IEnumerable<CashDenominationCount> SelectedClosingCount => SelectedSession?.ClosingCount ?? Array.Empty<CashDenominationCount>();
    public IEnumerable<SaleRecord> SelectedCashSales => SelectedSession is null
        ? Enumerable.Empty<SaleRecord>()
        : SelectedSession.CashSalesDetail;

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
        ExportCashReportCommand = new RelayCommand(() => CashReportActionMessage = "Demostración: la exportación a PDF no genera un archivo en este prototipo.");
        PrintCashReportCommand = new RelayCommand(() => CashReportActionMessage = "Demostración: la impresión no se envía a una impresora en este prototipo.");
    }

    private bool FilterSale(object item) => item is SaleRecord sale
        && (string.IsNullOrWhiteSpace(SearchText)
            || sale.Ticket.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || sale.CustomerName.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || sale.DateText.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || sale.PaymentText.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));

    private bool FilterSession(object item)
    {
        if (item is not CashSessionRecord session) return false;
        var sessionDate = (session.ClosedAt ?? session.OpenedAt).Date;
        if (SessionStartDate is DateTime start && sessionDate < start.Date) return false;
        if (SessionEndDate is DateTime end && sessionDate > end.Date) return false;
        return string.IsNullOrWhiteSpace(SessionSearchText)
            || session.OpenedText.Contains(SessionSearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || session.ClosedText.Contains(SessionSearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || $"Caja {session.Id}".Contains(SessionSearchText.Trim(), StringComparison.CurrentCultureIgnoreCase);
    }

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
