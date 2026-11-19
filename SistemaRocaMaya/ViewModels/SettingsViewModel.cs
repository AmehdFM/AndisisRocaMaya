using System.ComponentModel;
using Dapper;
using SistemaRocaMaya.Services;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SistemaRocaMaya.Models;

namespace SistemaRocaMaya.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private static SettingsViewModel? _instance;
    private readonly RocaMayaDatabase _database = RocaMayaDatabase.Instance;
    private string _authorizationCodeHash;
    private string _authorizationCodeStatus = "Código de autorización configurado.";
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

    public void SetCurrentUser(Usuario user)
    {
        _database.CurrentUserId = user.Id;
        CurrentUser = user;
    }

    public bool IsAuthorizationCodeValid(string code) => RocaMayaDatabase.VerifySecret(code, _authorizationCodeHash);

    public bool VerifyPassword(Usuario user, string password) => RocaMayaDatabase.VerifySecret(password, user.ContrasenaHash);

    public bool UpdateAuthorizationCode(string code)
    {
        if (code.Length != 4 || code.Any(character => !char.IsDigit(character)))
        {
            AuthorizationCodeStatus = "El código debe tener exactamente 4 dígitos.";
            return false;
        }

        var hash = RocaMayaDatabase.HashSecret(code);
        try
        {
            _database.Write(db => db.Execute("UPDATE configuracion SET valor=@hash WHERE clave='autorizacion'", new { hash }));
        }
        catch (InvalidOperationException exception)
        {
            AuthorizationCodeStatus = exception.Message;
            return false;
        }
        _authorizationCodeHash = hash;
        AuthorizationCodeStatus = "Código de autorización actualizado.";
        return true;
    }

    public Usuario? FindActiveUser(string name) => Users.FirstOrDefault(user => user.Activo
        && string.Equals(user.Nombre.Trim(), name.Trim(), StringComparison.CurrentCultureIgnoreCase));

    public SettingsViewModel()
    {
        using var connection = _database.Open();
        _authorizationCodeHash = connection.QuerySingle<string>("SELECT valor FROM configuracion WHERE clave='autorizacion'");
        var permissions = connection.Query("""
            SELECT rp.rol_id,p.codigo FROM rol_permisos rp JOIN permisos p ON p.id=rp.permiso_id ORDER BY p.id
            """).ToLookup(row => (int)row.rol_id, row => (string)row.codigo);
        foreach (var role in connection.Query<Rol>("SELECT id,nombre FROM roles ORDER BY id"))
        {
            var codes = permissions[role.Id].ToHashSet(StringComparer.Ordinal);
            role.Permisos = permissions[role.Id].Where(code => !code.Contains('_')).ToList();
            role.PuedeVenderCredito = codes.Contains("VENDER_CREDITO");
            role.PuedeAbrirCaja = codes.Contains("ABRIR_CAJA");
            role.PuedeCrearProductos = codes.Contains("CREAR_PRODUCTOS");
            role.PuedeCrearClientes = codes.Contains("CREAR_CLIENTES");
            role.PropertyChanged += PersistRolePermission;
            Roles.Add(role);
        }

        var roles = Roles.ToDictionary(role => role.Id);
        foreach (var user in connection.Query<Usuario>("""
            SELECT id,nombre_usuario AS Nombre,contrasena_hash AS ContrasenaHash,rol_id AS RolId,activo
            FROM usuarios ORDER BY id
            """))
        {
            user.Rol = roles[user.RolId];
            user.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(Usuario.Activo))
                    _database.Write(db => db.Execute("UPDATE usuarios SET activo=@Activo WHERE id=@Id", user));
            };
            Users.Add(user);
        }
    }

    private void PersistRolePermission(object? sender, PropertyChangedEventArgs args)
    {
        if (sender is not Rol role) return;
        var permission = args.PropertyName switch
        {
            nameof(Rol.PuedeVenderCredito) => ("VENDER_CREDITO", role.PuedeVenderCredito),
            nameof(Rol.PuedeAbrirCaja) => ("ABRIR_CAJA", role.PuedeAbrirCaja),
            nameof(Rol.PuedeCrearProductos) => ("CREAR_PRODUCTOS", role.PuedeCrearProductos),
            nameof(Rol.PuedeCrearClientes) => ("CREAR_CLIENTES", role.PuedeCrearClientes),
            _ => (null, false)
        };
        if (permission.Item1 is not { } code) return;
        _database.Write(db => db.Execute(permission.Item2
            ? "INSERT OR IGNORE INTO rol_permisos SELECT @Id,id FROM permisos WHERE codigo=@code"
            : "DELETE FROM rol_permisos WHERE rol_id=@Id AND permiso_id=(SELECT id FROM permisos WHERE codigo=@code)",
            new { role.Id, code }));
    }
}
