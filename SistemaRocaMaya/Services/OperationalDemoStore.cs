using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using SistemaRocaMaya.Models;
using static SistemaRocaMaya.Services.RocaMayaDatabase;

namespace SistemaRocaMaya.Services;

public sealed class OperationalDemoStore : ObservableObject
{
    private readonly RocaMayaDatabase _database;
    private CashSessionRecord? _activeCashSession;

    private static readonly Lazy<OperationalDemoStore> SharedStore = new(() => new(RocaMayaDatabase.Instance));
    public static OperationalDemoStore Instance => SharedStore.Value;
    public static CultureInfo HondurasCulture { get; } = CultureInfo.GetCultureInfo("es-HN");
    public static IReadOnlyList<decimal> CashDenominations { get; } = [1m, 2m, 5m, 10m, 20m, 50m, 100m, 200m, 500m];
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
            if (SetProperty(ref _activeCashSession, value)) OnPropertyChanged(nameof(IsCashOpen));
        }
    }

    public bool IsCashOpen => ActiveCashSession is not null;
    public string NextProductId
    {
        get
        {
            using var connection = _database.Open();
            var sequence = connection.ExecuteScalar<int>("SELECT ultimo FROM secuencias_folio WHERE tipo='PR' AND fecha=''");
            return $"PR-{sequence + 1:000000}";
        }
    }

    internal OperationalDemoStore(RocaMayaDatabase database)
    {
        _database = database;
        Load();
    }

    public CustomerRecord CreateCustomer(string name, string phone, CustomerType type, string taxOrIdentity)
    {
        name = name.Trim();
        phone = phone.Trim();
        taxOrIdentity = taxOrIdentity.Trim();
        var id = _database.Write(db => db.Insert("""
            INSERT INTO clientes(nombre,telefono,tipo_cliente_id,identidad_o_rtn)
            VALUES(@name,@phone,(SELECT id FROM tipos_cliente WHERE codigo=@code),@taxOrIdentity)
            """, new { name, phone, taxOrIdentity, code = type == CustomerType.Person ? "PERSONA" : "EMPRESA" }));
        var customer = new CustomerRecord(id, name, phone, type, taxOrIdentity);
        Customers.Add(customer);
        return customer;
    }

    public ProductRecord CreateProduct(string name, decimal salePrice, int initialQuantity = 0,
        DateTime? purchaseDate = null, DateTime? expirationDate = null)
    {
        name = name.Trim();
        var price = Cents(salePrice);
        var date = purchaseDate ?? DateTime.Today;
        var result = _database.Write(db =>
        {
            var code = db.Folio("PR");
            var id = db.Insert("INSERT INTO productos(codigo,nombre,precio_venta_centavos) VALUES(@code,@name,@price)",
                new { code, name, price });
            var lot = initialQuantity > 0 ? InsertLot(db, id, null, date, expirationDate, initialQuantity, 0, "Inventario inicial") : null;
            return (id, code, lot);
        });
        var product = new ProductRecord(result.code, name, Money(price)) { DatabaseId = result.id };
        if (result.lot is { } batch) ApplyLot(product, batch);
        Products.Add(product);
        return product;
    }

    public SupplierRecord CreateSupplier(string name, string phone, string contact)
    {
        name = name.Trim();
        phone = phone.Trim();
        contact = contact.Trim();
        var id = _database.Write(db => db.Insert("INSERT INTO proveedores(nombre,telefono,contacto) VALUES(@name,@phone,@contact)",
            new { name, phone, contact }));
        var supplier = new SupplierRecord(id, name, phone, contact);
        Suppliers.Add(supplier);
        return supplier;
    }

    public PurchaseRecord RecordPurchase(SupplierRecord supplier, DateTime date, IReadOnlyList<PurchaseLineInput> inputs,
        string? supplierInvoice = null)
    {
        if (inputs.Count == 0) throw new InvalidOperationException("Agrega al menos un producto a la compra.");
        if (inputs.Any(input => input.Quantity <= 0 || input.UnitCost < 0m))
            throw new InvalidOperationException("Las cantidades deben ser mayores que cero y el costo no puede ser negativo.");

        supplierInvoice = string.IsNullOrWhiteSpace(supplierInvoice) ? null : supplierInvoice.Trim();
        var lines = inputs.Select(input => new PurchaseLineRecord(input.Product, input.Quantity, Money(Cents(input.UnitCost)), input.ExpirationDate)).ToArray();
        var result = _database.Write(db =>
        {
            var number = db.Folio("CP", date);
            var user = _database.UserId;
            var id = db.Insert("""
                INSERT INTO compras(numero,proveedor_id,usuario_id,fecha_hora,referencia_factura)
                VALUES(@number,@supplier,@user,@date,@supplierInvoice)
                """, new { number, supplier = supplier.Id, user, date = Date(date), supplierInvoice });
            var batches = new List<InventoryBatchRecord>();
            foreach (var line in lines)
            {
                var cost = Cents(line.UnitCost);
                var detail = db.Insert("""
                    INSERT INTO compra_detalles(compra_id,producto_id,cantidad,costo_unitario_centavos)
                    VALUES(@id,@product,@quantity,@cost)
                    """, new { id, product = line.Product.DatabaseId, quantity = line.Quantity, cost });
                batches.Add(InsertLot(db, line.Product.DatabaseId, detail, date, line.ExpirationDate, line.Quantity, cost, supplier.Name));
            }
            db.Post(date, user, $"Compra a {supplier.Name}", "Inventario", "Proveedores",
                lines.Sum(line => Cents(line.UnitCost) * line.Quantity), purchase: id);
            return (id, number, batches);
        });

        var purchase = new PurchaseRecord(result.id, date, supplier, lines) { Number = result.number, SupplierInvoice = supplierInvoice };
        for (var index = 0; index < lines.Length; index++)
            ApplyLot(lines[index].Product, result.batches[index], purchase.Number);
        supplier.Purchases.Insert(0, purchase);
        Purchases.Insert(0, purchase);
        OnPropertyChanged(nameof(Purchases));
        return purchase;
    }

    public SaleRecord RecordSale(SalePaymentType paymentType, CustomerRecord? customer, IReadOnlyList<SaleLineInput> inputs,
        DateTime? date = null)
    {
        var session = ActiveCashSession ?? throw new InvalidOperationException("Abre la caja antes de registrar ventas.");
        if (inputs.Count == 0) throw new InvalidOperationException("Agrega productos a la venta.");
        if (paymentType == SalePaymentType.Credit && customer is null)
            throw new InvalidOperationException("Selecciona un cliente para registrar la venta a crédito.");
        if (inputs.Any(input => input.Quantity <= 0 || input.UnitPrice < 0m))
            throw new InvalidOperationException("Revisa las cantidades y precios de la venta.");

        var time = date ?? DateTime.Now;
        var lines = inputs.Select(input => new SaleLineRecord(input.Product, input.Quantity, Money(Cents(input.UnitPrice)))).ToArray();
        var total = lines.Sum(line => Cents(line.UnitPrice) * line.Quantity);
        if (paymentType == SalePaymentType.Credit && total == 0)
            throw new InvalidOperationException("Una venta a crédito debe tener un importe mayor que cero.");

        var result = _database.Write(db =>
        {
            if (db.Scalar<int>("SELECT count(*) FROM turnos_caja WHERE id=@Id AND cierre_fecha_hora IS NULL", new { session.Id }) == 0)
                throw new InvalidOperationException("El turno de caja ya está cerrado.");
            var user = _database.UserId;
            var ticket = db.Folio("VT", time);
            var id = db.Insert("""
                INSERT INTO ventas(ticket,turno_id,usuario_id,cliente_id,fecha_hora,tipo_pago)
                VALUES(@ticket,@turn,@user,@client,@time,@kind)
                """, new { ticket, turn = session.Id, user, client = customer?.Id, time = Date(time), kind = paymentType == SalePaymentType.Cash ? "CONTADO" : "CREDITO" });

            var physicalAllocations = new List<(ProductRecord Product, int LotId, int Quantity)>();
            var cost = 0L;
            foreach (var line in lines)
            {
                var detail = db.Insert("""
                    INSERT INTO venta_detalles(venta_id,producto_id,cantidad,precio_unitario_centavos)
                    VALUES(@id,@product,@quantity,@price)
                    """, new { id, product = line.Product.DatabaseId, quantity = line.Quantity, price = Cents(line.UnitPrice) });
                foreach (var lot in Allocate(db, line, detail, time, accounting: false))
                    physicalAllocations.Add((line.Product, lot.Id, lot.Quantity));
                cost += Allocate(db, line, detail, time, accounting: true).Sum(lot => lot.Quantity * lot.UnitCost);
            }

            if (paymentType == SalePaymentType.Credit)
                InsertCreditMovement(db, customer!.Id, user, time, total, "CARGO", $"Venta {ticket}", null, id);

            db.Post(time, user, "Venta de mercadería", paymentType == SalePaymentType.Cash ? "Caja" : "Crédito por cobrar",
                "Ingresos por ventas", total, sale: id, creditConcept: "Ingreso por venta de mercadería");
            db.Post(time, user, "Costo de venta", "Costo de ventas", "Inventario", cost, sale: id, creditConcept: "Salida de inventario");
            return (id, ticket, physicalAllocations);
        });

        var sale = new SaleRecord(result.id, time, paymentType, customer, lines) { Ticket = result.ticket };
        foreach (var allocation in result.physicalAllocations)
            allocation.Product.Batches.First(batch => batch.Id == allocation.LotId).Quantity -= allocation.Quantity;
        foreach (var line in lines)
            ApplySaleHistory(line.Product, line.Quantity, time, sale.Ticket);
        if (paymentType == SalePaymentType.Cash) AddCashSale(session, sale);
        else customer!.AddCredit(sale.Total, $"Venta {sale.Ticket}", time, sale.Ticket, time.AddDays(30));
        Sales.Insert(0, sale);
        return sale;
    }

    public CreditMovementRecord RecordCreditPayment(CustomerRecord customer, decimal amount, DateTime? date = null)
    {
        var cents = Cents(amount);
        var time = date ?? DateTime.Now;
        var folio = _database.Write(db =>
        {
            var balance = db.Scalar<long>("""
                SELECT COALESCE(SUM(CASE WHEN t.codigo='CARGO' THEN importe_centavos ELSE -importe_centavos END),0)
                FROM movimientos_credito m JOIN tipos_movimiento_credito t ON t.id=m.tipo_movimiento_id
                WHERE cliente_id=@Id
                """, new { customer.Id });
            if (cents <= 0 || cents > balance)
                throw new InvalidOperationException("El abono debe ser mayor que cero y no puede superar el saldo pendiente.");

            var number = db.Folio("AB", time);
            var user = _database.UserId;
            var movement = InsertCreditMovement(db, customer.Id, user, time, cents, "ABONO", "Abono recibido", number);
            db.Post(time, user, $"Abono recibido · {customer.Name}", "Caja", "Crédito por cobrar", cents, movement: movement);
            return number;
        });
        customer.AddPayment(Money(cents), "Abono recibido", time, folio);
        return customer.CreditHistory[0];
    }

    public CashSessionRecord OpenCashSession(IReadOnlyList<CashDenominationCount> openingCount, DateTime? openedAt = null)
    {
        if (ActiveCashSession is not null) throw new InvalidOperationException("Ya hay una caja abierta.");
        var time = openedAt ?? DateTime.Now;
        var counts = openingCount.Where(row => row.Quantity > 0).ToArray();
        var result = _database.Write(db =>
        {
            var folio = db.Folio("CJ", time);
            var id = db.Insert("""
                INSERT INTO turnos_caja(folio,abierto_por_usuario_id,apertura_fecha_hora)
                VALUES(@folio,@user,@time)
                """, new { folio, user = _database.UserId, time = Date(time) });
            InsertCashCounts(db, id, "APERTURA", counts);
            return (id, folio);
        });
        var session = new CashSessionRecord(result.id, time, counts) { Folio = result.folio };
        ActiveCashSession = session;
        CashSessions.Insert(0, session);
        return session;
    }

    public CashSessionRecord CloseCashSession(IReadOnlyList<CashDenominationCount> closingCount, DateTime? closedAt = null)
    {
        var session = ActiveCashSession ?? throw new InvalidOperationException("No hay una caja abierta para cerrar.");
        var time = closedAt ?? DateTime.Now;
        var counts = closingCount.Where(row => row.Quantity > 0).ToArray();
        _database.Write(db =>
        {
            var changed = db.Execute("""
                UPDATE turnos_caja SET cierre_fecha_hora=@time,cerrado_por_usuario_id=@user
                WHERE id=@Id AND cierre_fecha_hora IS NULL
                """, new { session.Id, time = Date(time), user = _database.UserId });
            if (changed != 1) throw new InvalidOperationException("El turno de caja ya está cerrado.");
            InsertCashCounts(db, session.Id, "CIERRE", counts);
            return 0;
        });
        session.ClosingCount = counts;
        session.ClosedAt = time;
        ActiveCashSession = null;
        OnPropertyChanged(nameof(CashSessions));
        return session;
    }

    public IEnumerable<(ProductRecord Product, InventoryBatchRecord Batch)> GetExpiringBatches(int withinDays = 30)
    {
        var end = DateTime.Today.AddDays(withinDays);
        return Products.SelectMany(product => product.Batches
            .Where(batch => batch.Quantity > 0 && batch.ExpirationDate is DateTime expiry && expiry.Date <= end)
            .Select(batch => (Product: product, Batch: batch))).OrderBy(row => row.Batch.ExpirationDate);
    }

    public static string FormatMoney(decimal amount) => $"L {amount.ToString("#,##0.00", HondurasCulture)}";

    private static InventoryBatchRecord InsertLot(DatabaseWrite db, int product, int? detail, DateTime date,
        DateTime? expiration, int quantity, long cost, string supplier)
    {
        var folio = db.Folio("LT", date);
        var id = db.Insert("""
            INSERT INTO lotes_inventario(folio,producto_id,compra_detalle_id,fecha_ingreso,fecha_vencimiento,cantidad_ingresada,costo_unitario_centavos)
            VALUES(@folio,@product,@detail,@date,@expiry,@quantity,@cost)
            """, new { folio, product, detail, date = Date(date), expiry = expiration.HasValue ? Date(expiration.Value) : null, quantity, cost });
        return new(id, quantity, date, expiration, Money(cost), supplier) { Folio = folio };
    }

    private static List<LotAllocation> Allocate(DatabaseWrite db, SaleLineRecord line, int detail, DateTime date, bool accounting)
    {
        var table = accounting ? "costeos_venta_lote" : "consumos_lote";
        var order = accounting ? "l.fecha_ingreso,l.id" : "l.fecha_vencimiento IS NULL,l.fecha_vencimiento,l.fecha_ingreso,l.id";
        var lots = db.Query<LotAllocation>($"""
            SELECT l.id AS Id, l.cantidad_ingresada-COALESCE(SUM(c.cantidad),0) AS Quantity, l.costo_unitario_centavos AS UnitCost
            FROM lotes_inventario l LEFT JOIN {table} c ON c.lote_id=l.id
            WHERE l.producto_id=@product AND l.fecha_ingreso<=@date
            GROUP BY l.id HAVING Quantity>0 ORDER BY {order}
            """, new { product = line.Product.DatabaseId, date = Date(date) });
        var remaining = line.Quantity;
        var allocations = new List<LotAllocation>();
        foreach (var lot in lots)
        {
            var quantity = Math.Min(remaining, lot.Quantity);
            var sql = accounting
                ? "INSERT INTO costeos_venta_lote(venta_detalle_id,lote_id,cantidad,costo_unitario_centavos) VALUES(@detail,@Id,@quantity,@UnitCost)"
                : "INSERT INTO consumos_lote(venta_detalle_id,lote_id,cantidad) VALUES(@detail,@Id,@quantity)";
            db.Execute(sql, new { detail, lot.Id, quantity, lot.UnitCost });
            allocations.Add(lot with { Quantity = quantity });
            remaining -= quantity;
            if (remaining == 0) break;
        }
        if (remaining != 0) throw new InvalidOperationException($"Existencia insuficiente para {line.Product.Name}.");
        return allocations;
    }

    private static int InsertCreditMovement(DatabaseWrite db, int client, int user, DateTime date, long amount,
        string kind, string concept, string? folio, int? sale = null) =>
        db.Insert("""
            INSERT INTO movimientos_credito(folio,cliente_id,venta_id,usuario_id,fecha_hora,tipo_movimiento_id,importe_centavos,fecha_vencimiento,concepto)
            VALUES(@folio,@client,@sale,@user,@date,(SELECT id FROM tipos_movimiento_credito WHERE codigo=@kind),@amount,@due,@concept)
            """, new { folio, client, sale, user, date = Date(date), kind, amount, due = kind == "CARGO" ? Date(date.AddDays(30)) : null, concept });

    private static void InsertCashCounts(DatabaseWrite db, int turn, string moment, IEnumerable<CashDenominationCount> counts)
    {
        foreach (var count in counts)
            db.Execute("""
                INSERT INTO conteos_caja(turno_id,momento_id,denominacion_centavos,cantidad)
                VALUES(@turn,(SELECT id FROM momentos_conteo_caja WHERE codigo=@moment),@bill,@quantity)
                """, new { turn, moment, bill = Cents(count.Denomination), quantity = count.Quantity });
    }

    private static void ApplyLot(ProductRecord product, InventoryBatchRecord batch, string? reference = null)
    {
        product.Batches.Add(batch);
        product.Stock += batch.Quantity;
        product.History.Insert(0, new InventoryMovementRecord(batch.PurchaseDate, "Entrada", batch.Quantity,
            reference ?? batch.SupplierName, batch.ExpirationDate) { LotFolio = batch.Folio });
    }

    private static void ApplySaleHistory(ProductRecord product, int quantity, DateTime date, string ticket)
    {
        product.Stock -= quantity;
        product.History.Insert(0, new InventoryMovementRecord(date, "Venta", -quantity, ticket, null));
    }

    private static void AddCashSale(CashSessionRecord session, SaleRecord sale)
    {
        session.CashSales += sale.Total;
        session.AddCashSale(sale);
    }

    private sealed record LotAllocation(int Id, int Quantity, long UnitCost)
    {
        public LotAllocation() : this(0, 0, 0L) { }
    }

    private void Load()
    {
        using var connection = _database.Open();
        foreach (var row in connection.Query("SELECT * FROM productos ORDER BY id"))
            Products.Add(new ProductRecord((string)row.codigo, (string)row.nombre, Money((long)row.precio_venta_centavos)) { DatabaseId = (int)row.id });
        foreach (var row in connection.Query("SELECT * FROM proveedores ORDER BY id"))
            Suppliers.Add(new SupplierRecord((int)row.id, (string)row.nombre, (string?)row.telefono ?? "", (string?)row.contacto ?? ""));
        foreach (var row in connection.Query("SELECT c.*, t.codigo AS tipo FROM clientes c JOIN tipos_cliente t ON t.id=c.tipo_cliente_id ORDER BY c.id"))
            Customers.Add(new CustomerRecord((int)row.id, (string)row.nombre, (string?)row.telefono ?? "",
                (string)row.tipo == "PERSONA" ? CustomerType.Person : CustomerType.Company, (string?)row.identidad_o_rtn ?? ""));

        var products = Products.ToDictionary(product => product.DatabaseId);
        var customers = Customers.ToDictionary(customer => customer.Id);
        var suppliers = Suppliers.ToDictionary(supplier => supplier.Id);
        var purchaseLines = connection.Query("""
            SELECT d.*,(SELECT MIN(l.fecha_vencimiento) FROM lotes_inventario l WHERE l.compra_detalle_id=d.id) AS fecha_vencimiento
            FROM compra_detalles d ORDER BY d.id
            """).ToLookup(row => (int)row.compra_id);
        foreach (var row in connection.Query("SELECT * FROM compras ORDER BY id"))
        {
            int id = (int)row.id;
            var supplier = suppliers[(int)row.proveedor_id];
            var lines = purchaseLines[id].Select(line => new PurchaseLineRecord(products[(int)line.producto_id],
                (int)line.cantidad, Money((long)line.costo_unitario_centavos), OptionalDate((string?)line.fecha_vencimiento))).ToArray();
            var purchase = new PurchaseRecord(id, ReadDate((string)row.fecha_hora), supplier, lines)
            { Number = (string)row.numero, SupplierInvoice = (string?)row.referencia_factura };
            Purchases.Insert(0, purchase);
            supplier.Purchases.Insert(0, purchase);
        }

        var batches = new Dictionary<int, InventoryBatchRecord>();
        foreach (var row in connection.Query("""
            SELECT l.*,c.numero,p.nombre AS proveedor FROM lotes_inventario l
            LEFT JOIN compra_detalles d ON d.id=l.compra_detalle_id
            LEFT JOIN compras c ON c.id=d.compra_id LEFT JOIN proveedores p ON p.id=c.proveedor_id ORDER BY l.id
            """))
        {
            var batch = new InventoryBatchRecord((int)row.id, (int)row.cantidad_ingresada, ReadDate((string)row.fecha_ingreso),
                OptionalDate((string?)row.fecha_vencimiento), Money((long)row.costo_unitario_centavos), (string?)row.proveedor ?? "Inventario inicial")
            { Folio = (string)row.folio };
            batches.Add(batch.Id, batch);
            ApplyLot(products[(int)row.producto_id], batch, (string?)row.numero);
        }
        foreach (var row in connection.Query("SELECT lote_id,SUM(cantidad) AS cantidad FROM consumos_lote GROUP BY lote_id"))
            batches[(int)row.lote_id].Quantity -= (int)row.cantidad;

        var counts = connection.Query("""
            SELECT c.*,m.codigo FROM conteos_caja c JOIN momentos_conteo_caja m ON m.id=c.momento_id
            ORDER BY denominacion_centavos DESC
            """).ToLookup(row => ((int)row.turno_id, (string)row.codigo));
        IReadOnlyList<CashDenominationCount> CashCounts(int turn, string moment) => counts[(turn, moment)]
            .Select(row => new CashDenominationCount(Money((long)row.denominacion_centavos), (int)row.cantidad)).ToArray();
        foreach (var row in connection.Query("SELECT * FROM turnos_caja ORDER BY id"))
        {
            int id = (int)row.id;
            var session = new CashSessionRecord(id, ReadDate((string)row.apertura_fecha_hora), CashCounts(id, "APERTURA"))
            { Folio = (string)row.folio, ClosedAt = OptionalDate((string?)row.cierre_fecha_hora), ClosingCount = CashCounts(id, "CIERRE") };
            CashSessions.Insert(0, session);
            if (session.ClosedAt is null) ActiveCashSession = session;
        }
        var sessions = CashSessions.ToDictionary(session => session.Id);
        var saleLines = connection.Query("SELECT * FROM venta_detalles ORDER BY id").ToLookup(row => (int)row.venta_id);
        foreach (var row in connection.Query("SELECT * FROM ventas ORDER BY id"))
        {
            int id = (int)row.id;
            var lines = saleLines[id].Select(line => new SaleLineRecord(products[(int)line.producto_id],
                (int)line.cantidad, Money((long)line.precio_unitario_centavos))).ToArray();
            var payment = (string)row.tipo_pago == "CONTADO" ? SalePaymentType.Cash : SalePaymentType.Credit;
            var customer = row.cliente_id is null ? null : customers[(int)row.cliente_id];
            var sale = new SaleRecord(id, ReadDate((string)row.fecha_hora), payment, customer, lines) { Ticket = (string)row.ticket };
            Sales.Insert(0, sale);
            foreach (var line in lines) ApplySaleHistory(line.Product, line.Quantity, sale.Date, sale.Ticket);
            if (payment == SalePaymentType.Cash) AddCashSale(sessions[(int)row.turno_id], sale);
        }

        foreach (var row in connection.Query("""
            SELECT m.*,t.codigo AS tipo,v.ticket FROM movimientos_credito m
            JOIN tipos_movimiento_credito t ON t.id=m.tipo_movimiento_id LEFT JOIN ventas v ON v.id=m.venta_id
            ORDER BY m.id
            """))
        {
            var customer = customers[(int)row.cliente_id];
            var date = ReadDate((string)row.fecha_hora);
            var amount = Money((long)row.importe_centavos);
            if ((string)row.tipo == "CARGO")
                customer.AddCredit(amount, (string)row.concepto, date, (string?)row.ticket, OptionalDate((string?)row.fecha_vencimiento));
            else customer.AddPayment(amount, (string)row.concepto, date, (string?)row.folio);
        }
    }

    private static DateTime? OptionalDate(string? value) => value is null ? null : ReadDate(value);
}

public sealed record PurchaseLineInput(ProductRecord Product, int Quantity, decimal UnitCost, DateTime? ExpirationDate);
public sealed record SaleLineInput(ProductRecord Product, int Quantity, decimal UnitPrice);
