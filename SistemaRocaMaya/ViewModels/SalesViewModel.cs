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
    private string _cashReportActionMessage = "";

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
        foreach (var session in _store.CashSessions.Where(session => session.ClosedAt?.Date == date)
                     .OrderBy(session => session.ClosedAt))
        {
            notes.Add($"Cierre {session.ClosingFolio} · Fondo inicial: {session.OpeningAmountText} · " +
                      $"Esperado: {session.ExpectedAmountText} · Contado: {session.ClosingAmountText} · Diferencia: {session.DifferenceText}");
        }

        return new("Ventas en efectivo por fecha", date.ToString("dd/MM/yyyy"),
            [new("Productos", 2.6f), new("Fecha y hora", 1.2f), new("Cliente", 1.3f),
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
        OnPropertyChanged(nameof(SelectedCashSales));
        OnPropertyChanged(nameof(DailyTotalText));
        CashReportActionMessage = SelectedDate is null ? "No hay fechas disponibles."
            : SelectedCashSales.Count == 0 ? "No hay ventas en efectivo para esta fecha." : "";
        ExportCashReportCommand.NotifyCanExecuteChanged();
        PrintCashReportCommand.NotifyCanExecuteChanged();
    }
}

public sealed record CashSaleRow(SaleRecord Sale)
{
    public string Products => string.Join(", ", Sale.Lines.Select(line => $"{line.ProductName} × {line.Quantity}"));
    public int Quantity => Sale.Lines.Sum(line => line.Quantity);
}
