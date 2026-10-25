using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.ViewModels;

public sealed class SuppliersViewModel : ObservableObject
{
    private string _searchText = "";
    private string _purchaseSearchText = "";
    private SupplierRecord? _selectedSupplier;

    public OperationalDemoStore Store { get; } = OperationalDemoStore.Instance;
    public ListCollectionView Suppliers { get; }
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                Suppliers.Refresh();
            }
        }
    }
    public SupplierRecord? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (SetProperty(ref _selectedSupplier, value))
            {
                SelectedSupplierPurchases = new ListCollectionView(value?.Purchases ?? new System.Collections.ObjectModel.ObservableCollection<PurchaseRecord>());
                SelectedSupplierPurchases.Filter = FilterPurchase;
                OnPropertyChanged(nameof(SelectedSupplierPurchases));
            }
        }
    }
    public ListCollectionView SelectedSupplierPurchases { get; private set; } = new(Array.Empty<PurchaseRecord>());
    public string PurchaseSearchText
    {
        get => _purchaseSearchText;
        set
        {
            if (SetProperty(ref _purchaseSearchText, value)) SelectedSupplierPurchases.Refresh();
        }
    }

    public SuppliersViewModel()
    {
        Suppliers = new ListCollectionView(Store.Suppliers);
        Suppliers.Filter = FilterSupplier;
        SelectedSupplier = Store.Suppliers.FirstOrDefault();
    }

    private bool FilterSupplier(object item) => item is SupplierRecord supplier
        && (string.IsNullOrWhiteSpace(SearchText)
            || supplier.Name.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || supplier.Phone.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || supplier.Contact.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));

    private bool FilterPurchase(object item) => item is PurchaseRecord purchase
        && (string.IsNullOrWhiteSpace(PurchaseSearchText)
            || purchase.Number.Contains(PurchaseSearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)
            || purchase.DateText.Contains(PurchaseSearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));
}
