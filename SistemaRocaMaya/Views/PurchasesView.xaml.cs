using System.Windows;
using System.Windows.Controls;
using SistemaRocaMaya.Services;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views;

public partial class PurchasesView : UserControl
{
    public PurchasesView()
    {
        InitializeComponent();
        DataContext = new PurchasesViewModel();
    }

    private void CreateProduct_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ProductEditWindow(OperationalDemoStore.Instance)
        {
            Owner = Window.GetWindow(this)
        };
        dialog.ShowDialog();
    }
}
