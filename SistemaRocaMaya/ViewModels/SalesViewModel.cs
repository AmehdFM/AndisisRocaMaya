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

    public DateTime? StartDate
    {
        get => _startDate;
        set
        {
            if (SetProperty(ref _startDate, value)) RefreshDates();
        }
    }

    public DateTime? EndDate
    {
        get => _endDate;
        set
        {
            if (SetProperty(ref _endDate, value)) RefreshDates();
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
            if (SetProperty(ref _selectedDate, value)) RefreshSelectedDay();
        }
    }

    public IReadOnlyList<CashSaleRow> SelectedCashSales { get; private set; } = [];
    public IReadOnlyList<string> CashClosingSummaries { get; private set; } = [];
    public string DailyTotalText => OperationalDemoStore.FormatMoney(SelectedCashSales.Sum(row => row.Sale.Total));

    public string CashReportActionMessage
    {
        get => _cashReportActionMessage;
        private set => SetProperty(ref _cashReportActionMessage, value);
    }

    public IRelayCommand ExportCashReportCommand { get; }
    public IRelayCommand PrintCashReportCommand { get; }

    public SalesViewModel()
    {
        ExportCashReportCommand = new RelayCommand(
            () => PdfExportService.Export(CreateCashReport, "Ventas_en_efectivo.pdf", message => CashReportActionMessage = message),
            () => SelectedDate.HasValue);
        PrintCashReportCommand = new RelayCommand(
            () => ReportPrintService.Print(CreateCashReport, message => CashReportActionMessage = message),
            () => SelectedDate.HasValue);

        _store.Sales.CollectionChanged += (_, _) => RefreshDates();
        _store.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(OperationalDemoStore.CashSessions)) RefreshDates();
        };
        RefreshDates();
    }

    public TableReportDocument CreateCashReport()
    {
        if (SelectedDate is not { } date)
            throw new InvalidOperationException("Selecciona una fecha de venta.");

        var notes = new List<string> { $"Total efectivo del día: {DailyTotalText}" };
        notes.AddRange(CashClosingSummaries);

        return new("Ventas en efectivo por fecha", date.ToString("dd/MM/yyyy"),
            [new("Productos y precio unitario", 2.6f), new("Fecha y hora", 1.2f), new("Cliente", 1.3f),
                new("Cantidad", 0.65f, true), new("Total efectivo", 1, true)],
            SelectedCashSales.Select(row => new ReportRow(
                [row.Products, row.Sale.DateText, row.Sale.CustomerName, row.Quantity.ToString(), row.Sale.TotalText])).ToArray(),
            landscape: true, notes: notes);
    }

    private void RefreshDates()
    {
        var previousDate = SelectedDate;
        var dates = _store.Sales.Where(sale => sale.PaymentType == SalePaymentType.Cash).Select(sale => sale.Date.Date)
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

    private void RefreshSelectedDay()
    {
        SelectedCashSales = _store.Sales
            .Where(sale => sale.PaymentType == SalePaymentType.Cash && sale.Date.Date == SelectedDate)
            .OrderBy(sale => sale.Date).ThenBy(sale => sale.Id)
            .Select(sale => new CashSaleRow(sale)).ToArray();
        CashClosingSummaries = SelectedDate is { } day
            ? _store.CashSessions.Where(session => session.ClosedAt?.Date == day)
                .OrderBy(session => session.ClosedAt)
                .Select(session => $"Cierre {session.ClosingFolio} · Fondo inicial: {session.OpeningAmountText} · " +
                    $"Efectivo esperado: {session.ExpectedAmountText} · Efectivo contado: {session.ClosingAmountText} · Diferencia: {session.DifferenceText}")
                .ToArray()
            : [];
        OnPropertyChanged(nameof(CashClosingSummaries));
        OnPropertyChanged(nameof(SelectedCashSales));
        OnPropertyChanged(nameof(DailyTotalText));
        CashReportActionMessage = StartDate?.Date > EndDate?.Date ? "La fecha inicial debe ser anterior o igual a la fecha final."
            : SelectedDate is null ? "No hay ventas ni cierres en el rango seleccionado."
            : SelectedCashSales.Count == 0 ? "No hay ventas en efectivo para esta fecha." : "";
        ExportCashReportCommand.NotifyCanExecuteChanged();
        PrintCashReportCommand.NotifyCanExecuteChanged();
    }
}

public sealed record CashSaleRow(SaleRecord Sale)
{
    public string Products => string.Join("\n", Sale.Lines.Select(line =>
        $"{line.ProductName} × {line.Quantity} ({OperationalDemoStore.FormatMoney(line.UnitPrice)} c/u)"));
    public int Quantity => Sale.Lines.Sum(line => line.Quantity);
}
