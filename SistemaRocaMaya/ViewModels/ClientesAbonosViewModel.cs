using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SistemaRocaMaya.ViewModels;

public sealed class ClientesAbonosViewModel : ObservableObject
{
    private ClienteCuentaViewModel? _clienteSeleccionado;
    private string _textoBusqueda = string.Empty;

    public ObservableCollection<ClienteCuentaViewModel> Clientes { get; } =
    [
        new("María López", "0801-0000-00001", 295.00m, "L 100.00 · 18 oct. 2026"),
        new("Carlos Mejía", "0801-0000-00002", 0.00m, "L 250.00 · 20 oct. 2026"),
        new("Ana Martínez", "0801-0000-00003", 390.00m, "L 75.00 · 12 oct. 2026"),
        new("José Flores", "0801-0000-00004", 125.00m, "Sin abonos registrados")
    ];

    public ICollectionView ClientesFiltrados { get; }

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

    public ClienteCuentaViewModel? ClienteSeleccionado
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

                OnPropertyChanged(nameof(SaldoSeleccionadoTexto));
                OnPropertyChanged(nameof(EstadoCuentaTexto));
                OnPropertyChanged(nameof(UltimoAbonoTexto));
                OnPropertyChanged(nameof(ClienteNombre));
                OnPropertyChanged(nameof(ClienteDni));
                OnPropertyChanged(nameof(TieneClienteSeleccionado));
            }
        }
    }

    public string SaldoTotalPendienteTexto => PosViewModel.FormatMoney(Clientes.Sum(cliente => cliente.SaldoPendiente));

    public int CuentasConSaldo => Clientes.Count(cliente => cliente.SaldoPendiente > 0);

    public string SaldoSeleccionadoTexto => ClienteSeleccionado is null
        ? "—"
        : PosViewModel.FormatMoney(ClienteSeleccionado.SaldoPendiente);

    public string EstadoCuentaTexto => ClienteSeleccionado is null
        ? "Selecciona un cliente"
        : ClienteSeleccionado.SaldoPendiente > 0 ? "Saldo pendiente" : "Al día";

    public string UltimoAbonoTexto => ClienteSeleccionado?.UltimoAbono ?? "—";

    public string ClienteNombre => ClienteSeleccionado?.Nombre ?? "Selecciona un cliente";

    public string ClienteDni => ClienteSeleccionado is null ? "" : $"DNI {ClienteSeleccionado.Dni}";

    public bool TieneClienteSeleccionado => ClienteSeleccionado is not null;

    public IRelayCommand<ClienteCuentaViewModel?> SeleccionarClienteCommand { get; }

    public ClientesAbonosViewModel()
    {
        ClientesFiltrados = CollectionViewSource.GetDefaultView(Clientes);
        ClientesFiltrados.Filter = FiltrarCliente;
        SeleccionarClienteCommand = new RelayCommand<ClienteCuentaViewModel?>(SeleccionarCliente);
        SeleccionarCliente(Clientes[0]);
    }

    private bool FiltrarCliente(object item)
    {
        if (item is not ClienteCuentaViewModel cliente)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(TextoBusqueda)
            || cliente.Nombre.Contains(TextoBusqueda, StringComparison.CurrentCultureIgnoreCase)
            || cliente.Dni.Contains(TextoBusqueda, StringComparison.CurrentCultureIgnoreCase);
    }

    private void SeleccionarCliente(ClienteCuentaViewModel? cliente)
    {
        ClienteSeleccionado = cliente;
    }
}

public sealed class ClienteCuentaViewModel : ObservableObject
{
    private bool _estaSeleccionado;

    public string Nombre { get; }

    public string Dni { get; }

    public decimal SaldoPendiente { get; }

    public string SaldoPendienteTexto => PosViewModel.FormatMoney(SaldoPendiente);

    public string UltimoAbono { get; }

    public string EstadoTexto => SaldoPendiente > 0 ? "Saldo pendiente" : "Al día";

    public bool EstaSeleccionado
    {
        get => _estaSeleccionado;
        set => SetProperty(ref _estaSeleccionado, value);
    }

    public ClienteCuentaViewModel(string nombre, string dni, decimal saldoPendiente, string ultimoAbono)
    {
        Nombre = nombre;
        Dni = dni;
        SaldoPendiente = saldoPendiente;
        UltimoAbono = ultimoAbono;
    }
}
