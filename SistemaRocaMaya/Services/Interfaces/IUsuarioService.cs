using SistemaRocaMaya.Models;

namespace SistemaRocaMaya.Services.Interfaces;

public interface IUsuarioService
{
    Task<IReadOnlyList<Usuario>> ObtenerTodosAsync(
        CancellationToken cancellationToken = default);

    Task<Usuario> CrearAsync(
        string nombre,
        // The implementation must hash this value before storing it.
        string contrasena,
        int rolId,
        CancellationToken cancellationToken = default);

    Task CambiarEstadoAsync(
        int usuarioId,
        bool activo,
        CancellationToken cancellationToken = default);
}
