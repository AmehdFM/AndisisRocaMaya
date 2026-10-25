using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.ViewModels;

public sealed class PosViewModel : ObservableObject
{
    private readonly OperationalDemoStore _store = OperationalDemoStore.Instance;
    private readonly Rol _currentRole;

    public ObservableCollection<PosProductViewModel> Products { get; } = [];

    public ObservableCollection<CartItemViewModel> CartItems { get; } = [];

    public ObservableCollection<string> PaymentMethods { get; } = ["Contado", "Crédito"];

    public ObservableCollection<CustomerRecord> Customers => _store.Customers;

    private string _selectedPaymentMethod = "Contado";
    public string SelectedPaymentMethod
    {
        get => _selectedPaymentMethod;
        set
        {
            if (SetProperty(ref _selectedPaymentMethod, value))
            {
                OnPropertyChanged(nameof(CreditDetailsVisibility));
            }
        }
    }

    private CustomerRecord? _selectedCustomer;
    public CustomerRecord? SelectedCustomer
    {
        get => _selectedCustomer;
        set => SetProperty(ref _selectedCustomer, value);
    }

    public bool IsCashOpen => _store.IsCashOpen;
    public string CashActionLabel => IsCashOpen ? "Cerrar caja" : "Abrir caja";
    public string CashStatusText => IsCashOpen
        ? $"Caja abierta · {_store.ActiveCashSession!.OpeningAmountText} inicial"
        : "Caja cerrada";

    public System.Windows.Visibility CreditDetailsVisibility => SelectedPaymentMethod == "Crédito"
        ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

    public string TotalText => FormatMoney(CartItems.Sum(item => item.Quantity * item.UnitPrice));

    public decimal TotalAmount => CartItems.Sum(item => item.Quantity * item.UnitPrice);

    public IRelayCommand<PosProductViewModel?> AddProductCommand { get; }

    public IRelayCommand<CartItemViewModel?> RemoveCartItemCommand { get; }

    public IRelayCommand ClearCartCommand { get; }
    public bool CanSellOnCredit => _currentRole.PuedeVenderCredito;
    public bool CanOpenCash => _currentRole.PuedeAbrirCaja;

    public PosViewModel(Rol currentRole)
    {
        _currentRole = currentRole;
        _currentRole.PropertyChanged += CurrentRole_PropertyChanged;
        foreach (var product in _store.Products)
        {
            Products.Add(new PosProductViewModel(product));
        }
        _store.Products.CollectionChanged += OnProductsChanged;
        _store.PropertyChanged += Store_PropertyChanged;

        AddProductCommand = new RelayCommand<PosProductViewModel?>(AddProduct);
        RemoveCartItemCommand = new RelayCommand<CartItemViewModel?>(RemoveCartItem);
        ClearCartCommand = new RelayCommand(ClearCart);
        SelectedCustomer = Customers[0];

    }

    private void CurrentRole_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Rol.PuedeVenderCredito) || e.PropertyName == nameof(Rol.PuedeAbrirCaja))
        {
            OnPropertyChanged(nameof(CanSellOnCredit));
            OnPropertyChanged(nameof(CanOpenCash));
        }
    }

    private void Store_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OperationalDemoStore.IsCashOpen))
        {
            OnPropertyChanged(nameof(IsCashOpen));
            OnPropertyChanged(nameof(CashActionLabel));
            OnPropertyChanged(nameof(CashStatusText));
        }
    }

    private void OnProductsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
        {
            foreach (ProductRecord product in e.NewItems)
            {
                Products.Add(new PosProductViewModel(product));
            }
        }
    }

    private void AddProduct(PosProductViewModel? product)
    {
        if (product is null)
        {
            return;
        }

        var cartItem = CartItems.FirstOrDefault(item => item.Product.Id == product.Product.Id);
        if (cartItem is null)
        {
            CartItems.Add(new CartItemViewModel(product.Product, product.Quantity));
        }
        else
        {
            cartItem.Quantity += product.Quantity;
        }

        product.Quantity = 1;

        OnPropertyChanged(nameof(TotalText));
        OnPropertyChanged(nameof(TotalAmount));
    }

    private void RemoveCartItem(CartItemViewModel? cartItem)
    {
        if (cartItem is not null && CartItems.Remove(cartItem))
        {
            OnPropertyChanged(nameof(TotalText));
            OnPropertyChanged(nameof(TotalAmount));
        }
    }

    private void ClearCart()
    {
        CartItems.Clear();
        OnPropertyChanged(nameof(TotalText));
        OnPropertyChanged(nameof(TotalAmount));
    }

    internal static string FormatMoney(decimal amount) => $"L {amount:0.00}";
}

public sealed class PosProductViewModel : ObservableObject
{
    public ProductRecord Product { get; }
    public string Name => Product.Name;
    public decimal Price => Product.SalePrice;
    public string PriceText => PosViewModel.FormatMoney(Price);
    public string StockText => Product.StockText;

    private int _quantity = 1;
    public int Quantity
    {
        get => _quantity;
        set { if (value > 0) SetProperty(ref _quantity, value); }
    }

    public PosProductViewModel(ProductRecord product)
    {
        Product = product;
    }
}

public sealed class CartItemViewModel : ObservableObject
{
    private int _quantity = 1;

    public string Name { get; }

    public ProductRecord Product { get; }

    public decimal UnitPrice { get; }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                OnPropertyChanged(nameof(QuantityAndPriceText));
                OnPropertyChanged(nameof(LineTotalText));
            }
        }
    }

    public string QuantityAndPriceText => $"{Quantity} × {PosViewModel.FormatMoney(UnitPrice)}";

    public string LineTotalText => PosViewModel.FormatMoney(Quantity * UnitPrice);

    public CartItemViewModel(ProductRecord product, int quantity = 1)
    {
        Product = product;
        Name = product.Name;
        UnitPrice = product.SalePrice;
        Quantity = quantity;
    }
}
