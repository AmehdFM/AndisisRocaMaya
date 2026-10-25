using System.Windows.Controls;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views;

public partial class CreditsView : UserControl
{
    public CreditsView()
    {
        InitializeComponent();
        DataContext = new CreditsViewModel();
    }
}
