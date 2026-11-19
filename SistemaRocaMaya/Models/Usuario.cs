using CommunityToolkit.Mvvm.ComponentModel;

namespace SistemaRocaMaya.Models;

public class Usuario : ObservableObject
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    // Store only a password hash. Never save the plain-text password here.
    public string ContrasenaHash { get; set; } = string.Empty;

    public int RolId { get; set; }

    public Rol? Rol { get; set; }

    private bool _activo = true;
    public bool Activo
    {
        get => _activo;
        set => SetProperty(ref _activo, value);
    }
}
