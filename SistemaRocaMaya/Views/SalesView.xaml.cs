using System.Windows.Controls;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views;

public partial class SalesView : UserControl
{
    public SalesView()
    {
        InitializeComponent();
        DataContext = new SalesViewModel();
    }
}
