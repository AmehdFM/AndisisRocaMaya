using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.ViewModels;

public sealed class PurchasesViewModel : ObservableObject
{
    private readonly OperationalDemoStore _store = OperationalDemoStore.Instance;
    private SupplierRecord? _selectedSupplier;
    private DateTime? _purchaseDate = DateTime.Today;
    private string _statusMessage = "";
    private string _productSearchText = "";
    private string _historySearchText = "";
    private string _selectedReportSupplier = "Todos los proveedores";
    private string _reportProductSearchText = "";
    private DateTime? _reportStartDate = DateTime.Today.AddDays(-180);
    private DateTime? _reportEndDate = DateTime.Today;
    private string _purchaseReportActionMessage = "Consulta de demostración · compara cada costo con la compra anterior del mismo producto y proveedor.";
    private readonly ObservableCollection<PurchaseCostHistoryRowViewModel> _allPurchaseCostRows = [];

    public ObservableCollection<SupplierRecord> Suppliers => _store.Suppliers;
    public ObservableCollection<PurchaseEntryViewModel> ProductRows { get; } = [];
    public ObservableCollection<PurchaseRecord> Purchases => _store.Purchases;
    public ICollectionView ProductRowsView { get; }
    public ICollectionView PurchasesView { get; }
    public ListCollectionView PurchaseCostRows { get; }
    public ObservableCollection<string> ReportSupplierNames { get; } = ["Todos los proveedores"];
    public string PurchaseReportActionMessage
    {
        get => _purchaseReportActionMessage;
        private set => SetProperty(ref _purchaseReportActionMessage, value);
    }
    public string SelectedReportSupplier
    {
        get => _selectedReportSupplier;
        set
        {
            if (SetProperty(ref _selectedReportSupplier, value)) RefreshPurchaseCostRows();
        }
    }
    public string ReportProductSearchText
    {
        get => _reportProductSearchText;
        set
        {
            if (SetProperty(ref _reportProductSearchText, value))
            {
                PurchaseCostRows.Refresh();
                NotifyPurchaseReportTotals();
            }
        }
    }
    public DateTime? ReportStartDate
    {
        get => _reportStartDate;
        set
        {
            if (SetProperty(ref _reportStartDate, value)) RefreshPurchaseCostRows();
        }
    }
    public DateTime? ReportEndDate
    {
        get => _reportEndDate;
        set
        {
            if (SetProperty(ref _reportEndDate, value)) RefreshPurchaseCostRows();
        }
    }
    public string PurchaseReportTotalText => OperationalDemoStore.FormatMoney(PurchaseCostRows.Cast<PurchaseCostHistoryRowViewModel>().Sum(row => row.LineTotal));
    public string PurchaseReportPurchaseCountText => PurchaseCostRows.Cast<PurchaseCostHistoryRowViewModel>().Select(row => row.PurchaseNumber).Distinct().Count().ToString();
    public string PurchaseReportLineCountText => PurchaseCostRows.Cast<PurchaseCostHistoryRowViewModel>().Count().ToString();
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
        PurchaseCostRows = new ListCollectionView(_allPurchaseCostRows);
        PurchaseCostRows.Filter = FilterPurchaseCostRow;
        ExportPurchaseReportCommand = new RelayCommand(() => PurchaseReportActionMessage = "Demostración: la exportación a PDF no genera un archivo en este prototipo.");
        PrintPurchaseReportCommand = new RelayCommand(() => PurchaseReportActionMessage = "Demostración: la impresión no se envía a una impresora en este prototipo.");
        _store.Purchases.CollectionChanged += Purchases_CollectionChanged;
        _store.Suppliers.CollectionChanged += Suppliers_CollectionChanged;
        foreach (var supplier in Suppliers) ReportSupplierNames.Add(supplier.Name);
        foreach (var product in _store.Products)
        {
            AddRow(product);
        }
        _store.Products.CollectionChanged += Products_CollectionChanged;
        RegisterPurchaseCommand = new RelayCommand(RegisterPurchase, CanRegisterPurchase);
        SelectedSupplier = Suppliers.FirstOrDefault();
        RefreshPurchaseCostRows();
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

    private bool FilterPurchaseCostRow(object item) => item is PurchaseCostHistoryRowViewModel row
        && (string.IsNullOrWhiteSpace(ReportProductSearchText)
            || row.ProductName.Contains(ReportProductSearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || row.ProductId.Contains(ReportProductSearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));

    private void RefreshPurchaseCostRows()
    {
        _allPurchaseCostRows.Clear();
        var previousUnitCosts = new Dictionary<(int SupplierId, string ProductId), decimal>();
        foreach (var purchase in Purchases.OrderBy(purchase => purchase.Date).ThenBy(purchase => purchase.Id))
        {
            foreach (var line in purchase.Lines)
            {
                var key = (purchase.Supplier.Id, line.Product.Id);
                var hasPrevious = previousUnitCosts.TryGetValue(key, out var previousCost);
                var inRange = (ReportStartDate is not DateTime start || purchase.Date.Date >= start.Date)
                    && (ReportEndDate is not DateTime end || purchase.Date.Date <= end.Date);
                var matchesSupplier = SelectedReportSupplier == "Todos los proveedores"
                    || purchase.Supplier.Name == SelectedReportSupplier;

                if (inRange && matchesSupplier)
                {
                    _allPurchaseCostRows.Insert(0, new PurchaseCostHistoryRowViewModel(
                        purchase.Date,
                        purchase.Number,
                        purchase.Supplier.Name,
                        line.Product.Name,
                        line.Product.Id,
                        line.Quantity,
                        line.UnitCost,
                        previousCost,
                        hasPrevious));
                }

                previousUnitCosts[key] = line.UnitCost;
            }
        }

        PurchaseCostRows.Refresh();
        NotifyPurchaseReportTotals();
    }

    private void NotifyPurchaseReportTotals()
    {
        OnPropertyChanged(nameof(PurchaseReportTotalText));
        OnPropertyChanged(nameof(PurchaseReportPurchaseCountText));
        OnPropertyChanged(nameof(PurchaseReportLineCountText));
    }

    private void Purchases_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        PurchasesView.Refresh();
        RefreshPurchaseCostRows();
    }

    private void Suppliers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is null) return;
        foreach (SupplierRecord supplier in e.NewItems)
        {
            if (!ReportSupplierNames.Contains(supplier.Name)) ReportSupplierNames.Add(supplier.Name);
        }
    }

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
            _store.RecordPurchase(SelectedSupplier, PurchaseDate.Value, inputs);
            foreach (var row in ProductRows)
            {
                row.Reset();
            }
            StatusMessage = "Compra registrada e inventario actualizado.";
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
