using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SistemaRocaMaya.Models;

namespace SistemaRocaMaya.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private static SettingsViewModel? _instance;
    private string _authorizationCode = "1234";
    private string _authorizationCodeStatus = "Código inicial de demostración: 1234";
    private Usuario? _currentUser;

    public static SettingsViewModel Instance => _instance ??= new SettingsViewModel();

    public ObservableCollection<Rol> Roles { get; } = [];
    public ObservableCollection<Usuario> Users { get; } = [];
    public Usuario? CurrentUser
    {
        get => _currentUser;
        private set => SetProperty(ref _currentUser, value);
    }
    public string AuthorizationCodeStatus
    {
        get => _authorizationCodeStatus;
        private set => SetProperty(ref _authorizationCodeStatus, value);
    }

    public void SetCurrentUser(Usuario user) => CurrentUser = user;

    public bool IsAuthorizationCodeValid(string code) => string.Equals(_authorizationCode, code, StringComparison.Ordinal);

    public bool VerifyDemoPassword(Usuario user, string password) => user.Nombre switch
    {
        "Administrador" => string.Equals(password, "Admin1234", StringComparison.Ordinal),
        "Empleado" => string.Equals(password, "Empleado1234", StringComparison.Ordinal),
        _ => false
    };

    public bool UpdateAuthorizationCode(string code)
    {
        if (code.Length != 4 || code.Any(character => !char.IsDigit(character)))
        {
            AuthorizationCodeStatus = "El código debe tener exactamente 4 dígitos.";
            return false;
        }

        _authorizationCode = code;
        AuthorizationCodeStatus = "Código de autorización actualizado.";
        return true;
    }

    public Usuario? FindActiveUser(string name) => Users.FirstOrDefault(user => user.Activo
        && string.Equals(user.Nombre.Trim(), name.Trim(), StringComparison.CurrentCultureIgnoreCase));

    public SettingsViewModel()
    {
        var managerRole = new Rol
        {
            Id = 1,
            Nombre = "Gerencia",
            Permisos = ["Resumen", "Caja", "Clientes", "Compras", "Crédito", "Proveedores", "Inventario", "Ventas", "Configuración"],
            PuedeVenderCredito = true,
            PuedeAbrirCaja = true,
            PuedeCrearProductos = true,
            PuedeCrearClientes = true
        };
        var employeeRole = new Rol
        {
            Id = 2,
            Nombre = "Empleado",
            Permisos = ["Caja", "Clientes", "Crédito", "Inventario"],
            PuedeVenderCredito = false,
            PuedeAbrirCaja = false,
            PuedeCrearProductos = false,
            PuedeCrearClientes = false
        };

        Roles.Add(managerRole);
        Roles.Add(employeeRole);
        Users.Add(new Usuario { Id = 1, Nombre = "Administrador", RolId = managerRole.Id, Rol = managerRole, Activo = true });
        Users.Add(new Usuario { Id = 2, Nombre = "Empleado", RolId = employeeRole.Id, Rol = employeeRole, Activo = true });
    }
}
