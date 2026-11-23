using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;
using SistemaRocaMaya.Services.Reports;

namespace SistemaRocaMaya.ViewModels;

public sealed class PurchasesViewModel : ObservableObject
{
    private readonly OperationalDemoStore _store = OperationalDemoStore.Instance;
    private SupplierRecord? _selectedSupplier;
    private DateTime? _purchaseDate = DateTime.Today;
    private string _statusMessage = "";
    private string _productSearchText = "";
    private string _historySearchText = "";
    private SupplierRecord? _selectedReportSupplier;
    private ProductRecord? _selectedReportProduct;
    private DateTime? _reportStartDate = DateTime.Today.AddDays(-180);
    private DateTime? _reportEndDate = DateTime.Today;
    private string _purchaseReportActionMessage = "";
    private readonly ObservableCollection<PurchaseCostHistoryRowViewModel> _allPurchaseCostRows = [];

    public ObservableCollection<SupplierRecord> Suppliers => _store.Suppliers;
    public ObservableCollection<PurchaseEntryViewModel> ProductRows { get; } = [];
    public ObservableCollection<PurchaseRecord> Purchases => _store.Purchases;
    public ICollectionView ProductRowsView { get; }
    public ICollectionView PurchasesView { get; }
    public ListCollectionView PurchaseCostRows { get; }
    public IReadOnlyList<SupplierRecord> ReportSuppliers => Suppliers.OrderBy(supplier => supplier.Name).ToArray();
    public IReadOnlyList<ProductRecord> ReportProducts => Purchases
        .Where(purchase => purchase.Supplier.Id == SelectedReportSupplier?.Id)
        .SelectMany(purchase => purchase.Lines).Select(line => line.Product)
        .DistinctBy(product => product.Id).OrderBy(product => product.Name).ToArray();
    public bool CanSelectReportProduct => SelectedReportSupplier is not null;
    public bool HasPurchaseReport => _allPurchaseCostRows.Count > 0;
    public decimal PurchaseChartMaximum => Math.Max(1m, _allPurchaseCostRows.Select(row => row.UnitCost).DefaultIfEmpty().Max());
    public string PurchaseReportActionMessage
    {
        get => _purchaseReportActionMessage;
        private set => SetProperty(ref _purchaseReportActionMessage, value);
    }
    public SupplierRecord? SelectedReportSupplier
    {
        get => _selectedReportSupplier;
        set
        {
            if (!SetProperty(ref _selectedReportSupplier, value)) return;
            SelectedReportProduct = null;
            OnPropertyChanged(nameof(ReportProducts));
            OnPropertyChanged(nameof(CanSelectReportProduct));
            ClearPurchaseReport();
        }
    }
    public ProductRecord? SelectedReportProduct
    {
        get => _selectedReportProduct;
        set
        {
            if (SetProperty(ref _selectedReportProduct, value)) ClearPurchaseReport();
        }
    }
    public DateTime? ReportStartDate
    {
        get => _reportStartDate;
        set
        {
            if (SetProperty(ref _reportStartDate, value)) ClearPurchaseReport();
        }
    }
    public DateTime? ReportEndDate
    {
        get => _reportEndDate;
        set
        {
            if (SetProperty(ref _reportEndDate, value)) ClearPurchaseReport();
        }
    }
    public string PurchaseReportTotalText => OperationalDemoStore.FormatMoney(PurchaseCostRows.Cast<PurchaseCostHistoryRowViewModel>().Sum(row => row.LineTotal));
    public string PurchaseReportPurchaseCountText => PurchaseCostRows.Cast<PurchaseCostHistoryRowViewModel>().Select(row => row.PurchaseNumber).Distinct().Count().ToString();
    public string PurchaseReportLineCountText => PurchaseCostRows.Cast<PurchaseCostHistoryRowViewModel>().Count().ToString();
    public IRelayCommand ShowPurchaseReportCommand { get; }
    public IRelayCommand ExportPurchaseReportCommand { get; }
    public IRelayCommand PrintPurchaseReportCommand { get; }
    public string ProductSearchText
    {
        get => _productSearchText;
        set
        {
            if (SetProperty(ref _productSearchText, value)) ProductRowsView.Refresh();
        }
    }
    public string HistorySearchText
    {
        get => _historySearchText;
        set
        {
            if (SetProperty(ref _historySearchText, value)) PurchasesView.Refresh();
        }
    }
    public SupplierRecord? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (SetProperty(ref _selectedSupplier, value))
            {
                RegisterPurchaseCommand.NotifyCanExecuteChanged();
            }
        }
    }
    private string _supplierInvoice = "";
    public string SupplierInvoice
    {
        get => _supplierInvoice;
        set => SetProperty(ref _supplierInvoice, value);
    }

    public DateTime? PurchaseDate
    {
        get => _purchaseDate;
        set
        {
            if (SetProperty(ref _purchaseDate, value))
            {
                RegisterPurchaseCommand.NotifyCanExecuteChanged();
            }
        }
    }
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }
    public string PurchaseTotalText => OperationalDemoStore.FormatMoney(ProductRows.Sum(row => row.LineTotal));
    public IRelayCommand RegisterPurchaseCommand { get; }

    public PurchasesViewModel()
    {
        ProductRowsView = CollectionViewSource.GetDefaultView(ProductRows);
        ProductRowsView.Filter = FilterProductRow;
        PurchasesView = CollectionViewSource.GetDefaultView(Purchases);
        PurchasesView.Filter = FilterPurchase;
        PurchasesView.SortDescriptions.Add(new(nameof(PurchaseRecord.Date), ListSortDirection.Ascending));
        PurchaseCostRows = new ListCollectionView(_allPurchaseCostRows);
        ShowPurchaseReportCommand = new RelayCommand(RefreshPurchaseCostRows,
            () => SelectedReportSupplier is not null && SelectedReportProduct is not null);
        ExportPurchaseReportCommand = new RelayCommand(() => PdfExportService.Export(CreatePurchaseReport, "Compras_por_proveedor.pdf", message => PurchaseReportActionMessage = message), () => HasPurchaseReport);
        PrintPurchaseReportCommand = new RelayCommand(() => ReportPrintService.Print(CreatePurchaseReport, message => PurchaseReportActionMessage = message), () => HasPurchaseReport);
        _store.Purchases.CollectionChanged += Purchases_CollectionChanged;
        _store.Suppliers.CollectionChanged += Suppliers_CollectionChanged;
        foreach (var product in _store.Products)
        {
            AddRow(product);
        }
        _store.Products.CollectionChanged += Products_CollectionChanged;
        RegisterPurchaseCommand = new RelayCommand(RegisterPurchase, CanRegisterPurchase);
        SelectedSupplier = Suppliers.FirstOrDefault();
    }

    private bool _showSupplierDetails;

    public bool ShowSupplierDetails
    {
        get => _showSupplierDetails;
        set => SetProperty(ref _showSupplierDetails, value);
    }

    public TableReportDocument CreatePurchaseReport()
    {
        var detailed = ShowSupplierDetails;
        ReportColumn[] columns = detailed
            ? [new("Fecha"), new("Compra", 0.8f), new("Proveedor", 1.4f), new("Factura", 1.15f), new("Producto", 1.4f), new("Cantidad", 0.7f, true),
                new("Costo unitario", 1, true), new("Costo anterior", 1, true), new("Variación L", 1, true),
                new("Variación %", 0.9f, true), new("Total", 1, true)]
            : [new("Fecha"), new("Producto", 2), new("Cantidad", 1, true), new("Costo", 1, true), new("Total", 1, true)];
        var rows = PurchaseCostRows.Cast<PurchaseCostHistoryRowViewModel>().Select(row => new ReportRow(detailed
            ? [row.DateText, row.PurchaseNumber, row.SupplierName, row.SupplierInvoice, row.ProductName, row.QuantityText,
                row.UnitCostText, row.PreviousUnitCostText, row.UnitCostDifferenceText, row.UnitCostVariationText, row.LineTotalText]
            : [row.DateText, row.ProductName, row.QuantityText, row.UnitCostText, row.LineTotalText])).ToArray();
        return new("Historial de compras por proveedor",
            $"{SelectedReportSupplier?.Name} · {ReportStartDate:dd/MM/yyyy} – {ReportEndDate:dd/MM/yyyy} · {SelectedReportProduct?.Name}",
            columns, rows, landscape: true, chart: new ReportChart("Costo unitario por compra",
                _allPurchaseCostRows.Select(row => row.DateText).ToArray(),
                [new("Costo unitario", _allPurchaseCostRows.Select(row => row.UnitCost).ToArray())], "Fecha de compra"),
            summary: new("Totales de compras",
                ["Compras totales", "Productos totales", "Valor total"],
                [new([PurchaseReportPurchaseCountText, PurchaseReportLineCountText, PurchaseReportTotalText], IsTotal: true)]));
    }

    private bool FilterProductRow(object item) => item is PurchaseEntryViewModel row
        && (string.IsNullOrWhiteSpace(ProductSearchText)
            || row.Name.Contains(ProductSearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || row.ProductId.Contains(ProductSearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));

    private bool FilterPurchase(object item) => item is PurchaseRecord purchase
        && (string.IsNullOrWhiteSpace(HistorySearchText)
            || purchase.Supplier.Name.Contains(HistorySearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || purchase.Number.Contains(HistorySearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || purchase.DateText.Contains(HistorySearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));

    private void ClearPurchaseReport()
    {
        _allPurchaseCostRows.Clear();
        PurchaseReportActionMessage = "";
        NotifyPurchaseReportTotals();
    }

    private void RefreshPurchaseCostRows()
    {
        ClearPurchaseReport();
        if (SelectedReportSupplier is not { } supplier || SelectedReportProduct is not { } product) return;
        if (ReportStartDate?.Date > ReportEndDate?.Date)
        {
            PurchaseReportActionMessage = "La fecha inicial debe ser anterior o igual a la fecha final.";
            return;
        }

        decimal? previousCost = null;
        foreach (var purchase in Purchases.Where(purchase => purchase.Supplier.Id == supplier.Id)
                     .OrderBy(purchase => purchase.Date).ThenBy(purchase => purchase.Id))
        {
            foreach (var line in purchase.Lines.Where(line => line.Product.Id == product.Id))
            {
                if ((ReportStartDate is not DateTime start || purchase.Date.Date >= start.Date)
                    && (ReportEndDate is not DateTime end || purchase.Date.Date <= end.Date))
                {
                    _allPurchaseCostRows.Add(new PurchaseCostHistoryRowViewModel(
                        purchase.Date, purchase.Number, supplier.Name, purchase.SupplierInvoice ?? "—",
                        product.Name, product.Id, line.Quantity, line.UnitCost, previousCost.GetValueOrDefault(),
                        previousCost.HasValue));
                }
                previousCost = line.UnitCost;
            }
        }

        NotifyPurchaseReportTotals();
    }

    private void NotifyPurchaseReportTotals()
    {
        OnPropertyChanged(nameof(HasPurchaseReport));
        OnPropertyChanged(nameof(PurchaseChartMaximum));
        ShowPurchaseReportCommand.NotifyCanExecuteChanged();
        ExportPurchaseReportCommand.NotifyCanExecuteChanged();
        PrintPurchaseReportCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(PurchaseReportTotalText));
        OnPropertyChanged(nameof(PurchaseReportPurchaseCountText));
        OnPropertyChanged(nameof(PurchaseReportLineCountText));
    }

    private void Purchases_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        PurchasesView.Refresh();
        var product = SelectedReportProduct;
        var hadResults = HasPurchaseReport;
        OnPropertyChanged(nameof(ReportProducts));
        if (product is not null && ReportProducts.Contains(product)) SelectedReportProduct = product;
        if (hadResults) RefreshPurchaseCostRows();
    }

    private void Suppliers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        OnPropertyChanged(nameof(ReportSuppliers));

    public void AddRow(ProductRecord product)
    {
        if (ProductRows.Any(row => row.Product.Id == product.Id))
        {
            return;
        }
        var row = new PurchaseEntryViewModel(product);
        row.PropertyChanged += Row_PropertyChanged;
        ProductRows.Add(row);
    }

    private bool CanRegisterPurchase() => SelectedSupplier is not null
        && PurchaseDate is not null
        && ProductRows.Any(row => row.TryBuildInput(out _));

    private void RegisterPurchase()
    {
        if (SelectedSupplier is null || PurchaseDate is null)
        {
            return;
        }

        var inputs = ProductRows
            .Select(row => row.TryBuildInput(out var input) ? input : null)
            .Where(input => input is not null)
            .Cast<PurchaseLineInput>()
            .ToArray();
        if (inputs.Length == 0)
        {
            StatusMessage = "Agrega una cantidad y costo para al menos un producto.";
            return;
        }

        try
        {
            var purchase = _store.RecordPurchase(SelectedSupplier, PurchaseDate.Value, inputs, SupplierInvoice);
            SupplierInvoice = "";
            foreach (var row in ProductRows)
            {
                row.Reset();
            }
            StatusMessage = $"Compra {purchase.Number} registrada e inventario actualizado.";
            OnPropertyChanged(nameof(PurchaseTotalText));
            RegisterPurchaseCommand.NotifyCanExecuteChanged();
        }
        catch (InvalidOperationException exception)
        {
            StatusMessage = exception.Message;
        }
    }

    private void Products_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (ProductRecord product in e.NewItems)
            {
                AddRow(product);
            }
        }
    }

    private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(PurchaseTotalText));
        RegisterPurchaseCommand.NotifyCanExecuteChanged();
    }
}

public sealed record PurchaseCostHistoryRowViewModel(
    DateTime Date,
    string PurchaseNumber,
    string SupplierName,
    string SupplierInvoice,
    string ProductName,
    string ProductId,
    int Quantity,
    decimal UnitCost,
    decimal PreviousUnitCost,
    bool HasPreviousCost)
{
    public string DateText => Date.ToString("dd/MM/yyyy");
    public string QuantityText => Quantity.ToString("N0", OperationalDemoStore.HondurasCulture);
    public decimal LineTotal => Quantity * UnitCost;
    public string UnitCostText => OperationalDemoStore.FormatMoney(UnitCost);
    public string PreviousUnitCostText => HasPreviousCost ? OperationalDemoStore.FormatMoney(PreviousUnitCost) : "Sin compra anterior";
    public decimal UnitCostDifference => UnitCost - PreviousUnitCost;
    public string UnitCostDifferenceText => !HasPreviousCost ? "—" : $"{(UnitCostDifference > 0 ? "+" : "")}{OperationalDemoStore.FormatMoney(UnitCostDifference)}";
    public decimal? UnitCostVariationPercent => !HasPreviousCost || PreviousUnitCost == 0m
        ? null
        : UnitCostDifference / PreviousUnitCost * 100m;
    public string UnitCostVariationText => UnitCostVariationPercent is decimal percent ? $"{percent:+0.00;-0.00;0.00}%" : "Sin base";
    public string LineTotalText => OperationalDemoStore.FormatMoney(LineTotal);
}

public sealed class PurchaseEntryViewModel : ObservableObject
{
    private string _quantityText = "0";
    private string _unitCostText = "0.00";
    private DateTime? _expirationDate;

    public ProductRecord Product { get; }
    public string Name => Product.Name;
    public string ProductId => Product.Id;
    public string StockText => Product.StockText;
    public string QuantityText
    {
        get => _quantityText;
        set => SetProperty(ref _quantityText, value);
    }
    public string UnitCostText
    {
        get => _unitCostText;
        set => SetProperty(ref _unitCostText, value);
    }
    public DateTime? ExpirationDate
    {
        get => _expirationDate;
        set => SetProperty(ref _expirationDate, value);
    }
    public decimal LineTotal => int.TryParse(QuantityText, out var quantity)
        && decimal.TryParse(UnitCostText, NumberStyles.Number, OperationalDemoStore.HondurasCulture, out var cost)
        ? quantity * cost
        : 0m;

    public PurchaseEntryViewModel(ProductRecord product)
    {
        Product = product;
        Product.PropertyChanged += Product_PropertyChanged;
    }

    private void Product_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductRecord.StockText))
        {
            OnPropertyChanged(nameof(StockText));
        }
    }

    public bool TryBuildInput(out PurchaseLineInput input)
    {
        input = new PurchaseLineInput(Product, 0, 0m, ExpirationDate);
        if (!int.TryParse(QuantityText, out var quantity) || quantity <= 0
            || !decimal.TryParse(UnitCostText, NumberStyles.Number, OperationalDemoStore.HondurasCulture, out var cost)
            || cost < 0)
        {
            return false;
        }

        input = new PurchaseLineInput(Product, quantity, cost, ExpirationDate);
        return true;
    }

    public void Reset()
    {
        QuantityText = "0";
        UnitCostText = "0.00";
        ExpirationDate = null;
    }
}
