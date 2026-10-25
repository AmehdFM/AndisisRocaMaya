using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.Models;

public enum CustomerType
{
    Person,
    Company
}

public enum SalePaymentType
{
    Cash,
    Credit
}

public enum CreditMovementType
{
    Charge,
    Payment
}

public sealed class CustomerRecord : ObservableObject
{
    private decimal _balance;

    public int Id { get; }
    public string Name { get; }
    public string Phone { get; }
    public CustomerType Type { get; }
    public string TaxOrIdentity { get; }
    public string DisplayText => $"{Name} · {Phone}";
    public string TypeLabel => Type == CustomerType.Person ? "Persona" : "Empresa";
    public string IdentificationLabel => Type == CustomerType.Person ? "Identidad" : "RTN";
    public decimal Balance
    {
        get => _balance;
        private set => SetProperty(ref _balance, value);
    }
    public string BalanceText => OperationalDemoStore.FormatMoney(Balance);
    public ObservableCollection<CreditMovementRecord> CreditHistory { get; } = [];

    public CustomerRecord(int id, string name, string phone, CustomerType type, string taxOrIdentity)
    {
        Id = id;
        Name = name;
        Phone = phone;
        Type = type;
        TaxOrIdentity = taxOrIdentity;
    }

    internal void AddCredit(decimal amount, string description, DateTime date)
    {
        Balance += amount;
        OnPropertyChanged(nameof(BalanceText));
        CreditHistory.Insert(0, new CreditMovementRecord(date, CreditMovementType.Charge, description, amount, Balance));
    }

    internal void AddPayment(decimal amount, string description, DateTime date)
    {
        Balance = Math.Max(0, Balance - amount);
        OnPropertyChanged(nameof(BalanceText));
        CreditHistory.Insert(0, new CreditMovementRecord(date, CreditMovementType.Payment, description, amount, Balance));
    }
}

public sealed record CreditMovementRecord(
    DateTime Date,
    CreditMovementType Type,
    string Description,
    decimal Amount,
    decimal BalanceAfter)
{
    public string TypeLabel => Type == CreditMovementType.Charge ? "Cargo" : "Abono";
    public string AmountText => OperationalDemoStore.FormatMoney(Amount);
    public string BalanceText => OperationalDemoStore.FormatMoney(BalanceAfter);
}

public sealed class ProductRecord : ObservableObject
{
    private int _stock;

    public string Id { get; }
    public string Name { get; }
    public decimal SalePrice { get; }
    public int Stock
    {
        get => _stock;
        internal set
        {
            if (SetProperty(ref _stock, value))
            {
                OnPropertyChanged(nameof(StockText));
            }
        }
    }
    public string StockText => Stock.ToString();
    public string SalePriceText => OperationalDemoStore.FormatMoney(SalePrice);
    public ObservableCollection<InventoryBatchRecord> Batches { get; } = [];
    public ObservableCollection<InventoryMovementRecord> History { get; } = [];

    public ProductRecord(string id, string name, decimal salePrice)
    {
        Id = id;
        Name = name;
        SalePrice = salePrice;
    }
}

public sealed class InventoryBatchRecord(
    int id,
    int quantity,
    DateTime purchaseDate,
    DateTime? expirationDate,
    decimal unitCost,
    string supplierName)
{
    public int Id { get; } = id;
    public int Quantity { get; internal set; } = quantity;
    public DateTime PurchaseDate { get; } = purchaseDate;
    public DateTime? ExpirationDate { get; } = expirationDate;
    public decimal UnitCost { get; } = unitCost;
    public string SupplierName { get; } = supplierName;
}

public sealed record InventoryMovementRecord(
    DateTime Date,
    string Movement,
    int Quantity,
    string Reference,
    DateTime? ExpirationDate)
{
    public string DateText => Date.ToString("dd/MM/yyyy");
    public string QuantityText => Quantity > 0 ? $"+{Quantity}" : Quantity.ToString();
    public string ExpirationText => ExpirationDate?.ToString("dd/MM/yyyy") ?? "Sin fecha";
}

public sealed class SupplierRecord(int id, string name, string phone, string contact)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public string Phone { get; } = phone;
    public string Contact { get; } = contact;
    public ObservableCollection<PurchaseRecord> Purchases { get; } = [];
}

public sealed class PurchaseRecord(
    int id,
    DateTime date,
    SupplierRecord supplier,
    IReadOnlyList<PurchaseLineRecord> lines)
{
    public int Id { get; } = id;
    public DateTime Date { get; } = date;
    public SupplierRecord Supplier { get; } = supplier;
    public IReadOnlyList<PurchaseLineRecord> Lines { get; } = lines;
    public decimal Total => Lines.Sum(line => line.Quantity * line.UnitCost);
    public string Number => $"C-{Id:0000}";
    public string DateText => Date.ToString("dd/MM/yyyy");
    public string TotalText => OperationalDemoStore.FormatMoney(Total);
}

public sealed record PurchaseLineRecord(
    ProductRecord Product,
    int Quantity,
    decimal UnitCost,
    DateTime? ExpirationDate)
{
    public string ProductName => Product.Name;
    public string ProductId => Product.Id;
    public string UnitCostText => OperationalDemoStore.FormatMoney(UnitCost);
}

public sealed record SaleLineRecord(ProductRecord Product, int Quantity, decimal UnitPrice)
{
    public string ProductName => Product.Name;
    public string ProductId => Product.Id;
    public string LineTotalText => OperationalDemoStore.FormatMoney(Quantity * UnitPrice);
}

public sealed class SaleRecord(
    int id,
    DateTime date,
    SalePaymentType paymentType,
    CustomerRecord? customer,
    IReadOnlyList<SaleLineRecord> lines)
{
    public int Id { get; } = id;
    public DateTime Date { get; } = date;
    public SalePaymentType PaymentType { get; } = paymentType;
    public CustomerRecord? Customer { get; } = customer;
    public IReadOnlyList<SaleLineRecord> Lines { get; } = lines;
    public decimal Total => Lines.Sum(line => line.Quantity * line.UnitPrice);
    public string Ticket => $"V-{Id:0000}";
    public string DateText => Date.ToString("dd/MM/yyyy HH:mm");
    public string PaymentText => PaymentType == SalePaymentType.Cash ? "Contado" : "Crédito";
    public string CustomerName => Customer?.Name ?? "Venta de mostrador";
    public string TotalText => OperationalDemoStore.FormatMoney(Total);
}

public sealed record CashDenominationCount(decimal Denomination, int Quantity)
{
    public decimal Subtotal => Denomination * Quantity;
    public string DenominationText => $"L {Denomination:0.00}";
    public string SubtotalText => OperationalDemoStore.FormatMoney(Subtotal);
}

public sealed class CashSessionRecord(
    int id,
    DateTime openedAt,
    IReadOnlyList<CashDenominationCount> openingCount) : ObservableObject
{
    private DateTime? _closedAt;
    private IReadOnlyList<CashDenominationCount> _closingCount = [];
    private decimal _cashSales;

    public int Id { get; } = id;
    public DateTime OpenedAt { get; } = openedAt;
    public DateTime? ClosedAt
    {
        get => _closedAt;
        internal set
        {
            if (SetProperty(ref _closedAt, value))
            {
                OnPropertyChanged(nameof(ClosedText));
            }
        }
    }
    public IReadOnlyList<CashDenominationCount> OpeningCount { get; } = openingCount;
    public IReadOnlyList<CashDenominationCount> ClosingCount
    {
        get => _closingCount;
        internal set
        {
            if (SetProperty(ref _closingCount, value))
            {
                OnPropertyChanged(nameof(ClosingAmount));
                OnPropertyChanged(nameof(ClosingAmountText));
                OnPropertyChanged(nameof(Difference));
                OnPropertyChanged(nameof(DifferenceText));
            }
        }
    }
    public decimal OpeningAmount => OpeningCount.Sum(row => row.Subtotal);
    public decimal ClosingAmount => ClosingCount.Sum(row => row.Subtotal);
    public decimal CashSales
    {
        get => _cashSales;
        internal set
        {
            if (SetProperty(ref _cashSales, value))
            {
                OnPropertyChanged(nameof(ExpectedAmount));
                OnPropertyChanged(nameof(ExpectedAmountText));
                OnPropertyChanged(nameof(Difference));
                OnPropertyChanged(nameof(DifferenceText));
            }
        }
    }
    public decimal ExpectedAmount => OpeningAmount + CashSales;
    public decimal Difference => ClosingAmount - ExpectedAmount;
    public string OpeningAmountText => OperationalDemoStore.FormatMoney(OpeningAmount);
    public string ClosingAmountText => OperationalDemoStore.FormatMoney(ClosingAmount);
    public string ExpectedAmountText => OperationalDemoStore.FormatMoney(ExpectedAmount);
    public string DifferenceText => OperationalDemoStore.FormatMoney(Difference);
    public string OpenedText => OpenedAt.ToString("dd/MM/yyyy HH:mm");
    public string ClosedText => ClosedAt?.ToString("dd/MM/yyyy HH:mm") ?? "Abierta";
}
