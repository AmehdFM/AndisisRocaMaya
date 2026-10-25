using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SistemaRocaMaya.Models;

namespace SistemaRocaMaya.Services;

public sealed class OperationalDemoStore : ObservableObject
{
    private static readonly decimal[] DemoDenominations = [1m, 2m, 5m, 10m, 20m, 50m, 100m, 200m, 500m];
    private int _nextCustomerId = 1;
    private int _nextProductId = 1;
    private int _nextSupplierId = 1;
    private int _nextPurchaseId = 1;
    private int _nextSaleId = 1;
    private int _nextCashSessionId = 1;
    private int _nextBatchId = 1;
    private CashSessionRecord? _activeCashSession;

    public static OperationalDemoStore Instance { get; } = new();

    public ObservableCollection<CustomerRecord> Customers { get; } = [];
    public ObservableCollection<ProductRecord> Products { get; } = [];
    public ObservableCollection<SupplierRecord> Suppliers { get; } = [];
    public ObservableCollection<PurchaseRecord> Purchases { get; } = [];
    public ObservableCollection<SaleRecord> Sales { get; } = [];
    public ObservableCollection<CashSessionRecord> CashSessions { get; } = [];

    public CashSessionRecord? ActiveCashSession
    {
        get => _activeCashSession;
        private set
        {
            if (SetProperty(ref _activeCashSession, value))
            {
                OnPropertyChanged(nameof(IsCashOpen));
            }
        }
    }

    public bool IsCashOpen => ActiveCashSession is not null;

    public string NextProductId => $"PR-{_nextProductId:000}";

    public static IReadOnlyList<decimal> CashDenominations => DemoDenominations;

    private OperationalDemoStore()
    {
        SeedDemoData();
    }

    public CustomerRecord CreateCustomer(string name, string phone, CustomerType type, string taxOrIdentity)
    {
        var customer = new CustomerRecord(_nextCustomerId++, name.Trim(), phone.Trim(), type, taxOrIdentity.Trim());
        Customers.Add(customer);
        return customer;
    }

    public ProductRecord CreateProduct(
        string name,
        decimal salePrice,
        int initialQuantity = 0,
        DateTime? purchaseDate = null,
        DateTime? expirationDate = null)
    {
        var product = new ProductRecord(NextProductId, name.Trim(), salePrice);
        _nextProductId++;
        Products.Add(product);
        if (initialQuantity > 0)
        {
            AddStock(product, initialQuantity, purchaseDate ?? DateTime.Today, expirationDate, 0m, "Inventario inicial");
        }

        return product;
    }

    public SupplierRecord CreateSupplier(string name, string phone, string contact)
    {
        var supplier = new SupplierRecord(_nextSupplierId++, name.Trim(), phone.Trim(), contact.Trim());
        Suppliers.Add(supplier);
        return supplier;
    }

    public PurchaseRecord RecordPurchase(
        SupplierRecord supplier,
        DateTime date,
        IReadOnlyList<PurchaseLineInput> inputs)
    {
        if (inputs.Count == 0)
        {
            throw new InvalidOperationException("Agrega al menos un producto a la compra.");
        }
        if (inputs.Any(input => input.Quantity <= 0 || input.UnitCost < 0))
        {
            throw new InvalidOperationException("Las cantidades deben ser mayores que cero y el costo no puede ser negativo.");
        }

        var lines = inputs.Select(input => new PurchaseLineRecord(
            input.Product,
            input.Quantity,
            input.UnitCost,
            input.ExpirationDate)).ToArray();
        var purchase = new PurchaseRecord(_nextPurchaseId++, date, supplier, lines);

        foreach (var line in lines)
        {
            AddStock(line.Product, line.Quantity, date, line.ExpirationDate, line.UnitCost, supplier.Name);
        }

        Purchases.Insert(0, purchase);
        supplier.Purchases.Insert(0, purchase);
        OnPropertyChanged(nameof(Purchases));
        return purchase;
    }

    public SaleRecord RecordSale(
        SalePaymentType paymentType,
        CustomerRecord? customer,
        IReadOnlyList<SaleLineInput> inputs,
        DateTime? date = null)
    {
        if (ActiveCashSession is null)
        {
            throw new InvalidOperationException("Abre la caja antes de registrar ventas.");
        }
        if (inputs.Count == 0)
        {
            throw new InvalidOperationException("Agrega productos a la venta.");
        }
        if (paymentType == SalePaymentType.Credit && customer is null)
        {
            throw new InvalidOperationException("Selecciona un cliente para registrar la venta a crédito.");
        }

        foreach (var input in inputs)
        {
            if (input.Quantity <= 0 || input.Product.Stock < input.Quantity)
            {
                throw new InvalidOperationException($"Existencia insuficiente para {input.Product.Name}.");
            }
        }

        var lines = inputs.Select(input => new SaleLineRecord(input.Product, input.Quantity, input.UnitPrice)).ToArray();
        var saleDate = date ?? DateTime.Now;
        var sale = new SaleRecord(_nextSaleId++, saleDate, paymentType, customer, lines);

        foreach (var line in lines)
        {
            RemoveStock(line.Product, line.Quantity, saleDate, sale.Ticket);
        }

        Sales.Insert(0, sale);
        if (paymentType == SalePaymentType.Cash)
        {
            ActiveCashSession.CashSales += sale.Total;
        }
        else
        {
            customer!.AddCredit(sale.Total, $"Venta {sale.Ticket}", saleDate);
        }

        return sale;
    }

    public CreditMovementRecord RecordCreditPayment(CustomerRecord customer, decimal amount, DateTime? date = null)
    {
        if (amount <= 0 || amount > customer.Balance)
        {
            throw new InvalidOperationException("El abono debe ser mayor que cero y no puede superar el saldo pendiente.");
        }

        customer.AddPayment(amount, "Abono recibido", date ?? DateTime.Now);
        return customer.CreditHistory[0];
    }

    public CashSessionRecord OpenCashSession(IReadOnlyList<CashDenominationCount> openingCount, DateTime? openedAt = null)
    {
        if (ActiveCashSession is not null)
        {
            throw new InvalidOperationException("Ya hay una caja abierta.");
        }

        var session = new CashSessionRecord(
            _nextCashSessionId++,
            openedAt ?? DateTime.Now,
            openingCount.Where(row => row.Quantity > 0).ToArray());
        CashSessions.Insert(0, session);
        ActiveCashSession = session;
        return session;
    }

    public CashSessionRecord CloseCashSession(IReadOnlyList<CashDenominationCount> closingCount, DateTime? closedAt = null)
    {
        var session = ActiveCashSession ?? throw new InvalidOperationException("No hay una caja abierta para cerrar.");
        session.ClosingCount = closingCount.Where(row => row.Quantity > 0).ToArray();
        session.ClosedAt = closedAt ?? DateTime.Now;
        ActiveCashSession = null;
        OnPropertyChanged(nameof(CashSessions));
        return session;
    }

    public IEnumerable<(ProductRecord Product, InventoryBatchRecord Batch)> GetExpiringBatches(int withinDays = 30)
    {
        var today = DateTime.Today;
        var end = today.AddDays(withinDays);
        return Products
            .SelectMany(product => product.Batches
                .Where(batch => batch.Quantity > 0 && batch.ExpirationDate is DateTime expiry && expiry.Date <= end)
                .Select(batch => (Product: product, Batch: batch)))
            .OrderBy(row => row.Batch.ExpirationDate);
    }

    public static string FormatMoney(decimal amount) => $"L {amount:0.00}";

    private void AddStock(
        ProductRecord product,
        int quantity,
        DateTime date,
        DateTime? expirationDate,
        decimal unitCost,
        string reference)
    {
        var batch = new InventoryBatchRecord(_nextBatchId++, quantity, date, expirationDate, unitCost, reference);
        product.Batches.Add(batch);
        product.Stock += quantity;
        product.History.Insert(0, new InventoryMovementRecord(date, "Entrada", quantity, reference, expirationDate));
    }

    private static void RemoveStock(ProductRecord product, int quantity, DateTime date, string reference)
    {
        var remaining = quantity;
        foreach (var batch in product.Batches.Where(batch => batch.Quantity > 0).OrderBy(batch => batch.ExpirationDate ?? DateTime.MaxValue).ThenBy(batch => batch.PurchaseDate))
        {
            var taken = Math.Min(batch.Quantity, remaining);
            batch.Quantity -= taken;
            remaining -= taken;
            if (remaining == 0)
            {
                break;
            }
        }

        product.Stock -= quantity;
        product.History.Insert(0, new InventoryMovementRecord(date, "Venta", -quantity, reference, null));
    }

    private void SeedDemoData()
    {
        var distributor = CreateSupplier("Distribuidora Central", "2234-1200", "Ana Rivera");
        var dairySupplier = CreateSupplier("Lácteos del Norte", "2238-4455", "Luis Mejía");

        var cocaCola = CreateProduct("Coca-Cola 600 ml", 22m);
        var rice = CreateProduct("Arroz 1 libra", 23.50m);
        var beans = CreateProduct("Frijoles 1 libra", 34m);
        var milk = CreateProduct("Leche entera 946 ml", 31m);
        var coffee = CreateProduct("Café molido 400 g", 76m);
        var bread = CreateProduct("Pan blanco", 14m);

        var yesterday = DateTime.Today.AddDays(-1);
        RecordPurchase(distributor, yesterday, [
            new(cocaCola, 30, 16m, DateTime.Today.AddDays(60)),
            new(rice, 24, 18m, DateTime.Today.AddDays(240)),
            new(beans, 20, 25m, DateTime.Today.AddDays(180)),
            new(coffee, 8, 58m, DateTime.Today.AddDays(120))
        ]);
        RecordPurchase(dairySupplier, DateTime.Today, [
            new(milk, 18, 24m, DateTime.Today.AddDays(18)),
            new(bread, 20, 9m, DateTime.Today.AddDays(4))
        ]);

        var maria = CreateCustomer("María López", "9999-1234", CustomerType.Person, "0801-1990-12345");
        var carlos = CreateCustomer("Carlos Méndez", "9888-4321", CustomerType.Company, "08011990123456");
        maria.AddCredit(260m, "Saldo inicial de demostración", DateTime.Today.AddDays(-6));
        maria.AddPayment(100m, "Abono de demostración", DateTime.Today.AddDays(-2));
        carlos.AddCredit(420m, "Saldo inicial de demostración", DateTime.Today.AddDays(-3));
    }
}

public sealed record PurchaseLineInput(ProductRecord Product, int Quantity, decimal UnitCost, DateTime? ExpirationDate);

public sealed record SaleLineInput(ProductRecord Product, int Quantity, decimal UnitPrice);
