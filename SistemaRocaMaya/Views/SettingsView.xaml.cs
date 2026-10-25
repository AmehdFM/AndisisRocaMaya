using System.Windows.Controls;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();
    }
}
