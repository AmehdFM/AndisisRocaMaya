using CommunityToolkit.Mvvm.ComponentModel;

namespace SistemaRocaMaya.Models;

public class Rol : ObservableObject
{
    private bool _puedeVenderCredito;
    private bool _puedeAbrirCaja;
    private bool _puedeCrearProductos;
    private bool _puedeCrearClientes;

    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public List<string> Permisos { get; set; } = [];

    public bool PuedeVenderCredito
    {
        get => _puedeVenderCredito;
        set => SetProperty(ref _puedeVenderCredito, value);
    }

    public bool PuedeAbrirCaja
    {
        get => _puedeAbrirCaja;
        set => SetProperty(ref _puedeAbrirCaja, value);
    }

    public bool PuedeCrearProductos
    {
        get => _puedeCrearProductos;
        set => SetProperty(ref _puedeCrearProductos, value);
    }

    public bool PuedeCrearClientes
    {
        get => _puedeCrearClientes;
        set => SetProperty(ref _puedeCrearClientes, value);
    }

    public string PermisosTexto => string.Join(" · ", Permisos);

    public bool PuedeAcceder(string seccion) => Permisos.Contains(seccion, StringComparer.OrdinalIgnoreCase);
}
