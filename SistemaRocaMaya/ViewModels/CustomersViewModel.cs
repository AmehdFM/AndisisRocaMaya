using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.ViewModels;

public sealed class CustomersViewModel : ObservableObject
{
    private string _searchText = "";

    public OperationalDemoStore Store { get; } = OperationalDemoStore.Instance;
    public ICollectionView Customers { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                Customers.Refresh();
            }
        }
    }

    public CustomersViewModel()
    {
        Customers = new ListCollectionView(Store.Customers);
        Customers.Filter = FilterCustomer;
    }

    private bool FilterCustomer(object item) => item is CustomerRecord customer
        && (string.IsNullOrWhiteSpace(SearchText)
            || customer.Name.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));
}
