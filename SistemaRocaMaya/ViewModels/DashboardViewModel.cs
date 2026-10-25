using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly OperationalDemoStore _store = OperationalDemoStore.Instance;

    public ObservableCollection<SaleRecord> RecentSales { get; } = [];
    public ObservableCollection<ExpiringProductViewModel> ExpiringProducts { get; } = [];
    public string SalesTodayText => OperationalDemoStore.FormatMoney(_store.Sales.Where(sale => sale.Date.Date == DateTime.Today).Sum(sale => sale.Total));
    public int SalesTodayCount => _store.Sales.Count(sale => sale.Date.Date == DateTime.Today);
    public string CashStateText => _store.ActiveCashSession is null
        ? "Caja cerrada"
        : $"Caja abierta · {_store.ActiveCashSession.OpeningAmountText} de fondo";
    public int ExpiringCount => ExpiringProducts.Count;

    public DashboardViewModel()
    {
        _store.Sales.CollectionChanged += Sales_CollectionChanged;
        _store.Purchases.CollectionChanged += Purchases_CollectionChanged;
        _store.Products.CollectionChanged += Products_CollectionChanged;
        _store.PropertyChanged += Store_PropertyChanged;
        foreach (var product in _store.Products)
        {
            product.PropertyChanged += Product_PropertyChanged;
        }
        RefreshSales();
        RefreshExpiringProducts();
    }

    private void Sales_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshSales();

    private void Purchases_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshExpiringProducts();

    private void Products_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (ProductRecord product in e.NewItems)
            {
                product.PropertyChanged += Product_PropertyChanged;
            }
        }
        RefreshExpiringProducts();
    }

    private void Product_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductRecord.Stock))
        {
            RefreshExpiringProducts();
        }
    }

    private void Store_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OperationalDemoStore.IsCashOpen))
        {
            OnPropertyChanged(nameof(CashStateText));
        }
    }

    private void RefreshSales()
    {
        RecentSales.Clear();
        foreach (var sale in _store.Sales.Where(sale => sale.Date.Date == DateTime.Today).Take(5))
        {
            RecentSales.Add(sale);
        }
        OnPropertyChanged(nameof(SalesTodayText));
        OnPropertyChanged(nameof(SalesTodayCount));
    }

    private void RefreshExpiringProducts()
    {
        ExpiringProducts.Clear();
        foreach (var row in _store.GetExpiringBatches())
        {
            ExpiringProducts.Add(new ExpiringProductViewModel(row.Product, row.Batch));
        }
        OnPropertyChanged(nameof(ExpiringCount));
    }
}

public sealed class ExpiringProductViewModel(ProductRecord product, InventoryBatchRecord batch)
{
    public string ProductName { get; } = product.Name;
    public string ProductId { get; } = product.Id;
    public int Quantity { get; } = batch.Quantity;
    public DateTime ExpirationDate { get; } = batch.ExpirationDate!.Value;
    public string ExpirationDateText => ExpirationDate.ToString("dd/MM/yyyy");
    public string ExpirationState => ExpirationDate.Date < DateTime.Today
        ? "Vencido"
        : ExpirationDate.Date == DateTime.Today ? "Vence hoy" : $"En {(ExpirationDate.Date - DateTime.Today).Days} días";
}
