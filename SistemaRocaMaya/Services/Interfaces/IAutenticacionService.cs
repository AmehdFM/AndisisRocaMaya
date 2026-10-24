using SistemaRocaMaya.Models;

namespace SistemaRocaMaya.Services.Interfaces;

public interface IAutenticacionService
{
    // Returns null when the credentials are invalid or the account is inactive.
    // The implementation must verify the password against its stored hash.
    Task<Usuario?> IniciarSesionAsync(
        string nombre,
        string contrasena,
        CancellationToken cancellationToken = default);

    Task CerrarSesionAsync(CancellationToken cancellationToken = default);
}
