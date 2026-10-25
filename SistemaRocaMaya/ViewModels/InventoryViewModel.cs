using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.ViewModels;

public sealed class InventoryViewModel : ObservableObject
{
    private string _searchText = "";
    private string _historySearchText = "";
    private ProductRecord? _selectedProduct;

    public OperationalDemoStore Store { get; } = OperationalDemoStore.Instance;
    public ListCollectionView Products { get; }
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                Products.Refresh();
            }
        }
    }
    public ProductRecord? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
            {
                SelectedProductHistory = new ListCollectionView(value?.History ?? new System.Collections.ObjectModel.ObservableCollection<InventoryMovementRecord>());
                SelectedProductHistory.Filter = FilterMovement;
                OnPropertyChanged(nameof(SelectedProductHistory));
            }
        }
    }
    public ListCollectionView SelectedProductHistory { get; private set; } = new(Array.Empty<InventoryMovementRecord>());
    public string HistorySearchText
    {
        get => _historySearchText;
        set
        {
            if (SetProperty(ref _historySearchText, value)) SelectedProductHistory.Refresh();
        }
    }
    public int ProductCount => Store.Products.Count;
    public int TotalStockUnits => Store.Products.Sum(product => product.Stock);
    public int ExpiringBatchCount => Store.GetExpiringBatches().Count();

    public InventoryViewModel()
    {
        Products = new ListCollectionView(Store.Products);
        Products.Filter = FilterProduct;
        Store.Products.CollectionChanged += Products_CollectionChanged;
        foreach (var product in Store.Products)
        {
            product.PropertyChanged += Product_PropertyChanged;
        }
        SelectedProduct = Store.Products.FirstOrDefault();
    }

    private bool FilterProduct(object item) => item is ProductRecord product
        && (string.IsNullOrWhiteSpace(SearchText)
            || product.Name.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || product.Id.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));

    private bool FilterMovement(object item) => item is InventoryMovementRecord movement
        && (string.IsNullOrWhiteSpace(HistorySearchText)
            || movement.Movement.Contains(HistorySearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || movement.Reference.Contains(HistorySearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || movement.DateText.Contains(HistorySearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));

    private void Products_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (ProductRecord product in e.NewItems)
            {
                product.PropertyChanged += Product_PropertyChanged;
            }
        }
        OnPropertyChanged(nameof(ProductCount));
        OnPropertyChanged(nameof(TotalStockUnits));
        OnPropertyChanged(nameof(ExpiringBatchCount));
    }

    private void Product_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductRecord.Stock))
        {
            OnPropertyChanged(nameof(TotalStockUnits));
            OnPropertyChanged(nameof(ExpiringBatchCount));
        }
    }
}
