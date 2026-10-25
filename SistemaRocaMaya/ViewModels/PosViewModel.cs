using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SistemaRocaMaya.ViewModels;

public sealed class PosViewModel : ObservableObject
{
    public ObservableCollection<PosProductViewModel> Products { get; } =
    [
        new("Coca-Cola 600 ml", "Bebidas", 22.00m, "CO"),
        new("Arroz 1 libra", "Abarrotes", 23.50m, "AR"),
        new("Frijoles 1 libra", "Abarrotes", 34.00m, "FR"),
        new("Leche entera 946 ml", "Lácteos", 31.00m, "LE"),
        new("Café molido 400 g", "Abarrotes", 76.00m, "CA"),
        new("Pan blanco", "Panadería", 14.00m, "PA")
    ];

    public ObservableCollection<CartItemViewModel> CartItems { get; } = [];

    public ObservableCollection<string> PaymentMethods { get; } = ["Contado", "Crédito"];

    public ObservableCollection<PosCustomerViewModel> Customers { get; } =
    [
        new("María López", "9999-1234"),
        new("Carlos Méndez", "9888-4321")
    ];

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

    private PosCustomerViewModel? _selectedCustomer;
    public PosCustomerViewModel? SelectedCustomer
    {
        get => _selectedCustomer;
        set => SetProperty(ref _selectedCustomer, value);
    }

    public System.Windows.Visibility CreditDetailsVisibility => SelectedPaymentMethod == "Crédito"
        ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

    public string TotalText => FormatMoney(CartItems.Sum(item => item.Quantity * item.UnitPrice));

    public decimal TotalAmount => CartItems.Sum(item => item.Quantity * item.UnitPrice);

    public IRelayCommand<PosProductViewModel?> AddProductCommand { get; }

    public IRelayCommand<CartItemViewModel?> RemoveCartItemCommand { get; }

    public IRelayCommand ClearCartCommand { get; }
    public PosViewModel()
    {
        AddProductCommand = new RelayCommand<PosProductViewModel?>(AddProduct);
        RemoveCartItemCommand = new RelayCommand<CartItemViewModel?>(RemoveCartItem);
        ClearCartCommand = new RelayCommand(ClearCart);
        SelectedCustomer = Customers[0];

        // Initial lines are sample data so the prototype shows the intended cart layout.
        AddProduct(Products[0]);
        AddProduct(Products[5]);
        AddProduct(Products[5]);
    }

    private void AddProduct(PosProductViewModel? product)
    {
        if (product is null)
        {
            return;
        }

        var cartItem = CartItems.FirstOrDefault(item => item.Name == product.Name);
        if (cartItem is null)
        {
            CartItems.Add(new CartItemViewModel(product.Name, product.Price, product.Quantity));
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
    public string Name { get; }

    public string Category { get; }

    public decimal Price { get; }

    public string PriceText => PosViewModel.FormatMoney(Price);

    public string Icon { get; }

    private int _quantity = 1;
    public int Quantity
    {
        get => _quantity;
        set { if (value > 0) SetProperty(ref _quantity, value); }
    }

    public PosProductViewModel(string name, string category, decimal price, string icon)
    {
        Name = name;
        Category = category;
        Price = price;
        Icon = icon;
    }
}

public sealed class PosCustomerViewModel(string name, string phone)
{
    public string Name { get; } = name;
    public string Phone { get; } = phone;
    public string DisplayText => $"{Name} · {Phone}";
}

public sealed class CartItemViewModel : ObservableObject
{
    private int _quantity = 1;

    public string Name { get; }

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

    public CartItemViewModel(string name, decimal unitPrice, int quantity = 1)
    {
        Name = name;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}
