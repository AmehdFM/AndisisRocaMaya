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

namespace SistemaRocaMaya.ViewModels;

public sealed class AccountingViewModel : ObservableObject
{
    private static readonly HashSet<string> AccountingSections = new(StringComparer.Ordinal)
    {
        "Resumen contable",
        "Libro diario",
        "Libro mayor",
        "Kardex PEPS",
        "Estado de situación financiera",
        "Estado de resultados"
    };

    private readonly OperationalDemoStore _store = OperationalDemoStore.Instance;
    private string _selectedSection = "Resumen contable";
    private string _journalSearchText = "";
    private string _ledgerSearchText = "";
    private string _selectedLedgerAccount = "Caja";
    private DateTime? _journalStartDate = DateTime.Today.AddDays(-31);
    private DateTime? _journalEndDate = DateTime.Today;
    private DateTime? _ledgerStartDate = DateTime.Today.AddDays(-31);
    private DateTime? _ledgerEndDate = DateTime.Today;
    private DateTime? _financialPositionCutoffMonth = FirstDayOfMonth(DateTime.Today);
    private DateTime? _incomeStartDate = FirstDayOfMonth(DateTime.Today).AddMonths(-1);
    private DateTime? _incomeEndDate = FirstDayOfMonth(DateTime.Today);
    private DateTime? _kardexStartDate = DateTime.Today.AddDays(-31);
    private DateTime? _kardexEndDate = DateTime.Today;
    private ProductRecord? _selectedKardexProduct;
    private string _kardexActionMessage = "Datos de demostración";
    private string _journalActionMessage = "Datos de demostración";
    private string _ledgerActionMessage = "Datos de demostración";
    private string _statementActionMessage = "Datos de demostración";
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
    private readonly ObservableCollection<LedgerEntryViewModel> _allLedgerEntries = [];

    public AccountingViewModel()
    {
        JournalEntries = new ListCollectionView(_allJournalEntries);
        JournalEntries.Filter = FilterJournalEntry;
        LedgerEntries = new ListCollectionView(_allLedgerEntries);
        LedgerEntries.Filter = FilterLedgerEntry;
        KardexMovements = new ListCollectionView(_allKardexMovements);
        KardexMovements.Filter = FilterKardexMovement;
        _selectedKardexProduct = KardexProducts.FirstOrDefault(product => product.Name == "Arroz 1 libra")
            ?? KardexProducts.FirstOrDefault();
        InitializeOperationBasedReports();
        ExportJournalCommand = new RelayCommand(() => JournalActionMessage = "Demostración: la exportación a PDF no genera un archivo en este prototipo.");
        ExportLedgerCommand = new RelayCommand(() => LedgerActionMessage = "Demostración: la exportación a PDF no genera un archivo en este prototipo.");
        PrintLedgerCommand = new RelayCommand(() => LedgerActionMessage = "Demostración: la impresión no se envía a una impresora en este prototipo.");
        ExportFinancialPositionCommand = new RelayCommand(() => StatementActionMessage = "Demostración: la exportación a PDF no genera un archivo en este prototipo.");
        ExportIncomeStatementCommand = new RelayCommand(() => StatementActionMessage = "Demostración: la exportación a PDF no genera un archivo en este prototipo.");
        ExportKardexCommand = new RelayCommand(() => KardexActionMessage = "Demostración: la exportación a PDF no genera un archivo en este prototipo.");
        PrintKardexCommand = new RelayCommand(() => KardexActionMessage = "Vista de impresión de demostración · Página 1 de 1 · Papel carta horizontal · Encabezados previstos para repetirse.");
        PrintJournalCommand = new RelayCommand(() => JournalActionMessage = "Vista de impresión de demostración · Página 1 de 1 · Papel carta horizontal · Encabezados previstos para repetirse.");
        PrintFinancialPositionCommand = new RelayCommand(() => StatementActionMessage = "Vista de impresión de demostración · Página 1 de 1 · Papel carta vertical · Encabezados previstos para repetirse.");
        PrintIncomeStatementCommand = new RelayCommand(() => StatementActionMessage = "Vista de impresión de demostración · Página 1 de 1 · Papel carta vertical · Encabezados previstos para repetirse.");
        SubscribeToDemoChanges();
    }

    public string SelectedSection
    {
        get => _selectedSection;
        private set
        {
            if (SetProperty(ref _selectedSection, value))
            {
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
            if (SetProperty(ref _kardexEndDate, value)) RefreshKardex();
        }
    }

    public ListCollectionView JournalEntries { get; }
    public ListCollectionView LedgerEntries { get; }

    public DateTime? JournalStartDate
    {
        get => _journalStartDate;
        set
        {
            if (SetProperty(ref _journalStartDate, value)) RefreshJournal();
        }
    }

    public DateTime? JournalEndDate
    {
        get => _journalEndDate;
        set
        {
            if (SetProperty(ref _journalEndDate, value)) RefreshJournal();
        }
    }

    public string JournalSearchText
    {
        get => _journalSearchText;
        set
        {
            if (SetProperty(ref _journalSearchText, value)) RefreshJournal();
        }
    }

    public DateTime? LedgerStartDate
    {
        get => _ledgerStartDate;
        set
        {
            if (SetProperty(ref _ledgerStartDate, value)) RefreshLedger();
        }
    }

    public DateTime? LedgerEndDate
    {
        get => _ledgerEndDate;
        set
        {
            if (SetProperty(ref _ledgerEndDate, value)) RefreshLedger();
        }
    }

    public ObservableCollection<string> LedgerAccounts { get; } = ["Caja", "Crédito por cobrar", "Inventario", "Costo de ventas", "Ingresos por ventas", "Proveedores"];

    public string SelectedLedgerAccount
    {
        get => _selectedLedgerAccount;
        set
        {
            if (SetProperty(ref _selectedLedgerAccount, value)) RefreshLedger();
        }
    }

    public string LedgerSearchText
    {
        get => _ledgerSearchText;
        set
        {
            if (SetProperty(ref _ledgerSearchText, value)) RefreshLedger();
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
    public string KardexClosingStock => $"{GetKardexClosingMovement()?.TotalStockQuantity ?? 0} unidades";
    public string KardexClosingValue => $"Costo final: {FormatMoney(GetKardexClosingMovement()?.TotalStockCost ?? 0m)}";
    public string KardexActionMessage
    {
        get => _kardexActionMessage;
        private set => SetProperty(ref _kardexActionMessage, value);
    }

    public Visibility SummaryVisibility => VisibilityFor("Resumen contable");
    public Visibility JournalVisibility => VisibilityFor("Libro diario");
    public Visibility LedgerVisibility => VisibilityFor("Libro mayor");
    public Visibility KardexVisibility => VisibilityFor("Kardex PEPS");
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
        var periodStart = JournalStartDate?.Date ?? DateTime.Today.AddDays(-31);
        var periodEnd = JournalEndDate?.Date ?? DateTime.Today;
        var saleCosts = CalculateSaleCosts();
        var postings = new List<AccountingPosting>();

        foreach (var purchase in _store.Purchases.Where(purchase => IsInPeriod(purchase.Date, periodStart, periodEnd)))
        {
            postings.Add(new(purchase.Date, purchase.Number, $"Compra a {purchase.Supplier.Name}", "Inventario", purchase.Total, 0m));
            postings.Add(new(purchase.Date, purchase.Number, $"Compra a {purchase.Supplier.Name}", "Proveedores", 0m, purchase.Total));
        }

        foreach (var sale in _store.Sales.Where(sale => IsInPeriod(sale.Date, periodStart, periodEnd)))
        {
            var account = sale.PaymentType == SalePaymentType.Cash ? "Caja" : "Crédito por cobrar";
            postings.Add(new(sale.Date, sale.Ticket, "Venta de mercadería", account, sale.Total, 0m));
            postings.Add(new(sale.Date, sale.Ticket, "Venta de mercadería", "Ingresos por ventas", 0m, sale.Total));
            var cost = saleCosts.GetValueOrDefault(sale.Id);
            if (cost > 0m)
            {
                postings.Add(new(sale.Date, sale.Ticket, "Costo de mercadería vendida", "Costo de ventas", cost, 0m));
                postings.Add(new(sale.Date, sale.Ticket, "Costo de mercadería vendida", "Inventario", 0m, cost));
            }
        }

        foreach (var customer in _store.Customers)
        foreach (var payment in customer.CreditHistory.Where(movement => movement.Type == CreditMovementType.Payment && IsInPeriod(movement.Date, periodStart, periodEnd)))
        {
            var reference = $"AB-{customer.Id:000}-{payment.Date:MMdd}";
            postings.Add(new(payment.Date, reference, $"Abono recibido · {customer.Name}", "Caja", payment.Amount, 0m));
            postings.Add(new(payment.Date, reference, $"Abono recibido · {customer.Name}", "Crédito por cobrar", 0m, payment.Amount));
        }

        foreach (var posting in postings.OrderBy(posting => posting.Date).ThenBy(posting => posting.Reference).ThenBy(posting => posting.Account))
            _allJournalEntries.Add(new(posting.Date, posting.Reference, posting.Description, posting.Account, posting.Debit, posting.Credit));

        _reportRevenue = _store.Sales.Where(sale => IsInPeriod(sale.Date, periodStart, periodEnd)).Sum(sale => sale.Total);
        _reportCostOfSales = _store.Sales.Where(sale => IsInPeriod(sale.Date, periodStart, periodEnd)).Sum(sale => saleCosts.GetValueOrDefault(sale.Id));
        _reportReceivables = _store.Customers.Sum(customer => customer.Balance);
        _reportInventory = _store.Products.SelectMany(product => product.Batches).Sum(batch => batch.Quantity * batch.UnitCost);
        _reportCash = _store.CashSessions.OrderByDescending(session => session.ClosedAt ?? session.OpenedAt).FirstOrDefault() is { } session
            ? session.ClosedAt is null ? session.ExpectedAmount : session.ClosingAmount
            : 0m;
        _reportPayables = _store.Purchases.Sum(purchase => purchase.Total);
        _reportAssets = _reportCash + _reportReceivables + _reportInventory;
        _reportEquity = _reportAssets - _reportPayables;
        _reportNetIncome = _reportRevenue - _reportCostOfSales;

        var endingBalances = new Dictionary<string, decimal>(StringComparer.Ordinal)
        {
            ["Caja"] = _reportCash,
            ["Crédito por cobrar"] = _reportReceivables,
            ["Inventario"] = _reportInventory,
            ["Costo de ventas"] = _reportCostOfSales,
            ["Ingresos por ventas"] = _reportRevenue,
            ["Proveedores"] = _reportPayables
        };
        foreach (var account in LedgerAccounts)
        {
            var creditNormal = IsCreditNormal(account);
            var entries = postings.Where(posting => posting.Account == account).OrderBy(posting => posting.Date).ThenBy(posting => posting.Reference).ToArray();
            var movement = entries.Sum(posting => creditNormal ? posting.Credit - posting.Debit : posting.Debit - posting.Credit);
            var balance = endingBalances.GetValueOrDefault(account) - movement;
            _allLedgerEntries.Add(new(account, periodStart, "", "Saldo inicial", 0m, 0m, balance));
            foreach (var posting in entries)
            {
                balance += creditNormal ? posting.Credit - posting.Debit : posting.Debit - posting.Credit;
                _allLedgerEntries.Add(new(account, posting.Date, posting.Reference, posting.Description, posting.Debit, posting.Credit, balance));
            }
        }

        BuildKardex();
        PopulateFinancialReports();
        RefreshManagementIndicators(saleCosts);
        JournalEntries.Refresh();
        LedgerEntries.Refresh();
        KardexMovements.Refresh();
        OnPropertyChanged(nameof(KardexClosingStock));
        OnPropertyChanged(nameof(KardexClosingValue));
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

    private Dictionary<int, decimal> CalculateSaleCosts()
    {
        var layersByProduct = new Dictionary<string, List<InventoryCostLayer>>(StringComparer.Ordinal);
        var costsBySale = new Dictionary<int, decimal>();
        var timeline = _store.Purchases.Select(purchase => (purchase.Date, Purchase: (PurchaseRecord?)purchase, Sale: (SaleRecord?)null, IsPurchase: true))
            .Concat(_store.Sales.Select(sale => (sale.Date, Purchase: (PurchaseRecord?)null, Sale: (SaleRecord?)sale, IsPurchase: false)))
            .OrderBy(item => item.Date).ThenBy(item => item.IsPurchase ? 0 : 1);

        foreach (var item in timeline)
        {
            if (item.IsPurchase && item.Purchase is { } purchase)
            {
                foreach (var line in purchase.Lines)
                {
                    if (!layersByProduct.TryGetValue(line.Product.Id, out var layers)) layersByProduct[line.Product.Id] = layers = [];
                    layers.Add(new InventoryCostLayer($"L-{purchase.Id:0000}", line.Quantity, line.UnitCost));
                }
                continue;
            }
            if (item.Sale is not { } sale) continue;

            var totalCost = 0m;
            foreach (var line in sale.Lines)
            {
                if (!layersByProduct.TryGetValue(line.Product.Id, out var layers)) continue;
                var remaining = line.Quantity;
                foreach (var layer in layers)
                {
                    var consumed = Math.Min(layer.RemainingQuantity, remaining);
                    if (consumed <= 0) continue;
                    layer.RemainingQuantity -= consumed;
                    remaining -= consumed;
                    totalCost += consumed * layer.UnitCost;
                    if (remaining == 0) break;
                }
            }
            costsBySale[sale.Id] = totalCost;
        }

        return costsBySale;
    }

    private void BuildKardex()
    {
        _allKardexMovements.Clear();
        if (SelectedKardexProduct is not { } product) return;
        var layers = new List<InventoryCostLayer>();
        var timeline = _store.Purchases.Select(purchase => (purchase.Date, Purchase: (PurchaseRecord?)purchase, Sale: (SaleRecord?)null, IsPurchase: true))
            .Concat(_store.Sales.Select(sale => (sale.Date, Purchase: (PurchaseRecord?)null, Sale: (SaleRecord?)sale, IsPurchase: false)))
            .OrderBy(item => item.Date).ThenBy(item => item.IsPurchase ? 0 : 1);

        foreach (var item in timeline)
        {
            if (item.IsPurchase && item.Purchase is { } purchase)
            {
                foreach (var line in purchase.Lines.Where(line => line.Product.Id == product.Id))
                {
                    var layer = new InventoryCostLayer($"L-{purchase.Id:0000}", line.Quantity, line.UnitCost);
                    layers.Add(layer);
                    var totalQuantity = layers.Sum(existing => existing.RemainingQuantity);
                    var totalCost = layers.Sum(existing => existing.RemainingQuantity * existing.UnitCost);
                    _allKardexMovements.Add(new(purchase.Date, $"Entrada · compra {purchase.Number}", layer.Lot, line.Quantity.ToString(), FormatMoney(line.UnitCost), FormatMoney(line.Quantity * line.UnitCost), "—", "—", "—", layer.RemainingQuantity.ToString(), FormatMoney(layer.UnitCost), FormatMoney(layer.RemainingQuantity * layer.UnitCost), totalQuantity, totalCost));
                }
                continue;
            }
            if (item.Sale is not { } sale) continue;

            foreach (var saleLine in sale.Lines.Where(line => line.Product.Id == product.Id))
            {
                var remaining = saleLine.Quantity;
                foreach (var layer in layers)
                {
                    var consumed = Math.Min(layer.RemainingQuantity, remaining);
                    if (consumed <= 0) continue;
                    layer.RemainingQuantity -= consumed;
                    remaining -= consumed;
                    var totalQuantity = layers.Sum(existing => existing.RemainingQuantity);
                    var totalCost = layers.Sum(existing => existing.RemainingQuantity * existing.UnitCost);
                    _allKardexMovements.Add(new(sale.Date, $"Salida · venta {sale.Ticket}", layer.Lot, "—", "—", "—", consumed.ToString(), FormatMoney(layer.UnitCost), FormatMoney(consumed * layer.UnitCost), layer.RemainingQuantity.ToString(), FormatMoney(layer.UnitCost), FormatMoney(layer.RemainingQuantity * layer.UnitCost), totalQuantity, totalCost));
                    if (remaining == 0) break;
                }
            }
        }
    }

    private void PopulateFinancialReports()
    {
        RefreshFinancialPositionOutput();
        RefreshIncomeStatementOutput();
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
            .Sum(sale => sale.Total - costBySale.GetValueOrDefault(sale.Id));
        var assetMetricIndex = SummaryMetrics.ToList().FindIndex(metric => metric.Label == "ACTIVOS");
        if (assetMetricIndex >= 0) SummaryMetrics[assetMetricIndex] = new("ACTIVOS", FormatMoney(_reportAssets), "");

        FinancialPositionMetrics.Clear();
        FinancialPositionMetrics.Add(new("ACTIVOS", FormatMoney(_reportAssets), ""));
        FinancialPositionMetrics.Add(new("PASIVOS", FormatMoney(_reportPayables), ""));
        FinancialPositionMetrics.Add(new("PATRIMONIO", FormatMoney(_reportEquity), ""));
        FinancialPositionLines.Clear();
        FinancialPositionLines.Add(new("ACTIVOS", "", "", false, true));
        FinancialPositionLines.Add(new("", "Caja", FormatMoney(_reportCash), false));
        FinancialPositionLines.Add(new("", "Crédito por cobrar", FormatMoney(_reportReceivables), false));
        FinancialPositionLines.Add(new("", "Inventario de mercadería · costo PEPS", FormatMoney(_reportInventory), false));
        FinancialPositionLines.Add(new("", "Pagos anticipados", FormatMoney(0m), false));
        FinancialPositionLines.Add(new("TOTAL ACTIVOS CORRIENTES", "", FormatMoney(_reportAssets), true));
        FinancialPositionLines.Add(new("ACTIVOS NO CORRIENTES", "", "", false, true));
        FinancialPositionLines.Add(new("", "Propiedad, planta y equipo neto", FormatMoney(0m), false));
        FinancialPositionLines.Add(new("TOTAL ACTIVOS", "", FormatMoney(_reportAssets), true));
        FinancialPositionLines.Add(new("PASIVOS", "", "", false, true));
        FinancialPositionLines.Add(new("", "Proveedores", FormatMoney(_reportPayables), false));
        FinancialPositionLines.Add(new("TOTAL PASIVOS", "", FormatMoney(_reportPayables), true));
        FinancialPositionLines.Add(new("PATRIMONIO", "", "", false, true));
        FinancialPositionLines.Add(new("", "Capital y resultados acumulados", FormatMoney(_reportEquity - _reportNetIncomeAtCutoff), false));
        FinancialPositionLines.Add(new("", "Resultado neto acumulado al corte", FormatMoney(_reportNetIncomeAtCutoff), false));
        FinancialPositionLines.Add(new("TOTAL PATRIMONIO", "", FormatMoney(_reportEquity), true));
        FinancialPositionLines.Add(new("TOTAL PASIVOS Y PATRIMONIO", "", FormatMoney(_reportPayables + _reportEquity), true));
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

        const decimal operatingExpenses = 0m;
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
        IncomeStatementLines.Add(new("Gastos operativos", "Sin gastos registrados en la demo", FormatMoney(operatingExpenses), false));
        IncomeStatementLines.Add(new("Utilidad operativa", "Utilidad bruta menos gastos operativos", FormatMoney(operatingProfit), true));
        IncomeStatementLines.Add(new("Gastos financieros", "Sin intereses registrados en la demo", FormatMoney(financialExpenses), false));
        IncomeStatementLines.Add(new("Utilidad después de intereses, antes de impuestos", "Utilidad operativa menos gastos financieros", FormatMoney(preTaxProfit), true));
        IncomeStatementLines.Add(new("Impuestos", "Sin cálculo fiscal en la demo", FormatMoney(taxes), false));
        IncomeStatementLines.Add(new("Utilidad neta", "Utilidad antes de impuestos menos impuestos", FormatMoney(_reportNetIncome), true));

        RefreshIncomeChart(sales, saleCosts, start, end);
    }

    private void RefreshIncomeChart(IEnumerable<SaleRecord> sales, IReadOnlyDictionary<int, decimal> saleCosts, DateTime start, DateTime end)
    {
        var totalsByMonth = sales.GroupBy(sale => new DateTime(sale.Date.Year, sale.Date.Month, 1))
            .ToDictionary(month => month.Key, month => (
                Sales: month.Sum(sale => sale.Total),
                Costs: month.Sum(sale => saleCosts.GetValueOrDefault(sale.Id))));
        var firstMonth = new DateTime(start.Year, start.Month, 1);
        var monthCount = (end.Year - start.Year) * 12 + end.Month - start.Month + 1;
        // Keep an unbounded or very long range readable by showing its months with activity.
        var showEmptyMonths = start != DateTime.MinValue && start <= end;
        var months = showEmptyMonths
            ? Enumerable.Range(0, monthCount).Select(firstMonth.AddMonths)
            : totalsByMonth.Keys.OrderBy(month => month);
        var monthlyValues = months.Select(month => (Date: month, Totals: totalsByMonth.GetValueOrDefault(month))).ToArray();

        var amounts = monthlyValues.SelectMany(month => new[] { month.Totals.Sales, month.Totals.Costs, month.Totals.Sales - month.Totals.Costs }).ToArray();
        var scale = Math.Max(1m, amounts.Select(Math.Abs).DefaultIfEmpty().Max());
        IncomeChartNegativeHeight = (double)(-Math.Min(0m, amounts.DefaultIfEmpty().Min()) / scale * 100m);
        IncomeChartStatusMessage = start > end ? "El mes inicial debe ser anterior o igual al mes final."
            : totalsByMonth.Count == 0 ? "Sin ventas en el período seleccionado."
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
            FormatMoney(amount));

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


    private decimal GetCashAsOf(DateTime cutoff)
    {
        var session = _store.CashSessions.OrderBy(item => item.OpenedAt).FirstOrDefault();
        if (session is null) return 0m;
        var priorSales = _store.Sales.Where(sale => sale.PaymentType == SalePaymentType.Cash && sale.Date < session.OpenedAt).Sum(sale => sale.Total);
        var priorPayments = _store.Customers.SelectMany(customer => customer.CreditHistory)
            .Where(movement => movement.Type == CreditMovementType.Payment && movement.Date < session.OpenedAt).Sum(movement => movement.Amount);
        var openingCash = session.OpeningAmount - priorSales - priorPayments;
        var latestClose = _store.CashSessions.Where(item => item.ClosedAt is DateTime closed && closed.Date <= cutoff.Date)
            .OrderByDescending(item => item.ClosedAt).FirstOrDefault();
        if (latestClose is not null) return latestClose.ClosingAmount;
        return openingCash
            + _store.Sales.Where(sale => sale.PaymentType == SalePaymentType.Cash && sale.Date.Date <= cutoff.Date).Sum(sale => sale.Total)
            + _store.Customers.SelectMany(customer => customer.CreditHistory)
                .Where(movement => movement.Type == CreditMovementType.Payment && movement.Date.Date <= cutoff.Date).Sum(movement => movement.Amount);
    }

    private decimal GetInventoryValueAsOf(DateTime cutoff)
    {
        var layersByProduct = new Dictionary<string, List<InventoryCostLayer>>(StringComparer.Ordinal);
        var timeline = _store.Purchases.Where(purchase => purchase.Date.Date <= cutoff.Date)
            .Select(purchase => (purchase.Date, Purchase: (PurchaseRecord?)purchase, Sale: (SaleRecord?)null, IsPurchase: true))
            .Concat(_store.Sales.Where(sale => sale.Date.Date <= cutoff.Date)
                .Select(sale => (sale.Date, Purchase: (PurchaseRecord?)null, Sale: (SaleRecord?)sale, IsPurchase: false)))
            .OrderBy(item => item.Date).ThenBy(item => item.IsPurchase ? 0 : 1);

        foreach (var item in timeline)
        {
            if (item.IsPurchase && item.Purchase is { } purchase)
            {
                foreach (var line in purchase.Lines)
                {
                    if (!layersByProduct.TryGetValue(line.Product.Id, out var layers)) layersByProduct[line.Product.Id] = layers = [];
                    layers.Add(new InventoryCostLayer($"L-{purchase.Id:0000}", line.Quantity, line.UnitCost));
                }
            }
            else if (item.Sale is { } sale)
            {
                foreach (var line in sale.Lines)
                {
                    if (!layersByProduct.TryGetValue(line.Product.Id, out var layers)) continue;
                    var remaining = line.Quantity;
                    foreach (var layer in layers)
                    {
                        var consumed = Math.Min(layer.RemainingQuantity, remaining);
                        layer.RemainingQuantity -= consumed;
                        remaining -= consumed;
                        if (remaining == 0) break;
                    }
                }
            }
        }

        return layersByProduct.Values.SelectMany(layers => layers).Sum(layer => layer.RemainingQuantity * layer.UnitCost);
    }

    private void RefreshManagementIndicators(IReadOnlyDictionary<int, decimal> saleCosts)
    {
        var currentMonth = FirstDayOfMonth(DateTime.Today);
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
                var profit = revenue - cost; // The demo has no recorded operating expenses, interest or taxes.

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
            Func<decimal, string> interpret, decimal? threshold = null) =>
            BuildIndicator(name, unit, labels, values.ToArray(), interpret, threshold);

        var indicators = new[]
        {
            Indicator("Margen bruto", "%", months.Select(month => month.GrossMargin),
                value => $"Por cada L 100 vendidos quedan {FormatMoney(value)} después de cubrir la mercadería."),
            Indicator("Margen neto", "%", months.Select(month => month.NetMargin),
                value => $"Por cada L 100 vendidos, el resultado neto de la demo es {FormatMoney(value)}."),
            Indicator("Liquidez corriente", "veces", months.Select(month => month.Liquidity),
                value => $"Por cada L 1 de pasivo corriente hay {FormatMoney(value)} en activos corrientes.", 1.5m),
            Indicator("Prueba ácida", "veces", months.Select(month => month.QuickRatio),
                value => $"Por cada L 1 de pasivo corriente hay {FormatMoney(value)} en caja y cuentas por cobrar.", 1.0m),
            Indicator("Endeudamiento", "%", months.Select(month => month.DebtRatio),
                value => $"De cada L 100 en activos, {FormatMoney(value)} están financiados con pasivos.", 60m),
            Indicator("Rentabilidad del patrimonio", "%", months.Select(month => month.ReturnOnEquity),
                value => $"Por cada L 100 de patrimonio al corte, la utilidad del mes fue {FormatMoney(value)}.")
        };


        if (ManagementIndicators.Count == 0)
        {
            foreach (var indicator in indicators) ManagementIndicators.Add(indicator);
        }
        else
        {
            for (var index = 0; index < indicators.Length; index++)
                ManagementIndicators[index] = indicators[index];
        }
    }

    private static decimal? Ratio(decimal numerator, decimal denominator, decimal multiplier = 1m) =>
        denominator > 0m ? numerator / denominator * multiplier : null;

    private static AccountingIndicatorViewModel BuildIndicator(string name, string unit,
        IReadOnlyList<string> labels, IReadOnlyList<decimal?> values, Func<decimal, string> interpret, decimal? threshold)
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
            periods[index] = new(labels[index], values[index] is decimal amount ? $"{amount:0.00} {unit}" : "—");
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
            current is decimal result ? interpret(result) : "Este indicador no se puede calcular para el mes actual porque falta su base de cálculo.",
            $"{top:0.##}", $"{(top + bottom) / 2m:0.##}", $"{bottom:0.##}", line, dots, thresholdLine, thresholdLabel, periods);
    }


    private static bool IsInPeriod(DateTime date, DateTime start, DateTime end) => date.Date >= start.Date && date.Date <= end.Date;
    private static bool IsCreditNormal(string account) => account is "Proveedores" or "Ingresos por ventas";

    private bool FilterJournalEntry(object item)
    {
        if (item is not JournalEntryViewModel entry) return false;
        if (JournalStartDate is DateTime start && entry.Date.Date < start.Date) return false;
        if (JournalEndDate is DateTime end && entry.Date.Date > end.Date) return false;

        var query = JournalSearchText.Trim();
        return query.Length == 0
            || entry.Reference.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || entry.Description.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || entry.DateText.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    private bool FilterLedgerEntry(object item)
    {
        if (item is not LedgerEntryViewModel entry || entry.Account != SelectedLedgerAccount) return false;
        if (LedgerStartDate is DateTime start && entry.Date.Date < start.Date) return false;
        if (LedgerEndDate is DateTime end && entry.Date.Date > end.Date) return false;

        var query = LedgerSearchText.Trim();
        return query.Length == 0
            || entry.Reference.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || entry.Description.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || entry.DateText.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

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
        OnPropertyChanged(nameof(KardexClosingStock));
        OnPropertyChanged(nameof(KardexClosingValue));
    }

    private KardexMovementViewModel? GetKardexClosingMovement() => _allKardexMovements
        .Where(movement => KardexEndDate is not DateTime end || movement.Date.Date <= end.Date)
        .OrderBy(movement => movement.Date)
        .LastOrDefault();

    private void RefreshJournal()
    {
        JournalEntries.Refresh();
    }

    private void RefreshLedger()
    {
        LedgerEntries.Refresh();
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

    private sealed record AccountingPosting(DateTime Date, string Reference, string Description, string Account, decimal Debit, decimal Credit);

    private sealed class InventoryCostLayer(string lot, int remainingQuantity, decimal unitCost)
    {
        public string Lot { get; } = lot;
        public int RemainingQuantity { get; set; } = remainingQuantity;
        public decimal UnitCost { get; } = unitCost;
    }
}

public sealed record AccountingMetricViewModel(string Label, string Value, string Detail);

public sealed record AccountingIndicatorViewModel(
    string Name, string Value, string Unit, string Explanation,
    string AxisTop, string AxisMiddle, string AxisBottom,
    PathGeometry Line, GeometryGroup Dots, LineGeometry? ThresholdLine, string ThresholdLabel, IReadOnlyList<IndicatorMonth> Months);

public sealed record IndicatorMonth(string Label, string Value);

public sealed record JournalEntryViewModel(DateTime Date, string Reference, string Description, string Account, decimal Debit, decimal Credit)
{
    public string DateText => Date.ToString("dd/MM/yyyy");
    public string DebitText => Debit == 0m ? "—" : OperationalDemoStore.FormatMoney(Debit);
    public string CreditText => Credit == 0m ? "—" : OperationalDemoStore.FormatMoney(Credit);
}

public sealed record LedgerEntryViewModel(string Account, DateTime Date, string Reference, string Description, decimal Debit, decimal Credit, decimal Balance)
{
    public string DateText => Date.ToString("dd/MM/yyyy");
    public string DebitText => Debit == 0m ? "—" : OperationalDemoStore.FormatMoney(Debit);
    public string CreditText => Credit == 0m ? "—" : OperationalDemoStore.FormatMoney(Credit);
    public string BalanceText => OperationalDemoStore.FormatMoney(Balance);
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
    string BalanceTotal,
    int TotalStockQuantity,
    decimal TotalStockCost)
{
    public string DateText => Date.ToString("dd/MM/yyyy");
}

public sealed record StatementLineViewModel(string Group, string Account, string Amount, bool IsTotal, bool IsSectionHeader = false);

public sealed record IncomeChartBar(double AboveZero, double BelowZero, string Amount);
public sealed record IncomeChartMonth(string Label, IncomeChartBar Sales, IncomeChartBar Costs, IncomeChartBar NetIncome);