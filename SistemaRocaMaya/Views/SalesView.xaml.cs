using System.Windows.Controls;
using System.Windows.Threading;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views;

public partial class SalesView : UserControl
{
    public SalesView()
    {
        InitializeComponent();

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => (DataContext as SalesViewModel)?.RefreshToday();
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible && DataContext is SalesViewModel { CanBrowseHistory: false } sales)
            {
                sales.RefreshToday();
                timer.Start();
            }
            else
            {
                timer.Stop();
            }
        };
    }
}
