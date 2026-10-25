using System.Windows;
using System.Windows.Controls;
using SistemaRocaMaya.Services;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views;

public partial class SuppliersView : UserControl
{
    public SuppliersView()
    {
        InitializeComponent();
        DataContext = new SuppliersViewModel();
    }

    private void CreateSupplier_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SupplierEditWindow(OperationalDemoStore.Instance)
        {
            Owner = Window.GetWindow(this)
        };
        dialog.ShowDialog();
    }
}
