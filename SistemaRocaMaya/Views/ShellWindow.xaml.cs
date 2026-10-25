using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views;

public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();
        DataContext = new ShellViewModel();
    }

    private void SignOut_Click(object sender, RoutedEventArgs e)
    {
        new MainWindow().Show();
        Close();
    }

    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ShellViewModel viewModel)
        {
            return;
        }

        var startWidth = SidebarSurface.ActualWidth;
        viewModel.ToggleSidebarCommand.Execute(null);
        var targetWidth = viewModel.SidebarColumnWidth.Value;

        var animation = new DoubleAnimation(startWidth, targetWidth, TimeSpan.FromMilliseconds(240))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.Stop
        };
        animation.Completed += (_, _) =>
        {
            SidebarSurface.Width = targetWidth;
            SidebarSurface.BeginAnimation(FrameworkElement.WidthProperty, null);
        };

        SidebarSurface.BeginAnimation(
            FrameworkElement.WidthProperty,
            animation,
            HandoffBehavior.SnapshotAndReplace);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ShellViewModel viewModel)
        {
            return;
        }

        if (e.Key == Key.Escape && viewModel.IsFocusMode)
        {
            viewModel.ExitFocusCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F11 && viewModel.CurrentSectionTitle == "Caja")
        {
            viewModel.ToggleFocusCommand.Execute(null);
            e.Handled = true;
        }
    }
}
