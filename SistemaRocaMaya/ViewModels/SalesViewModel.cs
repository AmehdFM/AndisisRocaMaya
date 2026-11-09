using System.Collections.Specialized;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.ViewModels;

public sealed class SalesViewModel : ObservableObject
{
    private DateTime? _sessionStartDate = DateTime.Today.AddDays(-30);
    private DateTime? _sessionEndDate = DateTime.Today;
    private string _cashReportActionMessage = "Datos de demostración · Los cierres se conservan en memoria durante esta sesión.";
    private CashSessionRecord? _selectedSession;

    public OperationalDemoStore Store { get; } = OperationalDemoStore.Instance;
    public ListCollectionView CashSessions { get; }
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

    public string CashReportActionMessage
    {
        get => _cashReportActionMessage;
        private set => SetProperty(ref _cashReportActionMessage, value);
    }

    public IRelayCommand ExportCashReportCommand { get; }
    public IRelayCommand PrintCashReportCommand { get; }
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
        CashSessions = new ListCollectionView(Store.CashSessions);
        CashSessions.Filter = FilterSession;
        Store.CashSessions.CollectionChanged += CashSessions_CollectionChanged;
        SelectedSession = Store.CashSessions.FirstOrDefault();
        ExportCashReportCommand = new RelayCommand(() => CashReportActionMessage = "Demostración: la exportación a PDF no genera un archivo en este prototipo.");
        PrintCashReportCommand = new RelayCommand(() => CashReportActionMessage = "Demostración: la impresión no se envía a una impresora en este prototipo.");
    }

    private bool FilterSession(object item)
    {
        if (item is not CashSessionRecord session) return false;
        var sessionDate = (session.ClosedAt ?? session.OpenedAt).Date;
        if (SessionStartDate is DateTime start && sessionDate < start.Date) return false;
        if (SessionEndDate is DateTime end && sessionDate > end.Date) return false;
        return true;
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
