using System.Windows.Controls;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        DataContext = new DashboardViewModel();
    }
}
