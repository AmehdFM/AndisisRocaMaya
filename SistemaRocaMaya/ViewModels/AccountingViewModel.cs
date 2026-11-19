using Dapper;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;
using SistemaRocaMaya.Services.Reports;

namespace SistemaRocaMaya.ViewModels;

public sealed class AccountingViewModel : ObservableObject
{
    private static readonly HashSet<string> AccountingSections = new(StringComparer.Ordinal)
    {
        "Indicadores financieros",
        "Libro diario",
        "Libro mayor",
        "Hoja de Kardex",
        "Estado de situación financiera",
        "Estado de resultados"
    };

    private readonly OperationalDemoStore _store = OperationalDemoStore.Instance;
    private string _selectedSection = "Indicadores financieros";
    private int _selectedManagementIndicatorIndex;
    private string _selectedLedgerAccount = "Caja";
    private int _selectedJournalYear = DateTime.Today.Year;
    private int _selectedJournalMonth = DateTime.Today.Month;
    private DateTime? _selectedJournalDay;
    private bool _printWholeJournalMonth;
    private int _selectedLedgerYear = DateTime.Today.Year;
    private int _selectedLedgerMonth = DateTime.Today.Month;
    private DateTime? _financialPositionCutoffMonth = FirstDayOfMonth(DateTime.Today);
    private DateTime? _incomeStartDate = FirstDayOfMonth(DateTime.Today).AddMonths(-5);
    private DateTime? _incomeEndDate = FirstDayOfMonth(DateTime.Today);
    private DateTime? _kardexStartDate = DateTime.Today.AddDays(-31);
    private DateTime? _kardexEndDate = DateTime.Today;
    private ProductRecord? _selectedKardexProduct;
    private string _kardexActionMessage = "";
    private string _journalActionMessage = "";
    private string _ledgerActionMessage = "";
    private string _statementActionMessage = "";
    private decimal _reportRevenue;
    private decimal _reportCostOfSales;
    private decimal _reportReceivables;
    private decimal _reportInventory;
    private decimal _reportCash;
    private decimal _reportPayables;
    private decimal _reportAssets;
    private decimal _reportEquity;
    private decimal _reportNetIncome;
    private decimal _reportNetIncomeAtCutoff;

    private readonly ObservableCollection<JournalEntryViewModel> _allJournalEntries = [];
    private readonly List<LedgerEntryViewModel> _allLedgerEntries = [];

    public AccountingViewModel()
    {
        JournalEntries = new ListCollectionView(_allJournalEntries);
        JournalEntries.Filter = FilterJournalEntry;
        KardexMovements = new ListCollectionView(_allKardexMovements);
        KardexMovements.Filter = FilterKardexMovement;
        _selectedKardexProduct = KardexProducts.FirstOrDefault(product => product.Name == "Arroz 1 libra")
            ?? KardexProducts.FirstOrDefault();
        using (var connection = RocaMayaDatabase.Instance.Open())
        {
            foreach (var account in connection.Query<string>("SELECT nombre FROM cuentas_contables WHERE activa=1 ORDER BY codigo"))
                LedgerAccounts.Add(account);
        }
        InitializeOperationBasedReports();
        ExportJournalCommand = new RelayCommand(() => PdfExportService.Export(CreateJournalReport, "Libro_diario.pdf", message => JournalActionMessage = message));
        ExportLedgerCommand = new RelayCommand(() => PdfExportService.Export(CreateLedgerReport, "Libro_mayor.pdf", message => LedgerActionMessage = message));
        PrintLedgerCommand = new RelayCommand(() => ReportPrintService.Print(CreateLedgerReport, message => LedgerActionMessage = message));
        ExportFinancialPositionCommand = new RelayCommand(ExportFinancialPosition);
        ExportIncomeStatementCommand = new RelayCommand(() => PdfExportService.Export(CreateIncomeStatementReport, "Estado_resultados.pdf", message => StatementActionMessage = message));
        ExportKardexCommand = new RelayCommand(() => PdfExportService.Export(CreateKardexReport, "Kardex.pdf", message => KardexActionMessage = message));
        PrintKardexCommand = new RelayCommand(() => ReportPrintService.Print(CreateKardexReport, message => KardexActionMessage = message));
        PrintJournalCommand = new RelayCommand(() => ReportPrintService.Print(CreateJournalReport, message => JournalActionMessage = message));
        PrintFinancialPositionCommand = new RelayCommand(() => ReportPrintService.Print(CreateFinancialPositionReport, message => StatementActionMessage = message));
        PrintIncomeStatementCommand = new RelayCommand(() => ReportPrintService.Print(CreateIncomeStatementReport, message => StatementActionMessage = message));
        SubscribeToDemoChanges();
        ExportSummaryCommand = new RelayCommand(() => PdfExportService.Export(CreateSummaryReport,
            "Resumen_contable.pdf", message => SummaryActionMessage = message));
        PrintSummaryCommand = new RelayCommand(() => ReportPrintService.Print(CreateSummaryReport,
            message => SummaryActionMessage = message));
    }

    private string _summaryActionMessage = "";
    public string SummaryActionMessage
    {
        get => _summaryActionMessage;
        private set => SetProperty(ref _summaryActionMessage, value);
    }
    public IRelayCommand ExportSummaryCommand { get; }
    public IRelayCommand PrintSummaryCommand { get; }

    public FinancialIndicatorsDocument CreateSummaryReport()
    {
        RefreshManagementIndicators(CalculateSaleCosts());
        return new FinancialIndicatorsDocument(DateTime.Today, ManagementIndicators.ToArray());
    }

    public string SelectedSection
    {
        get => _selectedSection;
        private set
        {
            if (SetProperty(ref _selectedSection, value))
            {
                if (value == "Indicadores financieros")
                {
                    SelectedManagementIndicatorIndex = 0;
                    OnPropertyChanged(nameof(SelectedManagementIndicatorIndex));
                }
                OnPropertyChanged(nameof(SummaryVisibility));
                OnPropertyChanged(nameof(JournalVisibility));
                OnPropertyChanged(nameof(LedgerVisibility));
                OnPropertyChanged(nameof(KardexVisibility));
                OnPropertyChanged(nameof(FinancialPositionVisibility));
                OnPropertyChanged(nameof(IncomeStatementVisibility));
            }
        }
    }


    public IReadOnlyList<DateTime> ReportMonthOptions { get; } = BuildReportMonthOptions();

    private static DateTime FirstDayOfMonth(DateTime date) => new(date.Year, date.Month, 1);

    private static DateTime LastDayOfMonth(DateTime date) => new(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));

    private static IReadOnlyList<DateTime> BuildReportMonthOptions()
    {
        var currentMonth = FirstDayOfMonth(DateTime.Today);
        var firstSale = OperationalDemoStore.Instance.Sales.Select(sale => sale.Date).DefaultIfEmpty(currentMonth).Min();
        var firstMonth = FirstDayOfMonth(firstSale < currentMonth.AddMonths(-11) ? firstSale : currentMonth.AddMonths(-11));
        var monthCount = (currentMonth.Year - firstMonth.Year) * 12 + currentMonth.Month - firstMonth.Month + 1;
        return Enumerable.Range(0, monthCount).Select(firstMonth.AddMonths).ToArray();
    }

    public string StatementActionMessage
    {
        get => _statementActionMessage;
        private set => SetProperty(ref _statementActionMessage, value);
    }

    public DateTime? FinancialPositionCutoffMonth
    {
        get => _financialPositionCutoffMonth;
        set
        {
            if (SetProperty(ref _financialPositionCutoffMonth, value))
            {
                RefreshFinancialPositionOutput();
            }
        }
    }

    public DateTime? IncomeStartDate
    {
        get => _incomeStartDate;
        set
        {
            if (SetProperty(ref _incomeStartDate, value))
            {
                RefreshIncomeStatementOutput();
            }
        }
    }

    public DateTime? IncomeEndDate
    {
        get => _incomeEndDate;
        set
        {
            if (SetProperty(ref _incomeEndDate, value))
            {
                RefreshIncomeStatementOutput();
            }
        }
    }

    public DateTime? KardexStartDate
    {
        get => _kardexStartDate;
        set
        {
            if (SetProperty(ref _kardexStartDate, value)) RefreshKardex();
        }
    }

    public DateTime? KardexEndDate
    {
        get => _kardexEndDate;
        set
        {
            if (SetProperty(ref _kardexEndDate, value))
            {
                BuildKardex();
                RefreshKardex();
            }
        }
    }

    public ListCollectionView JournalEntries { get; }
    public IReadOnlyList<LedgerEntryViewModel> LedgerEntries { get; private set; } = [];
    public IReadOnlyList<LedgerAccountTotals> LedgerTotals { get; private set; } = [];

    public IReadOnlyList<int> JournalYears { get; private set; } = [];
    public IReadOnlyList<JournalMonthOption> JournalMonths { get; } = Enumerable.Range(1, 12)
        .Select(month => new JournalMonthOption(month,
            OperationalDemoStore.HondurasCulture.DateTimeFormat.GetMonthName(month))).ToArray();
    public IReadOnlyList<DateTime> JournalDays { get; private set; } = [];

    public int SelectedJournalYear
    {
        get => _selectedJournalYear;
        set
        {
            if (SetProperty(ref _selectedJournalYear, value)) RefreshJournalDays();
        }
    }

    public int SelectedJournalMonth
    {
        get => _selectedJournalMonth;
        set
        {
            if (SetProperty(ref _selectedJournalMonth, value)) RefreshJournalDays();
        }
    }

    public DateTime? SelectedJournalDay
    {
        get => _selectedJournalDay;
        set
        {
            if (SetProperty(ref _selectedJournalDay, value)) JournalEntries.Refresh();
        }
    }

    public bool PrintWholeJournalMonth
    {
        get => _printWholeJournalMonth;
        set => SetProperty(ref _printWholeJournalMonth, value);
    }

    public int SelectedLedgerYear
    {
        get => _selectedLedgerYear;
        set
        {
            if (value is >= 1 and <= 9999 && SetProperty(ref _selectedLedgerYear, value)) RefreshLedger();
        }
    }

    public int SelectedLedgerMonth
    {
        get => _selectedLedgerMonth;
        set
        {
            if (value is >= 1 and <= 12 && SetProperty(ref _selectedLedgerMonth, value)) RefreshLedger();
        }
    }

    private DateTime LedgerStartDate => new(SelectedLedgerYear, SelectedLedgerMonth, 1);
    private DateTime LedgerEndDate => LastDayOfMonth(LedgerStartDate);
    public LedgerAccountTotals? SelectedLedgerTotals { get; private set; }

    public ObservableCollection<string> LedgerAccounts { get; } = [];

    public string SelectedLedgerAccount
    {
        get => _selectedLedgerAccount;
        set
        {
            if (SetProperty(ref _selectedLedgerAccount, value)) RefreshLedger();
        }
    }

    public string JournalActionMessage
    {
        get => _journalActionMessage;
        private set => SetProperty(ref _journalActionMessage, value);
    }

    public string LedgerActionMessage
    {
        get => _ledgerActionMessage;
        private set => SetProperty(ref _ledgerActionMessage, value);
    }

    public IRelayCommand ExportJournalCommand { get; }
    public IRelayCommand ExportLedgerCommand { get; }
    public IRelayCommand PrintLedgerCommand { get; }
    public IRelayCommand ExportFinancialPositionCommand { get; }
    public IRelayCommand ExportIncomeStatementCommand { get; }
    public IRelayCommand ExportKardexCommand { get; }
    public IRelayCommand PrintKardexCommand { get; }
    public IRelayCommand PrintJournalCommand { get; }
    public IRelayCommand PrintFinancialPositionCommand { get; }
    public IRelayCommand PrintIncomeStatementCommand { get; }

    public ObservableCollection<ProductRecord> KardexProducts => _store.Products;

    public ProductRecord? SelectedKardexProduct
    {
        get => _selectedKardexProduct;
        set
        {
            if (SetProperty(ref _selectedKardexProduct, value))
            {
                BuildKardex();
                RefreshKardex();
            }
        }
    }
    public string KardexClosingStock => $"{KardexRemainingStock.Sum(layer => layer.Quantity)} unidades";
    public string KardexClosingValue => $"Costo final: {FormatMoney(KardexRemainingStock.Sum(layer => layer.Quantity * layer.UnitValue))}";
    public string KardexActionMessage
    {
        get => _kardexActionMessage;
        private set => SetProperty(ref _kardexActionMessage, value);
    }

    public Visibility SummaryVisibility => VisibilityFor("Indicadores financieros");
    public Visibility JournalVisibility => VisibilityFor("Libro diario");
    public Visibility LedgerVisibility => VisibilityFor("Libro mayor");
    public Visibility KardexVisibility => VisibilityFor("Hoja de Kardex");
    public Visibility FinancialPositionVisibility => VisibilityFor("Estado de situación financiera");
    public Visibility IncomeStatementVisibility => VisibilityFor("Estado de resultados");

    public ObservableCollection<AccountingMetricViewModel> SummaryMetrics { get; } =
    [
        new("INGRESOS", "L 38,400.00", ""),
        new("COSTOS Y GASTOS", "L 25,120.00", ""),
        new("RESULTADO NETO", "L 13,280.00", ""),
        new("ACTIVOS", "L 52,900.00", "")
    ];

    public ObservableCollection<AccountingIndicatorViewModel> ManagementIndicators { get; } = [];
    public int SelectedManagementIndicatorIndex
    {
        get => _selectedManagementIndicatorIndex;
        set
        {
            if (value >= 0 && value < ManagementIndicators.Count)
                SetProperty(ref _selectedManagementIndicatorIndex, value);
        }
    }

    public IReadOnlyList<FinancialPositionBar> FinancialPositionBars { get; private set; } = [];
    public FinancialPositionAssessment FinancialPositionFinancing { get; private set; } = new(0m, 0m);
    public IReadOnlyList<IncomePeriodChange> IncomePeriodChanges { get; private set; } = [];
    public string FinancialPositionScaleTop { get; private set; } = "";
    public string FinancialPositionScaleMiddle { get; private set; } = "";
    public string FinancialPositionScaleBottom { get; private set; } = "";
    public double FinancialPositionNegativeHeight { get; private set; }

    public ObservableCollection<AccountingMetricViewModel> FinancialPositionMetrics { get; } =
    [
        new("ACTIVOS", "L 52,900.00", ""),
        new("PASIVOS", "L 7,600.00", ""),
        new("PATRIMONIO", "L 45,300.00", "")
    ];

    public ObservableCollection<IncomeChartMonth> IncomeChartMonths { get; } = [];
    public string IncomeChartStatusMessage { get; private set; } = "";
    public double IncomeChartNegativeHeight { get; private set; }
    public string IncomeChartMaximumText { get; private set; } = "";
    public string IncomeChartMidpointText { get; private set; } = "";
    public string IncomeChartMinimumText { get; private set; } = "";


    private readonly ObservableCollection<KardexMovementViewModel> _allKardexMovements = [];

    public ListCollectionView KardexMovements { get; }
    public IReadOnlyList<KardexStockRow> KardexRemainingStock { get; private set; } = [];

    public ObservableCollection<StatementLineViewModel> FinancialPositionLines { get; } =
    [
        new("ACTIVOS", "", "", false, true),
        new("", "Caja", "L 12,400.00", false),
        new("", "Crédito por cobrar", "L 3,300.00", false),
        new("", "Inventario de mercadería", "L 37,200.00", false),
        new("", "Pagos anticipados", "L 0.00", false),
        new("TOTAL ACTIVOS CORRIENTES", "", "L 52,900.00", true),
        new("ACTIVOS NO CORRIENTES", "", "", false, true),
        new("", "Propiedad, planta y equipo neto", "L 0.00", false),
        new("TOTAL ACTIVOS", "", "L 52,900.00", true),
        new("PASIVOS", "", "", false, true),
        new("", "Proveedores", "L 7,600.00", false),
        new("", "Impuestos y otras cuentas por pagar", "L 0.00", false),
        new("TOTAL PASIVOS CORRIENTES", "", "L 7,600.00", true),
        new("", "Deudas a largo plazo", "L 0.00", false),
        new("TOTAL PASIVOS", "", "L 7,600.00", true),
        new("PATRIMONIO", "", "", false, true),
        new("", "Capital social", "L 32,020.00", false),
        new("", "Resultados acumulados", "L 0.00", false),
        new("", "Resultado neto del período", "L 13,280.00", false),
        new("TOTAL PATRIMONIO", "", "L 45,300.00", true),
        new("TOTAL PASIVOS Y PATRIMONIO", "", "L 52,900.00", true)
    ];

    public ObservableCollection<StatementLineViewModel> IncomeStatementLines { get; } = [];

    private void InitializeOperationBasedReports()
    {
        _allJournalEntries.Clear();
        _allLedgerEntries.Clear();
        var saleCosts = CalculateSaleCosts();
        using var connection = RocaMayaDatabase.Instance.Open();
        var postings = connection.Query("""
            SELECT a.id,a.numero,a.fecha_hora,COALESCE(d.concepto,a.concepto) AS concepto,c.nombre,
                d.debito_centavos,d.credito_centavos,COALESCE(v.ticket,p.numero,m.folio) AS origen
            FROM asientos_contables a JOIN asiento_detalles d ON d.asiento_id=a.id
            JOIN cuentas_contables c ON c.id=d.cuenta_id
            LEFT JOIN ventas v ON v.id=a.venta_id LEFT JOIN compras p ON p.id=a.compra_id
            LEFT JOIN movimientos_credito m ON m.id=a.movimiento_credito_id
            ORDER BY a.fecha_hora,a.id,d.orden
            """).Select(row => new AccountingPosting((int)row.id, RocaMayaDatabase.ReadDate((string)row.fecha_hora),
                row.origen is string origin ? $"{(string)row.numero} · {origin}" : (string)row.numero,
                (string)row.concepto, (string)row.nombre, RocaMayaDatabase.Money((long)row.debito_centavos),
                RocaMayaDatabase.Money((long)row.credito_centavos))).ToArray();

        var journalPostings = postings.OrderBy(posting => posting.Date).ThenBy(posting => posting.Reference)
            .ThenBy(posting => posting.TransactionId).ThenBy(posting => posting.Credit > 0m).ThenBy(posting => posting.Account);
        foreach (var transaction in journalPostings.GroupBy(posting => posting.TransactionId))
        {
            var startsGroup = true;
            foreach (var posting in transaction)
            {
                _allJournalEntries.Add(new(posting.TransactionId, posting.Date, posting.Reference, posting.Description,
                    posting.Account, posting.Debit, posting.Credit, startsGroup));
                startsGroup = false;
            }
        }

        var creditAccounts = connection.Query<string>(
            "SELECT nombre FROM cuentas_contables WHERE naturaleza='ACREEDORA'").ToHashSet(StringComparer.Ordinal);
        foreach (var account in LedgerAccounts)
        {
            var creditNormal = creditAccounts.Contains(account);
            var entries = postings.Where(posting => posting.Account == account)
                .OrderBy(posting => posting.Date).ThenBy(posting => posting.TransactionId);
            var balance = 0m;
            foreach (var posting in entries)
            {
                balance += creditNormal ? posting.Credit - posting.Debit : posting.Debit - posting.Credit;
                _allLedgerEntries.Add(new(account, posting.Date, posting.Reference, posting.Description, posting.Debit, posting.Credit, balance));
            }
        }

        BuildKardex();
        PopulateFinancialReports();
        RefreshManagementIndicators(saleCosts);
        RefreshJournalPeriod();
        RefreshLedger();
        RefreshKardex();
    }

    private void SubscribeToDemoChanges()
    {
        _store.Purchases.CollectionChanged += DemoRecords_CollectionChanged;
        _store.Sales.CollectionChanged += DemoRecords_CollectionChanged;
        _store.Products.CollectionChanged += DemoRecords_CollectionChanged;
        _store.Customers.CollectionChanged += Customers_CollectionChanged;
        _store.CashSessions.CollectionChanged += CashSessions_CollectionChanged;
        _store.PropertyChanged += Store_PropertyChanged;
        foreach (var customer in _store.Customers) SubscribeCustomer(customer);
        foreach (var session in _store.CashSessions) session.PropertyChanged += CashSession_PropertyChanged;
    }

    private void Customers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (CustomerRecord customer in e.NewItems) SubscribeCustomer(customer);
        InitializeOperationBasedReports();
    }

    private void CashSessions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (CashSessionRecord session in e.NewItems) session.PropertyChanged += CashSession_PropertyChanged;
        InitializeOperationBasedReports();
    }

    private void SubscribeCustomer(CustomerRecord customer)
    {
        customer.PropertyChanged += Customer_PropertyChanged;
        customer.CreditHistory.CollectionChanged += DemoRecords_CollectionChanged;
    }

    private void DemoRecords_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InitializeOperationBasedReports();
    private void Customer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CustomerRecord.Balance)) InitializeOperationBasedReports();
    }
    private void CashSession_PropertyChanged(object? sender, PropertyChangedEventArgs e) => InitializeOperationBasedReports();
    private void Store_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OperationalDemoStore.CashSessions)) InitializeOperationBasedReports();
    }

    private static Dictionary<int, decimal> CalculateSaleCosts()
    {
        using var connection = RocaMayaDatabase.Instance.Open();
        return connection.Query("""
            SELECT d.venta_id,SUM(c.cantidad*c.costo_unitario_centavos) AS costo
            FROM costeos_venta_lote c JOIN venta_detalles d ON d.id=c.venta_detalle_id GROUP BY d.venta_id
            """).ToDictionary(row => (int)row.venta_id, row => RocaMayaDatabase.Money((long)row.costo));
    }

    private void BuildKardex()
    {
        _allKardexMovements.Clear();
        KardexRemainingStock = [];
        if (SelectedKardexProduct is not { } product) return;

        using var connection = RocaMayaDatabase.Instance.Open();
        var movements = connection.Query("""
            SELECT l.id,l.folio,l.fecha_ingreso AS fecha,l.cantidad_ingresada AS cantidad,
                l.costo_unitario_centavos AS costo,1 AS entrada,l.id AS orden
            FROM lotes_inventario l WHERE l.producto_id=@DatabaseId
            UNION ALL
            SELECT l.id,l.folio,v.fecha_hora,c.cantidad,c.costo_unitario_centavos,0,c.id
            FROM costeos_venta_lote c JOIN lotes_inventario l ON l.id=c.lote_id
            JOIN venta_detalles d ON d.id=c.venta_detalle_id JOIN ventas v ON v.id=d.venta_id
            WHERE l.producto_id=@DatabaseId
            ORDER BY fecha,entrada DESC,orden
            """, new { product.DatabaseId });

        var layers = new Dictionary<int, InventoryCostLayer>();
        foreach (var row in movements)
        {
            var date = RocaMayaDatabase.ReadDate((string)row.fecha);
            if (KardexEndDate is DateTime end && date.Date > end.Date) break;
            int id = (int)row.id;
            int quantity = (int)row.cantidad;
            var cost = RocaMayaDatabase.Money((long)row.costo);
            var entry = (int)row.entrada == 1;
            if (entry) layers.Add(id, new InventoryCostLayer((string)row.folio, quantity, cost));
            else layers[id].RemainingQuantity -= quantity;
            var layer = layers[id];
            var total = FormatMoney(quantity * cost);

            _allKardexMovements.Add(new(date, entry ? "Compra" : "Venta", layer.Lot,
                entry ? quantity.ToString() : "—", entry ? FormatMoney(cost) : "—", entry ? total : "—",
                entry ? "—" : quantity.ToString(), entry ? "—" : FormatMoney(cost), entry ? "—" : total,
                layer.RemainingQuantity.ToString(), FormatMoney(layer.UnitCost), FormatMoney(layer.RemainingQuantity * layer.UnitCost)));
        }
        KardexRemainingStock = layers.Values.Where(layer => layer.RemainingQuantity > 0)
            .Select(layer => new KardexStockRow(layer.Lot, layer.RemainingQuantity, layer.UnitCost)).ToArray();
    }

    private void PopulateFinancialReports()
    {
        RefreshFinancialPositionOutput();
        RefreshIncomeStatementOutput();
    }

    private void ExportFinancialPosition() => PdfExportService.Export(
        CreateFinancialPositionReport, $"Estado_situacion_financiera_{FinancialPositionCutoffMonth:yyyy-MM}.pdf",
        message => StatementActionMessage = message);

    public FinancialPositionDocument CreateFinancialPositionReport()
    {
        RefreshFinancialPositionOutput();
        var cutoff = LastDayOfMonth(FinancialPositionCutoffMonth ?? DateTime.Today);
        var rows = FinancialPositionLines.Select(line => new ReportStatementRow(
            line.Group, line.Account, line.Amount, line.IsTotal, line.IsSectionHeader)).ToArray();
        var chart = new ReportChart("Comparativa de activos, patrimonio y pasivos",
            ["Activos", "Patrimonio", "Pasivos"],
            [new("Saldos al corte", [_reportAssets, _reportEquity, _reportPayables])], "Grupo contable");
        return new FinancialPositionDocument(cutoff, rows, chart,
            new("Consejo de financiamiento", FinancialPositionFinancing.Recommendation, FinancialPositionFinancing.RecommendationColor));
    }

    public TableReportDocument CreateJournalReport()
    {
        var rows = _allJournalEntries
            .Where(entry => PrintWholeJournalMonth ? IsInJournalMonth(entry.Date) : entry.Date.Date == SelectedJournalDay)
            .GroupBy(entry => entry.TransactionId)
            .SelectMany(transaction => transaction.Select((entry, index) => new ReportRow(
                [index == 0 ? entry.DateText : "", entry.Reference, entry.Description, entry.DisplayAccount, entry.DebitText, entry.CreditText], StartsGroup: entry.StartsGroup)))
            .ToArray();
        if (rows.Length == 0)
            throw new InvalidOperationException("No hay partidas en el período seleccionado.");

        var period = PrintWholeJournalMonth
            ? new DateTime(SelectedJournalYear, SelectedJournalMonth, 1).ToString("MMMM yyyy", OperationalDemoStore.HondurasCulture)
            : SelectedJournalDay?.ToString("dd/MM/yyyy");
        return new("Libro Diario Contable", $"Período: {period}",
            [new("Fecha"), new("Documento"), new("Concepto", 2), new("Cuenta", 1.5f), new("Abonado", Centered: true), new("Acreditado", Centered: true)],
            rows, landscape: true, groupSeparators: true);
    }

    public TableReportDocument CreateLedgerReport()
    {
        var totals = SelectedLedgerTotals ?? throw new InvalidOperationException("Selecciona una cuenta contable.");
        var rows = LedgerEntries.Select(entry => new ReportRow(
            [entry.DateText, entry.Reference, entry.Description, entry.DebitText, entry.CreditText, entry.BalanceText])).ToArray();
        return new("Libro Mayor", $"{SelectedLedgerAccount} · {FormatPeriod(LedgerStartDate, LedgerEndDate)}",
            [new("Fecha"), new("Documento", 1.8f), new("Concepto", 2), new("Abonado", 1, true), new("Acreditado", 1, true), new("Saldo", 1, true)],
            rows, landscape: true, summary: new("Totales del período", ["Abonado", "Acreditado", "Saldo final"],
                [new([totals.DebitText, totals.CreditText, totals.ClosingText], IsTotal: true)]));
    }

    public TableReportDocument CreateKardexReport()
    {
        if (SelectedKardexProduct is null) throw new InvalidOperationException("Selecciona un producto para exportar su Kardex.");
        ReportColumn[] columns =
        [
            new("Fecha"), new("Movimiento", 1.2f),
            .. new[] { "Entrada", "Salida", "Totales" }.SelectMany(group => new ReportColumn[]
            {
                new("Cantidad", 0.8f, true, Group: group),
                new("Valor", 1, true, Group: group),
                new("Valor total", 1, true, Group: group)
            })
        ];
        var stockRows = KardexRemainingStock.Select(layer => new ReportRow(
            [layer.Folio, layer.Quantity.ToString(), layer.UnitValueText, layer.TotalValueText])).ToList();
        stockRows.Add(new(["Total", KardexRemainingStock.Sum(layer => layer.Quantity).ToString(), "—",
            FormatMoney(KardexRemainingStock.Sum(layer => layer.Quantity * layer.UnitValue))], IsTotal: true));

        return new("Hoja de Kardex",
            $"{SelectedKardexProduct.Name} · {FormatPeriod(KardexStartDate, KardexEndDate)}",
            columns,
            KardexMovements.Cast<KardexMovementViewModel>().Select(entry => new ReportRow(
                [entry.DateText, entry.Description, entry.EntryQuantity, entry.EntryUnitCost, entry.EntryTotal,
                    entry.ExitQuantity, entry.ExitUnitCost, entry.ExitTotal, entry.BalanceQuantity, entry.BalanceUnitCost, entry.BalanceTotal])).ToArray(),
            landscape: true, summary: new("Existencias por lote al corte · total en la última fila",
                ["Lote", "Cantidad", "Valor unitario", "Valor total"], stockRows));
    }

    public TableReportDocument CreateIncomeStatementReport()
    {
        RefreshIncomeStatementOutput();
        var months = IncomeChartMonths.ToArray();
        var chart = new ReportChart("Evolución mensual",
            months.Select(month => month.Label).ToArray(),
            [
                new("Ventas", months.Select(month => month.Sales.Value).ToArray()),
                new("Costos y gastos", months.Select(month => month.Costs.Value).ToArray()),
                new("Utilidad neta", months.Select(month => month.NetIncome.Value).ToArray())
            ]);
        return new("Estado de Resultados", $"Meses: {IncomeStartDate:MM/yyyy} – {IncomeEndDate:MM/yyyy}",
            [new("Categoría", 1.7f), new("Descripción", 2.2f), new("Cantidad", 1, true)],
            IncomeStatementLines.Select(line => new ReportRow([line.Group, line.Account, line.Amount], line.IsTotal)).ToArray(),
            chart: chart, chartCallouts: IncomePeriodChanges.Select(change => new ReportCallout(change.Label, change.Text,
                change.IsFavorable switch { true => "#24724F", false => "#A52532", null => "#455D6F" })).ToArray());
    }

    private void RefreshFinancialPositionOutput()
    {
        var cutoff = LastDayOfMonth(FinancialPositionCutoffMonth ?? DateTime.Today);
        _reportCash = GetCashAsOf(cutoff);
        _reportReceivables = GetReceivablesAsOf(cutoff);
        _reportInventory = GetInventoryValueAsOf(cutoff);
        _reportPayables = GetPayablesAsOf(cutoff);
        _reportAssets = _reportCash + _reportReceivables + _reportInventory;
        _reportEquity = _reportAssets - _reportPayables;
        var costBySale = CalculateSaleCosts();
        _reportNetIncomeAtCutoff = _store.Sales.Where(sale => sale.Date.Date <= cutoff)
            .Sum(sale => sale.Total - costBySale.GetValueOrDefault(sale.Id))
            - GetOperatingExpenses(DateTime.MinValue, cutoff).Sum(expense => expense.Amount);
        var assetMetricIndex = SummaryMetrics.ToList().FindIndex(metric => metric.Label == "ACTIVOS");
        if (assetMetricIndex >= 0) SummaryMetrics[assetMetricIndex] = new("ACTIVOS", FormatMoney(_reportAssets), "");

        FinancialPositionMetrics.Clear();
        FinancialPositionMetrics.Add(new("ACTIVOS", FormatMoney(_reportAssets), ""));
        FinancialPositionMetrics.Add(new("PASIVOS", FormatMoney(_reportPayables), ""));
        FinancialPositionMetrics.Add(new("PATRIMONIO", FormatMoney(_reportEquity), ""));
        FinancialPositionLines.Clear();
        FinancialPositionLines.Add(new("ACTIVOS CORRIENTES", "", "", false, true));
        FinancialPositionLines.Add(new("", "Caja", FormatMoney(_reportCash), false));
        FinancialPositionLines.Add(new("", "Crédito por cobrar", FormatMoney(_reportReceivables), false));
        FinancialPositionLines.Add(new("", "Inventario de mercadería", FormatMoney(_reportInventory), false));
        FinancialPositionLines.Add(new("", "Pagos anticipados", FormatMoney(0m), false));
        FinancialPositionLines.Add(new("TOTAL ACTIVOS CORRIENTES", "", FormatMoney(_reportAssets), true));
        FinancialPositionLines.Add(new("ACTIVOS NO CORRIENTES", "", "", false, true));
        FinancialPositionLines.Add(new("", "Propiedad, planta y equipo neto", FormatMoney(0m), false));
        FinancialPositionLines.Add(new("TOTAL ACTIVOS NO CORRIENTES", "", FormatMoney(0m), true));
        FinancialPositionLines.Add(new("TOTAL ACTIVOS", "", FormatMoney(_reportAssets), true));
        FinancialPositionLines.Add(new("PASIVOS CORRIENTES", "", "", false, true));
        FinancialPositionLines.Add(new("", "Proveedores", FormatMoney(_reportPayables), false));
        FinancialPositionLines.Add(new("TOTAL PASIVOS CORRIENTES", "", FormatMoney(_reportPayables), true));
        FinancialPositionLines.Add(new("PASIVOS NO CORRIENTES", "", "", false, true));
        FinancialPositionLines.Add(new("", "Deudas a largo plazo", FormatMoney(0m), false));
        FinancialPositionLines.Add(new("TOTAL PASIVOS NO CORRIENTES", "", FormatMoney(0m), true));
        FinancialPositionLines.Add(new("TOTAL PASIVOS", "", FormatMoney(_reportPayables), true));
        FinancialPositionLines.Add(new("PATRIMONIO", "", "", false, true));
        FinancialPositionLines.Add(new("", "Capital y resultados acumulados", FormatMoney(_reportEquity - _reportNetIncomeAtCutoff), false));
        FinancialPositionLines.Add(new("", "Resultado neto acumulado al corte", FormatMoney(_reportNetIncomeAtCutoff), false));
        FinancialPositionLines.Add(new("TOTAL PATRIMONIO", "", FormatMoney(_reportEquity), true));
        FinancialPositionLines.Add(new("TOTAL PASIVOS Y PATRIMONIO", "", FormatMoney(_reportPayables + _reportEquity), true));
        var balances = new (string Name, decimal Amount)[]
        {
            ("Activos", _reportAssets), ("Patrimonio", _reportEquity), ("Pasivos", _reportPayables)
        };
        var maximum = Math.Max(1m, balances.Max(item => Math.Abs(item.Amount)));
        var minimum = Math.Min(0m, balances.Min(item => item.Amount));
        FinancialPositionBars = balances.Select(item => new FinancialPositionBar(item.Name, FormatMoney(item.Amount),
            (double)(Math.Max(0m, item.Amount) / maximum * 150m),
            (double)(-Math.Min(0m, item.Amount) / maximum * 150m))).ToArray();
        FinancialPositionScaleTop = FormatMoney(maximum);
        FinancialPositionScaleMiddle = FormatMoney(maximum / 2m);
        FinancialPositionScaleBottom = minimum < 0 ? FormatMoney(minimum) : "";
        FinancialPositionNegativeHeight = (double)(-minimum / maximum * 150m);
        FinancialPositionFinancing = new(_reportAssets, _reportPayables);
        OnPropertyChanged(nameof(FinancialPositionBars));
        OnPropertyChanged(nameof(FinancialPositionScaleTop));
        OnPropertyChanged(nameof(FinancialPositionScaleMiddle));
        OnPropertyChanged(nameof(FinancialPositionScaleBottom));
        OnPropertyChanged(nameof(FinancialPositionNegativeHeight));
        OnPropertyChanged(nameof(FinancialPositionFinancing));
    }

    private void RefreshIncomeStatementOutput()
    {
        var start = IncomeStartDate?.Date ?? DateTime.MinValue;
        var endMonth = IncomeEndDate ?? DateTime.Today;
        var end = new DateTime(endMonth.Year, endMonth.Month, DateTime.DaysInMonth(endMonth.Year, endMonth.Month));
        var saleCosts = CalculateSaleCosts();
        var sales = _store.Sales.Where(sale => IsInPeriod(sale.Date, start, end)).ToArray();
        _reportRevenue = sales.Sum(sale => sale.Total);
        _reportCostOfSales = sales.Sum(sale => saleCosts.GetValueOrDefault(sale.Id));

        var expenses = GetOperatingExpenses(start, end);
        var operatingExpenses = expenses.Sum(expense => expense.Amount);
        const decimal financialExpenses = 0m;
        const decimal taxes = 0m;
        var grossProfit = _reportRevenue - _reportCostOfSales;
        var operatingProfit = grossProfit - operatingExpenses;
        var preTaxProfit = operatingProfit - financialExpenses;
        _reportNetIncome = preTaxProfit - taxes;
        var totalCostsAndExpenses = _reportCostOfSales + operatingExpenses + financialExpenses + taxes;

        SummaryMetrics.Clear();
        SummaryMetrics.Add(new("INGRESOS", FormatMoney(_reportRevenue), ""));
        SummaryMetrics.Add(new("COSTOS Y GASTOS", FormatMoney(totalCostsAndExpenses), ""));
        SummaryMetrics.Add(new("RESULTADO NETO", FormatMoney(_reportNetIncome), ""));
        SummaryMetrics.Add(new("ACTIVOS", FormatMoney(_reportAssets), ""));
        IncomeStatementLines.Clear();
        IncomeStatementLines.Add(new("Ventas", "Ventas al contado y a crédito", FormatMoney(_reportRevenue), false));
        IncomeStatementLines.Add(new("Costo de ventas", "Mercadería vendida a costo PEPS", FormatMoney(_reportCostOfSales), false));
        IncomeStatementLines.Add(new("Utilidad bruta", "Ventas menos costo de ventas", FormatMoney(grossProfit), true));
        IncomeStatementLines.Add(new("Gastos operativos", expenses.Length == 0 ? "Sin gastos registrados" : "Total del período",
            FormatMoney(operatingExpenses), true));
        foreach (var account in expenses.GroupBy(expense => expense.Account).OrderBy(account => account.Key))
        {
            IncomeStatementLines.Add(new("", account.Key, FormatMoney(account.Sum(expense => expense.Amount)), false));
        }
        IncomeStatementLines.Add(new("Utilidad operativa", "Utilidad bruta menos gastos operativos", FormatMoney(operatingProfit), true));
        IncomeStatementLines.Add(new("Gastos financieros", "Sin intereses registrados", FormatMoney(financialExpenses), false));
        IncomeStatementLines.Add(new("Utilidad después de intereses, antes de impuestos", "Utilidad operativa menos gastos financieros", FormatMoney(preTaxProfit), true));
        IncomeStatementLines.Add(new("Impuestos", "Sin cálculo fiscal", FormatMoney(taxes), false));
        IncomeStatementLines.Add(new("Utilidad neta", "Utilidad antes de impuestos menos impuestos", FormatMoney(_reportNetIncome), true));

        RefreshIncomeChart(sales, saleCosts, expenses, start, end);
    }

    private static OperatingExpense[] GetOperatingExpenses(DateTime start, DateTime end)
    {
        using var connection = RocaMayaDatabase.Instance.Open();
        return connection.Query("""
            SELECT substr(a.fecha_hora,1,10) AS fecha,c.nombre,
                SUM(d.debito_centavos-d.credito_centavos) AS importe
            FROM asientos_contables a JOIN asiento_detalles d ON d.asiento_id=a.id
            JOIN cuentas_contables c ON c.id=d.cuenta_id
            WHERE c.tipo='GASTO' AND a.fecha_hora BETWEEN @start AND @end
            GROUP BY fecha,c.id
            """, new { start = RocaMayaDatabase.Date(start.Date), end = RocaMayaDatabase.Date(end.Date.AddHours(23).AddMinutes(59).AddSeconds(59)) })
            .Select(row => new OperatingExpense(RocaMayaDatabase.ReadDate((string)row.fecha + "T00:00:00"),
                (string)row.nombre, RocaMayaDatabase.Money((long)row.importe))).ToArray();
    }

    private sealed record OperatingExpense(DateTime Date, string Account, decimal Amount);

    private void RefreshIncomeChart(IEnumerable<SaleRecord> sales, IReadOnlyDictionary<int, decimal> saleCosts,
        IEnumerable<OperatingExpense> expenses, DateTime start, DateTime end)
    {
        var totalsByMonth = sales.GroupBy(sale => new DateTime(sale.Date.Year, sale.Date.Month, 1))
            .ToDictionary(month => month.Key, month => (
                Sales: month.Sum(sale => sale.Total),
                Costs: month.Sum(sale => saleCosts.GetValueOrDefault(sale.Id))));
        foreach (var month in expenses.GroupBy(expense => FirstDayOfMonth(expense.Date)))
        {
            var totals = totalsByMonth.GetValueOrDefault(month.Key);
            totalsByMonth[month.Key] = (totals.Sales, totals.Costs + month.Sum(expense => expense.Amount));
        }
        var firstMonth = new DateTime(start.Year, start.Month, 1);
        var monthCount = (end.Year - start.Year) * 12 + end.Month - start.Month + 1;
        // Keep an unbounded or very long range readable by showing its months with activity.
        var showEmptyMonths = start != DateTime.MinValue && start <= end;
        var months = showEmptyMonths
            ? Enumerable.Range(0, monthCount).Select(firstMonth.AddMonths)
            : totalsByMonth.Keys.OrderBy(month => month);
        var monthlyValues = months.Select(month => (Date: month, Totals: totalsByMonth.GetValueOrDefault(month))).ToArray();

        IncomePeriodChanges = [];
        if (showEmptyMonths && monthlyValues.Length > 1)
        {
            var first = monthlyValues[0].Totals;
            var last = monthlyValues[^1].Totals;
            IncomePeriodChanges =
            [
                IncomePeriodChange.Compare("Ventas", first.Sales, last.Sales),
                IncomePeriodChange.Compare("Costos y gastos", first.Costs, last.Costs, lowerIsBetter: true),
                IncomePeriodChange.Compare("Utilidad neta", first.Sales - first.Costs, last.Sales - last.Costs)
            ];
        }
        OnPropertyChanged(nameof(IncomePeriodChanges));

        var amounts = monthlyValues.SelectMany(month => new[] { month.Totals.Sales, month.Totals.Costs, month.Totals.Sales - month.Totals.Costs }).ToArray();
        var scale = Math.Max(1m, amounts.Select(Math.Abs).DefaultIfEmpty().Max());
        IncomeChartNegativeHeight = (double)(-Math.Min(0m, amounts.DefaultIfEmpty().Min()) / scale * 100m);
        IncomeChartStatusMessage = start > end ? "El mes inicial debe ser anterior o igual al mes final."
            : totalsByMonth.Count == 0 ? "Sin ventas ni gastos en el período seleccionado."
            : "";
        OnPropertyChanged(nameof(IncomeChartNegativeHeight));
        IncomeChartMaximumText = FormatMoney(scale);
        IncomeChartMidpointText = FormatMoney(scale / 2m);
        IncomeChartMinimumText = IncomeChartNegativeHeight > 0 ? FormatMoney(amounts.Min()) : "";
        OnPropertyChanged(nameof(IncomeChartStatusMessage));
        OnPropertyChanged(nameof(IncomeChartMaximumText));
        OnPropertyChanged(nameof(IncomeChartMidpointText));
        OnPropertyChanged(nameof(IncomeChartMinimumText));

        IncomeChartBar Bar(decimal amount) => new(
            (double)(Math.Max(0m, amount) / scale * 100m),
            (double)(-Math.Min(0m, amount) / scale * 100m),
            FormatMoney(amount), amount);

        IncomeChartMonths.Clear();
        foreach (var month in monthlyValues)
        {
            IncomeChartMonths.Add(new(
                month.Date.ToString("MMM yyyy", OperationalDemoStore.HondurasCulture),
                Bar(month.Totals.Sales), Bar(month.Totals.Costs), Bar(month.Totals.Sales - month.Totals.Costs)));
        }
    }


    private decimal GetReceivablesAsOf(DateTime cutoff) => _store.Customers.SelectMany(customer => customer.CreditHistory)
        .Where(movement => movement.Date.Date <= cutoff)
        .Sum(movement => movement.Type == CreditMovementType.Charge ? movement.Amount : -movement.Amount);

    private decimal GetPayablesAsOf(DateTime cutoff) =>
        _store.Purchases.Where(purchase => purchase.Date.Date <= cutoff).Sum(purchase => purchase.Total);


    private static decimal GetCashAsOf(DateTime cutoff)
    {
        using var connection = RocaMayaDatabase.Instance.Open();
        var until = RocaMayaDatabase.Date(cutoff.Date.AddDays(1));
        return RocaMayaDatabase.Money(connection.ExecuteScalar<long>("""
            SELECT COALESCE(SUM(d.debito_centavos-d.credito_centavos),0)
            FROM asiento_detalles d JOIN asientos_contables a ON a.id=d.asiento_id
            JOIN cuentas_contables c ON c.id=d.cuenta_id
            WHERE c.nombre='Caja' AND a.fecha_hora<@until
            """, new { until }));
    }

    private static decimal GetInventoryValueAsOf(DateTime cutoff)
    {
        using var connection = RocaMayaDatabase.Instance.Open();
        var until = RocaMayaDatabase.Date(cutoff.Date.AddDays(1));
        var total = connection.ExecuteScalar<long>("""
            SELECT COALESCE(SUM((l.cantidad_ingresada-COALESCE((
                SELECT SUM(c.cantidad) FROM costeos_venta_lote c
                JOIN venta_detalles d ON d.id=c.venta_detalle_id JOIN ventas v ON v.id=d.venta_id
                WHERE c.lote_id=l.id AND v.fecha_hora<@until),0))*l.costo_unitario_centavos),0)
            FROM lotes_inventario l WHERE l.fecha_ingreso<@until
            """, new { until });
        return RocaMayaDatabase.Money(total);
    }

    private void RefreshManagementIndicators(IReadOnlyDictionary<int, decimal> saleCosts)
    {
        var currentMonth = FirstDayOfMonth(DateTime.Today);
        var expenses = GetOperatingExpenses(currentMonth.AddMonths(-5), DateTime.Today)
            .ToLookup(expense => FirstDayOfMonth(expense.Date));
        var months = Enumerable.Range(-5, 6).Select(offset => currentMonth.AddMonths(offset))
            .Select(month =>
            {
                var cutoff = month == currentMonth ? DateTime.Today : LastDayOfMonth(month);
                var sales = _store.Sales.Where(sale => IsInPeriod(sale.Date, month, cutoff)).ToArray();
                var revenue = sales.Sum(sale => sale.Total);
                var cost = sales.Sum(sale => saleCosts.GetValueOrDefault(sale.Id));
                var inventory = GetInventoryValueAsOf(cutoff);
                var liquidAssets = GetCashAsOf(cutoff) + GetReceivablesAsOf(cutoff);
                var assets = liquidAssets + inventory;
                var payables = GetPayablesAsOf(cutoff);
                var profit = revenue - cost - expenses[month].Sum(expense => expense.Amount);

                return new
                {
                    Label = month.ToString("MMM yy", OperationalDemoStore.HondurasCulture),
                    GrossMargin = Ratio(revenue - cost, revenue, 100m),
                    NetMargin = Ratio(profit, revenue, 100m),
                    Liquidity = Ratio(assets, payables),
                    QuickRatio = Ratio(liquidAssets, payables),
                    DebtRatio = Ratio(payables, assets, 100m),
                    ReturnOnEquity = Ratio(profit, assets - payables, 100m)
                };
            }).ToArray();
        var labels = months.Select(month => month.Label).ToArray();

        AccountingIndicatorViewModel Indicator(string name, string unit, IEnumerable<decimal?> values,
            string definition, decimal? threshold = null, bool lowerIsBetter = false) =>
            BuildIndicator(name, unit, labels, values.ToArray(), definition, threshold, lowerIsBetter);

        var indicators = new[]
        {
            Indicator("Margen bruto", "%", months.Select(month => month.GrossMargin),
                "Porcentaje de las ventas que queda después de cubrir el costo de la mercadería vendida."),
            Indicator("Margen neto", "%", months.Select(month => month.NetMargin),
                "Porcentaje de las ventas que queda como utilidad después de descontar costos, gastos e impuestos."),
            Indicator("Liquidez corriente", "veces", months.Select(month => month.Liquidity),
                "Cuántos lempiras de activos corrientes respaldan cada lempira de deuda a corto plazo.", 1.5m),
            Indicator("Prueba ácida", "veces", months.Select(month => month.QuickRatio),
                "Cuántos lempiras de efectivo y cuentas por cobrar respaldan cada lempira de deuda a corto plazo, sin contar el inventario.", 1.0m),
            Indicator("Endeudamiento", "%", months.Select(month => month.DebtRatio),
                "Porcentaje de los activos que se financia con deudas y otras obligaciones.", 60m, lowerIsBetter: true),
            Indicator("Rentabilidad del patrimonio", "%", months.Select(month => month.ReturnOnEquity),
                "Porcentaje de utilidad que genera el negocio en relación con su patrimonio.")
        };


        var selectedIndex = SelectedManagementIndicatorIndex;
        if (ManagementIndicators.Count == 0)
        {
            foreach (var indicator in indicators) ManagementIndicators.Add(indicator);
        }
        else
        {
            for (var index = 0; index < indicators.Length; index++)
                ManagementIndicators[index] = indicators[index];
        }
        SelectedManagementIndicatorIndex = selectedIndex;
        OnPropertyChanged(nameof(SelectedManagementIndicatorIndex));
    }

    private static decimal? Ratio(decimal numerator, decimal denominator, decimal multiplier = 1m) =>
        denominator > 0m ? numerator / denominator * multiplier : null;

    private static AccountingIndicatorViewModel BuildIndicator(string name, string unit,
        IReadOnlyList<string> labels, IReadOnlyList<decimal?> values, string definition, decimal? threshold, bool lowerIsBetter)
    {
        var available = values.OfType<decimal>().ToArray();
        var bottom = Math.Min(0m, available.DefaultIfEmpty().Min());
        var top = Math.Max(0m, available.DefaultIfEmpty().Max());
        if (threshold is decimal reference)
        {
            bottom = Math.Min(bottom, reference);
            top = Math.Max(top, reference);
            if (top == reference) top += Math.Max((top - bottom) / 10m, 0.01m);
            if (bottom == reference) bottom -= Math.Max((top - bottom) / 10m, 0.01m);
        }
        if (bottom == top) top = 1m;

        double PlotY(decimal value) => (double)((top - value) / (top - bottom)) * 150;
        var thresholdLine = threshold is decimal limit
            ? new LineGeometry(new Point(0, PlotY(limit)), new Point(600, PlotY(limit)))
            : null;
        var thresholdLabel = threshold is decimal target ? $"Referencia: {target:0.0#} {unit}" : "";

        var line = new PathGeometry();
        var dots = new GeometryGroup();
        PathFigure? segment = null;
        var periods = new IndicatorMonth[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            periods[index] = new(labels[index], values[index] is decimal amount ? $"{amount:0.00} {unit}" : "—", values[index]);
            if (values[index] is not decimal value)
            {
                segment = null;
                continue;
            }

            var point = new Point((index + 0.5) * 600 / values.Count, PlotY(value));
            if (segment is null)
            {
                segment = new PathFigure { StartPoint = point };
                line.Figures.Add(segment);
            }
            else
            {
                segment.Segments.Add(new LineSegment(point, true));
            }
            dots.Children.Add(new EllipseGeometry(point, 3.5, 3.5));
        }

        var current = values[^1];
        return new(name, current is decimal latest ? $"{latest:0.00} {unit}" : "Sin dato", unit,
            definition, IndicatorMonthComparison.Compare(values.Count > 1 ? values[^2] : null, current, unit, lowerIsBetter),
            $"{top:0.##}", $"{(top + bottom) / 2m:0.##}", $"{bottom:0.##}", line, dots, thresholdLine, thresholdLabel, periods);
    }


    private static bool IsInPeriod(DateTime date, DateTime start, DateTime end) => date.Date >= start.Date && date.Date <= end.Date;

    private bool FilterJournalEntry(object item) =>
        item is JournalEntryViewModel entry && entry.Date.Date == SelectedJournalDay;

    private bool IsInJournalMonth(DateTime date) =>
        date.Year == SelectedJournalYear && date.Month == SelectedJournalMonth;

    private bool FilterKardexMovement(object item)
    {
        if (item is not KardexMovementViewModel movement) return false;
        if (KardexStartDate is DateTime start && movement.Date.Date < start.Date) return false;
        if (KardexEndDate is DateTime end && movement.Date.Date > end.Date) return false;
        return true;
    }

    private void RefreshKardex()
    {
        KardexMovements.Refresh();
        OnPropertyChanged(nameof(KardexRemainingStock));
        OnPropertyChanged(nameof(KardexClosingStock));
        OnPropertyChanged(nameof(KardexClosingValue));
    }

    private void RefreshJournalPeriod()
    {
        var years = _allJournalEntries.Select(entry => entry.Date.Year).Append(DateTime.Today.Year)
            .Distinct().OrderByDescending(year => year).ToArray();
        if (!JournalYears.SequenceEqual(years))
        {
            JournalYears = years;
            OnPropertyChanged(nameof(JournalYears));
            OnPropertyChanged(nameof(SelectedJournalYear));
        }

        RefreshJournalDays();
    }

    private void RefreshJournalDays()
    {
        var previousDay = SelectedJournalDay;
        var days = _allJournalEntries.Where(entry => IsInJournalMonth(entry.Date))
            .Select(entry => entry.Date.Date).Distinct().OrderBy(day => day).ToArray();
        if (!JournalDays.SequenceEqual(days))
        {
            JournalDays = days;
            OnPropertyChanged(nameof(JournalDays));
        }

        var selectedDay = previousDay is DateTime day && days.Contains(day)
            ? day
            : days.Select(date => (DateTime?)date).LastOrDefault();
        if (!SetProperty(ref _selectedJournalDay, selectedDay, nameof(SelectedJournalDay)))
            OnPropertyChanged(nameof(SelectedJournalDay));

        JournalEntries.Refresh();
    }

    private void RefreshLedger()
    {
        var start = LedgerStartDate;
        var end = LedgerEndDate;
        var totals = new List<LedgerAccountTotals>();
        var entriesByAccount = _allLedgerEntries.ToLookup(entry => entry.Account);
        LedgerEntries = [];
        SelectedLedgerTotals = null;

        foreach (var account in LedgerAccounts)
        {
            var entries = entriesByAccount[account];
            var openingBalance = entries.LastOrDefault(entry => entry.Date.Date < start)?.Balance
                ?? 0m;
            var rows = entries.Where(entry => IsInPeriod(entry.Date, start, end)).ToArray();
            var closingBalance = rows.LastOrDefault()?.Balance ?? openingBalance;
            var accountTotals = new LedgerAccountTotals(account, openingBalance,
                rows.Sum(entry => entry.Debit), rows.Sum(entry => entry.Credit), closingBalance);
            totals.Add(accountTotals);

            if (account == SelectedLedgerAccount)
            {
                LedgerEntries = [new(account, start, "", "Saldo inicial", 0m, 0m, openingBalance), .. rows];
                SelectedLedgerTotals = accountTotals;
            }
        }

        LedgerTotals =
        [
            .. totals,
            new("Total general", 0m, totals.Sum(account => account.Debit), totals.Sum(account => account.Credit), 0m, IsTotal: true)
        ];
        OnPropertyChanged(nameof(LedgerEntries));
        OnPropertyChanged(nameof(SelectedLedgerTotals));
        OnPropertyChanged(nameof(LedgerTotals));
    }

    private static string FormatMoney(decimal amount) => OperationalDemoStore.FormatMoney(amount);

    private static string FormatPeriod(DateTime? start, DateTime? end)
    {
        var from = start?.ToString("dd/MM/yyyy") ?? "inicio";
        var through = end?.ToString("dd/MM/yyyy") ?? "hoy";
        return $"Período: {from} – {through}";
    }

    public static bool IsAccountingSection(string title) => AccountingSections.Contains(title);

    public void SelectSection(string title)
    {
        if (IsAccountingSection(title))
        {
            SelectedSection = title;
        }
    }

    private Visibility VisibilityFor(string title) => SelectedSection == title ? Visibility.Visible : Visibility.Collapsed;

    private sealed record AccountingPosting(int TransactionId, DateTime Date, string Reference, string Description, string Account, decimal Debit, decimal Credit);

    private sealed class InventoryCostLayer(string lot, int remainingQuantity, decimal unitCost)
    {
        public string Lot { get; } = lot;
        public int RemainingQuantity { get; set; } = remainingQuantity;
        public decimal UnitCost { get; } = unitCost;
    }
}

public sealed record AccountingMetricViewModel(string Label, string Value, string Detail);

public sealed record AccountingIndicatorViewModel(
    string Name, string Value, string Unit, string Definition, IndicatorMonthComparison Comparison,
    string AxisTop, string AxisMiddle, string AxisBottom,
    PathGeometry Line, GeometryGroup Dots, LineGeometry? ThresholdLine, string ThresholdLabel, IReadOnlyList<IndicatorMonth> Months);

public sealed record IndicatorMonth(string Label, string Value, decimal? Amount);

public sealed record JournalMonthOption(int Number, string Name);

public sealed record JournalEntryViewModel(int TransactionId, DateTime Date, string Reference, string Description, string Account, decimal Debit, decimal Credit, bool StartsGroup)
{
    public string DisplayAccount => Credit > 0m ? "\u00A0\u00A0\u00A0\u00A0" + Account : Account;
    public string DateText => Date.ToString("dd/MM/yyyy");
    public string TimeText => Date.ToString("HH:mm");
    public string DebitText => Debit == 0m ? "—" : OperationalDemoStore.FormatMoney(Debit);
    public string CreditText => Credit == 0m ? "—" : OperationalDemoStore.FormatMoney(Credit);
}

public sealed record LedgerAccountTotals(string Account, decimal Opening, decimal Debit, decimal Credit, decimal Closing, bool IsTotal = false)
{
    public string OpeningText => IsTotal ? "" : OperationalDemoStore.FormatMoney(Opening);
    public string DebitText => OperationalDemoStore.FormatMoney(Debit);
    public string CreditText => OperationalDemoStore.FormatMoney(Credit);
    public string ClosingText => IsTotal ? "" : OperationalDemoStore.FormatMoney(Closing);
}

public sealed record LedgerEntryViewModel(string Account, DateTime Date, string Reference, string Description, decimal Debit, decimal Credit, decimal Balance)
{
    public string DateText => Date.ToString("dd/MM/yyyy");
    public string DebitText => Debit == 0m ? "—" : OperationalDemoStore.FormatMoney(Debit);
    public string CreditText => Credit == 0m ? "—" : OperationalDemoStore.FormatMoney(Credit);
    public string BalanceText => OperationalDemoStore.FormatMoney(Balance);
}

public sealed record KardexStockRow(string Folio, int Quantity, decimal UnitValue)
{
    public string UnitValueText => OperationalDemoStore.FormatMoney(UnitValue);
    public string TotalValueText => OperationalDemoStore.FormatMoney(Quantity * UnitValue);
}

public sealed record KardexMovementViewModel(
    DateTime Date,
    string Description,
    string Lot,
    string EntryQuantity,
    string EntryUnitCost,
    string EntryTotal,
    string ExitQuantity,
    string ExitUnitCost,
    string ExitTotal,
    string BalanceQuantity,
    string BalanceUnitCost,
    string BalanceTotal)
{
    public string DateText => Date.ToString("dd/MM/yyyy");
}

public sealed record StatementLineViewModel(string Group, string Account, string Amount, bool IsTotal, bool IsSectionHeader = false);

public sealed record IncomeChartBar(double AboveZero, double BelowZero, string Amount, decimal Value);
public sealed record IncomeChartMonth(string Label, IncomeChartBar Sales, IncomeChartBar Costs, IncomeChartBar NetIncome);

public sealed record FinancialPositionBar(string Name, string Amount, double PositiveHeight, double NegativeHeight);
