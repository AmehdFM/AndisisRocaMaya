using System.Windows;
using System.Windows.Controls;
using SistemaRocaMaya.Services;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views;

public partial class InventoryView : UserControl
{
    private readonly InventoryViewModel _viewModel;

    public InventoryView()
    {
        InitializeComponent();
        _viewModel = new InventoryViewModel();
        DataContext = _viewModel;
    }

    private void CreateProduct_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ProductEditWindow(OperationalDemoStore.Instance)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true && dialog.CreatedProduct is { } product)
        {
            _viewModel.SearchText = string.Empty;
            _viewModel.SelectedProduct = product;
        }
    }
}
