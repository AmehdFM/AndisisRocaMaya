using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
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

    public ObservableCollection<SupplierRecord> Suppliers => _store.Suppliers;
    public ObservableCollection<PurchaseEntryViewModel> ProductRows { get; } = [];
    public ObservableCollection<PurchaseRecord> Purchases => _store.Purchases;
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
        foreach (var product in _store.Products)
        {
            AddRow(product);
        }
        _store.Products.CollectionChanged += Products_CollectionChanged;
        RegisterPurchaseCommand = new RelayCommand(RegisterPurchase, CanRegisterPurchase);
        SelectedSupplier = Suppliers.FirstOrDefault();
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
        && decimal.TryParse(UnitCostText, NumberStyles.Number, CultureInfo.CurrentCulture, out var cost)
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
            || !decimal.TryParse(UnitCostText, NumberStyles.Number, CultureInfo.CurrentCulture, out var cost)
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
