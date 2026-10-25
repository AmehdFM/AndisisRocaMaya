using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.ViewModels;

public sealed class SuppliersViewModel : ObservableObject
{
    private string _searchText = "";
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
        set => SetProperty(ref _selectedSupplier, value);
    }

    public SuppliersViewModel()
    {
        Suppliers = new ListCollectionView(Store.Suppliers);
        Suppliers.Filter = FilterSupplier;
        SelectedSupplier = Store.Suppliers.FirstOrDefault();
    }

    private bool FilterSupplier(object item) => item is SupplierRecord supplier
        && (string.IsNullOrWhiteSpace(SearchText)
            || supplier.Name.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));
}
