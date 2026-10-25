using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.ComponentModel;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;
using SistemaRocaMaya.ViewModels;

namespace SistemaRocaMaya.Views;

public partial class ShellWindow : Window
{
    public ShellWindow(Usuario user)
    {
        InitializeComponent();
        DataContext = new ShellViewModel(user);
    }

    private void SignOut_Click(object sender, RoutedEventArgs e)
    {
        if (!TryCloseOpenCashSession())
        {
            return;
        }

        new MainWindow().Show();
        Close();
    }

    private void ShellWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (!TryCloseOpenCashSession())
        {
            e.Cancel = true;
        }
    }

    private bool TryCloseOpenCashSession()
    {
        var store = OperationalDemoStore.Instance;
        if (!store.IsCashOpen)
        {
            return true;
        }

        var answer = MessageBox.Show(this,
            "Hay una caja abierta. ¿Deseas contar y cerrar la caja antes de salir? Si eliges No, la aplicación seguirá abierta.",
            "Caja abierta", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes)
        {
            return false;
        }

        var dialog = new CashSessionWindow(true, store.ActiveCashSession!.ExpectedAmount)
        {
            Owner = this
        };
        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        try
        {
            store.CloseCashSession(dialog.CashCounts);
            return true;
        }
        catch (InvalidOperationException exception)
        {
            MessageBox.Show(this, exception.Message, "No se pudo cerrar la caja",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
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
