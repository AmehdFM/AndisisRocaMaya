using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SistemaRocaMaya.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    private bool _isSidebarExpanded = true;
    private bool _isFocusMode;
    private string _currentSectionTitle = "Panel gerencial";

    public ObservableCollection<NavigationItemViewModel> MenuItems { get; } = [];

    public PosViewModel PosViewModel { get; } = new();

    public FiadoViewModel FiadoViewModel { get; } = new();

    public ClientesAbonosViewModel ClientesAbonosViewModel { get; } = new();

    public NavigationItemViewModel? PosMenuItem => MenuItems.FirstOrDefault(item => item.Title == "POS / Caja");

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
                OnPropertyChanged(nameof(ClientesAbonosVisibility));
            }
        }
    }

    public string RoleLabel => "Gerencia";

    public string ReturnFromFiadoLabel => "Volver al panel gerencial";

    public GridLength SidebarColumnWidth => _isFocusMode ? new GridLength(0) : _isSidebarExpanded ? new GridLength(248) : new GridLength(72);

    public GridLength HeaderRowHeight => _isFocusMode ? new GridLength(0) : new GridLength(68);

    public Visibility SidebarTextVisibility => _isSidebarExpanded && !_isFocusMode ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ShellChromeVisibility => _isFocusMode ? Visibility.Collapsed : Visibility.Visible;

    public Visibility FocusInstructionsVisibility => _isFocusMode ? Visibility.Visible : Visibility.Collapsed;

    public Visibility DashboardVisibility => CurrentSectionTitle == "Panel gerencial" && !_isFocusMode ? Visibility.Visible : Visibility.Collapsed;

    public Visibility PlaceholderVisibility => CurrentSectionTitle != "Panel gerencial" && CurrentSectionTitle != "POS / Caja" && CurrentSectionTitle != "P04 · Fiado" && CurrentSectionTitle != "Clientes y abonos" && !_isFocusMode
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility PosVisibility => CurrentSectionTitle == "POS / Caja" ? Visibility.Visible : Visibility.Collapsed;

    public Visibility FiadoVisibility => CurrentSectionTitle == "P04 · Fiado" ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ClientesAbonosVisibility => CurrentSectionTitle == "Clientes y abonos" ? Visibility.Visible : Visibility.Collapsed;

    public bool IsFocusMode => _isFocusMode;

    public WindowStyle ShellWindowStyle => _isFocusMode ? WindowStyle.None : WindowStyle.SingleBorderWindow;

    public string FocusButtonLabel => _isFocusMode ? "Salir de enfoque  ·  Esc" : "Modo enfoque  ⛶";

    public IRelayCommand ToggleSidebarCommand { get; }

    public IRelayCommand ToggleFocusCommand { get; }

    public IRelayCommand ExitFocusCommand { get; }

    public IRelayCommand OpenFiadoCommand { get; }

    public IRelayCommand ReturnFromFiadoCommand { get; }

    public IRelayCommand OpenAbonoCommand { get; }

    public IRelayCommand<NavigationItemViewModel?> NavigateCommand { get; }

    public ShellViewModel()
    {
        ToggleSidebarCommand = new RelayCommand(ToggleSidebar);
        ToggleFocusCommand = new RelayCommand(ToggleFocus, CanToggleFocus);
        ExitFocusCommand = new RelayCommand(ExitFocus);
        OpenFiadoCommand = new RelayCommand(OpenFiado);
        ReturnFromFiadoCommand = new RelayCommand(() => SelectSection("Panel gerencial"));
        OpenAbonoCommand = new RelayCommand(() => SelectSection("P08 · Registrar abono"));
        NavigateCommand = new RelayCommand<NavigationItemViewModel?>(Navigate);
        LoadMenuItems();
        SelectSection("Panel gerencial");
    }

    private void ToggleSidebar()
    {
        _isSidebarExpanded = !_isSidebarExpanded;
        OnPropertyChanged(nameof(SidebarColumnWidth));
        OnPropertyChanged(nameof(SidebarTextVisibility));
    }

    private bool CanToggleFocus() => CurrentSectionTitle == "POS / Caja";

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
        OnPropertyChanged(nameof(HeaderRowHeight));
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
        FiadoViewModel.PrepararVenta(PosViewModel.TotalAmount);
        SelectSection("P04 · Fiado");
    }

    private void SelectSection(string title)
    {
        if (_isFocusMode && title != "POS / Caja")
        {
            ExitFocus();
        }

        CurrentSectionTitle = title;
        OnPropertyChanged(nameof(PosVisibility));
        OnPropertyChanged(nameof(FiadoVisibility));
        OnPropertyChanged(nameof(ClientesAbonosVisibility));
        ToggleFocusCommand.NotifyCanExecuteChanged();

        foreach (var item in MenuItems)
        {
            item.IsSelected = item.Destination == title;
        }
    }

    private void LoadMenuItems()
    {
        MenuItems.Clear();

        MenuItems.Add(new NavigationItemViewModel("Panel gerencial", "▦", "Panel gerencial"));

        MenuItems.Add(new NavigationItemViewModel("POS / Caja", "▣", "POS / Caja"));
        MenuItems.Add(new NavigationItemViewModel("Registrar fiado", "◷", "P04 · Fiado"));
        MenuItems.Add(new NavigationItemViewModel("Clientes y abonos", "♙", "Clientes y abonos"));
        MenuItems.Add(new NavigationItemViewModel("Lista de precios", "＄", "Lista de precios"));

        MenuItems.Add(new NavigationItemViewModel("Inventario", "▤", "Inventario"));
        MenuItems.Add(new NavigationItemViewModel("Fiados y cobranza", "◷", "Fiados y cobranza"));
        MenuItems.Add(new NavigationItemViewModel("Contabilidad", "≡", "Contabilidad"));
        MenuItems.Add(new NavigationItemViewModel("Caja diaria", "＄", "Caja diaria"));
        MenuItems.Add(new NavigationItemViewModel("Usuarios y roles", "♙", "Usuarios y roles"));

        MenuItems.Add(new NavigationItemViewModel("Configuración", "⚙", "Configuración"));
        OnPropertyChanged(nameof(PosMenuItem));
        OnPropertyChanged(nameof(InventoryMenuItem));
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
