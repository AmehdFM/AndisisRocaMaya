namespace SistemaRocaMaya.Models;

public class Usuario
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    // Store only a password hash. Never save the plain-text password here.
    public string ContrasenaHash { get; set; } = string.Empty;

    public int RolId { get; set; }

    public Rol? Rol { get; set; }

    public bool Activo { get; set; } = true;
}
