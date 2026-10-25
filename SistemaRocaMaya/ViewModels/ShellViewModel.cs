using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaRocaMaya.Models;
using SistemaRocaMaya.Services;

namespace SistemaRocaMaya.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    private bool _isSidebarExpanded = true;
    private bool _isFocusMode;
    private string _currentSectionTitle = "Resumen";
    private readonly Rol _currentRole;

    public ObservableCollection<NavigationItemViewModel> MenuItems { get; } = [];

    public PosViewModel PosViewModel { get; }

    public FiadoViewModel FiadoViewModel { get; } = new();

    public ClientesAbonosViewModel ClientesAbonosViewModel { get; } = new();

    public OperationalDemoStore OperationalStore { get; } = OperationalDemoStore.Instance;

    public NavigationItemViewModel? PosMenuItem => MenuItems.FirstOrDefault(item => item.Title == "Caja");

    public NavigationItemViewModel? InventoryMenuItem => MenuItems.FirstOrDefault(item => item.Title == "Inventario");

    public string CurrentSectionTitle
    {
        get => _currentSectionTitle;
        private set
        {
            if (SetProperty(ref _currentSectionTitle, value))
            {
                OnPropertyChanged(nameof(DashboardVisibility));
                OnPropertyChanged(nameof(PlaceholderVisibility));
                OnPropertyChanged(nameof(FiadoVisibility));
                OnPropertyChanged(nameof(CustomersVisibility));
                OnPropertyChanged(nameof(CreditsVisibility));
                OnPropertyChanged(nameof(InventoryVisibility));
                OnPropertyChanged(nameof(PurchasesVisibility));
                OnPropertyChanged(nameof(SuppliersVisibility));
                OnPropertyChanged(nameof(SalesVisibility));
                OnPropertyChanged(nameof(SettingsVisibility));
            }
        }
    }

    public string RoleLabel => _currentRole.Nombre;

    public string ReturnFromFiadoLabel => "Volver al resumen";

    public GridLength SidebarColumnWidth => _isFocusMode ? new GridLength(0) : _isSidebarExpanded ? new GridLength(232) : new GridLength(64);

    public HorizontalAlignment SidebarToggleAlignment => _isSidebarExpanded ? HorizontalAlignment.Right : HorizontalAlignment.Center;

    public Visibility SidebarTextVisibility => _isSidebarExpanded && !_isFocusMode ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ShellChromeVisibility => _isFocusMode ? Visibility.Collapsed : Visibility.Visible;

    public Visibility FocusInstructionsVisibility => _isFocusMode ? Visibility.Visible : Visibility.Collapsed;

    public Visibility DashboardVisibility => CurrentSectionTitle == "Resumen" && !_isFocusMode ? Visibility.Visible : Visibility.Collapsed;

    public Visibility PlaceholderVisibility => CurrentSectionTitle != "Resumen" && CurrentSectionTitle != "Caja" && CurrentSectionTitle != "P04 · Fiado" && CurrentSectionTitle != "Clientes" && CurrentSectionTitle != "Crédito" && CurrentSectionTitle != "Compras" && CurrentSectionTitle != "Proveedores" && CurrentSectionTitle != "Inventario" && CurrentSectionTitle != "Ventas" && CurrentSectionTitle != "Configuración" && !_isFocusMode
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility PosVisibility => CurrentSectionTitle == "Caja" ? Visibility.Visible : Visibility.Collapsed;

    public Visibility FiadoVisibility => CurrentSectionTitle == "P04 · Fiado" ? Visibility.Visible : Visibility.Collapsed;

    public Visibility CustomersVisibility => CurrentSectionTitle == "Clientes" ? Visibility.Visible : Visibility.Collapsed;

    public Visibility CreditsVisibility => CurrentSectionTitle == "Crédito" ? Visibility.Visible : Visibility.Collapsed;

    public Visibility PurchasesVisibility => CurrentSectionTitle == "Compras" ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SuppliersVisibility => CurrentSectionTitle == "Proveedores" ? Visibility.Visible : Visibility.Collapsed;

    public Visibility InventoryVisibility => CurrentSectionTitle == "Inventario" ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SalesVisibility => CurrentSectionTitle == "Ventas" ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SettingsVisibility => CurrentSectionTitle == "Configuración" ? Visibility.Visible : Visibility.Collapsed;

    public bool IsFocusMode => _isFocusMode;

    public WindowStyle ShellWindowStyle => _isFocusMode ? WindowStyle.None : WindowStyle.SingleBorderWindow;

    public string FocusButtonLabel => _isFocusMode ? "Salir del enfoque" : "Modo enfoque  ⛶";

    public IRelayCommand ToggleSidebarCommand { get; }

    public IRelayCommand ToggleFocusCommand { get; }

    public IRelayCommand ExitFocusCommand { get; }

    public IRelayCommand OpenFiadoCommand { get; }

    public IRelayCommand ReturnFromFiadoCommand { get; }

    public IRelayCommand OpenAbonoCommand { get; }

    public IRelayCommand<NavigationItemViewModel?> NavigateCommand { get; }

    public ShellViewModel(Usuario currentUser)
    {
        _currentRole = currentUser.Rol ?? throw new InvalidOperationException("El usuario no tiene un rol asignado.");
        PosViewModel = new PosViewModel(_currentRole);
        ToggleSidebarCommand = new RelayCommand(ToggleSidebar);
        ToggleFocusCommand = new RelayCommand(ToggleFocus, CanToggleFocus);
        ExitFocusCommand = new RelayCommand(ExitFocus);
        OpenFiadoCommand = new RelayCommand(OpenFiado);
        ReturnFromFiadoCommand = new RelayCommand(() => SelectSection(_currentRole.PuedeAcceder("Resumen") ? "Resumen" : "Caja"));
        OpenAbonoCommand = new RelayCommand(() => SelectSection("P08 · Registrar abono"));
        NavigateCommand = new RelayCommand<NavigationItemViewModel?>(Navigate);
        LoadMenuItems();
        SelectSection(_currentRole.PuedeAcceder("Resumen") ? "Resumen" : "Caja");
    }

    private void ToggleSidebar()
    {
        _isSidebarExpanded = !_isSidebarExpanded;
        OnPropertyChanged(nameof(SidebarColumnWidth));
        OnPropertyChanged(nameof(SidebarToggleAlignment));
        OnPropertyChanged(nameof(SidebarTextVisibility));
    }

    private bool CanToggleFocus() => CurrentSectionTitle == "Caja";

    private void ToggleFocus()
    {
        if (!CanToggleFocus())
        {
            return;
        }

        _isFocusMode = !_isFocusMode;
        OnPropertyChanged(nameof(IsFocusMode));
        OnPropertyChanged(nameof(ShellWindowStyle));
        OnPropertyChanged(nameof(FocusButtonLabel));
        OnPropertyChanged(nameof(SidebarColumnWidth));
        OnPropertyChanged(nameof(SidebarTextVisibility));
        OnPropertyChanged(nameof(ShellChromeVisibility));
        OnPropertyChanged(nameof(FocusInstructionsVisibility));
        OnPropertyChanged(nameof(DashboardVisibility));
        OnPropertyChanged(nameof(PlaceholderVisibility));
        ToggleFocusCommand.NotifyCanExecuteChanged();
    }

    private void ExitFocus()
    {
        if (_isFocusMode)
        {
            ToggleFocus();
        }
    }

    private void Navigate(NavigationItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        SelectSection(item.Destination);
    }

    private void OpenFiado()
    {
        if (!_currentRole.PuedeVenderCredito || !_currentRole.PuedeAcceder("Caja"))
        {
            return;
        }

        FiadoViewModel.PrepararVenta(PosViewModel.TotalAmount);
        SelectSection("P04 · Fiado");
    }

    private void SelectSection(string title)
    {
        if (title == "P04 · Fiado")
        {
            if (!_currentRole.PuedeVenderCredito || !_currentRole.PuedeAcceder("Caja")) return;
        }
        else if (!_currentRole.PuedeAcceder(title))
        {
            return;
        }

        if (_isFocusMode && title != "Caja")
        {
            ExitFocus();
        }

        CurrentSectionTitle = title;
        OnPropertyChanged(nameof(PosVisibility));
        OnPropertyChanged(nameof(FiadoVisibility));
        OnPropertyChanged(nameof(CustomersVisibility));
        OnPropertyChanged(nameof(CreditsVisibility));
        ToggleFocusCommand.NotifyCanExecuteChanged();

        foreach (var item in MenuItems)
        {
            item.IsSelected = item.Destination == title;
        }
    }

    private void LoadMenuItems()
    {
        MenuItems.Clear();

        AddMenuItem("Resumen", "▦");
        AddMenuItem("Caja", "▣");
        AddMenuItem("Clientes", "♙");
        AddMenuItem("Compras", "＋");
        AddMenuItem("Crédito", "◷");
        AddMenuItem("Proveedores", "♧");
        AddMenuItem("Inventario", "▤");
        AddMenuItem("Ventas", "▣");
        AddMenuItem("Configuración", "⚙");
        OnPropertyChanged(nameof(PosMenuItem));
        OnPropertyChanged(nameof(InventoryMenuItem));
    }

    private void AddMenuItem(string title, string icon)
    {
        if (_currentRole.PuedeAcceder(title))
        {
            MenuItems.Add(new NavigationItemViewModel(title, icon, title));
        }
    }
}

public sealed class NavigationItemViewModel : ObservableObject
{
    private bool _isSelected;

    public string Title { get; }

    public string Destination { get; }

    public string Icon { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public NavigationItemViewModel(string title, string icon, string? destination = null)
    {
        Title = title;
        Icon = icon;
        Destination = destination ?? title;
    }
}
