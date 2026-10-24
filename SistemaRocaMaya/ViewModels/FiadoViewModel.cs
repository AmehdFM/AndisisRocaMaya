using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SistemaRocaMaya.ViewModels;

public sealed class FiadoViewModel : ObservableObject
{
    private ClienteFiadoViewModel? _clienteSeleccionado;
    private string _textoBusqueda = string.Empty;
    private string _mensajeEstado = "Selecciona un cliente para revisar su cuenta.";
    private bool _fiadoPreparado;
    private decimal _totalVenta;

    public ObservableCollection<ClienteFiadoViewModel> Clientes { get; } =
    [
        new("María López", "0801-0000-00001", 245.00m),
        new("Carlos Mejía", "0801-0000-00002", 0.00m),
        new("Ana Martínez", "0801-0000-00003", 390.00m),
        new("José Flores", "0801-0000-00004", 125.00m)
    ];

    public ICollectionView ClientesFiltrados { get; }

    public ClienteFiadoViewModel? ClienteSeleccionado
    {
        get => _clienteSeleccionado;
        private set
        {
            if (SetProperty(ref _clienteSeleccionado, value))
            {
                foreach (var cliente in Clientes)
                {
                    cliente.EstaSeleccionado = cliente == value;
                }

                OnPropertyChanged(nameof(SaldoActualTexto));
                OnPropertyChanged(nameof(SaldoNuevoTexto));
                OnPropertyChanged(nameof(ClienteResumen));
                RegistrarFiadoCommand.NotifyCanExecuteChanged();
                FiadoPreparado = false;
            }
        }
    }

    public string TextoBusqueda
    {
        get => _textoBusqueda;
        set
        {
            if (SetProperty(ref _textoBusqueda, value))
            {
                ClientesFiltrados.Refresh();
            }
        }
    }

    public decimal TotalVenta
    {
        get => _totalVenta;
        private set
        {
            if (SetProperty(ref _totalVenta, value))
            {
                OnPropertyChanged(nameof(TotalVentaTexto));
                OnPropertyChanged(nameof(SaldoNuevoTexto));
            }
        }
    }

    public string TotalVentaTexto => PosViewModel.FormatMoney(TotalVenta);

    public string SaldoActualTexto => PosViewModel.FormatMoney(ClienteSeleccionado?.SaldoPendiente ?? 0m);

    public string SaldoNuevoTexto => PosViewModel.FormatMoney((ClienteSeleccionado?.SaldoPendiente ?? 0m) + TotalVenta);

    public string ClienteResumen => ClienteSeleccionado is null
        ? "Ningún cliente seleccionado"
        : $"{ClienteSeleccionado.Nombre} · DNI {ClienteSeleccionado.Dni}";

    public string MensajeEstado
    {
        get => _mensajeEstado;
        private set => SetProperty(ref _mensajeEstado, value);
    }

    public bool FiadoPreparado
    {
        get => _fiadoPreparado;
        private set
        {
            if (SetProperty(ref _fiadoPreparado, value))
            {
                OnPropertyChanged(nameof(ConfirmacionVisibility));
            }
        }
    }

    public Visibility ConfirmacionVisibility => FiadoPreparado ? Visibility.Visible : Visibility.Collapsed;

    public IRelayCommand<ClienteFiadoViewModel?> SeleccionarClienteCommand { get; }

    public IRelayCommand RegistrarFiadoCommand { get; }

    public FiadoViewModel()
    {
        ClientesFiltrados = CollectionViewSource.GetDefaultView(Clientes);
        ClientesFiltrados.Filter = FiltrarCliente;
        SeleccionarClienteCommand = new RelayCommand<ClienteFiadoViewModel?>(SeleccionarCliente);
        RegistrarFiadoCommand = new RelayCommand(RegistrarFiado, PuedeRegistrarFiado);
    }

    public void PrepararVenta(decimal totalVenta)
    {
        TotalVenta = totalVenta;
        FiadoPreparado = false;
        MensajeEstado = ClienteSeleccionado is null
            ? "Selecciona un cliente para revisar su cuenta."
            : "Revisa el saldo y confirma el fiado de demostración.";
    }

    private bool FiltrarCliente(object item)
    {
        if (item is not ClienteFiadoViewModel cliente)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(TextoBusqueda)
            || cliente.Nombre.Contains(TextoBusqueda, StringComparison.CurrentCultureIgnoreCase)
            || cliente.Dni.Contains(TextoBusqueda, StringComparison.CurrentCultureIgnoreCase);
    }

    private void SeleccionarCliente(ClienteFiadoViewModel? cliente)
    {
        ClienteSeleccionado = cliente;
        MensajeEstado = cliente is null
            ? "Selecciona un cliente para revisar su cuenta."
            : "Cliente seleccionado. Revisa el saldo antes de continuar.";
    }

    private bool PuedeRegistrarFiado() => ClienteSeleccionado is not null;

    private void RegistrarFiado()
    {
        if (ClienteSeleccionado is null)
        {
            return;
        }

        FiadoPreparado = true;
        MensajeEstado = "Fiado preparado para el ticket de demostración.";
    }
}

public sealed class ClienteFiadoViewModel : ObservableObject
{
    private bool _estaSeleccionado;

    public string Nombre { get; }

    public string Dni { get; }

    public decimal SaldoPendiente { get; }

    public string SaldoPendienteTexto => PosViewModel.FormatMoney(SaldoPendiente);

    public bool EstaSeleccionado
    {
        get => _estaSeleccionado;
        set => SetProperty(ref _estaSeleccionado, value);
    }

    public ClienteFiadoViewModel(string nombre, string dni, decimal saldoPendiente)
    {
        Nombre = nombre;
        Dni = dni;
        SaldoPendiente = saldoPendiente;
    }
}
