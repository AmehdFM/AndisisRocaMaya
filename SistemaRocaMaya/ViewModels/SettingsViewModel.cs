using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SistemaRocaMaya.Models;

namespace SistemaRocaMaya.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    public ObservableCollection<Rol> Roles { get; } = [];
    public ObservableCollection<Usuario> Users { get; } = [];

    public SettingsViewModel()
    {
        var managerRole = new Rol
        {
            Id = 1,
            Nombre = "Gerencia",
            Permisos = ["Resumen", "Caja", "Clientes", "Compras", "Inventario", "Ventas", "Configuración"]
        };
        var cashierRole = new Rol
        {
            Id = 2,
            Nombre = "Caja",
            Permisos = ["Caja", "Clientes"]
        };

        Roles.Add(managerRole);
        Roles.Add(cashierRole);
        Users.Add(new Usuario { Id = 1, Nombre = "Administrador", RolId = managerRole.Id, Rol = managerRole, Activo = true });
        Users.Add(new Usuario { Id = 2, Nombre = "Cajero de demostración", RolId = cashierRole.Id, Rol = cashierRole, Activo = true });
    }
}
