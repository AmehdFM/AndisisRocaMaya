using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;
using SistemaRocaMaya.Services.Reports;

namespace SistemaRocaMaya.ViewModels;

public sealed class SalesViewModel : ObservableObject
{
    private readonly OperationalDemoStore _store = OperationalDemoStore.Instance;
    private IReadOnlyList<DateTime> _saleDates = [];
    private DateTime? _selectedDate;
    private DateTime? _startDate = DateTime.Today.AddDays(-30);
    private DateTime? _endDate = DateTime.Today;
    private string _cashReportActionMessage = "";

    public bool CanBrowseHistory { get; }

    public DateTime? StartDate
    {
        get => _startDate;
        set
        {
            if (CanBrowseHistory && SetProperty(ref _startDate, value)) RefreshDates();
        }
    }

    public DateTime? EndDate
    {
        get => _endDate;
        set
        {
            if (CanBrowseHistory && SetProperty(ref _endDate, value)) RefreshDates();
        }
    }

    public IReadOnlyList<DateTime> SaleDates
    {
        get => _saleDates;
        private set => SetProperty(ref _saleDates, value);
    }

    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (value is DateTime day && !CanBrowseHistory && day.Date != DateTime.Today) return;
            if (SetProperty(ref _selectedDate, value)) RefreshSelectedDay();
        }
    }

    public IReadOnlyList<CashSaleRow> SelectedCashSales { get; private set; } = [];
    public IReadOnlyList<ReportCallout> CashSummaryCards { get; private set; } = [];
    public string Recipient { get; }
    public string ReportContext => SelectedDate is { } date ? $"{Recipient} · {date:dd/MM/yyyy}" : Recipient;

    public string CashReportActionMessage
    {
        get => _cashReportActionMessage;
        private set => SetProperty(ref _cashReportActionMessage, value);
    }

    public IRelayCommand ExportCashReportCommand { get; }
    public IRelayCommand PrintCashReportCommand { get; }

    public SalesViewModel(bool isManagement)
    {
        CanBrowseHistory = isManagement;
        Recipient = isManagement ? "Preparado para la gerencia" : "Preparado para el auxiliar de ventas";
        ExportCashReportCommand = new RelayCommand(
            () => PdfExportService.Export(CreateCashReport, "Ventas_en_efectivo.pdf", message => CashReportActionMessage = message),
            CanReportSelectedDate);
        PrintCashReportCommand = new RelayCommand(
            () => ReportPrintService.Print(CreateCashReport, message => CashReportActionMessage = message),
            CanReportSelectedDate);

        _store.Sales.CollectionChanged += (_, _) => RefreshDates();
        _store.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(OperationalDemoStore.CashSessions)) RefreshDates();
        };
        RefreshDates();
    }

    public TableReportDocument CreateCashReport()
    {
        if (!CanReportSelectedDate() || SelectedDate is not { } day)
            throw new InvalidOperationException("Selecciona una fecha de venta.");

        var closedAt = GetClosedSessions(day).Select(session => session.ClosedAt)
            .OrderBy(date => date).Select(date => $"{date:dd/MM/yyyy HH:mm:ss}").ToArray();
        var context = closedAt.Length > 0
            ? $"{Recipient} · {string.Join(" / ", closedAt)}"
            : $"{Recipient} · {day:dd/MM/yyyy} · Caja sin cerrar";

        return new("Ventas en efectivo por fecha", context,
            [new("Hora", 0.65f), new("Cliente", 1.4f), new("Producto", 2.2f),
                new("Precio unitario", 1.1f, true), new("Cantidad", 0.75f, true), new("Total", 1, true)],
            SelectedCashSales.Select(row => new ReportRow(
                [row.Time, row.Customer, row.Line.ProductName, row.UnitPrice, row.Line.Quantity.ToString(), row.Line.LineTotalText],
                StartsGroup: row.StartsGroup)).ToArray(),
            landscape: true, groupSeparators: true, headerCards: CashSummaryCards);
    }

    private bool CanReportSelectedDate() =>
        SelectedDate is { } day && (CanBrowseHistory || day.Date == DateTime.Today);

    internal void RefreshToday()
    {
        if (!CanBrowseHistory && SelectedDate != DateTime.Today) RefreshDates();
    }

    private void RefreshDates()
    {
        var previousDate = SelectedDate;
        DateTime[] dates = !CanBrowseHistory ? [DateTime.Today] : _store.Sales.Where(sale => sale.PaymentType == SalePaymentType.Cash).Select(sale => sale.Date.Date)
            .Union(_store.CashSessions.Where(session => session.ClosedAt.HasValue).Select(session => session.ClosedAt!.Value.Date))
            .Where(date => (StartDate is null || date >= StartDate.Value.Date)
                && (EndDate is null || date <= EndDate.Value.Date))
            .OrderBy(date => date).ToArray();

        if (!SaleDates.SequenceEqual(dates)) SaleDates = dates;
        SelectedDate = previousDate is { } previous && dates.Contains(previous)
            ? previous
            : dates.Select(date => (DateTime?)date).LastOrDefault();
        RefreshSelectedDay();
    }

    private CashSessionRecord[] GetClosedSessions(DateTime day) =>
        _store.CashSessions.Where(session => session.ClosedAt?.Date == day).ToArray();

    private void RefreshSelectedDay()
    {
        SelectedCashSales = _store.Sales
            .Where(sale => sale.PaymentType == SalePaymentType.Cash && sale.Date.Date == SelectedDate)
            .OrderBy(sale => sale.Date).ThenBy(sale => sale.Id)
            .SelectMany(sale => sale.Lines.Select((line, index) => new CashSaleRow(sale, line, index == 0)))
            .ToArray();
        CashSummaryCards = [];
        if (SelectedDate is { } day)
        {
            var closedSessions = GetClosedSessions(day);
            string ClosingTotal(Func<CashSessionRecord, decimal> value) => closedSessions.Length == 0
                ? "Sin cierre" : OperationalDemoStore.FormatMoney(closedSessions.Sum(value));
            CashSummaryCards =
            [
                new("Efectivo del día", OperationalDemoStore.FormatMoney(SelectedCashSales.Sum(row => row.Line.Quantity * row.Line.UnitPrice))),
                new("Fondo inicial", ClosingTotal(session => session.OpeningAmount)),
                new("Efectivo esperado", ClosingTotal(session => session.ExpectedAmount)),
                new("Efectivo contado", ClosingTotal(session => session.ClosingAmount)),
                new("Diferencia", ClosingTotal(session => session.Difference))
            ];
        }
        OnPropertyChanged(nameof(ReportContext));
        OnPropertyChanged(nameof(CashSummaryCards));
        OnPropertyChanged(nameof(SelectedCashSales));
        CashReportActionMessage = StartDate?.Date > EndDate?.Date ? "La fecha inicial debe ser anterior o igual a la fecha final."
            : SelectedDate is null ? "No hay ventas ni cierres en el rango seleccionado."
            : SelectedCashSales.Count == 0 ? "No hay ventas en efectivo para esta fecha." : "";
        ExportCashReportCommand.NotifyCanExecuteChanged();
        PrintCashReportCommand.NotifyCanExecuteChanged();
    }
}

public sealed record CashSaleRow(SaleRecord Sale, SaleLineRecord Line, bool StartsGroup)
{
    public string Time => StartsGroup ? Sale.Date.ToString("HH:mm") : "";
    public string Customer => StartsGroup ? Sale.CustomerName : "";
    public string UnitPrice => OperationalDemoStore.FormatMoney(Line.UnitPrice);
}
