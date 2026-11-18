using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using SistemaRocaMaya.Models;

namespace SistemaRocaMaya.Services;

public sealed class OperationalDemoStore : ObservableObject
{
    public static CultureInfo HondurasCulture { get; } = CultureInfo.GetCultureInfo("es-HN");
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
            ActiveCashSession.AddCashSale(sale);
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

    public static string FormatMoney(decimal amount) => $"L {amount.ToString("#,##0.00", HondurasCulture)}";

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

        var grocery = CreateSupplier("Abastos Valle Verde", "2200-0101", "Sofía Duarte");
        var farm = CreateSupplier("Huerta Los Pinos", "2200-0102", "Mateo Rosales");

        var cocaCola = CreateProduct("Coca-Cola 600 ml", 22m);
        var rice = CreateProduct("Arroz 1 libra", 23.50m);
        var beans = CreateProduct("Frijoles 1 libra", 34m);
        var milk = CreateProduct("Leche entera 946 ml", 31m);
        var coffee = CreateProduct("Café molido 400 g", 76m);
        var bread = CreateProduct("Pan blanco", 14m);

        var oil = CreateProduct("Aceite vegetal 750 ml", 65m);
        var flour = CreateProduct("Harina de maíz 2 libras", 36m);
        var sugar = CreateProduct("Azúcar 1 libra", 26m);
        var oats = CreateProduct("Avena en hojuelas 400 g", 48m);
        var eggs = CreateProduct("Huevos 12 unidades", 72m);
        var tomatoes = CreateProduct("Tomate 1 libra", 28m);
        var yogurt = CreateProduct("Yogur natural 200 g", 24m);
        var cheese = CreateProduct("Queso fresco 1 libra", 90m);

        var maria = CreateCustomer("María López", "9999-1234", CustomerType.Person, "0801-1990-12345");
        var carlos = CreateCustomer("Carlos Méndez", "9888-4321", CustomerType.Company, "08011990123456");
        var ana = CreateCustomer("Ana Martínez", "9777-2233", CustomerType.Person, "0801-1995-22334");
        maria.AddCredit(260m, "Saldo inicial de demostración", DateTime.Today.AddDays(-42));
        maria.AddPayment(100m, "Abono de demostración", DateTime.Today.AddDays(-2));
        carlos.AddCredit(420m, "Saldo inicial de demostración", DateTime.Today.AddDays(-78));
        ana.AddCredit(150m, "Saldo inicial de demostración", DateTime.Today.AddDays(-12));

        var lucia = CreateCustomer("Lucía Pineda", "9000-0101", CustomerType.Person, "0801-1992-01001");
        var daniel = CreateCustomer("Daniel Fúnez", "9000-0102", CustomerType.Person, "0801-1988-01002");
        var comedor = CreateCustomer("Comedor Los Almendros", "9000-0103", CustomerType.Company, "08019026001003");
        var cafeteria = CreateCustomer("Cafetería Brisa del Valle", "9000-0104", CustomerType.Company, "08019026001004");

        var andrea = CreateCustomer("Andrea Zelaya", "9000-0201", CustomerType.Person, "0801-1981-02001");
        var gabriel = CreateCustomer("Gabriel Cruz", "9000-0202", CustomerType.Person, "0801-1982-02002");
        var rosa = CreateCustomer("Rosa Valladares", "9000-0203", CustomerType.Person, "0801-1983-02003");
        var hector = CreateCustomer("Héctor Portillo", "9000-0204", CustomerType.Person, "0801-1984-02004");
        var paola = CreateCustomer("Paola Reyes", "9000-0205", CustomerType.Person, "0801-1985-02005");
        var jorge = CreateCustomer("Jorge Aguilar", "9000-0206", CustomerType.Person, "0801-1986-02006");
        var valeria = CreateCustomer("Valeria Cáceres", "9000-0207", CustomerType.Person, "0801-1987-02007");
        var oscar = CreateCustomer("Óscar Bonilla", "9000-0208", CustomerType.Person, "0801-1988-02008");
        var lidia = CreateCustomer("Lidia Padilla", "9000-0209", CustomerType.Person, "0801-1989-02009");
        var emilio = CreateCustomer("Emilio Lagos", "9000-0210", CustomerType.Person, "0801-1990-02010");
        var ines = CreateCustomer("Inés Orellana", "9000-0211", CustomerType.Person, "0801-1991-02011");
        var tomas = CreateCustomer("Tomás Suazo", "9000-0212", CustomerType.Person, "0801-1992-02012");
        var natalia = CreateCustomer("Natalia Flores", "9000-0213", CustomerType.Person, "0801-1993-02013");
        var diego = CreateCustomer("Diego Lagos", "9000-0214", CustomerType.Person, "0801-1994-02014");
        var sara = CreateCustomer("Sara Castro", "9000-0215", CustomerType.Person, "0801-1995-02015");
        var mario = CreateCustomer("Mario Ponce", "9000-0216", CustomerType.Person, "0801-1996-02016");
        var camila = CreateCustomer("Camila Rivera", "9000-0217", CustomerType.Person, "0801-1997-02017");
        var david = CreateCustomer("David Soto", "9000-0218", CustomerType.Person, "0801-1998-02018");
        var marta = CreateCustomer("Marta Pineda", "9000-0219", CustomerType.Person, "0801-1999-02019");
        var raul = CreateCustomer("Raúl Villanueva", "9000-0220", CustomerType.Person, "0801-2000-02020");
        var bodega = CreateCustomer("Bodega El Arrayán", "9000-0221", CustomerType.Company, "08019027002021");
        var merendero = CreateCustomer("Merendero Las Jacarandas", "9000-0222", CustomerType.Company, "08019027002022");
        var cafetin = CreateCustomer("Cafetín Las Acacias", "9000-0223", CustomerType.Company, "08019027002023");
        var pulperia = CreateCustomer("Pulpería El Guayacán", "9000-0224", CustomerType.Company, "08019027002024");
        var hostal = CreateCustomer("Hostal La Ceiba Alta", "9000-0225", CustomerType.Company, "08019027002025");
        var reposteria = CreateCustomer("Repostería Vista del Valle", "9000-0226", CustomerType.Company, "08019027002026");

        var reportDay115 = DateTime.Today.AddDays(-115);
        RecordPurchase(distributor, reportDay115, [
            new(rice, 32, 17.50m, reportDay115.AddDays(200)),
            new(beans, 16, 25m, reportDay115.AddDays(160)),
            new(cocaCola, 20, 16m, reportDay115.AddDays(80))
        ]);
        OpenCashSession([new(200m, 1), new(50m, 2)], reportDay115.AddHours(7));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(beans, 1, beans.SalePrice)
        ], reportDay115.AddHours(8).AddMinutes(0));
        RecordSale(SalePaymentType.Cash, gabriel, [
            new(rice, 2, rice.SalePrice), new(cocaCola, 2, cocaCola.SalePrice)
        ], reportDay115.AddHours(8).AddMinutes(47));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(beans, 3, beans.SalePrice)
        ], reportDay115.AddHours(8).AddMinutes(94));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(cocaCola, 1, cocaCola.SalePrice)
        ], reportDay115.AddHours(8).AddMinutes(141));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(beans, 2, beans.SalePrice)
        ], reportDay115.AddHours(8).AddMinutes(188));
        RecordSale(SalePaymentType.Cash, jorge, [
            new(rice, 2, rice.SalePrice), new(cocaCola, 3, cocaCola.SalePrice)
        ], reportDay115.AddHours(8).AddMinutes(235));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(beans, 1, beans.SalePrice)
        ], reportDay115.AddHours(8).AddMinutes(282));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(cocaCola, 2, cocaCola.SalePrice)
        ], reportDay115.AddHours(8).AddMinutes(329));
        RecordSale(SalePaymentType.Credit, bodega, [
            new(rice, 4, rice.SalePrice), new(beans, 3, beans.SalePrice)
        ], reportDay115.AddHours(15).AddMinutes(15));
        RecordSale(SalePaymentType.Credit, merendero, [
            new(rice, 6, rice.SalePrice), new(cocaCola, 4, cocaCola.SalePrice)
        ], reportDay115.AddHours(16).AddMinutes(15));
        CloseCashSession([new(500m, 2), new(200m, 1), new(20m, 1), new(10m, 1), new(1m, 1)], reportDay115.AddHours(17));
        RecordCreditPayment(bodega, 35m, reportDay115.AddHours(18));

        var reportDay82 = DateTime.Today.AddDays(-82);
        RecordPurchase(grocery, reportDay82, [
            new(rice, 36, 18.50m, reportDay82.AddDays(200)),
            new(coffee, 16, 57m, reportDay82.AddDays(120)),
            new(oil, 20, 49m, reportDay82.AddDays(150))
        ]);
        OpenCashSession([new(200m, 1), new(50m, 2)], reportDay82.AddHours(7));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(oil, 2, oil.SalePrice)
        ], reportDay82.AddHours(8).AddMinutes(0));
        RecordSale(SalePaymentType.Cash, rosa, [
            new(rice, 2, rice.SalePrice), new(coffee, 3, coffee.SalePrice)
        ], reportDay82.AddHours(8).AddMinutes(47));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(oil, 1, oil.SalePrice)
        ], reportDay82.AddHours(8).AddMinutes(94));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(coffee, 2, coffee.SalePrice)
        ], reportDay82.AddHours(8).AddMinutes(141));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(oil, 3, oil.SalePrice)
        ], reportDay82.AddHours(8).AddMinutes(188));
        RecordSale(SalePaymentType.Cash, valeria, [
            new(rice, 2, rice.SalePrice), new(coffee, 1, coffee.SalePrice)
        ], reportDay82.AddHours(8).AddMinutes(235));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(oil, 2, oil.SalePrice)
        ], reportDay82.AddHours(8).AddMinutes(282));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(coffee, 3, coffee.SalePrice)
        ], reportDay82.AddHours(8).AddMinutes(329));
        RecordSale(SalePaymentType.Credit, cafetin, [
            new(rice, 4, rice.SalePrice), new(coffee, 3, coffee.SalePrice)
        ], reportDay82.AddHours(15).AddMinutes(15));
        RecordSale(SalePaymentType.Credit, pulperia, [
            new(rice, 6, rice.SalePrice), new(oil, 4, oil.SalePrice)
        ], reportDay82.AddHours(16).AddMinutes(15));
        CloseCashSession([new(500m, 4), new(20m, 1), new(1m, 1)], reportDay82.AddHours(17));

        var reportDay51 = DateTime.Today.AddDays(-51);
        RecordPurchase(distributor, reportDay51, [
            new(rice, 40, 19.50m, reportDay51.AddDays(200)),
            new(flour, 16, 26m, reportDay51.AddDays(120)),
            new(sugar, 20, 19m, reportDay51.AddDays(170))
        ]);
        OpenCashSession([new(200m, 1), new(50m, 2)], reportDay51.AddHours(7));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(flour, 3, flour.SalePrice)
        ], reportDay51.AddHours(8).AddMinutes(0));
        RecordSale(SalePaymentType.Cash, hector, [
            new(rice, 2, rice.SalePrice), new(sugar, 1, sugar.SalePrice)
        ], reportDay51.AddHours(8).AddMinutes(47));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(flour, 2, flour.SalePrice)
        ], reportDay51.AddHours(8).AddMinutes(94));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(sugar, 3, sugar.SalePrice)
        ], reportDay51.AddHours(8).AddMinutes(141));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(flour, 1, flour.SalePrice)
        ], reportDay51.AddHours(8).AddMinutes(188));
        RecordSale(SalePaymentType.Cash, oscar, [
            new(rice, 2, rice.SalePrice), new(sugar, 2, sugar.SalePrice)
        ], reportDay51.AddHours(8).AddMinutes(235));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(flour, 3, flour.SalePrice)
        ], reportDay51.AddHours(8).AddMinutes(282));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(sugar, 1, sugar.SalePrice)
        ], reportDay51.AddHours(8).AddMinutes(329));
        RecordSale(SalePaymentType.Credit, hostal, [
            new(rice, 4, rice.SalePrice), new(flour, 3, flour.SalePrice)
        ], reportDay51.AddHours(15).AddMinutes(15));
        RecordSale(SalePaymentType.Credit, reposteria, [
            new(rice, 6, rice.SalePrice), new(sugar, 4, sugar.SalePrice)
        ], reportDay51.AddHours(16).AddMinutes(15));
        CloseCashSession([new(500m, 2), new(200m, 1), new(100m, 1), new(20m, 1), new(2m, 1), new(1m, 1)], reportDay51.AddHours(17));
        RecordCreditPayment(hostal, 45m, reportDay51.AddHours(18));

        var yesterday = DateTime.Today.AddDays(-1);
        RecordPurchase(distributor, DateTime.Today.AddDays(-31), [
            new(cocaCola, 20, 15.50m, DateTime.Today.AddDays(45)),
            new(rice, 20, 17.50m, DateTime.Today.AddDays(210)),
            new(beans, 18, 24m, DateTime.Today.AddDays(150)),
            new(coffee, 6, 56m, DateTime.Today.AddDays(90))
        ]);
        var reportDay29 = DateTime.Today.AddDays(-29);
        RecordPurchase(grocery, reportDay29, [
            new(rice, 44, 17.50m, reportDay29.AddDays(200)),
            new(oats, 16, 35m, reportDay29.AddDays(135)),
            new(beans, 20, 25m, reportDay29.AddDays(160))
        ]);
        OpenCashSession([new(200m, 1), new(50m, 2)], reportDay29.AddHours(7));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(beans, 1, beans.SalePrice)
        ], reportDay29.AddHours(8).AddMinutes(0));
        RecordSale(SalePaymentType.Cash, paola, [
            new(rice, 2, rice.SalePrice), new(oats, 2, oats.SalePrice)
        ], reportDay29.AddHours(8).AddMinutes(47));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(beans, 3, beans.SalePrice)
        ], reportDay29.AddHours(8).AddMinutes(94));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(oats, 1, oats.SalePrice)
        ], reportDay29.AddHours(8).AddMinutes(141));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(beans, 2, beans.SalePrice)
        ], reportDay29.AddHours(8).AddMinutes(188));
        RecordSale(SalePaymentType.Cash, lidia, [
            new(rice, 2, rice.SalePrice), new(oats, 3, oats.SalePrice)
        ], reportDay29.AddHours(8).AddMinutes(235));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(beans, 1, beans.SalePrice)
        ], reportDay29.AddHours(8).AddMinutes(282));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(oats, 2, oats.SalePrice)
        ], reportDay29.AddHours(8).AddMinutes(329));
        RecordSale(SalePaymentType.Credit, andrea, [
            new(rice, 4, rice.SalePrice), new(oats, 3, oats.SalePrice)
        ], reportDay29.AddHours(15).AddMinutes(15));
        RecordSale(SalePaymentType.Credit, gabriel, [
            new(rice, 6, rice.SalePrice), new(beans, 4, beans.SalePrice)
        ], reportDay29.AddHours(16).AddMinutes(15));
        CloseCashSession([new(500m, 2), new(200m, 2), new(20m, 1), new(10m, 1), new(5m, 1), new(2m, 2)], reportDay29.AddHours(17));

        RecordPurchase(grocery, DateTime.Today.AddDays(-27), [
            new(oil, 24, 48m, DateTime.Today.AddDays(115)),
            new(flour, 30, 25m, DateTime.Today.AddDays(90)),
            new(sugar, 35, 19m, DateTime.Today.AddDays(160)),
            new(oats, 18, 34m, DateTime.Today.AddDays(100))
        ]);

        var reportDay26 = DateTime.Today.AddDays(-26);
        RecordPurchase(distributor, reportDay26, [
            new(rice, 32, 18.50m, reportDay26.AddDays(200)),
            new(cocaCola, 16, 16m, reportDay26.AddDays(80)),
            new(coffee, 20, 57m, reportDay26.AddDays(120))
        ]);
        OpenCashSession([new(200m, 1), new(50m, 2)], reportDay26.AddHours(7));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(cocaCola, 2, cocaCola.SalePrice)
        ], reportDay26.AddHours(8).AddMinutes(0));
        RecordSale(SalePaymentType.Cash, jorge, [
            new(rice, 2, rice.SalePrice), new(coffee, 3, coffee.SalePrice)
        ], reportDay26.AddHours(8).AddMinutes(47));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(cocaCola, 1, cocaCola.SalePrice)
        ], reportDay26.AddHours(8).AddMinutes(94));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(coffee, 2, coffee.SalePrice)
        ], reportDay26.AddHours(8).AddMinutes(141));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(cocaCola, 3, cocaCola.SalePrice)
        ], reportDay26.AddHours(8).AddMinutes(188));
        RecordSale(SalePaymentType.Cash, emilio, [
            new(rice, 2, rice.SalePrice), new(coffee, 1, coffee.SalePrice)
        ], reportDay26.AddHours(8).AddMinutes(235));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(cocaCola, 2, cocaCola.SalePrice)
        ], reportDay26.AddHours(8).AddMinutes(282));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(coffee, 3, coffee.SalePrice)
        ], reportDay26.AddHours(8).AddMinutes(329));
        RecordSale(SalePaymentType.Credit, rosa, [
            new(rice, 4, rice.SalePrice), new(cocaCola, 3, cocaCola.SalePrice)
        ], reportDay26.AddHours(15).AddMinutes(15));
        RecordSale(SalePaymentType.Credit, hector, [
            new(rice, 6, rice.SalePrice), new(coffee, 4, coffee.SalePrice)
        ], reportDay26.AddHours(16).AddMinutes(15));
        CloseCashSession([new(500m, 3), new(100m, 1), new(50m, 1), new(20m, 1), new(5m, 1), new(2m, 1)], reportDay26.AddHours(17));
        RecordCreditPayment(rosa, 55m, reportDay26.AddHours(18));

        var sessionDay24 = DateTime.Today.AddDays(-24);
        OpenCashSession([new(200m, 1)], sessionDay24.AddHours(7));
        RecordSale(SalePaymentType.Cash, null, [
            new(oil, 2, oil.SalePrice),
            new(flour, 2, flour.SalePrice)
        ], sessionDay24.AddHours(8));
        RecordSale(SalePaymentType.Credit, comedor, [
            new(flour, 6, flour.SalePrice),
            new(sugar, 4, sugar.SalePrice),
            new(oil, 4, oil.SalePrice)
        ], sessionDay24.AddHours(10));
        RecordSale(SalePaymentType.Cash, null, [
            new(sugar, 2, sugar.SalePrice),
            new(oats, 1, oats.SalePrice)
        ], sessionDay24.AddHours(12));
        CloseCashSession([
            new(200m, 2),
            new(100m, 1),
            new(2m, 1)
        ], sessionDay24.AddHours(16));

        var reportDay23 = DateTime.Today.AddDays(-23);
        RecordPurchase(grocery, reportDay23, [
            new(rice, 36, 19.50m, reportDay23.AddDays(200)),
            new(oil, 16, 49m, reportDay23.AddDays(150)),
            new(flour, 20, 26m, reportDay23.AddDays(120))
        ]);
        OpenCashSession([new(200m, 1), new(50m, 2)], reportDay23.AddHours(7));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(flour, 3, flour.SalePrice)
        ], reportDay23.AddHours(8).AddMinutes(0));
        RecordSale(SalePaymentType.Cash, valeria, [
            new(rice, 2, rice.SalePrice), new(oil, 1, oil.SalePrice)
        ], reportDay23.AddHours(8).AddMinutes(47));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(flour, 2, flour.SalePrice)
        ], reportDay23.AddHours(8).AddMinutes(94));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(oil, 3, oil.SalePrice)
        ], reportDay23.AddHours(8).AddMinutes(141));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(flour, 1, flour.SalePrice)
        ], reportDay23.AddHours(8).AddMinutes(188));
        RecordSale(SalePaymentType.Cash, ines, [
            new(rice, 2, rice.SalePrice), new(oil, 2, oil.SalePrice)
        ], reportDay23.AddHours(8).AddMinutes(235));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(flour, 3, flour.SalePrice)
        ], reportDay23.AddHours(8).AddMinutes(282));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(oil, 1, oil.SalePrice)
        ], reportDay23.AddHours(8).AddMinutes(329));
        RecordSale(SalePaymentType.Credit, paola, [
            new(rice, 4, rice.SalePrice), new(oil, 3, oil.SalePrice)
        ], reportDay23.AddHours(15).AddMinutes(15));
        RecordSale(SalePaymentType.Credit, jorge, [
            new(rice, 6, rice.SalePrice), new(flour, 4, flour.SalePrice)
        ], reportDay23.AddHours(16).AddMinutes(15));
        CloseCashSession([new(500m, 3), new(50m, 1), new(20m, 2), new(5m, 1), new(1m, 1)], reportDay23.AddHours(17));

        var reportDay20 = DateTime.Today.AddDays(-20);
        RecordPurchase(distributor, reportDay20, [
            new(rice, 40, 17.50m, reportDay20.AddDays(200)),
            new(sugar, 16, 19m, reportDay20.AddDays(170)),
            new(oats, 20, 35m, reportDay20.AddDays(135))
        ]);
        OpenCashSession([new(200m, 1), new(50m, 2)], reportDay20.AddHours(7));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(sugar, 1, sugar.SalePrice)
        ], reportDay20.AddHours(8).AddMinutes(0));
        RecordSale(SalePaymentType.Cash, oscar, [
            new(rice, 2, rice.SalePrice), new(oats, 2, oats.SalePrice)
        ], reportDay20.AddHours(8).AddMinutes(47));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(sugar, 3, sugar.SalePrice)
        ], reportDay20.AddHours(8).AddMinutes(94));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(oats, 1, oats.SalePrice)
        ], reportDay20.AddHours(8).AddMinutes(141));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(sugar, 2, sugar.SalePrice)
        ], reportDay20.AddHours(8).AddMinutes(188));
        RecordSale(SalePaymentType.Cash, tomas, [
            new(rice, 2, rice.SalePrice), new(oats, 3, oats.SalePrice)
        ], reportDay20.AddHours(8).AddMinutes(235));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(sugar, 1, sugar.SalePrice)
        ], reportDay20.AddHours(8).AddMinutes(282));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(oats, 2, oats.SalePrice)
        ], reportDay20.AddHours(8).AddMinutes(329));
        RecordSale(SalePaymentType.Credit, valeria, [
            new(rice, 4, rice.SalePrice), new(sugar, 3, sugar.SalePrice)
        ], reportDay20.AddHours(15).AddMinutes(15));
        RecordSale(SalePaymentType.Credit, oscar, [
            new(rice, 6, rice.SalePrice), new(oats, 4, oats.SalePrice)
        ], reportDay20.AddHours(16).AddMinutes(15));
        CloseCashSession([new(500m, 2), new(200m, 1), new(100m, 1), new(50m, 1), new(20m, 1), new(10m, 1), new(2m, 1), new(1m, 1)], reportDay20.AddHours(17));
        RecordCreditPayment(valeria, 65m, reportDay20.AddHours(18));

        var reportDay17 = DateTime.Today.AddDays(-17);
        RecordPurchase(grocery, reportDay17, [
            new(rice, 44, 18.50m, reportDay17.AddDays(200)),
            new(beans, 16, 25m, reportDay17.AddDays(160)),
            new(cocaCola, 20, 16m, reportDay17.AddDays(80))
        ]);
        OpenCashSession([new(200m, 1), new(50m, 2)], reportDay17.AddHours(7));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(cocaCola, 2, cocaCola.SalePrice)
        ], reportDay17.AddHours(8).AddMinutes(0));
        RecordSale(SalePaymentType.Cash, lidia, [
            new(rice, 2, rice.SalePrice), new(beans, 3, beans.SalePrice)
        ], reportDay17.AddHours(8).AddMinutes(47));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(cocaCola, 1, cocaCola.SalePrice)
        ], reportDay17.AddHours(8).AddMinutes(94));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(beans, 2, beans.SalePrice)
        ], reportDay17.AddHours(8).AddMinutes(141));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(cocaCola, 3, cocaCola.SalePrice)
        ], reportDay17.AddHours(8).AddMinutes(188));
        RecordSale(SalePaymentType.Cash, natalia, [
            new(rice, 2, rice.SalePrice), new(beans, 1, beans.SalePrice)
        ], reportDay17.AddHours(8).AddMinutes(235));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(cocaCola, 2, cocaCola.SalePrice)
        ], reportDay17.AddHours(8).AddMinutes(282));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(beans, 3, beans.SalePrice)
        ], reportDay17.AddHours(8).AddMinutes(329));
        RecordSale(SalePaymentType.Credit, lidia, [
            new(rice, 4, rice.SalePrice), new(beans, 3, beans.SalePrice)
        ], reportDay17.AddHours(15).AddMinutes(15));
        RecordSale(SalePaymentType.Credit, emilio, [
            new(rice, 6, rice.SalePrice), new(cocaCola, 4, cocaCola.SalePrice)
        ], reportDay17.AddHours(16).AddMinutes(15));
        CloseCashSession([new(500m, 2), new(200m, 1), new(50m, 1), new(20m, 2), new(5m, 1), new(2m, 2)], reportDay17.AddHours(17));

        RecordCreditPayment(comedor, 250m, DateTime.Today.AddDays(-16).AddHours(10));

        var reportDay14 = DateTime.Today.AddDays(-14);
        RecordPurchase(distributor, reportDay14, [
            new(rice, 32, 19.50m, reportDay14.AddDays(200)),
            new(coffee, 16, 57m, reportDay14.AddDays(120)),
            new(oil, 20, 49m, reportDay14.AddDays(150))
        ]);
        OpenCashSession([new(200m, 1), new(50m, 2)], reportDay14.AddHours(7));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(coffee, 3, coffee.SalePrice)
        ], reportDay14.AddHours(8).AddMinutes(0));
        RecordSale(SalePaymentType.Cash, emilio, [
            new(rice, 2, rice.SalePrice), new(oil, 1, oil.SalePrice)
        ], reportDay14.AddHours(8).AddMinutes(47));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(coffee, 2, coffee.SalePrice)
        ], reportDay14.AddHours(8).AddMinutes(94));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(oil, 3, oil.SalePrice)
        ], reportDay14.AddHours(8).AddMinutes(141));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(coffee, 1, coffee.SalePrice)
        ], reportDay14.AddHours(8).AddMinutes(188));
        RecordSale(SalePaymentType.Cash, diego, [
            new(rice, 2, rice.SalePrice), new(oil, 2, oil.SalePrice)
        ], reportDay14.AddHours(8).AddMinutes(235));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(coffee, 3, coffee.SalePrice)
        ], reportDay14.AddHours(8).AddMinutes(282));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(oil, 1, oil.SalePrice)
        ], reportDay14.AddHours(8).AddMinutes(329));
        RecordSale(SalePaymentType.Credit, ines, [
            new(rice, 4, rice.SalePrice), new(coffee, 3, coffee.SalePrice)
        ], reportDay14.AddHours(15).AddMinutes(15));
        RecordSale(SalePaymentType.Credit, tomas, [
            new(rice, 6, rice.SalePrice), new(oil, 4, oil.SalePrice)
        ], reportDay14.AddHours(16).AddMinutes(15));
        CloseCashSession([new(500m, 3), new(200m, 2), new(50m, 1), new(5m, 1), new(1m, 1)], reportDay14.AddHours(17));
        RecordCreditPayment(ines, 75m, reportDay14.AddHours(18));

        RecordPurchase(farm, DateTime.Today.AddDays(-12), [
            new(eggs, 24, 54m, DateTime.Today.AddDays(10))
        ]);

        var reportDay11 = DateTime.Today.AddDays(-11);
        RecordPurchase(grocery, reportDay11, [
            new(rice, 36, 17.50m, reportDay11.AddDays(200)),
            new(flour, 16, 26m, reportDay11.AddDays(120)),
            new(sugar, 20, 19m, reportDay11.AddDays(170))
        ]);
        OpenCashSession([new(200m, 1), new(50m, 2)], reportDay11.AddHours(7));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(sugar, 1, sugar.SalePrice)
        ], reportDay11.AddHours(8).AddMinutes(0));
        RecordSale(SalePaymentType.Cash, ines, [
            new(rice, 2, rice.SalePrice), new(flour, 2, flour.SalePrice)
        ], reportDay11.AddHours(8).AddMinutes(47));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(sugar, 3, sugar.SalePrice)
        ], reportDay11.AddHours(8).AddMinutes(94));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(flour, 1, flour.SalePrice)
        ], reportDay11.AddHours(8).AddMinutes(141));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(sugar, 2, sugar.SalePrice)
        ], reportDay11.AddHours(8).AddMinutes(188));
        RecordSale(SalePaymentType.Cash, sara, [
            new(rice, 2, rice.SalePrice), new(flour, 3, flour.SalePrice)
        ], reportDay11.AddHours(8).AddMinutes(235));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(sugar, 1, sugar.SalePrice)
        ], reportDay11.AddHours(8).AddMinutes(282));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(flour, 2, flour.SalePrice)
        ], reportDay11.AddHours(8).AddMinutes(329));
        RecordSale(SalePaymentType.Credit, natalia, [
            new(rice, 4, rice.SalePrice), new(flour, 3, flour.SalePrice)
        ], reportDay11.AddHours(15).AddMinutes(15));
        RecordSale(SalePaymentType.Credit, diego, [
            new(rice, 6, rice.SalePrice), new(sugar, 4, sugar.SalePrice)
        ], reportDay11.AddHours(16).AddMinutes(15));
        CloseCashSession([new(500m, 2), new(200m, 1), new(50m, 1), new(20m, 1), new(10m, 1), new(5m, 1), new(2m, 1)], reportDay11.AddHours(17));

        RecordPurchase(dairySupplier, DateTime.Today.AddDays(-10), [
            new(yogurt, 24, 16m, DateTime.Today.AddDays(8)),
            new(cheese, 16, 66m, DateTime.Today.AddDays(12))
        ]);

        var sessionDay9 = DateTime.Today.AddDays(-9);
        OpenCashSession([new(200m, 1)], sessionDay9.AddHours(7));
        RecordSale(SalePaymentType.Cash, null, [
            new(eggs, 2, eggs.SalePrice),
            new(yogurt, 3, yogurt.SalePrice)
        ], sessionDay9.AddHours(8));
        RecordSale(SalePaymentType.Credit, cafeteria, [
            new(cheese, 4, cheese.SalePrice),
            new(oats, 3, oats.SalePrice),
            new(sugar, 4, sugar.SalePrice)
        ], sessionDay9.AddHours(10));
        RecordSale(SalePaymentType.Credit, lucia, [
            new(eggs, 1, eggs.SalePrice),
            new(cheese, 1, cheese.SalePrice)
        ], sessionDay9.AddHours(12));
        RecordSale(SalePaymentType.Cash, null, [
            new(cheese, 1, cheese.SalePrice),
            new(flour, 1, flour.SalePrice)
        ], sessionDay9.AddHours(14));
        CloseCashSession([
            new(500m, 1),
            new(20m, 2),
            new(2m, 1)
        ], sessionDay9.AddHours(16));

        var reportDay8 = DateTime.Today.AddDays(-8);
        RecordPurchase(distributor, reportDay8, [
            new(rice, 40, 18.50m, reportDay8.AddDays(200)),
            new(oats, 16, 35m, reportDay8.AddDays(135)),
            new(beans, 20, 25m, reportDay8.AddDays(160))
        ]);
        OpenCashSession([new(200m, 1), new(50m, 2)], reportDay8.AddHours(7));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(oats, 2, oats.SalePrice)
        ], reportDay8.AddHours(8).AddMinutes(0));
        RecordSale(SalePaymentType.Cash, tomas, [
            new(rice, 2, rice.SalePrice), new(beans, 3, beans.SalePrice)
        ], reportDay8.AddHours(8).AddMinutes(47));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(oats, 1, oats.SalePrice)
        ], reportDay8.AddHours(8).AddMinutes(94));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(beans, 2, beans.SalePrice)
        ], reportDay8.AddHours(8).AddMinutes(141));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(oats, 3, oats.SalePrice)
        ], reportDay8.AddHours(8).AddMinutes(188));
        RecordSale(SalePaymentType.Cash, mario, [
            new(rice, 2, rice.SalePrice), new(beans, 1, beans.SalePrice)
        ], reportDay8.AddHours(8).AddMinutes(235));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(oats, 2, oats.SalePrice)
        ], reportDay8.AddHours(8).AddMinutes(282));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(beans, 3, beans.SalePrice)
        ], reportDay8.AddHours(8).AddMinutes(329));
        RecordSale(SalePaymentType.Credit, sara, [
            new(rice, 4, rice.SalePrice), new(oats, 3, oats.SalePrice)
        ], reportDay8.AddHours(15).AddMinutes(15));
        RecordSale(SalePaymentType.Credit, mario, [
            new(rice, 6, rice.SalePrice), new(beans, 4, beans.SalePrice)
        ], reportDay8.AddHours(16).AddMinutes(15));
        CloseCashSession([new(500m, 3), new(5m, 1), new(2m, 1)], reportDay8.AddHours(17));
        RecordCreditPayment(sara, 85m, reportDay8.AddHours(18));

        RecordPurchase(grocery, DateTime.Today.AddDays(-6), [
            new(oil, 16, 49m, DateTime.Today.AddDays(130)),
            new(flour, 24, 26m, DateTime.Today.AddDays(105)),
            new(oats, 20, 35m, DateTime.Today.AddDays(120))
        ]);

        RecordCreditPayment(cafeteria, 300m, DateTime.Today.AddDays(-5).AddHours(10));

        var reportDay5 = DateTime.Today.AddDays(-5);
        RecordPurchase(grocery, reportDay5, [
            new(rice, 44, 19.50m, reportDay5.AddDays(200)),
            new(cocaCola, 16, 16m, reportDay5.AddDays(80)),
            new(coffee, 20, 57m, reportDay5.AddDays(120))
        ]);
        OpenCashSession([new(200m, 1), new(50m, 2)], reportDay5.AddHours(7));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(coffee, 3, coffee.SalePrice)
        ], reportDay5.AddHours(8).AddMinutes(0));
        RecordSale(SalePaymentType.Cash, natalia, [
            new(rice, 2, rice.SalePrice), new(cocaCola, 1, cocaCola.SalePrice)
        ], reportDay5.AddHours(8).AddMinutes(47));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(coffee, 2, coffee.SalePrice)
        ], reportDay5.AddHours(8).AddMinutes(94));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(cocaCola, 3, cocaCola.SalePrice)
        ], reportDay5.AddHours(8).AddMinutes(141));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(coffee, 1, coffee.SalePrice)
        ], reportDay5.AddHours(8).AddMinutes(188));
        RecordSale(SalePaymentType.Cash, camila, [
            new(rice, 2, rice.SalePrice), new(cocaCola, 2, cocaCola.SalePrice)
        ], reportDay5.AddHours(8).AddMinutes(235));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(coffee, 3, coffee.SalePrice)
        ], reportDay5.AddHours(8).AddMinutes(282));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(cocaCola, 1, cocaCola.SalePrice)
        ], reportDay5.AddHours(8).AddMinutes(329));
        RecordSale(SalePaymentType.Credit, camila, [
            new(rice, 4, rice.SalePrice), new(cocaCola, 3, cocaCola.SalePrice)
        ], reportDay5.AddHours(15).AddMinutes(15));
        RecordSale(SalePaymentType.Credit, david, [
            new(rice, 6, rice.SalePrice), new(coffee, 4, coffee.SalePrice)
        ], reportDay5.AddHours(16).AddMinutes(15));
        CloseCashSession([new(500m, 3), new(100m, 1), new(50m, 1), new(5m, 1)], reportDay5.AddHours(17));

        var sessionDay4 = DateTime.Today.AddDays(-4);
        OpenCashSession([new(200m, 1)], sessionDay4.AddHours(7));
        RecordSale(SalePaymentType.Cash, null, [
            new(oil, 1, oil.SalePrice),
            new(oats, 2, oats.SalePrice)
        ], sessionDay4.AddHours(8));
        RecordSale(SalePaymentType.Credit, daniel, [
            new(oil, 2, oil.SalePrice),
            new(flour, 3, flour.SalePrice),
            new(eggs, 2, eggs.SalePrice)
        ], sessionDay4.AddHours(10));
        RecordSale(SalePaymentType.Cash, null, [
            new(yogurt, 4, yogurt.SalePrice),
            new(cheese, 1, cheese.SalePrice)
        ], sessionDay4.AddHours(12));
        CloseCashSession([
            new(500m, 1),
            new(20m, 2),
            new(5m, 1),
            new(2m, 1)
        ], sessionDay4.AddHours(16));

        RecordPurchase(farm, DateTime.Today.AddDays(-3), [
            new(tomatoes, 28, 18m, DateTime.Today.AddDays(4)),
            new(eggs, 18, 55m, DateTime.Today.AddDays(17))
        ]);

        RecordCreditPayment(daniel, 382m, DateTime.Today.AddDays(-3).AddHours(10));

        var sessionDay2 = DateTime.Today.AddDays(-2);
        OpenCashSession([new(200m, 1)], sessionDay2.AddHours(7));
        RecordSale(SalePaymentType.Cash, null, [
            new(tomatoes, 3, tomatoes.SalePrice),
            new(eggs, 2, eggs.SalePrice)
        ], sessionDay2.AddHours(8));
        RecordSale(SalePaymentType.Credit, comedor, [
            new(tomatoes, 6, tomatoes.SalePrice),
            new(eggs, 3, eggs.SalePrice),
            new(oil, 2, oil.SalePrice)
        ], sessionDay2.AddHours(10));
        RecordSale(SalePaymentType.Credit, lucia, [
            new(tomatoes, 2, tomatoes.SalePrice),
            new(yogurt, 2, yogurt.SalePrice)
        ], sessionDay2.AddHours(12));
        RecordSale(SalePaymentType.Cash, null, [
            new(flour, 2, flour.SalePrice),
            new(sugar, 3, sugar.SalePrice)
        ], sessionDay2.AddHours(14));
        CloseCashSession([
            new(500m, 1),
            new(50m, 1),
            new(20m, 1),
            new(5m, 1),
            new(2m, 1),
            new(1m, 1)
        ], sessionDay2.AddHours(16));

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

        OpenCashSession(
        [
            new CashDenominationCount(200m, 1)
        ], yesterday.Date.AddHours(7));
        RecordSale(SalePaymentType.Cash, null,
        [
            new SaleLineInput(rice, 2, rice.SalePrice),
            new SaleLineInput(cocaCola, 1, cocaCola.SalePrice)
        ], yesterday.Date.AddHours(7).AddMinutes(30));
        RecordSale(SalePaymentType.Cash, null,
        [
            new SaleLineInput(beans, 1, beans.SalePrice),
            new SaleLineInput(coffee, 1, coffee.SalePrice)
        ], yesterday.Date.AddHours(7).AddMinutes(45));
        CloseCashSession(
        [
            new CashDenominationCount(200m, 1),
            new CashDenominationCount(100m, 1),
            new CashDenominationCount(50m, 1),
            new CashDenominationCount(20m, 1),
            new CashDenominationCount(5m, 1),
            new CashDenominationCount(2m, 2)
        ], yesterday.Date.AddHours(16));

        RecordCreditPayment(comedor, 150m, yesterday.AddHours(11));
        RecordCreditPayment(lucia, 100m, yesterday.AddHours(12));
        var reportDay0 = DateTime.Today;
        RecordPurchase(distributor, reportDay0, [
            new(rice, 92, 17.50m, reportDay0.AddDays(200)),
            new(oil, 44, 49m, reportDay0.AddDays(150)),
            new(flour, 52, 26m, reportDay0.AddDays(120))
        ]);
        OpenCashSession([new(200m, 1), new(50m, 2)], reportDay0.AddHours(7));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(oil, 1, oil.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(0));
        RecordSale(SalePaymentType.Cash, diego, [
            new(rice, 2, rice.SalePrice), new(flour, 2, flour.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(13));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(oil, 3, oil.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(26));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(flour, 1, flour.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(39));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(oil, 2, oil.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(52));
        RecordSale(SalePaymentType.Cash, david, [
            new(rice, 2, rice.SalePrice), new(flour, 3, flour.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(65));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(oil, 1, oil.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(78));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(flour, 2, flour.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(91));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(oil, 3, oil.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(104));
        RecordSale(SalePaymentType.Cash, merendero, [
            new(rice, 4, rice.SalePrice), new(flour, 1, flour.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(117));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(oil, 2, oil.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(130));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(flour, 3, flour.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(143));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(oil, 1, oil.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(156));
        RecordSale(SalePaymentType.Cash, reposteria, [
            new(rice, 2, rice.SalePrice), new(flour, 2, flour.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(169));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(oil, 3, oil.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(182));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(flour, 1, flour.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(195));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(oil, 2, oil.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(208));
        RecordSale(SalePaymentType.Cash, hector, [
            new(rice, 2, rice.SalePrice), new(flour, 3, flour.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(221));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(oil, 1, oil.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(234));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(flour, 2, flour.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(247));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(oil, 3, oil.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(260));
        RecordSale(SalePaymentType.Cash, oscar, [
            new(rice, 4, rice.SalePrice), new(flour, 1, flour.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(273));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(oil, 2, oil.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(286));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(flour, 3, flour.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(299));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(oil, 1, oil.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(312));
        RecordSale(SalePaymentType.Cash, tomas, [
            new(rice, 2, rice.SalePrice), new(flour, 2, flour.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(325));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(oil, 3, oil.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(338));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(flour, 1, flour.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(351));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(oil, 2, oil.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(364));
        RecordSale(SalePaymentType.Cash, mario, [
            new(rice, 2, rice.SalePrice), new(flour, 3, flour.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(377));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 4, rice.SalePrice), new(oil, 1, oil.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(390));
        RecordSale(SalePaymentType.Cash, null, [
            new(rice, 2, rice.SalePrice), new(flour, 2, flour.SalePrice)
        ], reportDay0.AddHours(8).AddMinutes(403));
        RecordSale(SalePaymentType.Credit, marta, [
            new(rice, 4, rice.SalePrice), new(oil, 3, oil.SalePrice)
        ], reportDay0.AddHours(15).AddMinutes(15));
        RecordSale(SalePaymentType.Credit, raul, [
            new(rice, 6, rice.SalePrice), new(flour, 4, flour.SalePrice)
        ], reportDay0.AddHours(16).AddMinutes(15));
        CloseCashSession([new(500m, 10), new(200m, 2), new(50m, 1), new(20m, 1), new(10m, 1), new(5m, 1), new(2m, 1), new(1m, 1)], reportDay0.AddHours(17));
        RecordCreditPayment(marta, 95m, reportDay0.AddHours(18));
    }
}

public sealed record PurchaseLineInput(ProductRecord Product, int Quantity, decimal UnitCost, DateTime? ExpirationDate);

public sealed record SaleLineInput(ProductRecord Product, int Quantity, decimal UnitPrice);
